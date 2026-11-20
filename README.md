# Unity FPS Gameplay Framework

A modular, production-ready first-person shooter framework for Unity. Features an advanced character controller with smooth movement mechanics, a weapon system with ricochets, interactive objects via interfaces, and adaptive audio feedback. Built with scalability and clean code principles in mind.

## 🎮 Core Features

### 1. Player Controller (`PlayerController`)
Built on top of Unity's `CharacterController` inside the `Game.Player` namespace. Uses a config-based architecture (`MovementConfig`, `JumpConfig`, etc.) for easy tuning without touching core logic.
-   **Smooth Acceleration & Deceleration:** Natural inertia; no instant stops or starts.
-   **Coyote Time & Jump Buffering:** Forgives imperfect timing when jumping off ledges or approaching ground.
-   **Variable Jump Height:** Hold longer to jump higher. Includes "Jump Squat" anticipation support.
-   **Slide & Dash:** Momentum-based sliding and directional dashing with obstacle detection and cooldowns.
-   **Event-Driven API:** Public Actions (`OnJump`, `OnLand`, `OnDashStart`, etc.) for easy integration with audio, particles, and UI.
-   **Interface-Based Interaction:** Communicates with camera/audio via `ICameraShaker` interface, not direct references.

### 2. Weapon System
Complete combat foundation for FPS games:
-   **Raycast Shooting with Ricochet:** Bullets bounce off surfaces based on material properties.
-   **Recoil & Sway:** Procedural camera kickback and weapon sway synced to player speed/state.
-   **ADS (Aim Down Sights):** Smooth FOV transition and weapon pivot adjustment.
-   **Visual Feedback:** Muzzle flashes, tracers, and impact effects.

### 3. Interaction System
Decoupled interaction logic using C# interfaces (`IInteractable`):
-   **Synced Doors & Panels:** Multi-panel doors that synchronize state across triggers.
-   **Lock Mechanism:** Visual and audio feedback for locked interactables.
-   **Dynamic Hints:** Context-aware UI prompts (e.g., "[E] Open") via TextMeshPro.

### 4. Adaptive Audio & Camera
-   **Smart Footstep System:** Blends walk/run sounds based on *actual velocity*, not just input. Prevents "running sound while stuck against a wall."
-   **Landing Shake:** Intensity scales with fall speed via `ICameraShaker` interface.
-   **Head Bob & Roll:** Dynamic camera movement tied to speed, crouching, and strafing.

### 5. Player Health
Lightweight HP manager with event-driven damage/death callbacks for easy UI integration.

## 🛠️ Setup Instructions

1.  Import the `Scripts` folder into your Unity project (**Unity 2021.3 LTS+ recommended**).
2.  Ensure **TextMeshPro** is installed (Window > TextMeshPro > Import TMP Essentials).
3.  Create the required interfaces if missing:
    ```csharp
    // ICameraShaker.cs
    public interface ICameraShaker { void Shake(float intensity, float duration); }
    
    // IInteractable.cs
    public interface IInteractable { 
        void Interact(GameObject interactor); 
        string GetInteractPrompt(); 
    }
    ```
4.  Create a new GameObject named `Player` and add the **`PlayerController`** component (from namespace `Game.Player`).
5.  Create a child object named `CameraHolder` with a Camera and **`PlayerCamera`** script attached.
6.  In the `PlayerController` inspector:
    -   Assign `Camera Holder` → The `CameraHolder` Transform (required for mouse look).
    -   Configure movement speeds, gravity, and keybindings in the nested Config structs.
7.  For weapons, add `WeaponSystem` to your weapon prefab and link the main camera + muzzle point.
## 📂 Recommended Hierarchy

For the weapon sway, recoil, and ADS systems to work correctly, organize your player prefab as follows:

```text
Player (AdvancedPlayerController)
└── CameraHolder (Transform for mouse look)
    └── Main Camera (PlayerCamera script)
        └── WeaponSafety
            └── WeaponPivot (Moves for ADS/Aim Down Sights)
                └── MouseSwayPivot (Tilts with mouse movement)
                    └── SwayPivot (Bobs while walking/running)
                        └── JumpSwayPivot (Reacts to jumping/landing)
                            └── [Your Weapon Model]
<img width="294" height="188" alt="image" src="https://github.com/user-attachments/assets/934e2e1b-e5b1-4a2b-bf8c-42d915551a03" />


## 🏗️ Architecture Highlights

This framework is designed to be extended without modifying core code:
-   **Namespace Isolation:** Core player logic is encapsulated in `Game.Player` namespace.
-   **Serialized Config Structs:** Keeps the main controller clean and allows designers to tune gameplay without touching code.
-   **Interface-Based Communication:** Systems communicate via `ICameraShaker` and `IInteractable` rather than direct references, reducing coupling.
-   **Input Abstraction:** Input reading is isolated in `ReadInput()`, making it trivial to swap to Unity's new Input System later.

## 📄 License

MIT License. Free for personal and commercial use. Attribution appreciated but not required.
