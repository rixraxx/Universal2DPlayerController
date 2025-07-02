# 🕹️ Universal 2D Platformer Player Controller for Unity

A feature-rich, modular 2D platformer controller built with Unity’s Rigidbody2D physics. Designed to support a wide range of platformer mechanics — from simple movement to advanced interactions like wall sliding, ledge grabbing, dashing, and more — without needing third-party assets or ScriptableObjects.

---

## 🚀 Features

- ✅ **Basic Movement** with acceleration and deceleration
- 🦘 **Jumping** with coyote time and jump buffering
- 🌬️ **Air Control** with custom gravity logic
- 🧍‍♂️ **Crouching** with ceiling detection
- 🧱 **Wall Slide** and **Wall Jump**
- 🧗 **Ledge Grab** and Climb
- ⚡ **Dashing** with cooldown and directional input
- 🌀 **Double Jump** (Optional)
- 🎞️ **Animation Hooks** and **Visual Effects**
- 🔊 **Audio Events** for jump, land, and dash
- 📦 Highly configurable through the Unity Inspector
- 🧠 **Event System** using `UnityEvent`s

---

## 🧠 Setup

1. **Import the Script**  
   Add `UniversalPlayerController.cs` to your Unity project.

2. **Attach Components**  
   Create a new GameObject and attach:
   - `Rigidbody2D`
   - `Collider2D` (Box, Capsule, etc.)
   - `UniversalPlayerController` (this script)

3. **Configure Parameters**  
   Tweak settings in the Inspector, including movement speeds, jump power, and ground detection.

4. **Assign Required Transforms and Layers**  
   - Ground & wall layers
   - Ground/wall/ledge/ceiling check transforms
   - (Optional) Particle Systems, Audio Clips, Animator, SpriteRenderer

---

## 🎮 Input

This script uses Unity's default input system:
| Action        | Input Key             |
|---------------|------------------------|
| Move Left/Right | `A` / `D` or Arrow Keys |
| Jump          | `Space`               |
| Crouch        | `S` or `Down Arrow`   |
| Dash          | `Left Shift`          |
| Climb / Drop (Ledge) | `W` / `S` or Arrow Keys |

---

## 🎨 Animator Parameters (Optional)

The controller updates the following Animator parameters:
- `Grounded` (bool)
- `Speed` (float)
- `InputSpeed` (float)
- `Crouch` (bool)
- `WallSliding` (bool)
- `LedgeHang` (bool)
- `VerticalSpeed` (float)
- `Dashing` (bool)
- `Dash` (trigger)

---

## 🧪 Events (UnityEvents)

Hook into these public events to extend behavior:
- `OnJump`
- `OnLand`
- `OnDash`
- `OnWallJump`
- `OnLedgeGrab`

---

## ⚙️ Public Methods

Use these at runtime:
```csharp
player.SetMaxSpeed(float newSpeed);
player.SetJumpPower(float newPower);
bool grounded = player.IsGrounded;
bool dashing = player.IsDashing;
Vector2 velocity = player.Velocity;
