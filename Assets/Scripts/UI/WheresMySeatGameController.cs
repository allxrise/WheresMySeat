using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WheresMySeat
{
    public class WheresMySeatGameController : MonoBehaviour
    {
        public static WheresMySeatGameController Instance { get; private set; }

        private Camera _mainCamera;
        private Transform _worldRoot;
        private RuntimeLevelDefinition _currentLevelDef;
        private Person _inspectedPerson;

        // UI Roots
        private Canvas _mainCanvas;
        private RectTransform _mainCanvasRect;
        private GameObject _homeScreenPanel;
        private GameObject _gameplayHudPanel;
        private GameObject _winModalPanel;
        private GameObject _loseModalPanel;
        private GameObject _eventModalPanel;

        // Home UI Elements
        private TextMeshProUGUI _homeCoinsTmp;
        private TextMeshProUGUI _homeTicketsTmp;
        private TextMeshProUGUI _homeStreakTmp;
        private TextMeshProUGUI _homeChapterTitleTmp;
        private TextMeshProUGUI _homeLevelSubtitleTmp;
        private TextMeshProUGUI _homePlayBtnTmp;
        private Image _homePlayBtnImg;
        private Image _homeLandmarkImg;
        private Image _preHintToggleBg;
        private Image _preMovesToggleBg;
        private readonly List<TextMeshProUGUI> _roadNodeLabels = new List<TextMeshProUGUI>();
        private readonly List<Image> _roadNodeImages = new List<Image>();
        private int _selectedLevelToPlay = 1;

        // Gameplay HUD Elements
        private TextMeshProUGUI _hudLevelTitleTmp;
        private TextMeshProUGUI _hudMovesTmp;
        private RectTransform _speechBubbleRect;
        private RectTransform _speechArrowRect;
        private TextMeshProUGUI _hudInspectorTitleTmp;
        private TextMeshProUGUI _hudInspectorRoleTmp;
        private Transform _hudRequireListRoot;
        private Transform _hudPersonTabsRoot;
        private TextMeshProUGUI _undoBadgeTmp;
        private TextMeshProUGUI _hintBadgeTmp;
        private TextMeshProUGUI _crownBadgeTmp;
        private TextMeshProUGUI _skipBadgeTmp;
        private bool _speechBubbleMinimized;

        // Toast Banner
        private GameObject _toastObj;
        private TextMeshProUGUI _toastTmp;
        private float _toastTimer;

        // Win Modal Elements
        private TextMeshProUGUI _winTitleTmp;
        private TextMeshProUGUI _winMultiplierBtnTmp;
        private RectTransform _multiplierNeedleRect;
        private int _currentMultiplier = 2;

        // Event Modal Elements
        private TextMeshProUGUI _eventModalTitleTmp;
        private TextMeshProUGUI _eventModalBodyTmp;
        private Transform _eventChoicesRoot;

        private void Awake()
        {
            Instance = this;
            EnsureCoreManagers();
            EnsureCameraAndEventSystem();
            BuildCompleteUI();
        }

        private void Start()
        {
            _selectedLevelToPlay = MetaResourceManager.Instance != null ? MetaResourceManager.Instance.CurrentLevel : 1;

            if (MetaResourceManager.Instance != null)
                MetaResourceManager.Instance.OnResourcesChanged += RefreshAllResourceUI;

            if (TurnManager.Instance != null)
                TurnManager.Instance.OnTurnChanged += OnTurnCountChanged;

            if (LevelCompletionManager.Instance != null)
                LevelCompletionManager.Instance.OnValidationRefreshed += RefreshInspectorUI;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnWinGame += HandleWinGame;
                GameManager.Instance.OnGameOver += HandleGameOver;
            }

            ShowHomeScreen();
        }

        private void Update()
        {
            if (_toastTimer > 0f)
            {
                _toastTimer -= Time.deltaTime;
                if (_toastTimer <= 0f && _toastObj != null)
                    _toastObj.SetActive(false);
            }

            // Update floating speech bubble position above the selected/dragged passenger
            if (_gameplayHudPanel != null && _gameplayHudPanel.activeSelf && _speechBubbleRect != null && _inspectedPerson != null && !_speechBubbleMinimized)
            {
                UpdateFloatingSpeechBubblePosition();
            }

            // Update Win Modal multiplier needle (APK reward_multiplier + arrow)
            if (_winModalPanel != null && _winModalPanel.activeSelf && _multiplierNeedleRect != null)
            {
                float t = Mathf.Sin(Time.time * 3.5f);
                _multiplierNeedleRect.anchoredPosition = new Vector2(t * 175f, -38f);
                float absT = Mathf.Abs(t);
                if (absT < 0.25f) _currentMultiplier = 5;
                else if (absT < 0.62f) _currentMultiplier = 3;
                else _currentMultiplier = 2;

                if (_winMultiplierBtnTmp != null)
                    _winMultiplierBtnTmp.text = $"CLAIM x{_currentMultiplier} ({80 * _currentMultiplier})";
            }
        }

        private void UpdateFloatingSpeechBubblePosition()
        {
            if (_mainCamera == null || _mainCanvasRect == null || _inspectedPerson == null) return;

            Vector3 worldPos = _inspectedPerson.transform.position + new Vector3(0f, 0.72f, 0f);
            Vector3 screenPos = _mainCamera.WorldToScreenPoint(worldPos);

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _mainCanvasRect,
                screenPos,
                null,
                out Vector2 canvasPos))
            {
                // Clamp inside 750x1334 canvas so the speech bubble never goes off-screen or covers the TopBar/BottomBar
                float clampedX = Mathf.Clamp(canvasPos.x, -172f, 172f);
                float clampedY = Mathf.Clamp(canvasPos.y + 88f, -195f, 385f);

                _speechBubbleRect.anchoredPosition = Vector2.Lerp(
                    _speechBubbleRect.anchoredPosition,
                    new Vector2(clampedX, clampedY),
                    Time.deltaTime * 18f
                );

                if (_speechArrowRect != null)
                {
                    float arrowX = Mathf.Clamp(canvasPos.x - clampedX, -135f, 135f);
                    _speechArrowRect.anchoredPosition = new Vector2(arrowX, -11f);
                }
            }
        }

        private void EnsureCoreManagers()
        {
            if (GetComponent<MetaResourceManager>() == null) gameObject.AddComponent<MetaResourceManager>();
            if (GetComponent<GameManager>() == null) gameObject.AddComponent<GameManager>();
            if (GetComponent<PlacementManager>() == null) gameObject.AddComponent<PlacementManager>();
            if (GetComponent<LevelCompletionManager>() == null) gameObject.AddComponent<LevelCompletionManager>();
            if (GetComponent<TurnManager>() == null) gameObject.AddComponent<TurnManager>();
            if (GetComponent<GameTimeManager>() == null) gameObject.AddComponent<GameTimeManager>();
            if (GetComponent<BoostersController>() == null) gameObject.AddComponent<BoostersController>();
        }

        private void EnsureCameraAndEventSystem()
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                _mainCamera = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            _mainCamera.orthographic = true;
            _mainCamera.orthographicSize = 6.2f;
            _mainCamera.clearFlags = CameraClearFlags.SolidColor;
            _mainCamera.backgroundColor = new Color(0.96f, 0.92f, 0.82f, 1f);
            _mainCamera.transform.position = new Vector3(0f, 0f, -10f);

            if (_mainCamera.GetComponent<Physics2DRaycaster>() == null)
            {
                _mainCamera.gameObject.AddComponent<Physics2DRaycaster>();
            }

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
            }
        }

        public void ShowHomeScreen()
        {
            ClearWorldLevel();
            _homeScreenPanel.SetActive(true);
            _gameplayHudPanel.SetActive(false);
            _winModalPanel.SetActive(false);
            _loseModalPanel.SetActive(false);
            _eventModalPanel.SetActive(false);

            RefreshHomeUI();
            RefreshAllResourceUI();
        }

        public void StartSelectedLevel()
        {
            StartLevel(_selectedLevelToPlay);
        }

        public void StartLevel(int levelNumber)
        {
            _selectedLevelToPlay = Mathf.Clamp(levelNumber, 1, LevelDatabase.TotalLevels);
            MetaResourceManager.Instance?.SetCurrentLevel(_selectedLevelToPlay);

            _homeScreenPanel.SetActive(false);
            _winModalPanel.SetActive(false);
            _loseModalPanel.SetActive(false);
            _eventModalPanel.SetActive(false);
            _gameplayHudPanel.SetActive(true);

            SpawnWorldLevel(_selectedLevelToPlay);
            RefreshAllResourceUI();
        }

        private void ClearWorldLevel()
        {
            PlacementManager.Instance?.ClearLevelObjects();
            if (_worldRoot != null)
            {
                Destroy(_worldRoot.gameObject);
                _worldRoot = null;
            }
            _inspectedPerson = null;
        }

        private void SpawnWorldLevel(int levelNumber)
        {
            ClearWorldLevel();

            _currentLevelDef = LevelDatabase.GetLevel(levelNumber);
            _mainCamera.orthographicSize = _currentLevelDef.cameraOrthoSize;
            _mainCamera.backgroundColor = new Color(0.95f, 0.91f, 0.81f, 1f);

            var rootGo = new GameObject($"LevelWorld_{levelNumber}");
            _worldRoot = rootGo.transform;

            var pm = PlacementManager.Instance;
            pm.distanceBetweenSeatToCheck = _currentLevelDef.distanceBetweenSeatToCheck;
            pm.horizontalRange = _currentLevelDef.horizontalRange;
            pm.verticalRange = _currentLevelDef.verticalRange;
            pm.sameRowXTolerance = _currentLevelDef.sameRowXTolerance;
            pm.sameColYTolerance = _currentLevelDef.sameColYTolerance;

            // 1. Full-screen Restyled APK Environment Background (EnvBG_0..11)
            int envIndex = (levelNumber - 1) % 6; // Prioritize our 6 AI-restyled & color-graded backgrounds
            string envSpriteName = $"EnvBG_{envIndex}";
            Sprite envSprite = ProceduralSprites.LoadAPKSprite(envSpriteName, 100f)
                            ?? ProceduralSprites.LoadAPKSprite("BG_Standart", 100f);

            if (envSprite != null)
            {
                var envGo = new GameObject("EnvironmentBackground");
                envGo.transform.SetParent(_worldRoot, false);
                envGo.transform.localPosition = Vector3.zero;
                var envSr = envGo.AddComponent<SpriteRenderer>();
                envSr.sprite = envSprite;
                envSr.sortingOrder = -20;

                float worldScreenHeight = _mainCamera.orthographicSize * 2f;
                float worldScreenWidth = worldScreenHeight * (750f / 1334f) * 1.12f;
                Vector2 spriteSize = envSprite.bounds.size;
                envGo.transform.localScale = new Vector3(
                    worldScreenWidth / Mathf.Max(0.1f, spriteSize.x),
                    worldScreenHeight / Mathf.Max(0.1f, spriteSize.y),
                    1f
                );
            }

            // 2. Subtle Vehicle / Stage Cabin Frame (bus-body or soft cream mat)
            bool useBusFrame = (levelNumber % 3 == 1);
            if (useBusFrame)
            {
                Sprite busSprite = ProceduralSprites.LoadAPKSprite("bus-body", 100f);
                if (busSprite != null)
                {
                    var busGo = new GameObject("BusCabinFrame");
                    busGo.transform.SetParent(_worldRoot, false);
                    busGo.transform.localPosition = new Vector3(0f, 0.95f, 0f);
                    busGo.transform.localScale = new Vector3(1.32f, 0.76f, 1f);
                    var busSr = busGo.AddComponent<SpriteRenderer>();
                    busSr.sprite = busSprite;
                    busSr.color = new Color(1f, 1f, 1f, 0.92f);
                    busSr.sortingOrder = -10;
                }
            }
            else
            {
                var cabinGo = new GameObject("CabinMat");
                cabinGo.transform.SetParent(_worldRoot, false);
                cabinGo.transform.localPosition = new Vector3(0f, 1.10f, 0f);
                cabinGo.transform.localScale = new Vector3(5.35f, 4.45f, 1f);
                var cabinSr = cabinGo.AddComponent<SpriteRenderer>();
                cabinSr.sprite = ProceduralSprites.GetRoundedRect(128, 128, 26f, 4f);
                cabinSr.color = new Color(1f, 0.97f, 0.90f, 0.58f);
                cabinSr.sortingOrder = -10;
            }

            // 3. Spawn SeatSlots
            var spawnedSeats = new List<SeatSlot>();
            for (int i = 0; i < _currentLevelDef.seats.Count; i++)
            {
                var seatSpec = _currentLevelDef.seats[i];
                var seatGo = new GameObject(seatSpec.id);
                seatGo.transform.SetParent(_worldRoot, false);
                seatGo.transform.localPosition = seatSpec.position;

                var seatSlot = seatGo.AddComponent<SeatSlot>();
                seatSlot.isBlockedByDance = seatSpec.isBlockedByDance;
                seatSlot.BuildVisuals(seatSpec.id, seatSpec.index, seatSpec.traits, _currentLevelDef.seatThemeColor);
                pm.RegisterSeat(seatSlot);
                spawnedSeats.Add(seatSlot);

                if (seatSpec.hasLamp)
                {
                    var lampGo = new GameObject($"Lamp_{seatSpec.index}");
                    lampGo.transform.SetParent(seatGo.transform, false);
                    lampGo.transform.localPosition = new Vector3(0.44f, 0.44f, 0f);
                    var onOff = lampGo.AddComponent<OnOffItem>();
                    onOff.BuildVisuals(seatSpec.index, seatSpec.lampInitialOn);
                    pm.RegisterOnOffItem(onOff);
                }
            }

            // 4. Spawn StartSlots & Persons in Waiting Queue (above the BottomBar)
            int pCount = _currentLevelDef.persons.Count;
            int maxColsPerQueueRow = Mathf.Min(pCount, 4);
            for (int i = 0; i < pCount; i++)
            {
                int qRow = i / maxColsPerQueueRow;
                int qCol = i % maxColsPerQueueRow;
                int colsInThisRow = (qRow == 0) ? Mathf.Min(pCount, maxColsPerQueueRow) : (pCount - maxColsPerQueueRow);
                float qSpacing = colsInThisRow >= 4 ? 1.25f : 1.42f;
                float qX = -(colsInThisRow - 1) * qSpacing * 0.5f + qCol * qSpacing;
                float qY = -1.92f - qRow * 1.05f;

                var startGo = new GameObject($"StartSlot_{i + 1}");
                startGo.transform.SetParent(_worldRoot, false);
                startGo.transform.localPosition = new Vector3(qX, qY, 0f);
                var startSlot = startGo.AddComponent<StartSlot>();
                startSlot.BuildVisuals($"Start_{i + 1}", i);
                pm.RegisterStartSlot(startSlot);

                var pSpec = _currentLevelDef.persons[i];
                var personGo = new GameObject($"Person_{pSpec.name}");
                personGo.transform.SetParent(_worldRoot, false);
                var person = personGo.AddComponent<Person>();
                person.BuildCharacterVisuals(pSpec.name, pSpec.color, pSpec.role, pSpec.requirements, pSpec.personTraits, pSpec.blockedTurns);

                if (pSpec.targetSeatIndex >= 0 && pSpec.targetSeatIndex < spawnedSeats.Count)
                {
                    person.hintSlot = spawnedSeats[pSpec.targetSeatIndex];
                }

                person.SetInitialSlot(startSlot);
                pm.RegisterPerson(person);
            }

            // 5. Initialize Moves & GameState
            int moves = _currentLevelDef.movesOrSeconds;
            if (MetaResourceManager.Instance != null && MetaResourceManager.Instance.PreLevelExtraMovesEnabled)
            {
                if (MetaResourceManager.Instance.TrySpendBoosterOrCoins(BoostType.Booster_PreLevelAddMoves, 200))
                {
                    moves += 2;
                    ShowToast("⚡ Pre-Level Booster: +2 Extra Moves active!");
                }
                MetaResourceManager.Instance.PreLevelExtraMovesEnabled = false;
            }

            GameManager.Instance.CurrentGameMode = _currentLevelDef.gameMode;
            GameManager.Instance.SetGameState(GameState.Playing);
            TurnManager.Instance.Init(moves);

            if (MetaResourceManager.Instance != null && MetaResourceManager.Instance.PreLevelAutoSeatEnabled)
            {
                if (MetaResourceManager.Instance.TrySpendBoosterOrCoins(BoostType.Booster_PreLevelHint, 250))
                {
                    pm.ApplyStartupHintBoosterRandomN(1);
                    ShowToast("💡 Pre-Level Booster: 1 Passenger auto-seated!");
                }
                MetaResourceManager.Instance.PreLevelAutoSeatEnabled = false;
            }

            LevelCompletionManager.Instance.ForceValidateAllPerson();

            if (_hudLevelTitleTmp != null)
            {
                _hudLevelTitleTmp.text = $"LEVEL {levelNumber}";
            }

            RebuildPersonQuickTabs();

            if (pm.Persons.Count > 0)
            {
                _speechBubbleMinimized = false;
                InspectPerson(pm.Persons[0]);
            }
        }

        public void InspectPerson(Person person)
        {
            if (person == null) return;
            _inspectedPerson = person;
            _speechBubbleMinimized = false;
            if (_speechBubbleRect != null)
                _speechBubbleRect.gameObject.SetActive(true);
            RefreshInspectorUI();
            UpdateFloatingSpeechBubblePosition();
        }

        private void RefreshInspectorUI()
        {
            if (_inspectedPerson == null || _hudInspectorTitleTmp == null || _hudRequireListRoot == null)
                return;

            string seatStatus = (_inspectedPerson.CurrentSlot != null && _inspectedPerson.CurrentSlot.IsSeatSlot)
                ? $"Seat #{_inspectedPerson.CurrentSlot.IndexOfSeat + 1}"
                : (_inspectedPerson.RoleType == RoleType.Kid ? "Kid" : "Adult");

            _hudInspectorTitleTmp.text = _inspectedPerson.DisplayName;
            if (_hudInspectorRoleTmp != null)
                _hudInspectorRoleTmp.text = seatStatus;

            ClearChildren(_hudRequireListRoot);

            if (_inspectedPerson.isUseSpecialBoster)
            {
                CreateRequirementRow(_hudRequireListRoot, "Magic Crown: Happy in ANY seat!", true, true);
                return;
            }

            bool isSeated = _inspectedPerson.CurrentSlot != null && _inspectedPerson.CurrentSlot.IsSeatSlot;
            for (int i = 0; i < _inspectedPerson.Requirements.Count; i++)
            {
                var req = _inspectedPerson.Requirements[i];
                bool valid = false;
                if (isSeated && LevelCompletionManager.Instance != null)
                {
                    valid = LevelCompletionManager.Instance.EvaluateRequirement(_inspectedPerson, req).IsValid;
                }
                CreateRequirementRow(_hudRequireListRoot, req.Description, isSeated, valid);
            }
        }

        private void CreateRequirementRow(Transform parent, string ruleText, bool isSeated, bool isValid)
        {
            var rowGo = new GameObject("UIRequireElement", typeof(RectTransform));
            rowGo.transform.SetParent(parent, false);
            var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10f;
            hlg.padding = new RectOffset(4, 4, 2, 2);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;

            // Tick Box (APKUI/game_tick_box.png + APKUI/v.png or APKUI/x.png)
            var boxGo = new GameObject("TickBox", typeof(RectTransform), typeof(Image));
            boxGo.transform.SetParent(rowGo.transform, false);
            var boxLe = boxGo.AddComponent<LayoutElement>();
            boxLe.preferredWidth = 26f;
            boxLe.preferredHeight = 26f;
            boxLe.minWidth = 26f;
            var boxImg = boxGo.GetComponent<Image>();
            boxImg.sprite = ProceduralSprites.LoadAPKSprite("game_tick_box") ?? ProceduralSprites.GetRoundedRect(32, 32, 6f);
            boxImg.color = Color.white;

            if (isSeated)
            {
                var markGo = new GameObject("TickMark", typeof(RectTransform), typeof(Image));
                markGo.transform.SetParent(boxGo.transform, false);
                var markRect = markGo.GetComponent<RectTransform>();
                markRect.sizeDelta = new Vector2(24f, 24f);
                var markImg = markGo.GetComponent<Image>();
                markImg.sprite = ProceduralSprites.LoadAPKSprite(isValid ? "v" : "x")
                              ?? ProceduralSprites.LoadAPKSprite("game_tick");
                markImg.color = Color.white;
            }

            var txtTmp = CreateText(rowGo.transform, ruleText, 19f, FontStyles.Bold);
            txtTmp.alignment = TextAlignmentOptions.Left;
            txtTmp.color = isSeated
                ? (isValid ? new Color(0.11f, 0.50f, 0.24f, 1f) : new Color(0.78f, 0.16f, 0.22f, 1f))
                : ProceduralSprites.DarkOutlineColor;
            var txtLe = txtTmp.gameObject.AddComponent<LayoutElement>();
            txtLe.flexibleWidth = 1f;
        }

        private void RebuildPersonQuickTabs()
        {
            if (_hudPersonTabsRoot == null || PlacementManager.Instance == null) return;
            ClearChildren(_hudPersonTabsRoot);

            foreach (var p in PlacementManager.Instance.Persons)
            {
                Person captured = p;
                var btn = CreateAPKButton(_hudPersonTabsRoot, "game_level_bar", p.DisplayName, () =>
                {
                    InspectPerson(captured);
                }, fontSize: 17f, textColor: ProceduralSprites.DarkOutlineColor);
                var le = btn.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = 104f;
                le.preferredHeight = 38f;
            }
        }

        public void AutoSolveCurrentLevelDemo()
        {
            if (PlacementManager.Instance == null) return;
            foreach (var p in PlacementManager.Instance.Persons)
            {
                if (p != null && p.hintSlot != null)
                {
                    p.SnapDirect(p.hintSlot);
                    PlacementManager.Instance.SyncLampForPersonIfRequired(p, p.hintSlot);
                }
            }
            LevelCompletionManager.Instance?.ForceValidateAllPerson();
        }

        public void ShowToast(string message)
        {
            if (_toastObj == null || _toastTmp == null) return;
            _toastTmp.text = message;
            _toastObj.SetActive(true);
            _toastTimer = 2.6f;
        }

        private void OnTurnCountChanged(int remainingMoves)
        {
            if (_hudMovesTmp != null)
            {
                _hudMovesTmp.text = remainingMoves.ToString();
                _hudMovesTmp.color = remainingMoves <= 2
                    ? new Color(0.88f, 0.18f, 0.24f, 1f)
                    : ProceduralSprites.DarkOutlineColor;
            }
        }

        private void HandleWinGame()
        {
            if (_winTitleTmp != null && _currentLevelDef != null)
            {
                _winTitleTmp.text = $"LEVEL {_currentLevelDef.levelNumber} COMPLETE!";
            }
            _winModalPanel.SetActive(true);
        }

        private void HandleGameOver()
        {
            _loseModalPanel.SetActive(true);
        }

        private void RefreshHomeUI()
        {
            MapID map = LevelDatabase.GetChapterForLevel(_selectedLevelToPlay);
            var def = LevelDatabase.GetLevel(_selectedLevelToPlay);

            if (_homeChapterTitleTmp != null)
                _homeChapterTitleTmp.text = LevelDatabase.GetChapterDisplayName(map).Replace("·", "-");
            if (_homeLevelSubtitleTmp != null)
                _homeLevelSubtitleTmp.text = $"{def.title} - {def.subtitle}";

            if (_homePlayBtnTmp != null)
            {
                string diffLabel = def.difficulty == LevelDifficulty.VeryHard ? "\n<size=17>VERY HARD</size>"
                    : (def.difficulty == LevelDifficulty.Hard ? "\n<size=17>HARD</size>" : "");
                _homePlayBtnTmp.text = $"LEVEL {_selectedLevelToPlay}{diffLabel}";
            }

            if (_homePlayBtnImg != null)
            {
                string playSprite = def.difficulty == LevelDifficulty.VeryHard ? "bt-play-very-hard_new"
                    : (def.difficulty == LevelDifficulty.Hard ? "bt-play-hard-new" : "bt-green-play");
                _homePlayBtnImg.sprite = ProceduralSprites.LoadAPKSprite(playSprite) ?? _homePlayBtnImg.sprite;
            }

            if (_homeLandmarkImg != null)
            {
                string landmarkName = GetLandmarkSpriteForMap(map);
                Sprite lm = ProceduralSprites.LoadAPKSprite(landmarkName);
                if (lm != null) _homeLandmarkImg.sprite = lm;
            }

            // Update interactive road nodes
            for (int i = 0; i < _roadNodeLabels.Count; i++)
            {
                int nodeLv = Mathf.Clamp(_selectedLevelToPlay + i - 1, 1, LevelDatabase.TotalLevels);
                if (_roadNodeLabels[i] != null)
                    _roadNodeLabels[i].text = nodeLv.ToString();
                if (_roadNodeImages[i] != null)
                {
                    bool isSelected = (nodeLv == _selectedLevelToPlay);
                    _roadNodeImages[i].sprite = ProceduralSprites.LoadAPKSprite(isSelected ? "shared_bt-yellow" : "game_booster_pop");
                    _roadNodeImages[i].transform.localScale = isSelected ? new Vector3(1.18f, 1.18f, 1f) : Vector3.one;
                }
            }

            if (_preHintToggleBg != null && MetaResourceManager.Instance != null)
                _preHintToggleBg.color = MetaResourceManager.Instance.PreLevelAutoSeatEnabled
                    ? new Color(0.65f, 1f, 0.78f, 1f)
                    : Color.white;

            if (_preMovesToggleBg != null && MetaResourceManager.Instance != null)
                _preMovesToggleBg.color = MetaResourceManager.Instance.PreLevelExtraMovesEnabled
                    ? new Color(0.65f, 1f, 0.78f, 1f)
                    : Color.white;
        }

        private string GetLandmarkSpriteForMap(MapID map)
        {
            switch (map)
            {
                case MapID.USA: return "USA_2";
                case MapID.England: return "England";
                case MapID.France: return "France_1_0";
                case MapID.Brazil: return "Brazil_1_0";
                case MapID.Japan: return "Japan";
                case MapID.Italy: return "Italy";
                case MapID.Germany: return "Germany";
                case MapID.Australia: return "Australia_1_0";
                default: return "USA_2";
            }
        }

        private void RefreshAllResourceUI()
        {
            var meta = MetaResourceManager.Instance;
            if (meta == null) return;

            if (_homeCoinsTmp != null) _homeCoinsTmp.text = meta.Coins.ToString();
            if (_homeTicketsTmp != null) _homeTicketsTmp.text = $"{meta.Tickets}/{meta.MaxTickets}";
            if (_homeStreakTmp != null) _homeStreakTmp.text = $"SEAT PASS - STREAK {meta.WinStreak}";

            if (_undoBadgeTmp != null) _undoBadgeTmp.text = meta.GetBoosterCount(BoostType.Booster_Undo).ToString();
            if (_hintBadgeTmp != null) _hintBadgeTmp.text = meta.GetBoosterCount(BoostType.Booster_Hint).ToString();
            if (_crownBadgeTmp != null) _crownBadgeTmp.text = meta.GetBoosterCount(BoostType.Booster_SpecialItem).ToString();
            if (_skipBadgeTmp != null) _skipBadgeTmp.text = meta.GetBoosterCount(BoostType.Booster_SkipLevel).ToString();
        }

        // =====================================================================
        // LIVE EVENTS MODALS (CLUE HUNT, SEAT TOWER, LEVEL RACE)
        // =====================================================================

        private void OpenClueHuntModal()
        {
            var meta = MetaResourceManager.Instance;
            if (meta == null || meta.ClueHuntLevels.Count == 0) return;

            var clue = meta.ClueHuntLevels[meta.ClueHuntIndex % meta.ClueHuntLevels.Count];
            _eventModalPanel.SetActive(true);
            _eventModalTitleTmp.text = $"CLUE HUNT · CASE #{clue.id}";
            _eventModalBodyTmp.text = $"<b>{clue.title}</b>\n{clue.description}\n\n<color=#B84A14><b>{clue.question}</b></color>";

            ClearChildren(_eventChoicesRoot);
            if (clue.choices != null)
            {
                foreach (var choice in clue.choices)
                {
                    string capturedChoice = choice;
                    var btn = CreateAPKButton(_eventChoicesRoot, "bt-yellow_0", $"Suspect: {choice}", () =>
                    {
                        if (string.Equals(capturedChoice, clue.answer, StringComparison.OrdinalIgnoreCase))
                        {
                            meta.CompleteCurrentClueHunt();
                            _eventModalBodyTmp.text = $"<color=#1D7A3A><b>✓ CORRECT! ({clue.answer})</b></color>\n\n{clue.explanation}\n\n<b>Reward: +150 Coins & +1 Hint!</b>";
                            ClearChildren(_eventChoicesRoot);
                            var nextBtn = CreateAPKButton(_eventChoicesRoot, "bt-green", "Next Case ▶", OpenClueHuntModal, 22f, Color.white);
                            nextBtn.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;
                        }
                        else
                        {
                            _eventModalBodyTmp.text = $"<color=#C92A35><b>✗ Not quite ({capturedChoice})!</b></color>\n\n{clue.description}\n\n<b>{clue.question}</b>";
                        }
                    }, 20f, ProceduralSprites.DarkOutlineColor);
                    btn.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;
                }
            }
        }

        private void OpenSeatTowerModal()
        {
            var meta = MetaResourceManager.Instance;
            if (meta == null) return;

            int totalSteps = meta.SeatTowerConfig != null ? meta.SeatTowerConfig.totalSteps : 7;
            int grandPrize = meta.SeatTowerConfig != null ? meta.SeatTowerConfig.grandPrizeCoins : 1000;

            _eventModalPanel.SetActive(true);
            _eventModalTitleTmp.text = "SEAT TOWER · 100 PLAYERS";
            _eventModalBodyTmp.text =
                $"Win {totalSteps} levels in a row without losing to split the <b>{grandPrize} Coins Grand Prize</b>!\n\n" +
                $"• Current Floor: <color=#1D7A3A><b>Step {meta.SeatTowerStep} / {totalSteps}</b></color>\n" +
                $"• Surviving Contenders: <color=#1A6699><b>{meta.SeatTowerRemainingPlayers} / 100 Players</b></color>\n\n" +
                "Every level you beat eliminates bots and climbs the Seat Tower!";

            ClearChildren(_eventChoicesRoot);
            var playBtn = CreateAPKButton(_eventChoicesRoot, "bt-green-play", "CLIMB NEXT FLOOR", () =>
            {
                _eventModalPanel.SetActive(false);
                StartSelectedLevel();
            }, 22f, Color.white);
            playBtn.gameObject.AddComponent<LayoutElement>().preferredHeight = 72f;
        }

        private void OpenSeatRaceModal()
        {
            var meta = MetaResourceManager.Instance;
            if (meta == null) return;

            int target = meta.SeatRaceConfig != null ? meta.SeatRaceConfig.NumberLevelTarget : 10;
            var lines = new List<string>
            {
                $"First player to beat <b>{target} Levels</b> wins the <b>Seat Race Chest (500 Coins + Boosters)</b>!\n",
                $"🏆 <b>YOU</b>  —  <color=#1D7A3A><b>{meta.LevelRacePlayerScore} / {target} Levels</b></color>"
            };
            for (int i = 0; i < meta.LevelRaceBotNames.Length; i++)
            {
                lines.Add($"🚌 {meta.LevelRaceBotNames[i]}  —  {meta.LevelRaceBotScores[i]} / {target} Levels");
            }

            _eventModalPanel.SetActive(true);
            _eventModalTitleTmp.text = "LEVEL RACE · LEADERBOARD";
            _eventModalBodyTmp.text = string.Join("\n", lines);

            ClearChildren(_eventChoicesRoot);
            var raceBtn = CreateAPKButton(_eventChoicesRoot, "bt-green-play", "RACE NOW!", () =>
            {
                _eventModalPanel.SetActive(false);
                StartSelectedLevel();
            }, 22f, Color.white);
            raceBtn.gameObject.AddComponent<LayoutElement>().preferredHeight = 72f;
        }

        // =====================================================================
        // AUTHENTIC 750x1334 APK UI CONSTRUCTION (WITH RESTYLED APK SPRITES)
        // =====================================================================

        private void BuildCompleteUI()
        {
            var canvasGo = new GameObject("WheresMySeatCanvas");
            canvasGo.transform.SetParent(transform, false);
            _mainCanvas = canvasGo.AddComponent<Canvas>();
            _mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _mainCanvas.sortingOrder = 100;
            _mainCanvasRect = canvasGo.GetComponent<RectTransform>();

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            // Exact APK CanvasScaler Reference Resolution from datapack.unity3d: 750 x 1334
            scaler.referenceResolution = new Vector2(750f, 1334f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            BuildHomeScreenPanel(canvasGo.transform);
            BuildGameplayHudPanel(canvasGo.transform);
            BuildWinModalPanel(canvasGo.transform);
            BuildLoseModalPanel(canvasGo.transform);
            BuildEventModalPanel(canvasGo.transform);
            BuildToastOverlay(canvasGo.transform);
        }

        private void BuildHomeScreenPanel(Transform parent)
        {
            _homeScreenPanel = CreateSpritePanel(parent, "HomeScreenPanel", "BG_Standart", Color.white);
            StretchFull(_homeScreenPanel.GetComponent<RectTransform>());

            // 1. Vertical Road Path (APKUI/line_road_0.png) in center
            var roadGo = CreateSpritePanel(_homeScreenPanel.transform, "RoadLine", "line_road_0", Color.white);
            var roadRect = roadGo.GetComponent<RectTransform>();
            roadRect.anchoredPosition = new Vector2(0f, -20f);
            roadRect.sizeDelta = new Vector2(30f, 760f);

            // 2. Chapter Landmark Card (USA_2, England, France_1_0, etc.)
            var landmarkCard = CreateSpritePanel(_homeScreenPanel.transform, "LandmarkCard", "game_dialog_2", Color.white);
            var lmCardRect = landmarkCard.GetComponent<RectTransform>();
            lmCardRect.anchoredPosition = new Vector2(0f, 255f);
            lmCardRect.sizeDelta = new Vector2(440f, 235f);

            _homeChapterTitleTmp = CreateText(landmarkCard.transform, "USA · Downtown Express", 22f, FontStyles.Bold);
            _homeChapterTitleTmp.color = ProceduralSprites.DarkOutlineColor;
            var chapTitleRect = _homeChapterTitleTmp.GetComponent<RectTransform>();
            chapTitleRect.anchorMin = new Vector2(0f, 0.76f);
            chapTitleRect.anchorMax = new Vector2(1f, 0.98f);
            chapTitleRect.offsetMin = Vector2.zero;
            chapTitleRect.offsetMax = Vector2.zero;

            var lmArtGo = CreateSpritePanel(landmarkCard.transform, "LandmarkIllustration", "USA_2", Color.white);
            var lmArtRect = lmArtGo.GetComponent<RectTransform>();
            lmArtRect.anchoredPosition = new Vector2(0f, -12f);
            lmArtRect.sizeDelta = new Vector2(240f, 132f);
            _homeLandmarkImg = lmArtGo.GetComponent<Image>();
            _homeLandmarkImg.preserveAspect = true;

            _homeLevelSubtitleTmp = CreateText(landmarkCard.transform, "Level 1 · First Bus Ride", 17f, FontStyles.Bold);
            _homeLevelSubtitleTmp.color = ProceduralSprites.DarkOutlineColor;
            var subRect = _homeLevelSubtitleTmp.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.04f, 0.03f);
            subRect.anchorMax = new Vector2(0.96f, 0.22f);
            subRect.offsetMin = Vector2.zero;
            subRect.offsetMax = Vector2.zero;

            // 3. Interactive Level Road Nodes along the Road
            _roadNodeLabels.Clear();
            _roadNodeImages.Clear();
            float[] nodeYs = { -215f, -105f, 5f, 105f };
            for (int i = 0; i < nodeYs.Length; i++)
            {
                int offset = i - 1;
                var nodeBtn = CreateAPKButton(_homeScreenPanel.transform, "game_booster_pop", (i + 1).ToString(), () =>
                {
                    int targetLv = Mathf.Clamp(_selectedLevelToPlay + offset, 1, LevelDatabase.TotalLevels);
                    _selectedLevelToPlay = targetLv;
                    RefreshHomeUI();
                }, 24f, ProceduralSprites.DarkOutlineColor);
                var nRect = nodeBtn.GetComponent<RectTransform>();
                nRect.anchoredPosition = new Vector2(0f, nodeYs[i]);
                nRect.sizeDelta = new Vector2(84f, 88f);
                _roadNodeLabels.Add(nodeBtn.GetComponentInChildren<TextMeshProUGUI>());
                _roadNodeImages.Add(nodeBtn.GetComponent<Image>());
            }

            // 4. Top Header Bar (HeaderUICanvas from APK: bar_top_new + Frame_Coral + avatar_2 + shared_coin_pop + shared_tiket)
            var topHeader = CreateSpritePanel(_homeScreenPanel.transform, "HeaderUICanvas", "bar_top_new", Color.white);
            var topRect = topHeader.GetComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0f, 1f);
            topRect.anchorMax = new Vector2(1f, 1f);
            topRect.pivot = new Vector2(0.5f, 1f);
            topRect.anchoredPosition = Vector2.zero;
            topRect.sizeDelta = new Vector2(0f, 118f);

            // Avatar (left)
            var avatarFrame = CreateSpritePanel(topHeader.transform, "AvatarFrame", "Frame_Coral", Color.white);
            var avRect = avatarFrame.GetComponent<RectTransform>();
            avRect.anchorMin = new Vector2(0f, 0.5f);
            avRect.anchorMax = new Vector2(0f, 0.5f);
            avRect.anchoredPosition = new Vector2(58f, 4f);
            avRect.sizeDelta = new Vector2(82f, 82f);

            var avatarInner = CreateSpritePanel(avatarFrame.transform, "AvatarImg", "avatar_2", Color.white);
            avatarInner.GetComponent<RectTransform>().sizeDelta = new Vector2(68f, 68f);

            // Coin Pill (center-left)
            var coinPill = CreateSpritePanel(topHeader.transform, "CoinPill", "shared_coin_pop", Color.white);
            var cpRect = coinPill.GetComponent<RectTransform>();
            cpRect.anchorMin = new Vector2(0.5f, 0.5f);
            cpRect.anchorMax = new Vector2(0.5f, 0.5f);
            cpRect.anchoredPosition = new Vector2(-78f, 6f);
            cpRect.sizeDelta = new Vector2(188f, 60f);

            var coinIcon = CreateSpritePanel(coinPill.transform, "CoinIcon", "Coin", Color.white);
            var ciRect = coinIcon.GetComponent<RectTransform>();
            ciRect.anchoredPosition = new Vector2(-68f, 0f);
            ciRect.sizeDelta = new Vector2(52f, 52f);

            _homeCoinsTmp = CreateText(coinPill.transform, "1200", 22f, FontStyles.Bold);
            _homeCoinsTmp.color = ProceduralSprites.DarkOutlineColor;
            _homeCoinsTmp.GetComponent<RectTransform>().anchoredPosition = new Vector2(4f, 0f);
            _homeCoinsTmp.GetComponent<RectTransform>().sizeDelta = new Vector2(110f, 46f);

            var addCoinBtn = CreateAPKButton(coinPill.transform, "add", "", () =>
            {
                MetaResourceManager.Instance?.AddCoins(500);
                ShowToast("+500 Bonus Coins added!");
            });
            var addRect = addCoinBtn.GetComponent<RectTransform>();
            addRect.anchoredPosition = new Vector2(72f, 0f);
            addRect.sizeDelta = new Vector2(36f, 36f);

            // Ticket Pill (center-right)
            var ticketPill = CreateSpritePanel(topHeader.transform, "TicketPill", "shared_coin_pop", Color.white);
            var tpRect = ticketPill.GetComponent<RectTransform>();
            tpRect.anchorMin = new Vector2(0.5f, 0.5f);
            tpRect.anchorMax = new Vector2(0.5f, 0.5f);
            tpRect.anchoredPosition = new Vector2(126f, 6f);
            tpRect.sizeDelta = new Vector2(172f, 60f);

            var ticketIcon = CreateSpritePanel(ticketPill.transform, "TicketIcon", "shared_tiket", Color.white);
            var tiRect = ticketIcon.GetComponent<RectTransform>();
            tiRect.anchoredPosition = new Vector2(-60f, 0f);
            tiRect.sizeDelta = new Vector2(46f, 54f);

            _homeTicketsTmp = CreateText(ticketPill.transform, "5/5", 22f, FontStyles.Bold);
            _homeTicketsTmp.color = ProceduralSprites.DarkOutlineColor;
            _homeTicketsTmp.GetComponent<RectTransform>().anchoredPosition = new Vector2(14f, 0f);
            _homeTicketsTmp.GetComponent<RectTransform>().sizeDelta = new Vector2(100f, 46f);

            // Settings Button (right)
            var setBtn = CreateAPKButton(topHeader.transform, "home_ic-setting", "", () =>
            {
                ShowToast("Sound & Haptics: ON · All 259 APK Levels Unlocked!");
            });
            var setRect = setBtn.GetComponent<RectTransform>();
            setRect.anchorMin = new Vector2(1f, 0.5f);
            setRect.anchorMax = new Vector2(1f, 0.5f);
            setRect.anchoredPosition = new Vector2(-46f, 6f);
            setRect.sizeDelta = new Vector2(52f, 56f);

            // 5. Seat Pass Banner below TopHeader (APK Btn_SeatPass: seat_pass_0 + progress_bar + progress_fill + key_0)
            var seatPassBanner = CreateSpritePanel(_homeScreenPanel.transform, "Btn_SeatPass", "game_level_bar", Color.white);
            var spRect = seatPassBanner.GetComponent<RectTransform>();
            spRect.anchorMin = new Vector2(0.5f, 1f);
            spRect.anchorMax = new Vector2(0.5f, 1f);
            spRect.anchoredPosition = new Vector2(0f, -168f);
            spRect.sizeDelta = new Vector2(460f, 78f);

            var spIcon = CreateSpritePanel(seatPassBanner.transform, "KeyIcon", "key_0", Color.white);
            spIcon.GetComponent<RectTransform>().anchoredPosition = new Vector2(-182f, 0f);
            spIcon.GetComponent<RectTransform>().sizeDelta = new Vector2(48f, 60f);

            var progBg = CreateSpritePanel(seatPassBanner.transform, "ProgressBar", "progress_bar", Color.white);
            progBg.GetComponent<RectTransform>().anchoredPosition = new Vector2(18f, -12f);
            progBg.GetComponent<RectTransform>().sizeDelta = new Vector2(280f, 32f);

            var progFill = CreateSpritePanel(progBg.transform, "ProgressFill", "progress_fill", Color.white);
            progFill.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            progFill.GetComponent<RectTransform>().sizeDelta = new Vector2(270f, 26f);

            _homeStreakTmp = CreateText(seatPassBanner.transform, "SEAT PASS · STREAK 0", 18f, FontStyles.Bold);
            _homeStreakTmp.color = ProceduralSprites.DarkOutlineColor;
            _homeStreakTmp.GetComponent<RectTransform>().anchoredPosition = new Vector2(18f, 15f);
            _homeStreakTmp.GetComponent<RectTransform>().sizeDelta = new Vector2(310f, 28f);

            // 6. Left Floating Event Column (Seat Tower & Clue Hunt)
            BuildHomeSideEventButton(_homeScreenPanel.transform, new Vector2(-296f, 20f), "home_event_button_main", "home_st_icon_0", "TOWER", OpenSeatTowerModal);
            BuildHomeSideEventButton(_homeScreenPanel.transform, new Vector2(-296f, -125f), "home_event_button_main", "nps_detective_0", "CLUE", OpenClueHuntModal);

            // 7. Right Floating Event Column (Seat Race & Pre-Level Boosters)
            BuildHomeSideEventButton(_homeScreenPanel.transform, new Vector2(296f, 20f), "home_event_button_main_level_race", "home-ic-level-race", "RACE", OpenSeatRaceModal);

            var preHintBtn = BuildHomeSideEventButton(_homeScreenPanel.transform, new Vector2(296f, -125f), "home_button_blind", "Booster_HintFindPos", "AUTO-1", () =>
            {
                if (MetaResourceManager.Instance != null)
                {
                    MetaResourceManager.Instance.PreLevelAutoSeatEnabled = !MetaResourceManager.Instance.PreLevelAutoSeatEnabled;
                    RefreshHomeUI();
                }
            });
            _preHintToggleBg = preHintBtn.GetComponent<Image>();

            var preMovesBtn = BuildHomeSideEventButton(_homeScreenPanel.transform, new Vector2(296f, -260f), "home_button_blind", "+3", "+2 MOV", () =>
            {
                if (MetaResourceManager.Instance != null)
                {
                    MetaResourceManager.Instance.PreLevelExtraMovesEnabled = !MetaResourceManager.Instance.PreLevelExtraMovesEnabled;
                    RefreshHomeUI();
                }
            });
            _preMovesToggleBg = preMovesBtn.GetComponent<Image>();

            // 8. Big Play Button + Quick Level Selector Arrows (above Bottom Nav Bar)
            var prev10Btn = CreateAPKButton(_homeScreenPanel.transform, "shared_bt-yellow", "-10", () =>
            {
                _selectedLevelToPlay = Mathf.Clamp(_selectedLevelToPlay - 10, 1, LevelDatabase.TotalLevels);
                RefreshHomeUI();
            }, 20f, ProceduralSprites.DarkOutlineColor);
            prev10Btn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-290f, -380f);
            prev10Btn.GetComponent<RectTransform>().sizeDelta = new Vector2(68f, 84f);

            var prev1Btn = CreateAPKButton(_homeScreenPanel.transform, "shared_bt-yellow", "◀", () =>
            {
                _selectedLevelToPlay = Mathf.Clamp(_selectedLevelToPlay - 1, 1, LevelDatabase.TotalLevels);
                RefreshHomeUI();
            }, 24f, ProceduralSprites.DarkOutlineColor);
            prev1Btn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-212f, -380f);
            prev1Btn.GetComponent<RectTransform>().sizeDelta = new Vector2(68f, 84f);

            var playBtn = CreateAPKButton(_homeScreenPanel.transform, "bt-green-play", "LEVEL 1", StartSelectedLevel, 30f, Color.white);
            var playRect = playBtn.GetComponent<RectTransform>();
            playRect.anchoredPosition = new Vector2(0f, -380f);
            playRect.sizeDelta = new Vector2(320f, 108f);
            _homePlayBtnImg = playBtn.GetComponent<Image>();
            _homePlayBtnTmp = playBtn.GetComponentInChildren<TextMeshProUGUI>();

            var next1Btn = CreateAPKButton(_homeScreenPanel.transform, "shared_bt-yellow", "▶", () =>
            {
                _selectedLevelToPlay = Mathf.Clamp(_selectedLevelToPlay + 1, 1, LevelDatabase.TotalLevels);
                RefreshHomeUI();
            }, 24f, ProceduralSprites.DarkOutlineColor);
            next1Btn.GetComponent<RectTransform>().anchoredPosition = new Vector2(212f, -380f);
            next1Btn.GetComponent<RectTransform>().sizeDelta = new Vector2(68f, 84f);

            var next10Btn = CreateAPKButton(_homeScreenPanel.transform, "shared_bt-yellow", "+10", () =>
            {
                _selectedLevelToPlay = Mathf.Clamp(_selectedLevelToPlay + 10, 1, LevelDatabase.TotalLevels);
                RefreshHomeUI();
            }, 20f, ProceduralSprites.DarkOutlineColor);
            next10Btn.GetComponent<RectTransform>().anchoredPosition = new Vector2(290f, -380f);
            next10Btn.GetComponent<RectTransform>().sizeDelta = new Vector2(68f, 84f);

            // 9. Bottom Navigation Bar (CanvasSetterCanvas from APK: booster_bottom_bar + selecting_bar + shop_icon / home_icon / daily_icon)
            var navBar = CreateSpritePanel(_homeScreenPanel.transform, "BottomNavBar", "booster_bottom_bar", Color.white);
            var navRect = navBar.GetComponent<RectTransform>();
            navRect.anchorMin = new Vector2(0f, 0f);
            navRect.anchorMax = new Vector2(1f, 0f);
            navRect.pivot = new Vector2(0.5f, 0f);
            navRect.anchoredPosition = Vector2.zero;
            navRect.sizeDelta = new Vector2(0f, 156f);

            var selHighlight = CreateSpritePanel(navBar.transform, "SelectingBar", "selecting_bar", Color.white);
            selHighlight.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 8f);
            selHighlight.GetComponent<RectTransform>().sizeDelta = new Vector2(230f, 146f);

            BuildBottomNavTab(navBar.transform, new Vector2(-235f, 4f), "shop_icon", "SHOP", Color.white, () =>
            {
                MetaResourceManager.Instance?.AddBooster(BoostType.Booster_Undo, 2);
                MetaResourceManager.Instance?.AddBooster(BoostType.Booster_Hint, 1);
                MetaResourceManager.Instance?.AddBooster(BoostType.Booster_SpecialItem, 1);
                ShowToast("Shop Bundle: +2 Undo, +1 Hint & +1 Magic Crown added!");
            });
            BuildBottomNavTab(navBar.transform, new Vector2(0f, 12f), "home_icon", "HOME", ProceduralSprites.DarkOutlineColor, () => { });
            BuildBottomNavTab(navBar.transform, new Vector2(235f, 4f), "daily_icon", "EVENTS", Color.white, OpenClueHuntModal);
        }

        private Button BuildHomeSideEventButton(Transform parent, Vector2 pos, string bgSprite, string iconSprite, string label, UnityEngine.Events.UnityAction onClick)
        {
            var btn = CreateAPKButton(parent, bgSprite, "", onClick);
            var rect = btn.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(102f, 128f);

            var iconGo = CreateSpritePanel(btn.transform, "Icon", iconSprite, Color.white);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchoredPosition = new Vector2(0f, 14f);
            iconRect.sizeDelta = new Vector2(72f, 72f);
            iconGo.GetComponent<Image>().preserveAspect = true;
            iconGo.GetComponent<Image>().raycastTarget = false;

            var txt = CreateText(btn.transform, label, 16f, FontStyles.Bold);
            txt.color = ProceduralSprites.DarkOutlineColor;
            var txtRect = txt.GetComponent<RectTransform>();
            txtRect.anchoredPosition = new Vector2(0f, -40f);
            txtRect.sizeDelta = new Vector2(96f, 28f);
            txt.raycastTarget = false;

            return btn;
        }

        private void BuildBottomNavTab(Transform parent, Vector2 pos, string iconSprite, string label, Color labelColor, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("NavTab_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(180f, 130f);
            var img = go.GetComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.01f);

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            var iconGo = CreateSpritePanel(go.transform, "Icon", iconSprite, Color.white);
            iconGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 16f);
            iconGo.GetComponent<RectTransform>().sizeDelta = new Vector2(72f, 68f);
            iconGo.GetComponent<Image>().preserveAspect = true;
            iconGo.GetComponent<Image>().raycastTarget = false;

            var tmp = CreateText(go.transform, label, 20f, FontStyles.Bold);
            tmp.color = labelColor;
            tmp.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -38f);
            tmp.GetComponent<RectTransform>().sizeDelta = new Vector2(160f, 32f);
            tmp.raycastTarget = false;
        }

        private void BuildGameplayHudPanel(Transform parent)
        {
            _gameplayHudPanel = new GameObject("GameplayHudPanel", typeof(RectTransform));
            _gameplayHudPanel.transform.SetParent(parent, false);
            StretchFull(_gameplayHudPanel.GetComponent<RectTransform>());

            // =================================================================
            // 1. TOP BAR (Exact APK ___Canvas___ -> TopBar layout)
            // =================================================================
            var topBar = new GameObject("TopBar", typeof(RectTransform));
            topBar.transform.SetParent(_gameplayHudPanel.transform, false);
            var topRect = topBar.GetComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0f, 1f);
            topRect.anchorMax = new Vector2(1f, 1f);
            topRect.pivot = new Vector2(0.5f, 1f);
            topRect.anchoredPosition = Vector2.zero;
            topRect.sizeDelta = new Vector2(0f, 160f);

            // 1a. Left Hanging Moves Sign (APK TurnManager: game_moves_pop 122x177)
            var movesPop = CreateSpritePanel(topBar.transform, "TurnManager", "game_moves_pop", Color.white);
            var mpRect = movesPop.GetComponent<RectTransform>();
            mpRect.anchorMin = new Vector2(0f, 1f);
            mpRect.anchorMax = new Vector2(0f, 1f);
            mpRect.pivot = new Vector2(0.5f, 1f);
            mpRect.anchoredPosition = new Vector2(82f, 4f);
            mpRect.sizeDelta = new Vector2(118f, 168f);

            var movesLbl = CreateText(movesPop.transform, "MOVES", 17f, FontStyles.Bold);
            movesLbl.color = ProceduralSprites.DarkOutlineColor;
            movesLbl.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -44f);
            movesLbl.GetComponent<RectTransform>().sizeDelta = new Vector2(104f, 28f);

            _hudMovesTmp = CreateText(movesPop.transform, "6", 44f, FontStyles.Bold);
            _hudMovesTmp.color = ProceduralSprites.DarkOutlineColor;
            _hudMovesTmp.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -104f);
            _hudMovesTmp.GetComponent<RectTransform>().sizeDelta = new Vector2(104f, 64f);

            // 1b. Center Level Pill (APK LevelBar: game_level_bar 168x58)
            var levelBar = CreateSpritePanel(topBar.transform, "LevelBar", "game_level_bar", Color.white);
            var lbRect = levelBar.GetComponent<RectTransform>();
            lbRect.anchorMin = new Vector2(0.5f, 1f);
            lbRect.anchorMax = new Vector2(0.5f, 1f);
            lbRect.pivot = new Vector2(0.5f, 0.5f);
            lbRect.anchoredPosition = new Vector2(-10f, -46f);
            lbRect.sizeDelta = new Vector2(216f, 64f);

            _hudLevelTitleTmp = CreateText(levelBar.transform, "LEVEL 1", 25f, FontStyles.Bold);
            _hudLevelTitleTmp.color = ProceduralSprites.DarkOutlineColor;
            StretchFull(_hudLevelTitleTmp.GetComponent<RectTransform>());

            // 1c. Right Action Buttons (Solve, Retry, Home/Pause)
            var solveBtn = CreateAPKButton(topBar.transform, "game_booster_pop", "SOLVE", AutoSolveCurrentLevelDemo, 15f, ProceduralSprites.DarkOutlineColor);
            var solveRect = solveBtn.GetComponent<RectTransform>();
            solveRect.anchorMin = new Vector2(1f, 1f);
            solveRect.anchorMax = new Vector2(1f, 1f);
            solveRect.anchoredPosition = new Vector2(-194f, -46f);
            solveRect.sizeDelta = new Vector2(72f, 72f);

            var retryBtn = CreateAPKButton(topBar.transform, "game_booster_pop", "RETRY", () => StartLevel(_selectedLevelToPlay), 15f, ProceduralSprites.DarkOutlineColor);
            var retryRect = retryBtn.GetComponent<RectTransform>();
            retryRect.anchorMin = new Vector2(1f, 1f);
            retryRect.anchorMax = new Vector2(1f, 1f);
            retryRect.anchoredPosition = new Vector2(-118f, -46f);
            retryRect.sizeDelta = new Vector2(72f, 72f);

            var homeBtn = CreateAPKButton(topBar.transform, "setting-home", "", ShowHomeScreen);
            var homeRect = homeBtn.GetComponent<RectTransform>();
            homeRect.anchorMin = new Vector2(1f, 1f);
            homeRect.anchorMax = new Vector2(1f, 1f);
            homeRect.anchoredPosition = new Vector2(-46f, -46f);
            homeRect.sizeDelta = new Vector2(66f, 72f);

            // 1d. Compact Passenger Quick-Switch Pills Row (lets player inspect any passenger with 1 tap)
            var tabsRow = new GameObject("PersonQuickTabs", typeof(RectTransform));
            tabsRow.transform.SetParent(topBar.transform, false);
            var tabsRect = tabsRow.GetComponent<RectTransform>();
            tabsRect.anchorMin = new Vector2(0.18f, 1f);
            tabsRect.anchorMax = new Vector2(0.98f, 1f);
            tabsRect.pivot = new Vector2(0.5f, 0.5f);
            tabsRect.anchoredPosition = new Vector2(0f, -108f);
            tabsRect.sizeDelta = new Vector2(0f, 40f);
            var tabsHlg = tabsRow.AddComponent<HorizontalLayoutGroup>();
            tabsHlg.spacing = 8f;
            tabsHlg.childAlignment = TextAnchor.MiddleCenter;
            tabsHlg.childControlWidth = false;
            tabsHlg.childControlHeight = false;
            _hudPersonTabsRoot = tabsRow.transform;

            // =================================================================
            // 2. FLOATING SPEECH BUBBLE (Exact APK PersonDialogCanvas -> DialogLayout)
            //    Uses game_dialog_2 + game_dialog_arrow, floats above selected Blob!
            // =================================================================
            var speechGo = CreateSpritePanel(_gameplayHudPanel.transform, "PersonDialogCanvas_DialogLayout", "game_dialog_2", Color.white);
            _speechBubbleRect = speechGo.GetComponent<RectTransform>();
            _speechBubbleRect.anchorMin = new Vector2(0.5f, 0.5f);
            _speechBubbleRect.anchorMax = new Vector2(0.5f, 0.5f);
            _speechBubbleRect.pivot = new Vector2(0.5f, 0.5f);
            _speechBubbleRect.anchoredPosition = new Vector2(0f, 260f);
            _speechBubbleRect.sizeDelta = new Vector2(384f, 188f);

            // Pointer Triangle Arrow at bottom of speech bubble (APKUI/game_dialog_arrow.png)
            var arrowGo = CreateSpritePanel(speechGo.transform, "Arrow", "game_dialog_arrow", Color.white);
            _speechArrowRect = arrowGo.GetComponent<RectTransform>();
            _speechArrowRect.anchorMin = new Vector2(0.5f, 0f);
            _speechArrowRect.anchorMax = new Vector2(0.5f, 0f);
            _speechArrowRect.pivot = new Vector2(0.5f, 1f);
            _speechArrowRect.anchoredPosition = new Vector2(0f, -2f);
            _speechArrowRect.sizeDelta = new Vector2(36f, 26f);

            // Caramel Header Bar inside game_dialog_2 (top 24% of card): txtName (left) & txtRole (right)
            _hudInspectorTitleTmp = CreateText(speechGo.transform, "Glia", 22f, FontStyles.Bold);
            _hudInspectorTitleTmp.alignment = TextAlignmentOptions.Left;
            _hudInspectorTitleTmp.color = ProceduralSprites.DarkOutlineColor;
            var nameHdrRect = _hudInspectorTitleTmp.GetComponent<RectTransform>();
            nameHdrRect.anchorMin = new Vector2(0.06f, 0.74f);
            nameHdrRect.anchorMax = new Vector2(0.58f, 0.96f);
            nameHdrRect.offsetMin = Vector2.zero;
            nameHdrRect.offsetMax = Vector2.zero;

            _hudInspectorRoleTmp = CreateText(speechGo.transform, "Adult", 18f, FontStyles.Bold);
            _hudInspectorRoleTmp.alignment = TextAlignmentOptions.Right;
            _hudInspectorRoleTmp.color = ProceduralSprites.DarkOutlineColor;
            var roleHdrRect = _hudInspectorRoleTmp.GetComponent<RectTransform>();
            roleHdrRect.anchorMin = new Vector2(0.52f, 0.74f);
            roleHdrRect.anchorMax = new Vector2(0.86f, 0.96f);
            roleHdrRect.offsetMin = Vector2.zero;
            roleHdrRect.offsetMax = Vector2.zero;

            // Minimize 'x' button on top-right of speech bubble
            var minBtn = CreateAPKButton(speechGo.transform, "ic-close_0", "", () =>
            {
                _speechBubbleMinimized = true;
                _speechBubbleRect.gameObject.SetActive(false);
            });
            var minRect = minBtn.GetComponent<RectTransform>();
            minRect.anchorMin = new Vector2(1f, 1f);
            minRect.anchorMax = new Vector2(1f, 1f);
            minRect.anchoredPosition = new Vector2(-22f, -22f);
            minRect.sizeDelta = new Vector2(32f, 32f);

            // Cream Body inside game_dialog_2 (bottom 72%): UIRequireElement rows
            var reqContainer = new GameObject("RequireList", typeof(RectTransform));
            reqContainer.transform.SetParent(speechGo.transform, false);
            var reqRect = reqContainer.GetComponent<RectTransform>();
            reqRect.anchorMin = new Vector2(0.05f, 0.07f);
            reqRect.anchorMax = new Vector2(0.95f, 0.71f);
            reqRect.offsetMin = Vector2.zero;
            reqRect.offsetMax = Vector2.zero;

            var reqVlg = reqContainer.AddComponent<VerticalLayoutGroup>();
            reqVlg.spacing = 6f;
            reqVlg.padding = new RectOffset(4, 4, 4, 4);
            reqVlg.childAlignment = TextAnchor.MiddleLeft;
            reqVlg.childControlWidth = true;
            reqVlg.childControlHeight = true;
            reqVlg.childForceExpandWidth = true;
            reqVlg.childForceExpandHeight = false;
            _hudRequireListRoot = reqContainer.transform;

            // =================================================================
            // 3. BOTTOM BOOSTER BAR (Exact APK ___Canvas___ -> BottomBar)
            //    Uses booster_bottom_bar + 4 game_booster_pop buttons + count_tag
            // =================================================================
            var bottomBar = CreateSpritePanel(_gameplayHudPanel.transform, "BottomBar", "booster_bottom_bar", Color.white);
            var botRect = bottomBar.GetComponent<RectTransform>();
            botRect.anchorMin = new Vector2(0f, 0f);
            botRect.anchorMax = new Vector2(1f, 0f);
            botRect.pivot = new Vector2(0.5f, 0f);
            botRect.anchoredPosition = Vector2.zero;
            botRect.sizeDelta = new Vector2(0f, 186f);

            _undoBadgeTmp = BuildAPKBoosterButton(
                bottomBar.transform,
                new Vector2(-246f, 8f),
                "ic-undo",
                new Vector2(64f, 62f),
                "3",
                () => BoostersController.Instance?.UseUndo()
            );

            _hintBadgeTmp = BuildAPKBoosterButton(
                bottomBar.transform,
                new Vector2(-82f, 8f),
                "ic-hint",
                new Vector2(44f, 74f),
                "2",
                () => BoostersController.Instance?.ActivateHintBooster(_inspectedPerson)
            );

            _crownBadgeTmp = BuildAPKBoosterButton(
                bottomBar.transform,
                new Vector2(82f, 8f),
                "ic-special",
                new Vector2(68f, 60f),
                "2",
                () => BoostersController.Instance?.ActivateSpecialBooster(_inspectedPerson)
            );

            _skipBadgeTmp = BuildAPKBoosterButton(
                bottomBar.transform,
                new Vector2(246f, 8f),
                "ic-skip",
                new Vector2(56f, 54f),
                "1",
                () => BoostersController.Instance?.UseSkipLevel()
            );
        }

        private TextMeshProUGUI BuildAPKBoosterButton(
            Transform parent,
            Vector2 anchoredPos,
            string iconSpriteName,
            Vector2 iconSize,
            string initialCount,
            UnityEngine.Events.UnityAction onClick)
        {
            var btn = CreateAPKButton(parent, "game_booster_pop", "", onClick);
            var rect = btn.GetComponent<RectTransform>();
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(116f, 122f);

            var iconGo = CreateSpritePanel(btn.transform, "Icon", iconSpriteName, Color.white);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchoredPosition = new Vector2(0f, 6f);
            iconRect.sizeDelta = iconSize;
            iconGo.GetComponent<Image>().preserveAspect = true;
            iconGo.GetComponent<Image>().raycastTarget = false;

            var tagGo = CreateSpritePanel(btn.transform, "CountTag", "count_tag", Color.white);
            var tagRect = tagGo.GetComponent<RectTransform>();
            tagRect.anchorMin = new Vector2(1f, 0f);
            tagRect.anchorMax = new Vector2(1f, 0f);
            tagRect.anchoredPosition = new Vector2(-10f, 12f);
            tagRect.sizeDelta = new Vector2(42f, 42f);
            tagGo.GetComponent<Image>().raycastTarget = false;

            var countTmp = CreateText(tagGo.transform, initialCount, 20f, FontStyles.Bold);
            countTmp.color = ProceduralSprites.DarkOutlineColor;
            StretchFull(countTmp.GetComponent<RectTransform>());
            countTmp.raycastTarget = false;

            return countTmp;
        }

        private void BuildWinModalPanel(Transform parent)
        {
            _winModalPanel = CreatePanel(parent, "WinPanelV2", new Color(0.08f, 0.05f, 0.14f, 0.86f));
            StretchFull(_winModalPanel.GetComponent<RectTransform>());

            // Spotlight behind card (APKUI/spot-light.png)
            var spotGo = CreateSpritePanel(_winModalPanel.transform, "SpotLight", "spot-light", new Color(1f, 1f, 1f, 0.45f));
            spotGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 120f);
            spotGo.GetComponent<RectTransform>().sizeDelta = new Vector2(560f, 760f);

            // Well Done Banner (APKUI/txt_wellDone.png)
            var wellDoneGo = CreateSpritePanel(_winModalPanel.transform, "TxtWellDone", "txt_wellDone", Color.white);
            wellDoneGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 355f);
            wellDoneGo.GetComponent<RectTransform>().sizeDelta = new Vector2(450f, 118f);

            // Main Cream Card (APKUI/game_dialog_2.png)
            var card = CreateSpritePanel(_winModalPanel.transform, "WinCard", "game_dialog_2", Color.white);
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchoredPosition = new Vector2(0f, -20f);
            cardRect.sizeDelta = new Vector2(610f, 540f);

            _winTitleTmp = CreateText(card.transform, "LEVEL 1 COMPLETE!", 28f, FontStyles.Bold);
            _winTitleTmp.color = ProceduralSprites.DarkOutlineColor;
            var wtRect = _winTitleTmp.GetComponent<RectTransform>();
            wtRect.anchorMin = new Vector2(0.05f, 0.76f);
            wtRect.anchorMax = new Vector2(0.95f, 0.96f);
            wtRect.offsetMin = Vector2.zero;
            wtRect.offsetMax = Vector2.zero;

            var subTmp = CreateText(card.transform, "Every passenger is seated happily!\nStop the Multiplier Gauge for bonus Coins:", 21f, FontStyles.Bold);
            subTmp.color = ProceduralSprites.DarkOutlineColor;
            subTmp.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 105f);
            subTmp.GetComponent<RectTransform>().sizeDelta = new Vector2(540f, 72f);

            // APK Reward Multiplier Gauge (APKUI/reward_multiplier.png + APKUI/arrow.png)
            var multBar = CreateSpritePanel(card.transform, "RewardMultiplierBar", "reward_multiplier", Color.white);
            var mbRect = multBar.GetComponent<RectTransform>();
            mbRect.anchoredPosition = new Vector2(0f, 12f);
            mbRect.sizeDelta = new Vector2(440f, 76f);

            var multLabels = CreateText(multBar.transform, "x2          x3          x5          x3          x2", 22f, FontStyles.Bold);
            multLabels.color = ProceduralSprites.DarkOutlineColor;
            StretchFull(multLabels.GetComponent<RectTransform>());

            var needleGo = CreateSpritePanel(multBar.transform, "NeedleArrow", "arrow", Color.white);
            _multiplierNeedleRect = needleGo.GetComponent<RectTransform>();
            _multiplierNeedleRect.anchoredPosition = new Vector2(0f, -38f);
            _multiplierNeedleRect.sizeDelta = new Vector2(40f, 38f);

            // Claim x2/x3/x5 Button (APKUI/bt-x2_reward.png)
            var claimMultBtn = CreateAPKButton(card.transform, "bt-x2_reward", "CLAIM x5 (400)", () =>
            {
                MetaResourceManager.Instance?.RecordLevelVictory(80, _currentMultiplier);
                StartLevel(MetaResourceManager.Instance.CurrentLevel);
            }, 24f, Color.white);
            claimMultBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -95f);
            claimMultBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(340f, 108f);
            _winMultiplierBtnTmp = claimMultBtn.GetComponentInChildren<TextMeshProUGUI>();

            // Continue Button (APKUI/bt-continue.png)
            var nextBtn = CreateAPKButton(card.transform, "bt-continue", "CONTINUE (+80)", () =>
            {
                MetaResourceManager.Instance?.RecordLevelVictory(80, 1);
                StartLevel(MetaResourceManager.Instance.CurrentLevel);
            }, 21f, Color.white);
            nextBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -205f);
            nextBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(260f, 92f);

            _winModalPanel.SetActive(false);
        }

        private void BuildLoseModalPanel(Transform parent)
        {
            _loseModalPanel = CreatePanel(parent, "OutOfMove", new Color(0.08f, 0.05f, 0.14f, 0.86f));
            StretchFull(_loseModalPanel.GetComponent<RectTransform>());

            var card = CreateSpritePanel(_loseModalPanel.transform, "LoseCard", "game_dialog_2", Color.white);
            card.GetComponent<RectTransform>().sizeDelta = new Vector2(580f, 560f);

            var redHdr = CreateSpritePanel(card.transform, "RedHeader", "shared_header_red", Color.white);
            redHdr.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 215f);
            redHdr.GetComponent<RectTransform>().sizeDelta = new Vector2(380f, 92f);

            var titleTmp = CreateText(redHdr.transform, "OUT OF MOVES!", 28f, FontStyles.Bold);
            titleTmp.color = Color.white;
            StretchFull(titleTmp.GetComponent<RectTransform>());

            var auraGo = CreateSpritePanel(card.transform, "Aura", "game_sfx-aura-yellow", Color.white);
            auraGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 65f);
            auraGo.GetComponent<RectTransform>().sizeDelta = new Vector2(190f, 190f);

            var moveIcon = CreateSpritePanel(auraGo.transform, "MoveIcon", "icon_move", Color.white);
            moveIcon.GetComponent<RectTransform>().sizeDelta = new Vector2(130f, 142f);

            var descTmp = CreateText(card.transform, "Need a few more moves to seat everyone?", 21f, FontStyles.Bold);
            descTmp.color = ProceduralSprites.DarkOutlineColor;
            descTmp.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -52f);
            descTmp.GetComponent<RectTransform>().sizeDelta = new Vector2(500f, 54f);

            var playOnBtn = CreateAPKButton(card.transform, "bt-green-play", "+5 MOVES (200 COINS)", () =>
            {
                if (MetaResourceManager.Instance != null && MetaResourceManager.Instance.TrySpendCoins(200))
                {
                    _loseModalPanel.SetActive(false);
                    TurnManager.Instance?.AddExtraTurn(5);
                }
                else
                {
                    ShowToast("Need 200 Coins for +5 Extra Moves!");
                }
            }, 22f, Color.white);
            playOnBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -135f);
            playOnBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(330f, 96f);

            var retryBtn = CreateAPKButton(card.transform, "bt-red", "RETRY LEVEL", () =>
            {
                MetaResourceManager.Instance?.RecordLevelDefeat();
                StartLevel(_selectedLevelToPlay);
            }, 21f, Color.white);
            retryBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -228f);
            retryBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(240f, 82f);

            _loseModalPanel.SetActive(false);
        }

        private void BuildEventModalPanel(Transform parent)
        {
            _eventModalPanel = CreatePanel(parent, "EventModalPanel", new Color(0.08f, 0.05f, 0.14f, 0.88f));
            StretchFull(_eventModalPanel.GetComponent<RectTransform>());

            var card = CreateSpritePanel(_eventModalPanel.transform, "EventCard", "game_dialog_2", Color.white);
            card.GetComponent<RectTransform>().sizeDelta = new Vector2(650f, 780f);

            _eventModalTitleTmp = CreateText(card.transform, "EVENT", 26f, FontStyles.Bold);
            _eventModalTitleTmp.color = ProceduralSprites.DarkOutlineColor;
            var etRect = _eventModalTitleTmp.GetComponent<RectTransform>();
            etRect.anchorMin = new Vector2(0.06f, 0.78f);
            etRect.anchorMax = new Vector2(0.88f, 0.96f);
            etRect.offsetMin = Vector2.zero;
            etRect.offsetMax = Vector2.zero;

            var closeBtn = CreateAPKButton(card.transform, "shared-close", "", () =>
            {
                _eventModalPanel.SetActive(false);
            });
            var closeRect = closeBtn.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-34f, -34f);
            closeRect.sizeDelta = new Vector2(54f, 54f);

            _eventModalBodyTmp = CreateText(card.transform, "", 20f, FontStyles.Bold);
            _eventModalBodyTmp.alignment = TextAlignmentOptions.TopLeft;
            _eventModalBodyTmp.color = ProceduralSprites.DarkOutlineColor;
            var bodyRect = _eventModalBodyTmp.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0.07f, 0.36f);
            bodyRect.anchorMax = new Vector2(0.93f, 0.74f);
            bodyRect.offsetMin = Vector2.zero;
            bodyRect.offsetMax = Vector2.zero;

            var choicesGo = new GameObject("ChoicesContainer", typeof(RectTransform));
            choicesGo.transform.SetParent(card.transform, false);
            var chRect = choicesGo.GetComponent<RectTransform>();
            chRect.anchorMin = new Vector2(0.08f, 0.05f);
            chRect.anchorMax = new Vector2(0.92f, 0.34f);
            chRect.offsetMin = Vector2.zero;
            chRect.offsetMax = Vector2.zero;

            var chVlg = choicesGo.AddComponent<VerticalLayoutGroup>();
            chVlg.spacing = 10f;
            chVlg.childControlWidth = true;
            chVlg.childControlHeight = true;
            _eventChoicesRoot = choicesGo.transform;

            _eventModalPanel.SetActive(false);
        }

        private void BuildToastOverlay(Transform parent)
        {
            _toastObj = CreateSpritePanel(parent, "ToastNotification", "game_level_bar", Color.white);
            var rect = _toastObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.08f, 0.16f);
            rect.anchorMax = new Vector2(0.92f, 0.22f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _toastTmp = CreateText(_toastObj.transform, "", 20f, FontStyles.Bold);
            _toastTmp.color = ProceduralSprites.DarkOutlineColor;
            StretchFull(_toastTmp.GetComponent<RectTransform>());
            _toastObj.SetActive(false);
        }

        // =====================================================================
        // UI HELPER METHODS
        // =====================================================================

        private GameObject CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            return go;
        }

        private GameObject CreateSpritePanel(Transform parent, string name, string apkSpriteName, Color tint)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = ProceduralSprites.LoadAPKSprite(apkSpriteName) ?? ProceduralSprites.GetRoundedRect(128, 128, 20f);
            img.color = tint;
            return go;
        }

        private TextMeshProUGUI CreateText(Transform parent, string text, float fontSize, FontStyles style)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            ProceduralSprites.ApplyGameFont(tmp);
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = ProceduralSprites.DarkOutlineColor;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            return tmp;
        }

        private Button CreateAPKButton(
            Transform parent,
            string apkSpriteName,
            string label,
            UnityEngine.Events.UnityAction onClick,
            float fontSize = 21f,
            Color? textColor = null)
        {
            var go = new GameObject("Btn_" + (string.IsNullOrEmpty(label) ? apkSpriteName : label), typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = ProceduralSprites.LoadAPKSprite(apkSpriteName) ?? ProceduralSprites.GetRoundedRect(128, 64, 18f);
            img.color = Color.white;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            if (onClick != null) btn.onClick.AddListener(onClick);

            if (!string.IsNullOrEmpty(label))
            {
                var tmp = CreateText(go.transform, label, fontSize, FontStyles.Bold);
                tmp.color = textColor ?? Color.white;
                StretchFull(tmp.GetComponent<RectTransform>());
                tmp.raycastTarget = false;
            }
            return btn;
        }

        private void ClearChildren(Transform root)
        {
            if (root == null) return;
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Destroy(root.GetChild(i).gameObject);
            }
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
