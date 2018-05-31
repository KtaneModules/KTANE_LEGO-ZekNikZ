using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using LEGO;
using KMBombInfoExtensions;
using Newtonsoft.Json;

public class LEGOModule : MonoBehaviour {
    // KTANE Hooks
    public KMBombInfo BombInfo;
    public KMBombModule BombModule;
    public KMAudio Audio;
    public KMModSettings ModSettings;

    // Module Hooks
    public KMSelectable[] GridButtons;
    public KMSelectable[] ColorButtons;
    public KMSelectable LeftButton;
    public KMSelectable RightButton;
    public KMSelectable SubmitButton;
    public TextMesh PageDisplayText;
    public Material[] Colors;
    private string[] ColorSymbols = new string[] { ".", "R", "G", "B", "C", "M", "Y", "O", "P", "A", "K" };

    // Generated Puzzle and Solution
    private Structure SolutionStructure;
    private List<int[]> SolutionPieceDisplays;
    private List<int[]> SolutionManualPages;
    private List<int[]> SolutionManualPagesBottomOnly;
    private Direction[] PageRotations;
    private int[] SolutionDisplay;
    private Direction SolutionRotation;
    private int SolutionFace;

    // Screen Page Tracker
    private int CurrentPage;
    private int MaxPages;
    private bool ManualPageDisplayTop;
    private float PageDisplayTimer;
    private int[] Submission;
    private int CurrentColor;

    // Controller Field to Enable/Disable Rotations of Pages
    private bool PAGE_ROTATIONS_ENABLED = false;
    private bool SOLUTION_TRANSFORMATIONS_ENABLED = true;

    // Logging Fields
    private static int ModuleIDCounter = 1;
    private int ModuleID;

    class Settings {
        public bool pageRotationsEnabled;
        public bool solutionTransformationsEnabled;
    }
    Settings modSettings;

    // Setup Methods

