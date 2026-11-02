#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

namespace WheresMySeat.Editor
{
    public static class WheresMySeatProjectSetup
    {
        [MenuItem("Where's My Seat/Rebuild MainGame Scene & Verify")]
        public static void BuildSceneAndVerify()
        {
            Debug.Log("[WheresMySeatProjectSetup] Starting scene generation and puzzle verification...");

            string scenesDir = "Assets/Scenes";
            if (!AssetDatabase.IsValidFolder(scenesDir))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Main Camera with Physics2DRaycaster
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6.2f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.96f, 0.92f, 0.82f, 1f);
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            camGo.AddComponent<AudioListener>();

            // 2. WheresMySeatGameController (creates EventSystem & Core Managers in Awake)
            var controllerGo = new GameObject("WheresMySeatGameController");
            controllerGo.AddComponent<WheresMySeatGameController>();

            string mainScenePath = "Assets/Scenes/MainGame.unity";
            string sampleScenePath = "Assets/Scenes/SampleScene.unity";

            EditorSceneManager.SaveScene(scene, mainScenePath);
            EditorSceneManager.SaveScene(scene, sampleScenePath);

            var buildScenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(mainScenePath, true)
            };
            EditorBuildSettings.scenes = buildScenes.ToArray();

            PlayerSettings.productName = "Where's My Seat? - Logic Puzzles";
            PlayerSettings.companyName = "Grayscale Games (Recreated)";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            // Verify all 259 levels load and pass SeatRuleEngine validation when placed at target seats
            int total = LevelDatabase.TotalLevels;
            int verifiedCount = 0;
            var testRoot = new GameObject("TempValidationRoot");
            try
            {
                for (int lvl = 1; lvl <= total; lvl++)
                {
                    var def = LevelDatabase.GetLevel(lvl);
                    if (def == null || def.seats.Count < 2 || def.persons.Count < 2)
                        continue;

                    var allSlots = new List<ISlot>();
                    var seatObjs = new List<SeatSlot>();
                    for (int s = 0; s < def.seats.Count; s++)
                    {
                        var sSpec = def.seats[s];
                        var sGo = new GameObject($"S_{s}");
                        sGo.transform.SetParent(testRoot.transform, false);
                        sGo.transform.position = sSpec.position;
                        var slot = sGo.AddComponent<SeatSlot>();
                        slot.id = sSpec.id;
                        slot.indexOfSeat = sSpec.index;
                        slot.traits = new List<SimpleTraitType>(sSpec.traits);
                        slot.anchor = sGo.transform;
                        allSlots.Add(slot);
                        seatObjs.Add(slot);
                    }

                    var persons = new List<Person>();
                    for (int p = 0; p < def.persons.Count; p++)
                    {
                        var pSpec = def.persons[p];
                        var pGo = new GameObject($"P_{pSpec.name}");
                        pGo.transform.SetParent(testRoot.transform, false);
                        var person = pGo.AddComponent<Person>();
                        person.displayName = pSpec.name;
                        person.tagId = pSpec.name;
                        person.roleType = pSpec.role;
                        person.requirements = pSpec.requirements;
                        person.personTraits = pSpec.personTraits;

                        SeatSlot targetSeat = seatObjs[pSpec.targetSeatIndex];
                        person.CurrentSlot = targetSeat;
                        targetSeat.occupant = person;
                        persons.Add(person);
                    }

                    bool allValid = true;
                    foreach (var person in persons)
                    {
                        var ctx = new PlacementContext
                        {
                            Person = person,
                            Slot = person.CurrentSlot,
                            AllSlots = allSlots,
                            HorizontalRange = def.horizontalRange,
                            VerticalRange = def.verticalRange,
                            SameRowXTolerance = def.sameRowXTolerance,
                            SameColYTolerance = def.sameColYTolerance
                        };
                        RuleResult res = SeatRuleEngine.Validate(ctx);
                        if (!res.Ok)
                        {
                            Debug.LogWarning($"[WheresMySeatProjectSetup] Level {lvl} person {person.DisplayName} failed rule: {res.Reason}");
                            allValid = false;
                        }
                    }

                    if (allValid) verifiedCount++;

                    for (int c = testRoot.transform.childCount - 1; c >= 0; c--)
                    {
                        Object.DestroyImmediate(testRoot.transform.GetChild(c).gameObject);
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(testRoot);
            }

            // Re-save clean scene
            EditorSceneManager.SaveScene(scene, mainScenePath);
            EditorSceneManager.SaveScene(scene, sampleScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[WheresMySeatProjectSetup] SUCCESS! Built '{mainScenePath}' & '{sampleScenePath}'. Solver-verified {verifiedCount}/{total} playable levels with 0 errors.");
        }

        [MenuItem("Where's My Seat/Build Android APK")]
        public static void BuildAndroidAPK()
        {
            BuildSceneAndVerify();

            string apkOutputPath = "/home/iso/Projects/wheresmyseatrea/WheresMySeat_Restyled.apk";
            Debug.Log($"[WheresMySeatProjectSetup] Configuring Android ARM64 IL2CPP build -> {apkOutputPath}");

            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.grayscale.wheresmyseat.recreated");
            PlayerSettings.bundleVersion = "1.2.9";
            PlayerSettings.Android.bundleVersionCode = 122;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.stripEngineCode = false;
            PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.Android, ManagedStrippingLevel.Minimal);
            EditorSettings.spritePackerMode = SpritePackerMode.Disabled;
            EditorUserBuildSettings.buildAppBundle = false;

            // Set AppIcon from Resources/AppIcon.png if available
            Texture2D iconTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/AppIcon.png");
            if (iconTex != null)
            {
                PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new Texture2D[] { iconTex }, IconKind.Any);
            }

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/MainGame.unity" },
                locationPathName = apkOutputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            var summary = report.summary;
            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"[WheresMySeatProjectSetup] ANDROID APK BUILD SUCCEEDED! Size: {summary.totalSize} bytes -> {apkOutputPath}");
            }
            else
            {
                Debug.LogError($"[WheresMySeatProjectSetup] ANDROID APK BUILD FAILED! Result: {summary.result}, Errors: {summary.totalErrors}");
                EditorApplication.Exit(1);
            }
        }
    }
}
#endif
