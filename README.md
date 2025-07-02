# 🕹️ Universal 2D Player Controller for Unity

A modular, feature-rich 2D platformer player controller built on Unity’s Rigidbody2D system. This controller supports both simple and advanced mechanics — including jumping, dashing, wall sliding, ledge grabbing, crouching, and more — making it ideal for any 2D platformer game.

---

## 🚀 Features

- ✅ **Basic Movement** with acceleration & deceleration
- 🦘 **Jumping** with coyote time & buffering
- 🌬️ **Air Control** with custom gravity handling
- 🧍‍♂️ **Crouching** with ceiling detection
- 🧱 **Wall Slide** & **Wall Jump**
- 🧗 **Ledge Grab** & Climb
- ⚡ **Dashing** with cooldown and direction control
- 🌀 **Double Jump** (optional)
- 🎞️ **Animation & Visual Effect Hooks**
- 🔊 **Sound Events** for jump, dash, and land
- 🧠 **UnityEvent Support** for plug-and-play reactions
- 🛠️ Highly configurable from the Unity Inspector

---

## 🧠 Setup

1. **Import the Script**  
   Add `UniversalPlayerController.cs` to your Unity project.

2. **Attach Required Components**  
   - `Rigidbody2D`
   - `Collider2D` (Box, Capsule, etc.)
   - `UniversalPlayerController`

3. **Configure Settings**  
   Tweak values like speed, jump power, gravity, and toggle optional features (dash, crouch, wall jump, etc.).

4. **Assign Optional Components**  
   - Animator
   - Audio Clips
   - Particle Systems
   - Transforms for ledge/ground/ceiling detection

---

## 🎮 Default Input Mapping

| Action        | Input Key             |
|---------------|------------------------|
| Move Left/Right | `A` / `D` or Arrow Keys |
| Jump          | `Space`               |
| Crouch        | `S` or `Down Arrow`   |
| Dash          | `Left Shift`          |
| Climb / Drop (Ledge) | `W` / `S` or Arrow Keys |

---

## 🎨 Animator Parameters (Optional)

The controller updates these Animator parameters:
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

## 🧪 Unity Events

Subscribe to the following UnityEvents in the Inspector:
- `OnJump`
- `OnLand`
- `OnDash`
- `OnWallJump`
- `OnLedgeGrab`

---

## 🧰 Public API

```csharp
player.SetMaxSpeed(float newSpeed);
player.SetJumpPower(float newPower);
bool grounded = player.IsGrounded;
bool dashing = player.IsDashing;
Vector2 velocity = player.Velocity;
```

---

## 📂 Suggested Folder Structure

```
/Scripts/Player/UniversalPlayerController.cs
/Prefabs/Player.prefab
/Animations/PlayerAnimator.controller
/Audio/Jump, Land, Dash.wav
/Particles/JumpFX, LandFX, DashFX
```

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).

---

## 🙋‍♂️ Contributions Welcome

Bug reports, improvements, and pull requests are appreciated. Let's make 2D platformers better together!
