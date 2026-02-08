using AltTester.AltTesterSDK.Driver;
using AltTester.AltTesterSDK.Driver.Commands;
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
}
