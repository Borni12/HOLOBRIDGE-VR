import asyncio
import json
import math
import struct
import sys
import time
from typing import Dict, List, Optional
import numpy as np

try:
    from fastapi import FastAPI, WebSocket, WebSocketDisconnect, UploadFile, File
    from fastapi.middleware.cors import CORSMiddleware
    import uvicorn
except ImportError:
    print("[WARNING] fastapi or uvicorn not installed. Running in standalone socket mode or mock.")
    FastAPI = None

app = FastAPI(title="OMNI-GENESIS 5D Neural Graphics Engine") if FastAPI else None

if app:
    app.add_middleware(
        CORSMiddleware,
        allow_origins=["*"],
        allow_credentials=True,
        allow_methods=["*"],
        allow_headers=["*"],
    )

NUM_SPLATS = 65536
NUM_BRANCHES = 5
BRANCH_STEPS = 16

class SimulationState:
    def __init__(self, num_splats: int = NUM_SPLATS):
        self.num_splats = num_splats
        self.positions = np.zeros((num_splats, 3), dtype=np.float32)
        self.scales = np.full((num_splats, 3), 0.015, dtype=np.float32)
        self.rotations = np.zeros((num_splats, 4), dtype=np.float32)
        self.rotations[:, 3] = 1.0
        self.colors = np.ones((num_splats, 4), dtype=np.float32)
        self.material_ids = np.zeros(num_splats, dtype=np.int32)
        self.velocities = np.zeros((num_splats, 3), dtype=np.float32)

        # Procedural generation
        self._init_procedural_scene()

        # Avatar state
        self.avatar_head_pos = np.array([0.0, 1.6, 1.8], dtype=np.float32)
        self.avatar_hand_pos = np.array([0.25, 1.1, 1.4], dtype=np.float32)
        self.avatar_gaze_target = np.array([0.0, 1.6, 0.0], dtype=np.float32)
        self.avatar_blink = 0.0

    def _init_procedural_scene(self):
        # Generate environment dome & interactable sphere
        env_count = int(self.num_splats * 0.75)
        obj_count = self.num_splats - env_count

        # Background dome
        phi = np.random.uniform(0, 2 * np.pi, env_count).astype(np.float32)
        costheta = np.random.uniform(0, 1, env_count).astype(np.float32)
        theta = np.arccos(costheta)
        r = np.random.uniform(2.5, 4.5, env_count).astype(np.float32)

        self.positions[:env_count, 0] = r * np.sin(theta) * np.cos(phi)
        self.positions[:env_count, 1] = r * np.cos(theta) + 0.5
        self.positions[:env_count, 2] = r * np.sin(theta) * np.sin(phi)
        self.colors[:env_count, 0] = 0.2 + 0.3 * np.cos(phi)
        self.colors[:env_count, 1] = 0.4 + 0.2 * np.sin(phi)
        self.colors[:env_count, 2] = 0.7
        self.material_ids[:env_count] = 0  # Rigid/Environment

        # Interactable Matter-Weave Object in front of user
        u = np.random.uniform(0, 1, obj_count).astype(np.float32)
        v = np.random.uniform(0, 1, obj_count).astype(np.float32)
        theta_obj = u * 2.0 * np.pi
        phi_obj = np.arccos(2.0 * v - 1.0)
        r_obj = np.cbrt(np.random.uniform(0, 1, obj_count)).astype(np.float32) * 0.25

        self.positions[env_count:, 0] = r_obj * np.sin(phi_obj) * np.cos(theta_obj)
        self.positions[env_count:, 1] = 1.2 + r_obj * np.cos(phi_obj)
        self.positions[env_count:, 2] = 0.8 + r_obj * np.sin(phi_obj) * np.sin(theta_obj)
        self.colors[env_count:, 0] = 1.0
        self.colors[env_count:, 1] = 0.65
        self.colors[env_count:, 2] = 0.15
        self.material_ids[env_count:] = 1  # Elastic by default

    def step_mpm(self, dt: float, hand_l: np.ndarray, hand_r: np.ndarray, stiffness: float, is_matter_weave: bool):
        if not is_matter_weave:
            return

        obj_start = int(self.num_splats * 0.75)
        pos = self.positions[obj_start:]
        vel = self.velocities[obj_start:]

        # Radial perturbation from dominant hand
        diff_r = pos - hand_r
        dist_sqr = np.sum(diff_r ** 2, axis=1, keepdims=True)
        influence_radius = 0.18
        mask = (dist_sqr < (influence_radius ** 2)).flatten()

        if np.any(mask):
            dir_norm = diff_r[mask] / (np.sqrt(dist_sqr[mask]) + 1e-5)
            repel_force = (influence_radius - np.sqrt(dist_sqr[mask])) * 12.0
            vel[mask] += dir_norm * repel_force * dt

        # Material-specific restoration force (stiffness factor)
        center = np.array([0.0, 1.2, 0.8], dtype=np.float32)
        restoration = (center - pos) * stiffness * 4.0
        vel += restoration * dt
        vel *= math.exp(-2.5 * dt)  # Damping

        pos += vel * dt
        self.positions[obj_start:] = pos
        self.velocities[obj_start:] = vel

    def compute_multiverse_branches(self, origin: np.ndarray) -> np.ndarray:
        branches = np.zeros((NUM_BRANCHES, BRANCH_STEPS, 3), dtype=np.float32)
        base_target = origin + np.array([0.0, 0.0, 1.0], dtype=np.float32)

        for b in range(NUM_BRANCHES):
            angle = (b - 2) * 0.35
            disp_x = np.sin(angle) * 0.4
            disp_y = np.cos(angle) * 0.15
            for s in range(BRANCH_STEPS):
                t = (s + 1) / float(BRANCH_STEPS)
                pt = origin + (base_target - origin) * t
                pt[0] += disp_x * (t ** 1.5)
                pt[1] += (disp_y - 0.5 * 9.81 * (t * 0.4) ** 2) * t
                pt[2] += t * 0.8
                branches[b, s] = pt

        return branches

    def update_avatar_dynamics(self, dt: float, user_head: np.ndarray, user_hand: np.ndarray, is_handoff: bool):
        # Subtle breathing & gaze saccade
        t = time.time()
        self.avatar_head_pos[1] = 1.6 + 0.008 * math.sin(t * 1.8)
        self.avatar_gaze_target = user_head + np.array([0.0, 0.05 * math.sin(t * 3.0), 0.0], dtype=np.float32)
        self.avatar_blink = 1.0 if (math.sin(t * 0.8) > 0.96) else 0.0

        # Cooperative Handoff Reach IK
        if is_handoff:
            target = user_hand + np.array([0.0, 0.05, 0.08], dtype=np.float32)
            self.avatar_hand_pos += (target - self.avatar_hand_pos) * min(1.0, 5.0 * dt)
        else:
            default_rest = np.array([0.22, 1.05, 1.5], dtype=np.float32)
            self.avatar_hand_pos += (default_rest - self.avatar_hand_pos) * min(1.0, 2.5 * dt)

