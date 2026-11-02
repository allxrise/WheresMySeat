using System;
using System.Collections.Generic;
using UnityEngine;

namespace WheresMySeat
{
    [Serializable]
    public class RuntimeSeatSpec
    {
        public string id;
        public int index;
        public Vector2 position;
        public List<SimpleTraitType> traits = new List<SimpleTraitType>();
        public bool hasLamp;
        public bool lampInitialOn;
        public bool isBlockedByDance;
    }

    [Serializable]
    public class RuntimePersonSpec
    {
        public string name;
        public Color color;
        public RoleType role = RoleType.Adult;
        public int targetSeatIndex;
        public int blockedTurns;
        public List<string> personTraits = new List<string>();
        public List<PersonRequirement> requirements = new List<PersonRequirement>();
    }

    public class RuntimeLevelDefinition
    {
        public int levelNumber;
        public string title;
        public string subtitle;
        public MapID chapterMap;
        public LevelDifficulty difficulty;
        public GameMode gameMode = GameMode.Moves;
        public int movesOrSeconds = 8;
        public float cameraOrthoSize = 6.2f;
        public Color environmentBgColor = new Color(0.11f, 0.14f, 0.22f, 1f);
        public Color cabinFloorColor = new Color(0.18f, 0.23f, 0.34f, 1f);
        public Color seatThemeColor = new Color(0.28f, 0.38f, 0.56f, 1f);

        public float distanceBetweenSeatToCheck = 1.65f;
        public float horizontalRange = 1.65f;
        public float verticalRange = 1.55f;
        public float sameRowXTolerance = 8.0f;
        public float sameColYTolerance = 0.45f;

        public List<RuntimeSeatSpec> seats = new List<RuntimeSeatSpec>();
        public List<RuntimePersonSpec> persons = new List<RuntimePersonSpec>();
    }

    public static class LevelDatabase
    {
        private static Dictionary<int, ExtractedLevelInfo> _extractedByNumber;
        private static int _totalExtractedCount = 259;

        private static readonly Color[] BlobPalette = new Color[]
        {
            new Color(1.00f, 0.42f, 0.52f, 1f), // Coral Pink (Glia)
            new Color(0.30f, 0.76f, 1.00f, 1f), // Sky Blue (Soli)
            new Color(1.00f, 0.78f, 0.22f, 1f), // Sunny Yellow (Milo)
            new Color(0.45f, 0.88f, 0.48f, 1f), // Mint Green (Pipo)
            new Color(0.72f, 0.48f, 0.98f, 1f), // Lavender Purple (Noir)
            new Color(1.00f, 0.56f, 0.24f, 1f), // Tangerine (Rubi)
            new Color(0.25f, 0.88f, 0.82f, 1f), // Aqua Teal (Ciel)
            new Color(0.96f, 0.46f, 0.82f, 1f), // Bubblegum (Lulu)
            new Color(0.62f, 0.84f, 0.28f, 1f), // Lime (Zane)
            new Color(0.95f, 0.34f, 0.36f, 1f), // Crimson (Boba)
        };

        private static readonly string[] DefaultBlobNames = new string[]
        {
            "Glia", "Soli", "Milo", "Pipo", "Noir", "Rubi", "Ciel", "Lulu", "Zane", "Boba", "Kiko", "Toto"
        };

        public static int TotalLevels
        {
            get
            {
                EnsureLoaded();
                return Mathf.Max(259, _totalExtractedCount);
            }
        }

