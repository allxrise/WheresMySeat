using System;
using System.Collections.Generic;
using UnityEngine;

namespace WheresMySeat
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState CurrentGameState { get; private set; } = GameState.Playing;
        public GameMode CurrentGameMode { get; set; } = GameMode.Moves;
        public GamePlayMode CurrentGamePlayMode { get; set; } = GamePlayMode.Normal;

        public event Action<GameState> OnGameStateChanged;
        public event Action OnWinGame;
        public event Action OnGameOver;

        private void Awake()
        {
            Instance = this;
        }

        public void SetGameState(GameState newState)
        {
            if (CurrentGameState == newState) return;
            CurrentGameState = newState;
            OnGameStateChanged?.Invoke(CurrentGameState);
        }

        public bool IsGameState(GameState state)
        {
            return CurrentGameState == state;
        }

        public void SetWinGame()
        {
            if (CurrentGameState == GameState.Win) return;
            SetGameState(GameState.Win);
            OnWinGame?.Invoke();
        }

        public void SetGameOver()
        {
            if (CurrentGameState == GameState.Lose) return;
            SetGameState(GameState.Lose);
            OnGameOver?.Invoke();
        }
    }

    public class PlacementManager : MonoBehaviour
    {
        public static PlacementManager Instance { get; private set; }

        public float distanceBetweenSeatToCheck = 1.65f;
        public float horizontalRange = 1.65f;
        public float verticalRange = 1.55f;
        public float sameRowXTolerance = 8.0f;
        public float sameColYTolerance = 0.45f;

        private readonly List<SeatSlot> _seatSlots = new List<SeatSlot>();
        private readonly List<StartSlot> _startSlots = new List<StartSlot>();
        private readonly List<Person> _persons = new List<Person>();
        private readonly List<OnOffItem> _onOffItems = new List<OnOffItem>();

        public List<Person> Persons => _persons;

        private void Awake()
        {
            Instance = this;
        }

        public void ClearLevelObjects()
        {
            foreach (var p in _persons)
            {
                if (p != null)
                {
                    p.PlacementChanged -= OnPersonPlacementChanged;
                }
            }
            _seatSlots.Clear();
            _startSlots.Clear();
            _persons.Clear();
            _onOffItems.Clear();
            PersonDragDrop.ClearUndoHistory();
        }

        public void RegisterSeat(SeatSlot seat)
        {
            if (seat != null && !_seatSlots.Contains(seat))
                _seatSlots.Add(seat);
        }

        public void RegisterStartSlot(StartSlot slot)
        {
            if (slot != null && !_startSlots.Contains(slot))
                _startSlots.Add(slot);
        }

        public void RegisterPerson(Person person)
        {
            if (person != null && !_persons.Contains(person))
            {
                _persons.Add(person);
                person.PlacementChanged += OnPersonPlacementChanged;
            }
        }

        public void RegisterOnOffItem(OnOffItem item)
        {
            if (item != null && !_onOffItems.Contains(item))
                _onOffItems.Add(item);
        }

        private void OnPersonPlacementChanged(Person person)
        {
            if (LevelCompletionManager.Instance != null)
            {
                LevelCompletionManager.Instance.ForceValidateAllPerson();
            }
        }

        public List<SeatSlot> GetAllSeatSlots() => _seatSlots;
        public List<StartSlot> GetAllStartSlots() => _startSlots;
        public List<OnOffItem> GetAllOnOffItems() => _onOffItems;

        public PlacementContext BuildContext(Person person)
        {
            var allSeats = new List<ISlot>(_seatSlots.Count);
            for (int i = 0; i < _seatSlots.Count; i++)
                allSeats.Add(_seatSlots[i]);

            var allPeople = new List<IPerson>(_persons.Count);
            for (int i = 0; i < _persons.Count; i++)
                allPeople.Add(_persons[i]);

            return new PlacementContext
            {
                Person = person,
                Seat = person != null ? person.CurrentSlot : null,
                AllSeats = allSeats,
                AllPersons = allPeople,
                AllOnOffItems = _onOffItems,
                DistanceBetweenSeatToCheck = distanceBetweenSeatToCheck,
                HorizontalRange = horizontalRange,
                VerticalRange = verticalRange,
                SameRowXTolerance = sameRowXTolerance,
                SameColYTolerance = sameColYTolerance
            };
        }

        public bool PlacePersonToHint(Person person)
        {
            if (person == null || person.hintSlot == null) return false;
            SeatSlot targetSeat = person.hintSlot;

            // Clear previous hint rings
            foreach (var s in _seatSlots)
            {
                if (s != null) s.ShowHintHighlight(false);
            }
            targetSeat.ShowHintHighlight(true);

            Person existing = targetSeat.Occupant;
            ISlot prevSlot = person.CurrentSlot;

            if (existing != null && existing != person)
            {
                existing.SnapDirect(prevSlot);
                person.SnapDirect(targetSeat);
            }
            else
            {
                person.SnapDirect(targetSeat);
            }

            SyncLampForPersonIfRequired(person, targetSeat);

            if (LevelCompletionManager.Instance != null)
            {
                LevelCompletionManager.Instance.ForceValidateAllPerson();
            }
            return true;
        }

        public void SyncLampForPersonIfRequired(Person person, SeatSlot seat)
        {
            if (person == null || seat == null || person.Requirements == null) return;
            for (int i = 0; i < person.Requirements.Count; i++)
            {
                if (person.Requirements[i] is SitWithOnOffItem lampReq)
                {
                    for (int j = 0; j < _onOffItems.Count; j++)
                    {
                        if (_onOffItems[j] != null && _onOffItems[j].IndexOfSeat == seat.IndexOfSeat)
                        {
                            if (lampReq.requireOn) _onOffItems[j].SwitchOn();
                            else _onOffItems[j].SwitchOff();
                        }
                    }
                }
            }
        }

        public int ApplyStartupHintBoosterRandomN(int count)
        {
            int placed = 0;
            var candidates = new List<Person>();
            foreach (var p in _persons)
            {
                if (p != null && p.hintSlot != null && p.CurrentSlot != p.hintSlot)
                    candidates.Add(p);
            }

            for (int i = 0; i < count && candidates.Count > 0; i++)
            {
                int idx = UnityEngine.Random.Range(0, candidates.Count);
                Person chosen = candidates[idx];
                candidates.RemoveAt(idx);
                if (PlacePersonToHint(chosen))
                    placed++;
            }
            return placed;
        }
    }

    public class LevelCompletionManager : MonoBehaviour
    {
        public static LevelCompletionManager Instance { get; private set; }

        public event Action OnValidationRefreshed;

        private void Awake()
        {
            Instance = this;
        }

        public RuleResult EvaluateRequirement(Person person, PersonRequirement req)
        {
            if (person == null || req == null || PlacementManager.Instance == null)
                return RuleResult.Pass();

            if (person.isUseSpecialBoster)
                return RuleResult.Pass();

            if (person.CurrentSlot == null || !person.CurrentSlot.IsSeatSlot)
                return RuleResult.Fail("Not seated yet.");

            var ctx = PlacementManager.Instance.BuildContext(person);
            return req.Validate(ctx);
        }

        public void EvaluatePerson(Person person)
        {
            if (person == null || PlacementManager.Instance == null) return;

            var ctx = PlacementManager.Instance.BuildContext(person);
            RuleResult res = SeatRuleEngine.Validate(ctx);
            person.SetPlacementValidity(res.IsValid, res.Reason);
        }

        public void ForceValidateAllPerson()
        {
            if (PlacementManager.Instance == null) return;

            var persons = PlacementManager.Instance.Persons;
            for (int i = 0; i < persons.Count; i++)
            {
                EvaluatePerson(persons[i]);
            }

            OnValidationRefreshed?.Invoke();
            CheckCompletion();
        }

        public bool CheckIfAllPersonAtSeat()
        {
            if (PlacementManager.Instance == null) return false;
            var persons = PlacementManager.Instance.Persons;
            if (persons.Count == 0) return false;

            for (int i = 0; i < persons.Count; i++)
            {
                var p = persons[i];
                if (p == null || p.CurrentSlot == null || !p.CurrentSlot.IsSeatSlot)
                    return false;
            }
            return true;
        }

        public bool AreAllPersonsValid()
        {
            if (!CheckIfAllPersonAtSeat()) return false;
            var persons = PlacementManager.Instance.Persons;
            for (int i = 0; i < persons.Count; i++)
            {
                if (!persons[i].IsPlacementValid)
                    return false;
            }
            return true;
        }

        public void CheckCompletion()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsGameState(GameState.Playing))
                return;

            if (AreAllPersonsValid())
            {
                GameManager.Instance.SetWinGame();
            }
        }
    }

    public class TurnManager : MonoBehaviour
    {
        public static TurnManager Instance { get; private set; }

        public int currentTurn;
        public int maxTurn;

        public event Action<int> OnTurnChanged;
        public event Action OnOutOfTurn;

        private void Awake()
        {
            Instance = this;
        }

        public void Init(int moves)
        {
            maxTurn = Mathf.Max(1, moves);
            currentTurn = maxTurn;
            OnTurnChanged?.Invoke(currentTurn);
        }

        public void UseTurn()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentGameMode != GameMode.Moves)
                return;

            currentTurn = Mathf.Max(0, currentTurn - 1);
            OnTurnChanged?.Invoke(currentTurn);

            if (LevelCompletionManager.Instance != null)
            {
                LevelCompletionManager.Instance.ForceValidateAllPerson();
                if (LevelCompletionManager.Instance.AreAllPersonsValid())
                    return;
            }

            if (currentTurn <= 0 && GameManager.Instance != null && GameManager.Instance.IsGameState(GameState.Playing))
            {
                OnOutOfTurn?.Invoke();
                GameManager.Instance.SetGameOver();
            }
        }

        public void AddExtraTurn(int count)
        {
            currentTurn += Mathf.Max(1, count);
            OnTurnChanged?.Invoke(currentTurn);
            if (GameManager.Instance != null && GameManager.Instance.IsGameState(GameState.Lose))
            {
                GameManager.Instance.SetGameState(GameState.Playing);
            }
        }
    }

    public class GameTimeManager : MonoBehaviour
    {
        public static GameTimeManager Instance { get; private set; }

        public float remainingSeconds;
        public bool isRunning;
        public event Action<float> OnTimerUpdated;

        private void Awake()
        {
            Instance = this;
        }

        public void StartTimer(float seconds)
        {
            remainingSeconds = seconds;
            isRunning = seconds > 0f;
        }

        public void StopTimer()
        {
            isRunning = false;
        }

        private void Update()
        {
            if (!isRunning) return;
            if (GameManager.Instance == null || !GameManager.Instance.IsGameState(GameState.Playing)) return;
            if (GameManager.Instance.CurrentGameMode != GameMode.Timer) return;

            remainingSeconds -= Time.deltaTime;
            OnTimerUpdated?.Invoke(Mathf.Max(0f, remainingSeconds));

            if (remainingSeconds <= 0f)
            {
                isRunning = false;
                if (LevelCompletionManager.Instance != null && LevelCompletionManager.Instance.AreAllPersonsValid())
                {
                    GameManager.Instance.SetWinGame();
                }
                else
                {
                    GameManager.Instance.SetGameOver();
                }
            }
        }
    }

    public class BoostersController : MonoBehaviour
    {
        public static BoostersController Instance { get; private set; }

        public BoostType? PendingTargetedBooster { get; private set; }
        public event Action<BoostType?> OnPendingBoosterChanged;

        private void Awake()
        {
            Instance = this;
        }

        public void CancelPendingBooster()
        {
            PendingTargetedBooster = null;
            OnPendingBoosterChanged?.Invoke(null);
        }

        public bool UseUndo()
        {
            if (!PersonDragDrop.CanUndo)
            {
                if (WheresMySeatGameController.Instance != null)
                    WheresMySeatGameController.Instance.ShowToast("Nothing to undo yet!");
                return false;
            }

            if (!MetaResourceManager.Instance.TrySpendBoosterOrCoins(BoostType.Booster_Undo, 50))
            {
                if (WheresMySeatGameController.Instance != null)
                    WheresMySeatGameController.Instance.ShowToast("Not enough Coins for Undo (50 Coins)!");
                return false;
            }

            bool undone = PersonDragDrop.UndoLastPlacement();
            if (undone && TurnManager.Instance != null)
            {
                TurnManager.Instance.AddExtraTurn(1);
                if (WheresMySeatGameController.Instance != null)
                    WheresMySeatGameController.Instance.ShowToast("Move undone (+1 Move restored)!");
            }
            return undone;
        }

        public void ActivateHintBooster(Person currentlySelectedPerson)
        {
            // If a person is already selected and not in their hint seat, place them immediately
            if (currentlySelectedPerson != null && currentlySelectedPerson.hintSlot != null && currentlySelectedPerson.CurrentSlot != currentlySelectedPerson.hintSlot)
            {
                if (!MetaResourceManager.Instance.TrySpendBoosterOrCoins(BoostType.Booster_Hint, 300))
                {
                    WheresMySeatGameController.Instance?.ShowToast("Need 300 Coins for Find Seat Hint!");
                    return;
                }
                PlacementManager.Instance.PlacePersonToHint(currentlySelectedPerson);
                WheresMySeatGameController.Instance?.InspectPerson(currentlySelectedPerson);
                WheresMySeatGameController.Instance?.ShowToast($"💡 Placed {currentlySelectedPerson.DisplayName} in their true seat!");
                return;
            }

            // Otherwise pick the first unseated or invalidly seated person and place them!
            if (PlacementManager.Instance != null)
            {
                foreach (var p in PlacementManager.Instance.Persons)
                {
                    if (p != null && p.hintSlot != null && p.CurrentSlot != p.hintSlot)
                    {
                        if (!MetaResourceManager.Instance.TrySpendBoosterOrCoins(BoostType.Booster_Hint, 300))
                        {
                            WheresMySeatGameController.Instance?.ShowToast("Need 300 Coins for Find Seat Hint!");
                            return;
                        }
                        PlacementManager.Instance.PlacePersonToHint(p);
                        WheresMySeatGameController.Instance?.InspectPerson(p);
                        WheresMySeatGameController.Instance?.ShowToast($"💡 Placed {p.DisplayName} in their true seat!");
                        return;
                    }
                }
            }

            WheresMySeatGameController.Instance?.ShowToast("Everyone is already in their ideal seat!");
        }

        public void ActivateSpecialBooster(Person currentlySelectedPerson)
        {
            if (currentlySelectedPerson != null && !currentlySelectedPerson.isUseSpecialBoster)
            {
                if (!MetaResourceManager.Instance.TrySpendBoosterOrCoins(BoostType.Booster_SpecialItem, 400))
                {
                    WheresMySeatGameController.Instance?.ShowToast("Need 400 Coins for Magic Crown Booster!");
                    return;
                }
                currentlySelectedPerson.SetUseSpecialBoster();
                WheresMySeatGameController.Instance?.InspectPerson(currentlySelectedPerson);
                WheresMySeatGameController.Instance?.ShowToast($"👑 {currentlySelectedPerson.DisplayName} now sits happily anywhere!");
                return;
            }

            if (PendingTargetedBooster == BoostType.Booster_SpecialItem)
            {
                CancelPendingBooster();
                return;
            }

            PendingTargetedBooster = BoostType.Booster_SpecialItem;
            OnPendingBoosterChanged?.Invoke(PendingTargetedBooster);
            WheresMySeatGameController.Instance?.ShowToast("👑 Tap any character to give them the Magic Crown!");
        }

        public bool TryConsumeTargetedBoosterOnPerson(Person person)
        {
            if (PendingTargetedBooster == null || person == null)
                return false;

            if (PendingTargetedBooster == BoostType.Booster_SpecialItem)
            {
                if (person.isUseSpecialBoster)
                {
                    WheresMySeatGameController.Instance?.ShowToast($"{person.DisplayName} already has the Magic Crown!");
                    CancelPendingBooster();
                    return true;
                }

                if (MetaResourceManager.Instance.TrySpendBoosterOrCoins(BoostType.Booster_SpecialItem, 400))
                {
                    person.SetUseSpecialBoster();
                    WheresMySeatGameController.Instance?.InspectPerson(person);
                    WheresMySeatGameController.Instance?.ShowToast($"👑 {person.DisplayName} now sits happily anywhere!");
                }
                else
                {
                    WheresMySeatGameController.Instance?.ShowToast("Need 400 Coins for Magic Crown Booster!");
                }
                CancelPendingBooster();
                return true;
            }

            CancelPendingBooster();
            return false;
        }

        public void UseSkipLevel()
        {
            if (!MetaResourceManager.Instance.TrySpendBoosterOrCoins(BoostType.Booster_SkipLevel, 500))
            {
                WheresMySeatGameController.Instance?.ShowToast("Need 500 Coins or a Skip Ticket to skip level!");
                return;
            }

            WheresMySeatGameController.Instance?.ShowToast("⏭ Level Skipped!");
            GameManager.Instance?.SetWinGame();
        }
    }
}