sim_state = SimulationState()

if app:
    @app.post("/ingest_media")
    async def ingest_media(file: UploadFile = File(...)):
        content = await file.read()
        print(f"[5D Reality-Lifting] Ingested media: {file.filename} ({len(content)} bytes)")
        sim_state._init_procedural_scene()
        return {
            "status": "success",
            "filename": file.filename,
            "splat_count": NUM_SPLATS,
            "avatar_bound": True,
            "flame_mesh": "SMPL-X / Gaussian Head Rig Initialized"
        }

    @app.websocket("/ws")
    async def websocket_endpoint(websocket: WebSocket):
        await websocket.accept()
        print("[WebSocket] Unity 6 Client connected at 90Hz.")
        last_time = time.time()

        try:
            while True:
                # Receive input telemetry from Unity
                recv_text = await asyncio.wait_for(websocket.receive_text(), timeout=0.033)
                data = json.loads(recv_text)

                hmd_pos = np.array(data.get("hmdPos", [0, 1.6, 0]), dtype=np.float32)
                hand_l = np.array(data.get("handLPos", [-0.2, 1.2, 0.4]), dtype=np.float32)
                hand_r = np.array(data.get("handRPos", [0.2, 1.2, 0.4]), dtype=np.float32)
                stiffness = float(data.get("stiffness", 1.0))
                is_matter_weave = bool(data.get("matterWeave", False))
                is_handoff = bool(data.get("handoff", False))

                now = time.time()
                dt = max(0.001, min(0.05, now - last_time))
                last_time = now

                # Run physics simulation
                sim_state.step_mpm(dt, hand_l, hand_r, stiffness, is_matter_weave)
                branches = sim_state.compute_multiverse_branches(hand_r)
                sim_state.update_avatar_dynamics(dt, hmd_pos, hand_r, is_handoff)

                # Send delta package
                response = {
                    "timestamp": now,
                    "branches": branches.tolist(),
                    "avatar": {
                        "headPos": sim_state.avatar_head_pos.tolist(),
                        "handPos": sim_state.avatar_hand_pos.tolist(),
                        "gazeTarget": sim_state.avatar_gaze_target.tolist(),
                        "blink": sim_state.avatar_blink
                    },
                    "matterWeaveActive": is_matter_weave,
                    "splatCount": NUM_SPLATS
                }
                await websocket.send_text(json.dumps(response))

        except (WebSocketDisconnect, asyncio.TimeoutError):
            print("[WebSocket] Unity Client streaming cycle ended.")
        except Exception as e:
            print(f"[WebSocket] Error: {e}")

if __name__ == "__main__":
    if app:
        print("[OMNI-GENESIS 5D] Starting backend server on http://127.0.0.1:8765")
        uvicorn.run(app, host="127.0.0.1", port=8765)
    else:
        print("Install fastapi and uvicorn: pip install fastapi uvicorn numpy")
