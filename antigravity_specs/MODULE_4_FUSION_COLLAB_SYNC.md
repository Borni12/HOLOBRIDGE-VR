# Module 4 Specification: Photon Fusion 2 State Synchronization

## Target Filename
`Assets/Scripts/Networking/FusionCollabController.cs`

## Functional Requirements
1. Fusion 2 Architecture:
   - Inherit directly from `NetworkBehaviour`.
   - Implement state authority handoffs: when a client interacts with a 3D object, call `Object.RequestStateAuthority()`.

2. Networked Properties & Soft-Locking:
   - Define `[Networked] ref NetworkBool IsLocked { get; }`
   - Define `[Networked] ref PlayerRef LockedBy { get; }`
   - Attach an `OnChanged` static callback to update mesh visual materials across all clients:
     - Owner Client: Subtle green selection highlight.
     - Remote Clients: Semi-transparent orange locked outline, rejecting concurrent edits.

3. Drawing Stroke Synchronization:
   - Implement an RPC or networked buffer pattern to broadcast new polyline points added by the authority client.
   - Enforce low-latency synchronization with interpolation for remote displays.
   - Ensure clean execution across Host, Client, and Shared Topology modes.
