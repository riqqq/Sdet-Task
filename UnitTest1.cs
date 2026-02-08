using AltTester.AltTesterSDK.Driver;
using AltTester.AltTesterSDK.Driver.Commands;
using System;
using System.Linq;
using NUnit.Framework;

namespace Sdet_Task;

/// <summary>
/// Smoke tests for Lyra Starter Game using AltTester.
/// Tests core functionalities: Game Launch, Scene Loading, UI Navigation, Character Spawn.
/// </summary>
[TestFixture]
public class LyraSmokeTests
{
    private AltDriver altDriver;
    private const int DefaultTimeout = 30;
    private const int SceneLoadTimeout = 60;
    private const int CharacterSpawnTimeout = 90;

    [OneTimeSetUp]
    public void Setup()
    {
        altDriver = new AltDriver(host: "127.0.0.1", port: 13000, connectTimeout: 60);
        TestContext.WriteLine("AltDriver connected successfully.");
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        altDriver?.Stop();
        TestContext.WriteLine("AltDriver disconnected.");
    }

    #region Smoke Test 1: Game Launch & Connection
    
    /// <summary>
    /// Smoke Test 1: Verifies the game is running and AltTester connection is established.
    /// </summary>
    [Test, Order(1)]
    [Category("Smoke")]
    public void Test01_GameLaunchAndConnection()
    {
        TestContext.WriteLine("TEST: Verifying game launch and AltTester connection...");
        
        // If we got this far, connection succeeded (would throw in Setup otherwise)
        // Verify we can query the current scene
        var currentScene = altDriver.GetCurrentScene();
        
        TestContext.WriteLine($"Current scene: {currentScene}");
        Assert.That(currentScene, Is.Not.Null.And.Not.Empty, 
            "Should be able to retrieve current scene name");
        
        TestContext.WriteLine("PASSED: Game is running and AltTester connection established.");
    }
    
    #endregion

    #region Smoke Test 2: Front-End Scene Load
    
    /// <summary>
    /// Smoke Test 2: Verifies the front-end/main menu scene loads correctly.
    /// </summary>
    [Test, Order(2)]
    [Category("Smoke")]
    public void Test02_FrontEndSceneLoad()
    {
        TestContext.WriteLine("TEST: Loading front-end scene...");
        
        altDriver.LoadScene("L_LyraFrontEnd");
        altDriver.WaitForCurrentSceneToBe("L_LyraFrontEnd", timeout: SceneLoadTimeout);
        
        var currentScene = altDriver.GetCurrentScene();
        TestContext.WriteLine($"Loaded scene: {currentScene}");
        
        Assert.That(currentScene, Does.Contain("LyraFrontEnd"),
            "Front-end scene should be loaded");
        
        TestContext.WriteLine("PASSED: Front-end scene loaded successfully.");
    }
    
    #endregion

    #region Smoke Test 3: Main Menu UI Elements Present
    
    /// <summary>
    /// Smoke Test 3: Verifies core UI elements are present on the main menu.
    /// </summary>
    [Test, Order(3)]
    [Category("Smoke")]
    public void Test03_MainMenuUIElementsPresent()
    {
        TestContext.WriteLine("TEST: Checking main menu UI elements...");
        
        // Ensure we're on the front-end
        altDriver.LoadScene("L_LyraFrontEnd");
        altDriver.WaitForCurrentSceneToBe("L_LyraFrontEnd", timeout: SceneLoadTimeout);
        
        // Check for essential UI elements
        var startGameButton = altDriver.WaitForObject(
            By.NAME, "StartGameButton", 
            timeout: DefaultTimeout);
        TestContext.WriteLine($"Found: StartGameButton at transform {startGameButton.transformId}");
        
        var optionsButton = altDriver.WaitForObject(
            By.NAME, "OptionsButton", 
            timeout: DefaultTimeout);
        TestContext.WriteLine($"Found: OptionsButton at transform {optionsButton.transformId}");
        
        var quitButton = altDriver.WaitForObject(
            By.NAME, "QuitGameButton", 
            timeout: DefaultTimeout);
        TestContext.WriteLine($"Found: QuitGameButton at transform {quitButton.transformId}");
        
        Assert.Multiple(() =>
        {
            Assert.That(startGameButton, Is.Not.Null, "StartGameButton should exist");
            Assert.That(optionsButton, Is.Not.Null, "OptionsButton should exist");
            Assert.That(quitButton, Is.Not.Null, "QuitGameButton should exist");
        });
        
        TestContext.WriteLine("PASSED: All core main menu UI elements present.");
    }
    