    private void Start() {
        modSettings = JsonConvert.DeserializeObject<Settings>(ModSettings.Settings);

        PAGE_ROTATIONS_ENABLED = modSettings.pageRotationsEnabled;
        SOLUTION_TRANSFORMATIONS_ENABLED = modSettings.solutionTransformationsEnabled;

        // Initialize Independent Variables
        Submission = new int[GridButtons.Length];
        PageDisplayTimer = Time.time;
        CurrentPage = 0;
        CurrentColor = 1;
        ManualPageDisplayTop = true;
        ModuleID = ModuleIDCounter++;

        // Setup Buttons
        for (int i = 0; i < GridButtons.Length; i++) {
            int j = i;
            GridButtons[i].OnInteract += delegate { HandleGridPress(j); return false; };
        }
        for (int i = 0; i < ColorButtons.Length; i++) {
            int j = i;
            ColorButtons[i].OnInteract += delegate { HandleColorPress(j); return false; };
        }
        LeftButton.OnInteract += delegate { HandlePageTurnPress(0); return false; };
        RightButton.OnInteract += delegate { HandlePageTurnPress(1); return false; };
        SubmitButton.OnInteract += delegate { HandleSubmit(); return false; };

        // Puzzle and Solution Generator
        // StructureGenerator gen = new StructureGenerator(10, new int[] { 8, 8, 8 }); // [OLD VERSION]
        StructureGenerator gen = new StructureGenerator(6, new int[] { 8, 8, 8 });
        SolutionStructure = gen.Generate();
        SolutionPieceDisplays = gen.GetPieceDisplays(true);
        for (int i = 0; i < SolutionStructure.Pieces.Count; i++) { 
            Brick piece = SolutionStructure.Pieces[i];
            Debug.LogFormat("[LEGO #{0}] Piece #{1}: Position: ({2}, {3}, {4}), Dimensions: {5}x{6}, Rotation: {7}, Color: {8}", ModuleID, i, piece.Position[0], piece.Position[1], piece.Position[2], piece.Dimensions[0], piece.Dimensions[1], piece.Facing, Colors[piece.BrickColor + 1].name);
        }

        Debug.LogFormat("[LEGO #{0}] Instruction Rotations are {1}.", ModuleID, PAGE_ROTATIONS_ENABLED ? "enabled" : "disabled");
        Debug.LogFormat("[LEGO #{0}] Solution Rotations are {1}.", ModuleID, SOLUTION_TRANSFORMATIONS_ENABLED ? "enabled" : "disabled");

        // Setup Instruction Pages
        if (PAGE_ROTATIONS_ENABLED) {
            PageRotations = new Direction[] {
                BombInfo.IsIndicatorOn(Indicator.NSA) ? Direction.NORTH : Direction.SOUTH,
                BombInfo.GetSerialNumberLetters().Any(x => "AEIOU".Contains(x)) ? Direction.EAST : Direction.WEST,
                BombInfo.GetPortCount(Port.RJ45) > 0 ? Direction.WEST : Direction.EAST,
                BombInfo.GetPortPlates().Any(x => x.Length == 0) ? Direction.SOUTH : Direction.NORTH,
                BombInfo.GetPortCount(Port.Parallel) == 0 ? Direction.SOUTH : Direction.NORTH,
                BombInfo.GetBatteryCount(Battery.D) > 3 ? Direction.WEST : Direction.EAST,
                BombInfo.IsIndicatorOff(Indicator.BOB) ? Direction.NORTH : Direction.SOUTH,
                BombInfo.IsIndicatorOn(Indicator.CAR) ? Direction.EAST : Direction.WEST,
                BombInfo.GetBatteryHolderCount() < 2 ? Direction.NORTH : Direction.SOUTH,
                BombInfo.GetSerialNumberNumbers().Count() == 3 ? Direction.WEST : Direction.EAST
            };
            Debug.LogFormat("[LEGO #{0}] Instruction Rotations (indexed by: page # mod 10): {1}", ModuleID, string.Join(", ", PageRotations.Select(x=>x.ToString()).ToArray()));

            SolutionManualPages = gen.GetManualPages(true).AsEnumerable().Select((x, i) => x.Rotate((int)PageRotations[i % 10], 8, 8)).ToList();
            SolutionManualPagesBottomOnly = gen.GetManualPages(false).AsEnumerable().Select((x, i) => x.Rotate((int)PageRotations[i % 10], 8, 8)).ToList();
        } else {
            SolutionManualPages = gen.GetManualPages(true);
            SolutionManualPagesBottomOnly = gen.GetManualPages(false);
        }
        for (int i = 0; i < SolutionManualPages.Count; i++) {
            Debug.LogFormat("[LEGO #{0}] Manual Page {1} w/ top:\n{2}", ModuleID, i + 1, string.Join("\n", string.Join("", SolutionManualPages[i].Select(x => ColorSymbols[x]).ToArray()).SplitInGroups(8).Reverse().ToArray()));
            Debug.LogFormat("[LEGO #{0}] Manual Page {1} w/o top:\n{2}", ModuleID, i + 1, string.Join("\n", string.Join("", SolutionManualPagesBottomOnly[i].Select(x => ColorSymbols[x]).ToArray()).SplitInGroups(8).Reverse().ToArray()));
        }

        // Setup Solution
        Brick yellowPiece = SolutionStructure.Pieces.Find(x => x.BrickColor == 5);
        /* [OLD VERSION]
        SolutionFace = SolutionStructure.Pieces.Count(x => x.Dimensions[0] * x.Dimensions[1] == 6) >= 5 ? 1 :
                       yellowPiece.Dimensions[0] * yellowPiece.Dimensions[1] == 3 ? 0 :
                       SolutionManualPages.Count >= 10 ? 1 : 0;
        */
        SolutionFace = SolutionStructure.Pieces.Count(x => x.Dimensions[0] * x.Dimensions[1] == 6) >= 3 ? 1 :
                       yellowPiece.Dimensions[0] * yellowPiece.Dimensions[1] == 3 ? 0 :
                       SolutionManualPages.Count >= 7 ? 1 : 0;
        Debug.LogFormat("[LEGO #{0}] Solution Face: {1}", ModuleID, SolutionFace == 0 ? "BOTTOM" : "TOP");
        if (SOLUTION_TRANSFORMATIONS_ENABLED) {
            /* [OLD VERSION]
            SolutionRotation = SolutionStructure.Pieces.Find(x => x.BrickColor == 1).Dimensions.SequenceEqual(SolutionStructure.Pieces.Find(x => x.BrickColor == 4).Dimensions) ? Direction.WEST :
                               SolutionStructure.Pieces.Max(x => x.Position[2]) > 2 ? Direction.NORTH :
                               SolutionStructure.Pieces.Find(x => x.BrickColor == 6).Position[2] > SolutionStructure.Pieces.Find(x => x.BrickColor == 0).Position[2] ? Direction.EAST : Direction.SOUTH;
            */
            SolutionRotation = SolutionStructure.Pieces.Find(x => x.BrickColor == 1).Dimensions.SequenceEqual(SolutionStructure.Pieces.Find(x => x.BrickColor == 4).Dimensions) ? Direction.WEST :
                   SolutionStructure.Pieces.Max(x => x.Position[2]) > 2 ? Direction.NORTH :
                   SolutionStructure.Pieces.Find(x => x.BrickColor == 2).Position[2] > SolutionStructure.Pieces.Find(x => x.BrickColor == 0).Position[2] ? Direction.EAST : Direction.SOUTH;
            Debug.LogFormat("[LEGO #{0}] Solution Rotation: {1}", ModuleID, SolutionRotation);
        } else {
            SolutionRotation = Direction.NORTH;
        }
        if (SolutionFace == 0) {
            if (SolutionRotation == Direction.EAST) {
                SolutionRotation = Direction.WEST;
            } else if (SolutionRotation == Direction.WEST) {
                SolutionRotation = Direction.EAST;
            }
        }
        SolutionDisplay = gen.GetSolutionDisplay(SolutionFace).Rotate((int)SolutionRotation, 8, 8);
        Debug.LogFormat("[LEGO #{0}] Solution:\n{1}", ModuleID, string.Join("\n", string.Join("", SolutionDisplay.Select(x=>ColorSymbols[x]).ToArray()).SplitInGroups(8).Reverse().ToArray()));

        // Module Setup
        BombModule.OnActivate += delegate { ActivateModule(); };

        // Post-Setup Initialization
        MaxPages = SolutionManualPages.Count + 2;
    }