        public static void EnsureLoaded()
        {
            if (_extractedByNumber != null) return;
            _extractedByNumber = new Dictionary<int, ExtractedLevelInfo>();

            TextAsset asset = Resources.Load<TextAsset>("JsonData/extracted_levels_info");
            if (asset != null && !string.IsNullOrEmpty(asset.text))
            {
                try
                {
                    var db = JsonUtility.FromJson<ExtractedLevelDatabase>(asset.text);
                    if (db != null && db.levels != null)
                    {
                        foreach (var lvl in db.levels)
                        {
                            if (lvl != null && lvl.levelNumber > 0 && !_extractedByNumber.ContainsKey(lvl.levelNumber))
                            {
                                _extractedByNumber[lvl.levelNumber] = lvl;
                            }
                        }
                        _totalExtractedCount = Mathf.Max(259, _extractedByNumber.Count);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to parse extracted_levels_info.json: {e.Message}");
                }
            }
        }

        public static MapID GetChapterForLevel(int levelNumber)
        {
            if (levelNumber <= 15) return MapID.USA;
            if (levelNumber <= 40) return MapID.England;
            if (levelNumber <= 70) return MapID.France;
            if (levelNumber <= 100) return MapID.Brazil;
            if (levelNumber <= 130) return MapID.India;
            if (levelNumber <= 160) return MapID.Japan;
            if (levelNumber <= 190) return MapID.Italy;
            if (levelNumber <= 220) return MapID.Germany;
            return MapID.Australia;
        }

        public static string GetChapterDisplayName(MapID map)
        {
            switch (map)
            {
                case MapID.USA: return "USA · Downtown Express";
                case MapID.England: return "England · London Double-Decker";
                case MapID.France: return "France · Paris Café & Metro";
                case MapID.Brazil: return "Brazil · Rio Carnival Coach";
                case MapID.India: return "India · Maharaja Rail";
                case MapID.Japan: return "Japan · Shinkansen Bullet";
                case MapID.Italy: return "Italy · Venice Vaporetto";
                case MapID.Germany: return "Germany · Bavarian Express";
                case MapID.Australia: return "Australia · Sydney Ferry";
                default: return "World Tour";
            }
        }

        public static void GetChapterThemeColors(MapID map, out Color envBg, out Color cabinFloor, out Color seatColor)
        {
            switch (map)
            {
                case MapID.USA:
                    envBg = new Color(0.10f, 0.14f, 0.23f, 1f);
                    cabinFloor = new Color(0.17f, 0.22f, 0.34f, 1f);
                    seatColor = new Color(0.26f, 0.42f, 0.64f, 1f);
                    break;
                case MapID.England:
                    envBg = new Color(0.15f, 0.11f, 0.18f, 1f);
                    cabinFloor = new Color(0.24f, 0.17f, 0.26f, 1f);
                    seatColor = new Color(0.62f, 0.22f, 0.28f, 1f);
                    break;
                case MapID.France:
                    envBg = new Color(0.11f, 0.16f, 0.22f, 1f);
                    cabinFloor = new Color(0.19f, 0.25f, 0.33f, 1f);
                    seatColor = new Color(0.32f, 0.48f, 0.58f, 1f);
                    break;
                case MapID.Brazil:
                    envBg = new Color(0.09f, 0.18f, 0.16f, 1f);
                    cabinFloor = new Color(0.15f, 0.27f, 0.24f, 1f);
                    seatColor = new Color(0.22f, 0.56f, 0.42f, 1f);
                    break;
                case MapID.Japan:
                    envBg = new Color(0.14f, 0.12f, 0.22f, 1f);
                    cabinFloor = new Color(0.21f, 0.19f, 0.32f, 1f);
                    seatColor = new Color(0.45f, 0.35f, 0.65f, 1f);
                    break;
                default:
                    envBg = new Color(0.11f, 0.15f, 0.22f, 1f);
                    cabinFloor = new Color(0.18f, 0.24f, 0.35f, 1f);
                    seatColor = new Color(0.30f, 0.44f, 0.62f, 1f);
                    break;
            }
        }

        public static RuntimeLevelDefinition GetLevel(int levelNumber)
        {
            EnsureLoaded();
            levelNumber = Mathf.Clamp(levelNumber, 1, TotalLevels);

            _extractedByNumber.TryGetValue(levelNumber, out ExtractedLevelInfo extracted);

            // Check hand-crafted signature levels first (1..10) for bespoke tutorial & story progression
            if (levelNumber <= 10)
            {
                return BuildHandcraftedSignatureLevel(levelNumber, extracted);
            }

            return BuildSynthesizedLevelFromExtracted(levelNumber, extracted);
        }

        private static RuntimeLevelDefinition BuildHandcraftedSignatureLevel(int levelNumber, ExtractedLevelInfo extracted)
        {
            MapID map = GetChapterForLevel(levelNumber);
            GetChapterThemeColors(map, out Color envBg, out Color cabinFloor, out Color seatColor);

            var def = new RuntimeLevelDefinition
            {
                levelNumber = levelNumber,
                chapterMap = map,
                difficulty = extracted != null ? (LevelDifficulty)Mathf.Clamp(extracted.difficulty, 0, 2) : LevelDifficulty.Easy,
                gameMode = GameMode.Moves,
                movesOrSeconds = extracted != null && extracted.valueGameMode > 0 ? Mathf.Max(extracted.valueGameMode, 5) : 6,
                cameraOrthoSize = 6.2f,
                environmentBgColor = envBg,
                cabinFloorColor = cabinFloor,
                seatThemeColor = seatColor,
                distanceBetweenSeatToCheck = 1.75f,
                horizontalRange = 1.75f,
                verticalRange = 1.65f,
                sameRowXTolerance = 8.0f,
                sameColYTolerance = 0.45f
            };

            switch (levelNumber)
            {
                case 1:
                {
                    def.title = "Level 1 · First Bus Ride";
                    def.subtitle = "Tap or drag each Blob to check their seat clue!";
                    def.movesOrSeconds = 4;
                    // 2 seats side by side in Row 1 (Window & Aisle)
                    AddGridSeats(def, rows: 1, cols: 2, xSpacing: 1.55f, ySpacing: 1.45f, centerY: 1.15f);
                    def.persons.Add(MakePerson("Glia", 0, targetSeat: 0,
                        SimpleSitRequirement.MustHave(SimpleTraitType.Window, "I want the Window seat!")));
                    def.persons.Add(MakePerson("Soli", 1, targetSeat: 1,
                        SimpleSitRequirement.MustHave(SimpleTraitType.Aisle, "I want the Aisle seat next to Glia!"),
                        new SitWithSomeOne(SeatDirectionCheck.Right, "Glia", "I want to sit to the right of Glia!")));
                    break;
                }
                case 2:
                {
                    def.title = "Level 2 · Front vs Back Row";
                    def.subtitle = "Pay attention to Row numbers and Front/Back traits!";
                    def.movesOrSeconds = 5;
                    AddGridSeats(def, rows: 2, cols: 2, xSpacing: 1.55f, ySpacing: 1.45f, centerY: 1.15f);
                    def.persons.Add(MakePerson("Glia", 0, targetSeat: 0,
                        SimpleSitRequirement.MustHave(SimpleTraitType.Row_1, "I want to sit in Row 1 (Front)!"),
                        SimpleSitRequirement.MustHave(SimpleTraitType.Window, "I want a Window seat!")));
                    def.persons.Add(MakePerson("Milo", 2, targetSeat: 1,
                        new SitWithSomeOne(SeatDirectionCheck.NextTo_Horizontal, "Glia", "I want to sit right next to Glia!")));
                    def.persons.Add(MakePerson("Noir", 4, targetSeat: 2,
                        SimpleSitRequirement.MustHave(SimpleTraitType.Row_2, "I want to be in Row 2 (Back)!"),
                        new SitWithSomeOne(SeatDirectionCheck.Behind, "Glia", "I want to sit directly behind Glia!")));
                    break;
                }
                case 3:
                {
                    def.title = "Level 3 · Best Friends & Rivals";
                    def.subtitle = "Some Blobs love company, others want space!";
                    def.movesOrSeconds = 6;
                    AddGridSeats(def, rows: 2, cols: 2, xSpacing: 1.55f, ySpacing: 1.45f, centerY: 1.15f);
                    def.persons.Add(MakePerson("Rubi", 5, targetSeat: 0,
                        SimpleSitRequirement.MustHave(SimpleTraitType.Front, "I need a Front row seat!"),
                        SimpleSitRequirement.MustHave(SimpleTraitType.Window, "Give me the Window seat!")));
                    def.persons.Add(MakePerson("Pipo", 3, targetSeat: 1,
                        new SitWithSomeOne(SeatDirectionCheck.Right, "Rubi", "I sit to the right of Rubi!")));
                    def.persons.Add(MakePerson("Ciel", 6, targetSeat: 3,
                        SimpleSitRequirement.MustHave(SimpleTraitType.Back, "I prefer the Back row!"),
                        new SitAvoidChar(SeatDirectionCheck.Behind, "Rubi", "I don't want to sit behind Rubi!"),
                        new SitWithSomeOne(SeatDirectionCheck.Behind, "Pipo", "I want to sit behind Pipo!")));
                    break;
                }
                case 4:
                {
                    def.title = "Level 4 · Watch Out for the Spill!";
                    def.subtitle = "Nobody wants to sit on a Dirty seat!";
                    def.movesOrSeconds = 6;
                    AddGridSeats(def, rows: 2, cols: 2, xSpacing: 1.55f, ySpacing: 1.45f, centerY: 1.15f);
                    def.seats[1].traits.Add(SimpleTraitType.Dirty);

                    def.persons.Add(MakePerson("Glia", 0, targetSeat: 0,
                        SimpleSitRequirement.MustHave(SimpleTraitType.Row_1, "I want Row 1!"),
                        new SitAvoidPositionTrait(SimpleTraitType.Dirty, "Eww! Keep me off the Dirty seat!")));
                    def.persons.Add(MakePerson("Soli", 1, targetSeat: 2,
                        new SitWithSomeOne(SeatDirectionCheck.Behind, "Glia", "I sit directly behind Glia!"),
                        new SitAvoidPositionTrait(SimpleTraitType.Dirty, "No Dirty seats for me!")));
                    def.persons.Add(MakePerson("Zane", 8, targetSeat: 3,
                        new SitWithSomeOne(SeatDirectionCheck.Right, "Soli", "I sit to the right of Soli!"),
                        new SitAvoidPositionTrait(SimpleTraitType.Dirty, "I refuse to sit on a Dirty seat!")));
                    break;
                }
                case 5:
                {
                    def.title = "Level 5 · Subway Car Rush";
                    def.subtitle = "6 seats across 3 rows — deduce the chain!";
                    def.movesOrSeconds = 7;
                    AddGridSeats(def, rows: 3, cols: 2, xSpacing: 1.55f, ySpacing: 1.38f, centerY: 1.25f);
                    def.persons.Add(MakePerson("Glia", 0, targetSeat: 0,
                        SimpleSitRequirement.MustHave(SimpleTraitType.Row_1, "I want Row 1!"),
                        SimpleSitRequirement.MustHave(SimpleTraitType.Window, "I want the Window seat!")));
                    def.persons.Add(MakePerson("Milo", 2, targetSeat: 1,
                        new SitWithSomeOne(SeatDirectionCheck.SameRow, "Glia", "I want to share a row with Glia!")));
                    def.persons.Add(MakePerson("Noir", 4, targetSeat: 3,
                        SimpleSitRequirement.MustHave(SimpleTraitType.Row_2, "I want Row 2!"),
                        new SitWithSomeOne(SeatDirectionCheck.Behind, "Milo", "I want to sit right behind Milo!")));
                    def.persons.Add(MakePerson("Lulu", 7, targetSeat: 4,
                        SimpleSitRequirement.MustHave(SimpleTraitType.Row_3, "I want Row 3 (Back)!"),
                        new SitWithSomeOne(SeatDirectionCheck.SameColumn, "Glia", "I want the same column as Glia!")));
                    break;
                }
                case 6:
                {
                    def.title = "Level 6 · Reading Lights in the Park";
                    def.subtitle = "Tap the 💡 Lamp above a seat to switch it ON or OFF!";
                    def.movesOrSeconds = 6;
                    AddGridSeats(def, rows: 2, cols: 2, xSpacing: 1.55f, ySpacing: 1.45f, centerY: 1.15f);
                    def.seats[0].hasLamp = true;
                    def.seats[0].lampInitialOn = false; // Player can tap to turn ON or seat according to rule
                    def.seats[1].hasLamp = true;
                    def.seats[1].lampInitialOn = true;

                    def.persons.Add(MakePerson("Glia", 0, targetSeat: 1,
                        SimpleSitRequirement.MustHave(SimpleTraitType.Row_1, "I want Row 1!"),
                        new SitWithOnOffItem(true, "I want to read! My seat lamp must be 💡ON!")));
                    def.persons.Add(MakePerson("Noir", 4, targetSeat: 0,
                        SimpleSitRequirement.MustHave(SimpleTraitType.Row_1, "I want Row 1!"),
                        new SitWithOnOffItem(false, "I want to nap! My seat lamp must be 💡OFF!")));
                    def.persons.Add(MakePerson("Soli", 1, targetSeat: 3,
                        new SitWithSomeOne(SeatDirectionCheck.Behind, "Glia", "I want to sit directly behind Glia!")));
                    break;
                }
                case 7:
                {
                    def.title = "Level 7 · Peace & Quiet";
                    def.subtitle = "Noir wants to sit completely alone with no neighbors!";
                    def.movesOrSeconds = 7;
                    AddGridSeats(def, rows: 3, cols: 2, xSpacing: 1.55f, ySpacing: 1.38f, centerY: 1.25f);
                    def.persons.Add(MakePerson("Noir", 4, targetSeat: 5,
                        SimpleSitRequirement.MustHave(SimpleTraitType.Row_3, "I want Row 3 (Back)!"),
                        new IWantSitAlone(SeatDirectionCheck.HorizontalAndVertical, "I want to sit completely alone (no adjacent neighbors)!")));
                    def.persons.Add(MakePerson("Glia", 0, targetSeat: 0,
                        SimpleSitRequirement.MustHave(SimpleTraitType.Row_1, "I want Row 1!"),
                        SimpleSitRequirement.MustHave(SimpleTraitType.Window, "I love the Window seat!")));
                    def.persons.Add(MakePerson("Soli", 1, targetSeat: 1,
                        new SitWithSomeOne(SeatDirectionCheck.Right, "Glia", "I sit to the right of Glia!")));
                    def.persons.Add(MakePerson("Pipo", 3, targetSeat: 2,
                        new SitWithSomeOne(SeatDirectionCheck.Behind, "Glia", "I sit directly behind Glia!"),
                        new SitAvoidChar(SeatDirectionCheck.HorizontalAndVertical, "Noir", "Don't put me next to Noir!")));
                    break;
                }
                case 8:
                {
                    def.title = "Level 8 · Kids & Chaperones";
                    def.subtitle = "Kids need an Adult nearby!";
                    def.movesOrSeconds = 7;
                    AddGridSeats(def, rows: 2, cols: 3, xSpacing: 1.45f, ySpacing: 1.45f, centerY: 1.15f);
                    var kid = MakePerson("Pipo", 3, targetSeat: 1,
                        SimpleSitRequirement.MustHave(SimpleTraitType.Row_1, "I want Row 1!"),
                        new SitWithRole(SeatDirectionCheck.Left, RoleType.Adult, "I'm a Kid! I need an Adult to my left!"),
                        new SitWithRole(SeatDirectionCheck.Right, RoleType.Adult, "I also want an Adult to my right!"));
                    kid.role = RoleType.Kid;

                    def.persons.Add(kid);
                    def.persons.Add(MakePerson("Glia", 0, targetSeat: 0,
                        SimpleSitRequirement.MustHave(SimpleTraitType.Row_1, "I sit in Row 1!"),
                        SimpleSitRequirement.MustHave(SimpleTraitType.Window, "I sit at the left Window!")));
                    def.persons.Add(MakePerson("Milo", 2, targetSeat: 2,
                        new SitWithSomeOne(SeatDirectionCheck.Right, "Pipo", "I sit to the right of Pipo!")));
                    def.persons.Add(MakePerson("Rubi", 5, targetSeat: 4,
                        new SitWithSomeOne(SeatDirectionCheck.Behind, "Pipo", "I sit directly behind Pipo!")));
                    break;
                }
                case 9:
                {
                    def.title = "Level 9 · Diner Booths & Tables";
                    def.subtitle = "Match faces, tables, and opposite-direction seats!";
                    def.movesOrSeconds = 8;
                    AddGridSeats(def, rows: 2, cols: 3, xSpacing: 1.45f, ySpacing: 1.45f, centerY: 1.15f);
                    for (int i = 0; i < 3; i++)
                    {
                        def.seats[i].traits.Add(SimpleTraitType.Opposite_Direction);
                        def.seats[i].traits.Add(SimpleTraitType.Table);
                        def.seats[i + 3].traits.Add(SimpleTraitType.Table);
                    }

                    def.persons.Add(MakePerson("Glia", 0, targetSeat: 0,
                        SimpleSitRequirement.MustHave(SimpleTraitType.Opposite_Direction, "I want a rear-facing booth seat (Row 1)!"),
                        SimpleSitRequirement.MustHave(SimpleTraitType.Window, "I want the left Window booth!")));
                    def.persons.Add(MakePerson("Soli", 1, targetSeat: 3,
                        new SitWithSomeOne(SeatDirectionCheck.Opposite, "Glia", "I want to sit facing Glia in the same column!"),
                        new SitAvoidPositionTrait(SimpleTraitType.Opposite_Direction, "I want a forward-facing seat!")));
                    def.persons.Add(MakePerson("Ciel", 6, targetSeat: 1,
                        new SitWithSomeOne(SeatDirectionCheck.Right, "Glia", "I sit to the right of Glia!")));
                    def.persons.Add(MakePerson("Boba", 9, targetSeat: 5,
                        SimpleSitRequirement.MustHave(SimpleTraitType.Row_2, "I want Row 2!"),
                        new SitAvoidChar(SeatDirectionCheck.Around, "Glia", "I sit far away from Glia!"),
                        new SitAvoidChar(SeatDirectionCheck.Around, "Soli", "I don't sit next to Soli!")));
                    break;
                }
                default:
                {
                    def.title = "Level 10 · VIP Express Challenge";
                    def.subtitle = "Hard Puzzle! Combine every clue carefully.";
                    def.difficulty = LevelDifficulty.Hard;
                    def.movesOrSeconds = 8;
                    AddGridSeats(def, rows: 3, cols: 2, xSpacing: 1.55f, ySpacing: 1.38f, centerY: 1.25f);
                    def.seats[0].traits.Add(SimpleTraitType.Private);
                    def.seats[4].traits.Add(SimpleTraitType.View_River);

                    def.persons.Add(MakePerson("Rubi", 5, targetSeat: 0,
                        SimpleSitRequirement.MustHave(SimpleTraitType.Private, "I booked the VIP seat!"),
                        SimpleSitRequirement.MustHave(SimpleTraitType.Row_1, "I sit in Row 1!")));
                    def.persons.Add(MakePerson("Glia", 0, targetSeat: 1,
                        new SitWithSomeOne(SeatDirectionCheck.Right, "Rubi", "I sit to the right of Rubi!")));
                    def.persons.Add(MakePerson("Milo", 2, targetSeat: 2,
                        new SitWithSomeOne(SeatDirectionCheck.Behind, "Rubi", "I sit directly behind Rubi!")));
                    def.persons.Add(MakePerson("Soli", 1, targetSeat: 3,
                        new SitWithSomeOne(SeatDirectionCheck.Right, "Milo", "I sit to the right of Milo!"),
                        new SitWithSomeOne(SeatDirectionCheck.Behind, "Glia", "I sit directly behind Glia!")));
                    def.persons.Add(MakePerson("Ciel", 6, targetSeat: 4,
                        SimpleSitRequirement.MustHave(SimpleTraitType.View_River, "I want the River View seat in Row 3!"),
                        new SitWithSomeOne(SeatDirectionCheck.Behind, "Milo", "I sit directly behind Milo!")));
                    break;
                }
            }

            return def;
        }

        private static RuntimeLevelDefinition BuildSynthesizedLevelFromExtracted(int levelNumber, ExtractedLevelInfo extracted)
        {
            MapID map = GetChapterForLevel(levelNumber);
            GetChapterThemeColors(map, out Color envBg, out Color cabinFloor, out Color seatColor);

            int personCount = 4;
            if (extracted != null && extracted.hintEntries != null && extracted.hintEntries.Count >= 2)
            {
                personCount = Mathf.Clamp(extracted.hintEntries.Count, 3, 8);
            }
            else
            {
                personCount = Mathf.Clamp(3 + (levelNumber % 5), 3, 7);
            }

            // Determine grid size that comfortably fits all characters plus 1-2 extra seats
            int cols = personCount <= 4 ? 2 : 3;
            int rows = Mathf.Clamp(Mathf.CeilToInt((personCount + 1) / (float)cols), 2, 4);
            int totalSeats = rows * cols;

            LevelDifficulty diff = extracted != null
                ? (LevelDifficulty)Mathf.Clamp(extracted.difficulty, 0, 2)
                : (levelNumber % 10 == 0 ? LevelDifficulty.VeryHard : (levelNumber % 5 == 0 ? LevelDifficulty.Hard : LevelDifficulty.Easy));

            int moves = extracted != null && extracted.valueGameMode > 0
                ? Mathf.Max(extracted.valueGameMode, personCount + 2)
                : personCount + 3;

            var def = new RuntimeLevelDefinition
            {
                levelNumber = levelNumber,
                title = $"Level {levelNumber} · {GetChapterDisplayName(map).Split('·')[0].Trim()}",
                subtitle = diff == LevelDifficulty.VeryHard
                    ? "Very Hard Logic Puzzle · Deduce every passenger's true seat!"
                    : (diff == LevelDifficulty.Hard ? "Hard Logic Puzzle · Check row & neighbor clues!" : "Match every Blob's seat requirements!"),
                chapterMap = map,
                difficulty = diff,
                gameMode = GameMode.Moves,
                movesOrSeconds = moves,
                cameraOrthoSize = rows >= 4 ? 6.8f : 6.2f,
                environmentBgColor = envBg,
                cabinFloorColor = cabinFloor,
                seatThemeColor = seatColor,
                distanceBetweenSeatToCheck = 1.75f,
                horizontalRange = 1.75f,
                verticalRange = 1.65f,
                sameRowXTolerance = 8.0f,
                sameColYTolerance = 0.45f
            };

            float xSpacing = cols == 3 ? 1.42f : 1.58f;
            float ySpacing = rows >= 4 ? 1.22f : 1.38f;
            AddGridSeats(def, rows, cols, xSpacing, ySpacing, centerY: 1.25f);

            // Deterministic RNG seeded by levelNumber
            var rng = new System.Random(levelNumber * 7919 + 31);

            // Pick distinct target seats for each person
            var availableSeatIndices = new List<int>();
            for (int i = 0; i < totalSeats; i++) availableSeatIndices.Add(i);
            for (int i = availableSeatIndices.Count - 1; i > 0; i--)
            {
                int swapIdx = rng.Next(i + 1);
                int tmp = availableSeatIndices[i];
                availableSeatIndices[i] = availableSeatIndices[swapIdx];
                availableSeatIndices[swapIdx] = tmp;
            }

            // Optionally mark one unused seat as Dirty or VIP on some levels
            int unusedSeatIdx = personCount < totalSeats ? availableSeatIndices[personCount] : -1;
            if (unusedSeatIdx >= 0 && levelNumber % 3 == 0)
            {
                def.seats[unusedSeatIdx].traits.Add(SimpleTraitType.Dirty);
            }

            // Create people using extracted names if available
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int pIdx = 0; pIdx < personCount; pIdx++)
            {
                string rawName = null;
                if (extracted != null && extracted.hintEntries != null && pIdx < extracted.hintEntries.Count)
                {
                    rawName = SanitizePersonName(extracted.hintEntries[pIdx].personName);
                }
                if (string.IsNullOrEmpty(rawName) || usedNames.Contains(rawName))
                {
                    rawName = DefaultBlobNames[pIdx % DefaultBlobNames.Length];
                    if (usedNames.Contains(rawName))
                        rawName = $"{rawName}{pIdx + 1}";
                }
                usedNames.Add(rawName);

                int targetSeat = availableSeatIndices[pIdx];
                var personSpec = new RuntimePersonSpec
                {
                    name = rawName,
                    color = BlobPalette[pIdx % BlobPalette.Length],
                    role = (pIdx == personCount - 1 && levelNumber % 4 == 0) ? RoleType.Kid : RoleType.Adult,
                    targetSeatIndex = targetSeat,
                    blockedTurns = 0
                };
                def.persons.Add(personSpec);
            }

            // Synthesize 100% true, deterministic clues for each person based on their target seat & neighbors
            for (int pIdx = 0; pIdx < def.persons.Count; pIdx++)
            {
                var p = def.persons[pIdx];
                var seat = def.seats[p.targetSeatIndex];
                int r = p.targetSeatIndex / cols;
                int c = p.targetSeatIndex % cols;

                // 1) Row or Position trait clue
                SimpleTraitType rowTrait = GetRowTrait(r);
                p.requirements.Add(SimpleSitRequirement.MustHave(rowTrait, $"I want to sit in Row {r + 1}!"));

                // 2) Relative neighbor clue if another person is in a known relation
                bool addedNeighborClue = false;
                for (int otherIdx = 0; otherIdx < def.persons.Count; otherIdx++)
                {
                    if (otherIdx == pIdx) continue;
                    var other = def.persons[otherIdx];
                    int orow = other.targetSeatIndex / cols;
                    int ocol = other.targetSeatIndex % cols;

                    if (orow == r && ocol == c - 1)
                    {
                        p.requirements.Add(new SitWithSomeOne(SeatDirectionCheck.Right, other.name, $"I want to sit to the right of {other.name}!"));
                        addedNeighborClue = true;
                        break;
                    }
                    if (orow == r && ocol == c + 1)
                    {
                        p.requirements.Add(new SitWithSomeOne(SeatDirectionCheck.Left, other.name, $"I want to sit to the left of {other.name}!"));
                        addedNeighborClue = true;
                        break;
                    }
                    if (ocol == c && orow == r - 1)
                    {
                        p.requirements.Add(new SitWithSomeOne(SeatDirectionCheck.Behind, other.name, $"I want to sit directly behind {other.name}!"));
                        addedNeighborClue = true;
                        break;
                    }
                    if (ocol == c && orow == r + 1)
                    {
                        p.requirements.Add(new SitWithSomeOne(SeatDirectionCheck.Front, other.name, $"I want to sit in front of {other.name}!"));
                        addedNeighborClue = true;
                        break;
                    }
                }

                // 3) Window / Aisle clue if not already uniquely pinned
                if (!addedNeighborClue || diff != LevelDifficulty.Easy)
                {
                    if (seat.traits.Contains(SimpleTraitType.Window))
                    {
                        p.requirements.Add(SimpleSitRequirement.MustHave(SimpleTraitType.Window, "I want a Window seat!"));
                    }
                    else if (seat.traits.Contains(SimpleTraitType.Aisle))
                    {
                        p.requirements.Add(SimpleSitRequirement.MustHave(SimpleTraitType.Aisle, "I want a middle Aisle seat!"));
                    }
                }

                // 4) Avoid Dirty seat if level has a dirty seat
                if (unusedSeatIdx >= 0 && def.seats[unusedSeatIdx].traits.Contains(SimpleTraitType.Dirty) && pIdx == 0)
                {
                    p.requirements.Add(new SitAvoidPositionTrait(SimpleTraitType.Dirty, "Keep me off any Dirty seat!"));
                }
            }

            return def;
        }

