# 🚀 Netlify 1-Click Deployment Guide

## 1. How to Host This Control Portal on Netlify (Free, 30 Seconds)
1. Go to [https://app.netlify.com/drop](https://app.netlify.com/drop) in your browser.
2. Sign in with your GitHub, Google, or Email account.
3. Drag & Drop the entire folder **`F:\VR Projects\HOLOBRIDGE-VR\Netlify_Portal`** onto the Netlify Drop area.
4. Netlify will deploy it instantly and give you a public URL like:
   `https://holobridge-portal-xyz.netlify.app`
5. Open that link on your smartphone, tablet, laptop, or share it with anyone in the world!

## 2. Connecting to Your Unity Host
- When the portal opens, enter your Public Server URL:
  - If using Cloudflare Tunnel: `https://your-subdomain.trycloudflare.com`
  - If on the same WiFi / Local Network: `http://192.168.0.24:8080`
- Click **"CONNECT"**.

## 3. Features Included
- **🔴 25 FPS Live Feed:** Real-time video from inside the Unity 3D room.
- **🎥 Multi-Camera / Multi-User Switcher:**
  - Click **🖥️ Host** for the host player's view.
  - Click **🌐 Arena** for an overhead tactical wide-angle view.
  - Click any **Connected User's Badge / Chip** (e.g. `Guest_8142`) to jump into their over-the-shoulder live perspective!
- **🕹️ D-Pad Locomotion:** WASD or on-screen directional pad to walk the selected character around the 3D room.
- **🔴 Laser Raycast:** Click or tap anywhere on the video feed to shoot a spatial laser beam and manipulate 3D objects.
- **🎨 Scene Theme Switcher:** Instant toggle between Cyberpunk Neon, Mars Colony, Minimalist Zen, and Medical Clean.
- **👥 Live Operator Counter:** Displays exact online users count (initially 0, updates live as guests connect).
