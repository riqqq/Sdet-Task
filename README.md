# Sdet-Task: Lyra Automation with AltTester

This repository contains the solution for the R8G Software Development Engineer in Test (SDET) assessment. It tests the **Lyra Starter Game** using **AltTester** (Unity/Unreal automation).

## 📂 Project Structure

- **UnitTest1.cs**: The main C# test file containing:
  -   **Smoke Tests**: 5 basic tests for Game Launch, Front-End, UI, Gameplay Navigation, and Spawning.
  -   **Helper Methods**: Custom logic for robust menu navigation and dynamic aiming.
- **Sdet-Task.csproj**: Project configuration.

## 🚀 Smoke Tests

The suite includes 5 distinct smoke tests designed to verify critical paths:

1.  **Test01_GameLaunchAndConnection**: Confirms the game process is active and AltTester can retrieve the current scene.
2.  **Test02_FrontEndSceneLoad**: Verifies the main menu (`L_LyraFrontEnd`) loads.
3.  **Test03_MainMenuUIElementsPresent**: Validates that critical buttons (Start, Options, Quit) exist on the UI.
4.  **Test04_StartGameFlow**: Executes a full user flow: *Play Lyra -> Start a Game -> Elimination -> L_Expanse*.
    -   *Robustness*: Includes retry logic for unstable UI tile selection.
5.  **Test05_CharacterSpawnInGameplay**: Confirms a player character (e.g., `B_Hero_ShooterMannequin`) spawns in the level.

## 🎯 Aiming Helper Method

### The Challenge
Lyra (UE 5.3) uses the **Enhanced Input System**, which abstracts input injection. Traditional automation methods that simulate mouse movement (e.g., generating 2D axis input events) are often flaky or ignored by the new input stack.

### The Solution: Direct Controller Rotation
Instead of simulating "mouse input" which attempts to push the camera, we implemented a robust **Direct Control** approach.

1.  **Math**: We calculate the exact Look-At rotation (Pitch/Yaw) from the Player to the Target using basic 3D vector trigonometry.
2.  **Execution**: We apply this rotation directly to the Player Controller via a custom Blueprint interface.

### Method Signature
```csharp
public void AimAt(AltObject target);
```

---

## 🛠️ Lyra Project Modifications

To enable the `AimAt` method to work reliably with Enhanced Input, a small modification to the Lyra project is required. We must expose a function that allows setting the Control Rotation from a string parameter (since AltTester calls methods via string parsing).

### Step-by-Step Instructions

1.  **Open Unreal Engine Editor**.
2.  Locate the Character Blueprint:
    -   Path: `Content/Characters/Heroes/B_Hero_ShooterMannequin` (or your active pawn).
3.  **Open the Event Graph**.
4.  **Create a New Custom Event**:
    -   Name: `Test_SetControlRotation`
    -   Inputs: `String Parameters` (Format: "Pitch,Yaw")
5.  **Implement the Logic**:
    -   Add a **Parse Into Array** node (Delimiter: `,`) connected to `Parameters`.
    -   Get `Player Controller` (Get Owner -> Cast to PlayerController).
    -   Call **Set Control Rotation** on the Controller.
    -   Convert `Array[0]` to Float (Pitch) and `Array[1]` to Float (Yaw).
    -   Construct a Rotator and plug it into `Set Control Rotation`.
6.  **Compile and Save**.

### Blueprint Snippet Logic
```
Event Test_SetControlRotation(String Parameters)
   -> ParseIntoArray(Parameters, ",") -> Array
   -> GetController -> SetControlRotation(MakeRotator(Pitch=Array[0], Yaw=Array[1], Roll=0))
```

This ensures 100% accurate aiming regardless of input sensitivity, frame rate, or input system configuration.
