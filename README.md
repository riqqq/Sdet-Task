# Sdet-Task: Lyra Automation with AltTester

This repository contains the solution for the R8G Software Development Engineer in Test (SDET) assessment. It tests the **Lyra Starter Game** using **AltTester** (Unity/Unreal automation).

## 📂 Project Structure

- **UnitTest1.cs**: The main C# test file containing:
  -   **Smoke Tests**: 5 basic tests for Game Launch, Front-End, UI, Gameplay Navigation, and Spawning.
  -   **Helper Methods**: Custom logic for robust menu navigation and dynamic aiming.
- **Sdet-Task.csproj**: Project configuration.

## 🚀 Smoke Tests & Rationale

The test suite is designed to validate the **Critical User Journey (CUJ)** of the Lyra Starter Game. I chose a sequential flow strategy to mimic real user behavior and optimize execution time, ensuring that the most fundamental components (Launch -> UI -> Gameplay) work before attempting complex interactions.

### Test Breakdown

1.  **`Test01_GameLaunchAndConnection`**
    -   **Goal**: Verify the application process starts and the AltTester driver can establish a socket connection.
    -   **Rationale**: This is the "Hello World" of automation. If this fails, no other tests can run. It rules out connectivity/firewall issues immediately.

2.  **`Test02_FrontEndSceneLoad`**
    -   **Goal**: Confirm the game enters the correct initial state (`L_LyraFrontEnd`).
    -   **Rationale**: Validates level loading logic and ensures the game doesn't crash post-launch.

3.  **`Test03_MainMenuUIElementsPresent`**
    -   **Goal**: Validate the presence and accessibility of core UI elements (Start, Options, Quit).
    -   **Rationale**: Ensures the UI layer is rendered and interactive. I check for *interactivity hints* (transform existence) rather than just visual pixels.

4.  **`Test04_StartGameFlow`**
    -   **Goal**: Execute the full flow from Main Menu to Gameplay (*Play -> Host -> Elimination -> L_Expanse*).
    -   **Rationale**: This is the core "Happy Path". It tests complex UI navigation, dynamic widget interaction (finding the "Elimination" tile among generated children), and scene transition logic.
    -   **Design Choice**: I implemented **polling retries** for the tile selection because UI widgets in Unreal often rebuild or animate, leading to "stale element" exceptions in strict automation.

5.  **`Test05_CharacterSpawnInGameplay`**
    -   **Goal**: Confirm a player character (`B_Hero_ShooterMannequin`) exists in the level.
    -   **Rationale**: Verifies the GameMode logic (spawning) and replication/player controller assignment.

### Test Design Philosophy
-   **Sequential Dependencies**: While unit tests should be independent, these E2E smoke tests follow a sequence to avoid restarting the game 5 times, dramatically reducing test suite duration.
-   **Robustness over Speed**: I use explicit waits and retry loops for UI interactions to account for the variable performance of the game engine (loading times, animation frames).

## 🎯 Aiming Helper Method (`AimAt`)

### The Challenge: Enhanced Input
Lyra (UE 5.3.2) utilizes the **Enhanced Input System**, which creates an abstraction layer between raw hardware inputs (Mouse/Keyboard) and game actions.
-   **Problem**: Traditional automation that injects raw mouse deltas (e.g., `Input.SimulateMouse`) is often ignored or smoothed out by the Enhanced Input layer, leading to imprecise aiming.

### The Solution: Direct Controller Rotation & Reflection
To ensure 100% reliability, I bypassed the input layer entirely in favor of a **Direct Control** approach.

1.  **Math**: I calculate the precise **Look-At Rotation** (Pitch/Yaw) required to face the target using 3D trigonometry.
2.  **Reflection**: I use **C# Reflection** to inspect and interact with the AltTester objects dynamically.
    -   *Why?* This makes the test code robust against different versions of the AltTester SDK or changes in the underlying C# class structures (`AltObject` properties vs fields).
3.  **Execution**: I invoke a custom Blueprint event on the Player Controller to set the rotation directly.

### Method Signature
```csharp
public void AimAt(object target);
```

### Why this approach?
-   **Determinism**: "Simulating mouse" for 500ms might turn the camera 90 degrees or 85 degrees depending on framerate. Setting `ControlRotation` to `(0, 90, 0)` is exact every time.
-   **Speed**: Rotation is instant, allowing for faster test execution.

---

## 🛠️ Lyra Project Modifications

To bridge the gap between my C# automation and the specific gameplay logic of Lyra, I need a "hook" to apply our calculated rotation.

### Required: `Test_SetControlRotation` Event
I chose to implement a **Custom Event** in the Character Blueprint rather than a C++ plugin to keep the integration lightweight and accessible to Scripters/Designers.

Modified files are in the **ProjectMods** folder. I have also included a step-by-step guide on how to implement it below.

**Instructions:**
1.  **Open Unreal Engine Editor**.
2.  Open **`B_Hero_ShooterMannequin`** Blueprint (Plugins\GameFeatures\ShooterCore\Content\Game).
3.  In the **Event Graph**, Create a New Custom Event named **`Test_SetControlRotation`**.
4.  Add an Input Parameter: `String Parameters` (Format: "Pitch,Yaw").
5.  Make sure to enable "Call In Editor" for the custom event.
6.  **Logic Implementation**:
    -   Use **Parse Into Array** (Delimiter: `,`) to split the string.
    -   **Get Array[0] as Float** and pass it as Y (Pitch) to **Make Rotator.**
    -   **Get Array[1] as Float** and pass it as Z (Yaw) to **Make Rotator.**
    -   **Get Player Controller** with Player index 0.
    -   **Set Control Rotation** using the parsed Pitch/Yaw values (Roll = 0) and Player Controller.
7.  **Compile & Save**.

This slight modification empowers the automation to act as a "Super User," enabling precise validation of aiming mechanisms without fighting the input system.


This ensures 100% accurate aiming regardless of input sensitivity, frame rate, or input system configuration.
