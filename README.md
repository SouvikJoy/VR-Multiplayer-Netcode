## Approach (VR)
This prototype uses **VR** with **XR Interaction Toolkit** because the core requirement is a shared immersive space where two users can interact with shared objects in real time.

## Multiplayer Framework
We use **Netcode for GameObjects** with **Unity Transport** and **Relay via Unity Multiplayer Services**.

## System Architecture Overview
### Scenes
- **MainMenu Scene**
  - UI: **Host**, **Join**, **Join Code Input**, **Start**, **Leave**
- **GamePlay Scene**
  - **XR Origin** for local VR rig (HMD + controllers)
  - **Network Player**
    - A simple network player root

### Networking / Authority

- **Shared Interaction (Cube Grab/Drag)**
  - Implemented as **server-authoritative**:
    - Client requests grab permission
    - Server locks the cube to that player while grabbed
    - Server moves the cube and replicates movement to all clients
  - Result: both players see the same interaction consistently.

### Key Scripts (example)
- `MainMenuUI.cs` — Host/Join/Start/Leave + Join Code flow
- `NetPlayer.cs` — Minimal networked player presence synced from VR pose
- `ServerGrabCube.cs` — Server-authoritative XR grab/drag sync