        private static string SanitizePersonName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            string clean = raw.Trim();
            int paren = clean.IndexOf('(');
            if (paren > 0) clean = clean.Substring(0, paren).Trim();
            int under = clean.IndexOf('_');
            if (under > 0) clean = clean.Substring(0, under).Trim();
            if (clean.StartsWith("Person", StringComparison.OrdinalIgnoreCase) || clean.Length > 10)
                return null;
            return char.ToUpper(clean[0]) + clean.Substring(1);
        }

        private static void AddGridSeats(RuntimeLevelDefinition def, int rows, int cols, float xSpacing, float ySpacing, float centerY)
        {
            int idx = 0;
            float totalWidth = (cols - 1) * xSpacing;
            float totalHeight = (rows - 1) * ySpacing;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    float x = -totalWidth * 0.5f + c * xSpacing;
                    float y = centerY + totalHeight * 0.5f - r * ySpacing;

                    var traits = new List<SimpleTraitType>();
                    traits.Add(GetRowTrait(r));
                    traits.Add(SimpleTraitType.Same_Direction);

                    if (r == 0) traits.Add(SimpleTraitType.Front);
                    if (r == rows - 1) traits.Add(SimpleTraitType.Back);

                    if (cols == 2)
                    {
                        if (c == 0) traits.Add(SimpleTraitType.Window);
                        else traits.Add(SimpleTraitType.Aisle);
                    }
                    else
                    {
                        if (c == 0 || c == cols - 1) traits.Add(SimpleTraitType.Window);
                        else traits.Add(SimpleTraitType.Aisle);
                    }

                    def.seats.Add(new RuntimeSeatSpec
                    {
                        id = $"Seat_{idx + 1}",
                        index = idx,
                        position = new Vector2(x, y),
                        traits = traits
                    });
                    idx++;
                }
            }
        }

        private static SimpleTraitType GetRowTrait(int zeroBasedRow)
        {
            switch (zeroBasedRow)
            {
                case 0: return SimpleTraitType.Row_1;
                case 1: return SimpleTraitType.Row_2;
                case 2: return SimpleTraitType.Row_3;
                case 3: return SimpleTraitType.Row_4;
                default: return SimpleTraitType.Row_5;
            }
        }

        private static RuntimePersonSpec MakePerson(string name, int paletteIdx, int targetSeat, params PersonRequirement[] reqs)
        {
            var spec = new RuntimePersonSpec
            {
                name = name,
                color = BlobPalette[paletteIdx % BlobPalette.Length],
                role = RoleType.Adult,
                targetSeatIndex = targetSeat,
                requirements = new List<PersonRequirement>(reqs)
            };
            return spec;
        }
    }
}