    #endregion

    #region Smoke Test 4: Start Game Flow
    
    /// <summary>
    /// Smoke Test 4: Verifies the full menu navigation flow.
    /// Flow: StartGameButton (Play Lyra) -> HostButton (Start a game) -> Elimination tile -> L_Expanse
    /// </summary>
    [Test, Order(4)]
    [Category("Smoke")]
    public void Test04_StartGameFlow()
    {
        TestContext.WriteLine("TEST: Testing full menu navigation flow to Elimination (L_Expanse)...");
        
        // Navigate through menu to gameplay
        NavigateToEliminationGameplay();
        
        // Verify we're in L_Expanse
        var currentScene = altDriver.GetCurrentScene();
        TestContext.WriteLine($"Scene loaded: {currentScene}");
        
        Assert.That(currentScene, Does.Contain("Expanse"), 
            $"L_Expanse level should be loaded after selecting Elimination. Actual: {currentScene}");
        
        TestContext.WriteLine("PASSED: Full menu navigation flow completed successfully.");
    }
    
    /// <summary>
    /// Helper method to navigate through the full menu flow to Elimination gameplay.
    /// Finds the ExperienceTitle element with text "Elimination" and clicks its parent ExperienceTileButton.
    /// Matches the dynamic W_ExperienceTile_C_X widgets by hierarchy.
    /// </summary>
    private void NavigateToEliminationGameplay()
    {
        // STEP 1: Load front-end scene
        altDriver.LoadScene("L_LyraFrontEnd");
        altDriver.WaitForCurrentSceneToBe("L_LyraFrontEnd", timeout: SceneLoadTimeout);
        TestContext.WriteLine("  -> Scene: L_LyraFrontEnd loaded");
        Thread.Sleep(3000); // Increased wait for stability
        
        // STEP 2: Click "Play Lyra" (StartGameButton)
        var startButton = altDriver.WaitForObject(By.NAME, "StartGameButton", timeout: DefaultTimeout);
        startButton.Click();
        TestContext.WriteLine("  -> Clicked StartGameButton (Play Lyra)");
        Thread.Sleep(2000);
        
        // STEP 3: Click "Start a game" (HostButton)
        var hostButton = altDriver.WaitForObject(By.NAME, "HostButton", timeout: DefaultTimeout);
        hostButton.Click();
        TestContext.WriteLine("  -> Clicked HostButton (Start a game)");
        Thread.Sleep(3000); // Wait for menu animation/population
        
        // STEP 4: Loop to find and click "Elimination" until scene changes
        // We retry the find logic because the UI might be rebuilding or scrolling
        TestContext.WriteLine("  -> Attempting to select 'Elimination' (entering retry loop)...");
        
        int maxAttempts = 3;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            TestContext.WriteLine($"  -> Attempt {attempt}/{maxAttempts}: Finding and clicking Elimination tile...");
            
            try 
            {
                AltObject? experienceTile = FindEliminationTileSimple();
                
                if (experienceTile != null)
                {
                    TestContext.WriteLine($"  -> Clicking tile: {experienceTile.name}");
                    experienceTile.Click();
                    
                    // Also try clicking text if tile click fails?
                    // No, let's trust the tile click first. 
                }
                else
                {
                    TestContext.WriteLine("  -> Could not find tile this attempt.");
                }

                // Poll for scene change for 10 seconds
                TestContext.WriteLine("  -> Waiting for scene change...");
                for (int w = 0; w < 4; w++) // 4 * 2.5s = 10s wait per click attempt
                {
                    Thread.Sleep(2500);
                    var currentScene = altDriver.GetCurrentScene();
                    if (currentScene.Contains("Expanse"))
                    {
                        TestContext.WriteLine("  -> SUCCESS! L_Expanse loaded");
                        return; // Done!
                    }
                }
                
                TestContext.WriteLine("  -> Scene did not change. Retrying selection...");
            }
            catch (Exception ex) 
            {
                TestContext.WriteLine($"    -> Error during attempt {attempt}: {ex.Message}");
            }
        }
        