    private void ActivateModule() {
        // HandleColorPress(Random.Range(0, 10)); // [OLD VERSION]
        HandleColorPress(Random.Range(0, 6));
    }

    // Interaction Handlers

    private void HandleGridPress(int button) {
        SubmitButton.AddInteractionPunch(0.1f);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, SubmitButton.transform);
        if (CurrentPage == MaxPages - 1) {
            if (Submission[button] == CurrentColor) {
                Submission[button] = 0;
                GridButtons[button].GetComponent<Renderer>().material = Colors[0];
            } else {
                Submission[button] = CurrentColor;
                GridButtons[button].GetComponent<Renderer>().material = Colors[CurrentColor];
            }
        }
    }

    private void HandleColorPress(int button) {
        SubmitButton.AddInteractionPunch(0.2f);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, SubmitButton.transform);
        //Debug.LogFormat("[LEGO #{0}] Current color changed to {1}.", ModuleID, Colors[button + 1].name);
        CurrentColor = button + 1;
        UpdateDisplays();
        foreach (KMSelectable b in ColorButtons) {
            b.gameObject.transform.localPosition = new Vector3(b.gameObject.transform.localPosition.x, 0, b.gameObject.transform.localPosition.z);
        }
        ColorButtons[button].gameObject.transform.localPosition = new Vector3(ColorButtons[button].gameObject.transform.localPosition.x, -0.003f, ColorButtons[button].gameObject.transform.localPosition.z);
    }

    private void HandlePageTurnPress(int direction) {
        SubmitButton.AddInteractionPunch(0.5f);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, SubmitButton.transform);
        if (direction == 0) {
            if (CurrentPage > 0) CurrentPage--;
        } else {
            if (CurrentPage < MaxPages - 1) CurrentPage++;
        }
        PageDisplayTimer = Time.time;
        ManualPageDisplayTop = true;
        UpdateDisplays();
    }

    private void HandleSubmit() {
        SubmitButton.AddInteractionPunch();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, SubmitButton.transform);
        int[] grid = ShiftGrid(Submission);
        int[] solution = ShiftGrid(SolutionDisplay);
        Debug.LogFormat("[LEGO #{0}] Submitting:\n{1}", ModuleID, string.Join("\n", string.Join("", Submission.Select(x => ColorSymbols[x]).ToArray()).SplitInGroups(8).Reverse().ToArray()));
        if (solution.SequenceEqual(grid)) {
            BombModule.HandlePass();
            Debug.LogFormat("[LEGO #{0}] Correct Submission! Module Defused.", ModuleID);
        } else {
            BombModule.HandleStrike();
            Debug.LogFormat("[LEGO #{0}] Wrong Submission! Strike!", ModuleID);
        }
    }

    // Update Methods

    private void UpdateDisplays() {
        if (CurrentPage == 0) {
            // PARTS Page
            PageDisplayText.text = "PARTS";
            for (int i = 0; i < GridButtons.Length; i++) {
                GridButtons[i].GetComponent<Renderer>().material = Colors[SolutionPieceDisplays[CurrentColor - 1][i]];
            }
        } else if (CurrentPage < MaxPages - 1) {
            // INSTRUCTION Pages
            PageDisplayText.text = "PAGE " + (CurrentPage);
            if (ManualPageDisplayTop) {
                for (int i = 0; i < GridButtons.Length; i++) {
                    GridButtons[i].GetComponent<Renderer>().material = Colors[SolutionManualPages[CurrentPage - 1][i]];
                }
            } else {
                for (int i = 0; i < GridButtons.Length; i++) {
                    GridButtons[i].GetComponent<Renderer>().material = Colors[SolutionManualPagesBottomOnly[CurrentPage - 1][i]];
                }
            }
        } else {
            // BUILD Page
            PageDisplayText.text = "BUILD";
            for (int i = 0; i < GridButtons.Length; i++) {
                GridButtons[i].GetComponent<Renderer>().material = Colors[Submission[i]];
            }
        }
    }

    private void Update() {
        // Automatically flash the top brick on INSTRUCTION pages.
        if (CurrentPage > 0 && CurrentPage < MaxPages - 1 && Time.time - PageDisplayTimer > 0.5f) {
            PageDisplayTimer = Time.time;
            ManualPageDisplayTop = !ManualPageDisplayTop;
            UpdateDisplays();
        }
    }

    // Helper Methods

    private int[] ShiftGrid(int[] data) {
        int[] grid = new int[data.Length];
        int minX = 99;
        int minY = 99;
        int maxX = 0;
        int maxY = 0;
        for (int x = 0; x < 8; x++) {
            for (int y = 0; y < 8; y++) {
                if (data[x + y * 8] != 0) {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        }
        int width = maxX - minX + 1;
        int height = maxY - minY + 1;
        int shiftX = (8 - width) / 2 - minX;
        int shiftY = (8 - height) / 2 - minY;
        for (int x = minX; x <= maxX; x++) {
            for (int y = minY; y <= maxY; y++) {
                grid[x + shiftX + (y + shiftY) * 8] = data[x + y * 8];
            }
        }
        return grid;
    }
}