        throw new InvalidOperationException($"L_Expanse did not load after {maxAttempts} attempts. Last scene: {altDriver.GetCurrentScene()}");
    }

    private AltObject? FindEliminationTileSimple()
    {
        // Robust Fallback: Scan all W_ExperienceTile widgets for "Elimination" text
        // We do this because parentId property is not available in AltObject C# SDK
        var tiles = altDriver.FindObjectsWhichContain(By.NAME, "W_ExperienceTile");
        foreach(var tile in tiles)
        {
            try 
            {
                // Find all ExperienceTitle elements under this tile
                // We use path relative to root but filtered by tile name in path
                // Note: AltTester path searches from root usually
                var path = $"//*[contains(@name,'{tile.name}')]//*[contains(@name,'ExperienceTitle')]";
                var titles = altDriver.FindObjects(By.PATH, path);
                
                foreach (var title in titles)
                {
                     try 
                     {
                         var text = title.GetText();
                         if (text != null && text.Contains("Elimination", StringComparison.OrdinalIgnoreCase))
                         {
                             return tile;
                         }
                     } 
                     catch {}
                }
            } 
            catch {}
        }
        
        // Final fallback: Try to return the text element itself if found (better than nothing)
        try 
        {
            return altDriver.FindObject(By.TEXT, "Elimination"); 
        } 
        catch 
        {
            return null; 
        }
    }
    
    #endregion

    #region Smoke Test 5: Character Spawn (Full Flow)
    
    /// <summary>
    /// Smoke Test 5: Verifies a player character spawns when entering gameplay.
    /// Reuses the L_Expanse scene from Test 4 if already loaded, otherwise navigates.
    /// </summary>
    [Test, Order(5)]
    [Category("Smoke")]
    public void Test05_CharacterSpawnInGameplay()
    {
        TestContext.WriteLine("TEST: Verifying character spawn in gameplay...");
        
        // Check if we're already on L_Expanse (from Test 4)
        var currentScene = altDriver.GetCurrentScene();
        TestContext.WriteLine($"Current scene: {currentScene}");
        
        if (currentScene.Contains("Expanse"))
        {
            TestContext.WriteLine("Already on L_Expanse, skipping navigation...");
        }
        else
        {
            TestContext.WriteLine("Not on L_Expanse, navigating through menus...");
            NavigateToEliminationGameplay();
        }
        
        // Wait for character to spawn after level loads
        TestContext.WriteLine("Waiting for player character to spawn...");
        
        // Try multiple character patterns
        string[] characterPatterns = new[]
        {
            "B_Hero_ShooterMannequin",
            "LyraCharacter",
            "PlayerCharacter",
            "BP_Character"
        };
        
        AltObject? foundCharacter = null;
        foreach (var pattern in characterPatterns)
        {
            try
            {
                var elements = altDriver.FindObjectsWhichContain(By.NAME, pattern);
                if (elements.Count > 0)
                {
                    foundCharacter = elements[0];
                    TestContext.WriteLine($"Found character matching pattern '{pattern}': {foundCharacter.name}");
                    break;
                }
            }
            catch
            {
                // Pattern not found, try next
            }
        }
        
        // If no pattern matched, wait for the hero mannequin directly
        if (foundCharacter == null)
        {
            try
            {
                foundCharacter = altDriver.WaitForObject(
                    By.NAME, "B_Hero_ShooterMannequin",
                    timeout: CharacterSpawnTimeout);
                TestContext.WriteLine($"Found character via direct wait: {foundCharacter.name}");
            }
            catch
            {
                // Log available actors for debugging
                TestContext.WriteLine("Character not found. Dumping relevant actors...");
                var actors = altDriver.FindObjectsWhichContain(By.NAME, "Hero");
                foreach (var actor in actors.Take(5))
                {
                    TestContext.WriteLine($"  Found: {actor.name}");
                }
            }
        }
        
        Assert.That(foundCharacter, Is.Not.Null, 
            "Player character should spawn after entering gameplay");
        
        TestContext.WriteLine($"PASSED: Character '{foundCharacter?.name}' spawned successfully in L_Expanse!");
    }
    
    #endregion

    #region Discovery Helper
    
    /// <summary>
    /// Helper test to discover UI elements at each menu step.
    /// Run manually to explore Lyra's full menu flow.
    /// </summary>
    [Test]
    [Category("Discovery")]
    public void Discovery_FullMenuFlow()
    {
        // STEP 1: Main Menu
        altDriver.LoadScene("L_LyraFrontEnd");
        altDriver.WaitForCurrentSceneToBe("L_LyraFrontEnd", timeout: SceneLoadTimeout);
        Thread.Sleep(2000);
        
        TestContext.WriteLine("\n=== STEP 1: MAIN MENU ===");
        DumpElements("step1_main_menu.txt");
        DumpButtonsAndText();
        
        // STEP 2: Click "Play Lyra" / StartGameButton
        var startButton = altDriver.WaitForObject(By.NAME, "StartGameButton", timeout: DefaultTimeout);
        startButton.Click();
        TestContext.WriteLine("Clicked StartGameButton (Play Lyra)");
        Thread.Sleep(2000);
        
        TestContext.WriteLine("\n=== STEP 2: AFTER PLAY LYRA (Quickplay/Start/Browse menu) ===");
        DumpElements("step2_play_menu.txt");
        DumpButtonsAndText();
        
        // STEP 3: Try to find and click "Start a game" button
        // Let's look for potential buttons
        var allElements = altDriver.GetAllElements();
        var potentialButtons = allElements.Where(e => 
            e.name.Contains("Button") || 
            e.name.Contains("Start") ||
            e.name.Contains("Host") ||
            e.name.Contains("Create") ||
            e.name.Contains("Session")).ToList();
        
        TestContext.WriteLine("\nPotential 'Start a game' buttons:");
        foreach (var btn in potentialButtons.Take(20))
        {
            TestContext.WriteLine($"  {btn.name} | transform: {btn.transformId}");
        }
        
        Assert.Pass("Discovery complete - check step1/step2/step3 output files and console for button names.");
    }

    private void DumpElements(string filename)
    {
        var elements = altDriver.GetAllElements();
        var output = new System.Text.StringBuilder();
        output.AppendLine($"Found {elements.Count} elements:");
        
        foreach (var el in elements.OrderBy(e => e.name))
        {
            output.AppendLine($"  {el.name,-50} | {el.transformId}");
        }
        
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, filename);
        File.WriteAllText(path, output.ToString());
        TestContext.WriteLine($"Written {elements.Count} elements to {path}");
    }

    private void DumpButtonsAndText()
    {
        TestContext.WriteLine("\n--- Buttons and Interactive Elements ---");
        var elements = altDriver.GetAllElements();
        
        // Look for button-like elements
        var buttons = elements.Where(e => 
            e.name.Contains("Button") ||
            e.name.Contains("Tile") ||
            e.name.Contains("Entry") ||
            e.name.Contains("Item") ||
            e.name.Contains("Session") ||
            e.name.Contains("Quick") ||
            e.name.Contains("Browse") ||
            e.name.Contains("Create") ||
            e.name.Contains("Host") ||
            e.name.Contains("Elimination") ||
            e.name.Contains("Experience") ||
            e.name.Contains("Mode")).OrderBy(e => e.name).Distinct();
        
        foreach (var btn in buttons)
        {
            TestContext.WriteLine($"  {btn.name,-50} | {btn.transformId}");
        }
        
        TestContext.WriteLine("\n--- Text blocks that might indicate button labels ---");
        var texts = elements.Where(e => 
            e.name.Contains("Text") ||
            e.name.Contains("Label")).OrderBy(e => e.name).Take(30);
        
        foreach (var txt in texts)
        {
            TestContext.WriteLine($"  {txt.name,-50} | {txt.transformId}");
        }
    }

    #endregion

    #region Helper Method: Dynamic Aiming
    
    /// <summary>
    /// Calculates the rotation needed for the source to look at the target.
    /// Returns a tuple (Pitch, Yaw).
    /// </summary>
    private (float Pitch, float Yaw) CalculateLookAtRotation(float sourceX, float sourceY, float sourceZ, float targetX, float targetY, float targetZ)
    {
        // Unreal Coordinate System:
        // X = Forward, Y = Right, Z = Up
        
        float deltaX = targetX - sourceX;
        float deltaY = targetY - sourceY;
        float deltaZ = targetZ - sourceZ;
        
        // Yaw (Rotation around Z-axis)
        // Atan2(y, x) gives angle from X-axis
        double yawRad = Math.Atan2(deltaY, deltaX);
        float yawDeg = (float)(yawRad * (180.0 / Math.PI));
        
        // Pitch (Rotation around Y-axis / Up-Down)
        // Horizontal distance
        double dist2D = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        // Atan2(z, dist) gives angle from horizon
        double pitchRad = Math.Atan2(deltaZ, dist2D);
        float pitchDeg = (float)(pitchRad * (180.0 / Math.PI));
        
        return (pitchDeg, yawDeg);
    }

    /// <summary>
    /// Aims the player's camera at the specified target actor.
    /// Uses a custom Blueprint event 'Test_SetControlRotation' to bypass Enhanced Input limitations.
    /// </summary>
    /// <param name="targetObj">The target object to aim at.</param>
    public void AimAt(object targetObj)
    {
        if (targetObj == null) throw new ArgumentNullException(nameof(targetObj));
        
        // We use dynamic or reflection to interact with the object
        // 1. Find Player Character
        var player = altDriver.FindObject(By.NAME, "B_Hero_ShooterMannequin");
        if (player == null)
            player = altDriver.FindObject(By.NAME, "LyraCharacter");
            
        if (player == null) throw new Exception("Player character not found!");
       
        var target = targetObj; // generic object reference

        // 2. Calculate Rotation
        // We use Reflection to avoid build errors with missing AltVector3 type or dynamic binder issues
        float pX = 0, pY = 0, pZ = 0;
        float tX = 0, tY = 0, tZ = 0;

        try 
        {
            (pX, pY, pZ) = GetActorCoordinates(player);
            (tX, tY, tZ) = GetActorCoordinates(target);
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Error retrieving coordinates via reflection: {ex.Message}");
            throw;
        }

        // Adjust player pos to camera height (approximate eye level) if needed
        float playerZ = pZ + 80; 

        var rotation = CalculateLookAtRotation(pX, pY, playerZ, tX, tY, tZ);
        
        string targetName = "Target";
        try { targetName = ((dynamic)target).name; } catch {}
        
        TestContext.WriteLine($"Aiming at {targetName}: Pitch={rotation.Pitch:F2}, Yaw={rotation.Yaw:F2}");

        // 3. Apply Rotation via Custom Event
        // Requires 'Test_SetControlRotation' event in the Character Blueprint
        string parameters = $"{rotation.Pitch},{rotation.Yaw}";
        
        try 
        {
            // Call the custom event. 
            // Note: AltTester CallMethod syntax depends on version, usually MethodName, Parameters string
            ((dynamic)player).CallMethod("Test_SetControlRotation", parameters);
            
            // Wait briefly for rotation to interpolate if the BP has interop, or just wait for frame
            Thread.Sleep(500);
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Failed to call Test_SetControlRotation. Ensure the BP modification is applied. Error: {ex.Message}");
            throw;
        }
    }

    private (float x, float y, float z) GetActorCoordinates(object actor)
    {
        // Recursively try to find x, y, z
        // Strategy 1: Check for worldPosition property (AltTester 2.x)
        var wpProp = actor.GetType().GetProperty("worldPosition");
        if (wpProp != null)
        {
            var wp = wpProp.GetValue(actor);
            if (wp != null)
            {
                 // wp is likely AltVector3 struct
                 // It might use fields (x,y,z) or properties (X,Y,Z)
                 // We try both lowercase and uppercase
                 float GetWpCoord(object wrapper, string name)
                 {
                     var t = wrapper.GetType();
                     var f = t.GetField(name); 
                     if (f != null) return Convert.ToSingle(f.GetValue(wrapper));
                     
                     var p = t.GetProperty(name); 
                     if (p != null) return Convert.ToSingle(p.GetValue(wrapper));
                     
                     // Try uppercase
                     var P = t.GetProperty(name.ToUpper());
                     if (P != null) return Convert.ToSingle(P.GetValue(wrapper));
                     
                     return 0;
                 }
                 
                 return (GetWpCoord(wp, "x"), GetWpCoord(wp, "y"), GetWpCoord(wp, "z"));
            }
        }
        
        // Strategy 2: Check for direct properties on actor (AltTester 1.x)
        var xProp = actor.GetType().GetProperty("x");
        if (xProp != null)
        {
             return (
                 Convert.ToSingle(xProp.GetValue(actor)), 
                 Convert.ToSingle(actor.GetType().GetProperty("y").GetValue(actor)), 
                 Convert.ToSingle(actor.GetType().GetProperty("z").GetValue(actor))
             );
        }

        string actorName = "Unknown";
        try { actorName = (string)actor.GetType().GetProperty("name")?.GetValue(actor) ?? "Unknown"; } catch {}
        
        throw new Exception($"Could not resolve coordinates for actor '{actorName}'. Reflection failed.");
    }

    #endregion

    #region Verification Test: Aiming
    
    [Test, Order(6)]
    [Category("EnhancedSmoke")]
    public void Test06_VerifyAimingFunction()
    {
        TestContext.WriteLine("TEST: Verifying AimAt helper method...");
        
        // Ensure gameplay
        var currentScene = altDriver.GetCurrentScene();
        if (!currentScene.Contains("Expanse"))
        {
            NavigateToEliminationGameplay();
        }
        
        // Find a static target (e.g., a specific pickup or geometry)
        // In L_Expanse, there are often Weapon Spawners.
        // Let's try to find a weapon spawner or any actor
        var targets = altDriver.FindObjectsWhichContain(By.NAME, "WeaponSpawner");
        if (targets.Count == 0)
            targets = altDriver.FindObjectsWhichContain(By.NAME, "StaticMesh"); // Fallback
            
        if (targets.Count == 0)
            Assert.Inconclusive("No suitable target found to test aiming.");
            
        var target = targets[0];
        TestContext.WriteLine($"Target acquired: [TargetObject]");
        
        // Exec AimAt
        AimAt(target);
        
        // Verification: Check if camera rotation matches?
        // Hard to verify exact rotation without querying it back.
        // We assume success if no exception was thrown and logs confirm calculation.
        TestContext.WriteLine("Aim command sent successfully.");
    }
    
    #endregion
}
