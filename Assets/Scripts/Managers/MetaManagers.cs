using System;
using System.Collections.Generic;
using UnityEngine;

namespace WheresMySeat
{
    public class MetaResourceManager : MonoBehaviour
    {
        public static MetaResourceManager Instance { get; private set; }

        public GameFlowData GameFlow { get; private set; }
        public CoinPricesData CoinPrices { get; private set; }
        public SeatTowerConfig SeatTowerConfig { get; private set; }
        public SeatRaceData SeatRaceConfig { get; private set; }
        public List<ClueHuntLevel> ClueHuntLevels { get; private set; } = new List<ClueHuntLevel>();

        public int Coins { get; private set; }
        public int Tickets { get; private set; }
        public int MaxTickets { get; private set; } = 5;
        public int CurrentLevel { get; private set; } = 1;
        public int WinStreak { get; private set; } = 0;

        // Live event states
        public int SeatTowerStep { get; private set; } = 0;
        public int SeatTowerRemainingPlayers { get; private set; } = 100;
        public int LevelRacePlayerScore { get; private set; } = 0;
        public int[] LevelRaceBotScores { get; private set; } = new int[] { 2, 4, 1, 3 };
        public string[] LevelRaceBotNames { get; private set; } = new string[] { "Luna_99", "BlobKing", "SeatMaster", "Quizzy" };
        public int ClueHuntIndex { get; private set; } = 0;

        // Pre-level booster toggles
        public bool PreLevelAutoSeatEnabled { get; set; }
        public bool PreLevelExtraMovesEnabled { get; set; }

        private readonly Dictionary<BoostType, int> _boosterCounts = new Dictionary<BoostType, int>();

        public event Action OnResourcesChanged;

        private const string PrefCoins = "WMS_Coins";
        private const string PrefTickets = "WMS_Tickets";
        private const string PrefLevel = "WMS_CurrentLevel";
        private const string PrefStreak = "WMS_WinStreak";
        private const string PrefTowerStep = "WMS_TowerStep";
        private const string PrefTowerPlayers = "WMS_TowerPlayers";
        private const string PrefRaceScore = "WMS_RaceScore";
        private const string PrefClueHuntIdx = "WMS_ClueHuntIdx";

        private void Awake()
        {
            Instance = this;
            LoadJsonConfigs();
            LoadPlayerState();
        }

        private void LoadJsonConfigs()
        {
            GameFlow = LoadJson<GameFlowData>("JsonData/game_flow") ?? new GameFlowData();
            CoinPrices = LoadJson<CoinPricesData>("JsonData/coin_prices") ?? new CoinPricesData();
            SeatTowerConfig = LoadJson<SeatTowerConfig>("JsonData/seat_tower") ?? new SeatTowerConfig();
            SeatRaceConfig = LoadJson<SeatRaceData>("JsonData/seat_race") ?? new SeatRaceData();

            TextAsset clueAsset = Resources.Load<TextAsset>("JsonData/clue_hunt_json");
            if (clueAsset != null && !string.IsNullOrEmpty(clueAsset.text))
            {
                var wrapper = JsonUtility.FromJson<ClueHuntRootWrapper>(clueAsset.text);
                if (wrapper != null && wrapper.clue_hunt_config != null && wrapper.clue_hunt_config.levels != null)
                {
                    ClueHuntLevels = wrapper.clue_hunt_config.levels;
                }
            }
        }

        private T LoadJson<T>(string resourcePath) where T : class
        {
            TextAsset asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null || string.IsNullOrEmpty(asset.text)) return null;
            try
            {
                return JsonUtility.FromJson<T>(asset.text);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to parse {resourcePath}: {e.Message}");
                return null;
            }
        }

        private void LoadPlayerState()
        {
            bool firstLaunch = !PlayerPrefs.HasKey(PrefCoins);
            if (firstLaunch)
            {
                Coins = 1200; // Generous starting coins + initial resources
                Tickets = 5;
                CurrentLevel = 1;
                WinStreak = 0;
                SeatTowerStep = 0;
                SeatTowerRemainingPlayers = SeatTowerConfig != null ? SeatTowerConfig.initialPlayerCount : 100;
                LevelRacePlayerScore = 1;
                ClueHuntIndex = 0;

                _boosterCounts[BoostType.Booster_Undo] = 3;
                _boosterCounts[BoostType.Booster_Hint] = 2;
                _boosterCounts[BoostType.Booster_SpecialItem] = 2;
                _boosterCounts[BoostType.Booster_SkipLevel] = 1;
                _boosterCounts[BoostType.Booster_PreLevelHint] = 2;
                _boosterCounts[BoostType.Booster_PreLevelAddMoves] = 2;
                SavePlayerState();
            }
            else
            {
                Coins = PlayerPrefs.GetInt(PrefCoins, 1200);
                Tickets = PlayerPrefs.GetInt(PrefTickets, 5);
                CurrentLevel = Mathf.Clamp(PlayerPrefs.GetInt(PrefLevel, 1), 1, 259);
                WinStreak = PlayerPrefs.GetInt(PrefStreak, 0);
                SeatTowerStep = PlayerPrefs.GetInt(PrefTowerStep, 0);
                SeatTowerRemainingPlayers = PlayerPrefs.GetInt(PrefTowerPlayers, 100);
                LevelRacePlayerScore = PlayerPrefs.GetInt(PrefRaceScore, 1);
                ClueHuntIndex = PlayerPrefs.GetInt(PrefClueHuntIdx, 0);

                foreach (BoostType bt in Enum.GetValues(typeof(BoostType)))
                {
                    _boosterCounts[bt] = PlayerPrefs.GetInt("WMS_Boost_" + bt, 2);
                }
            }
        }

        public void SavePlayerState()
        {
            PlayerPrefs.SetInt(PrefCoins, Coins);
            PlayerPrefs.SetInt(PrefTickets, Tickets);
            PlayerPrefs.SetInt(PrefLevel, CurrentLevel);
            PlayerPrefs.SetInt(PrefStreak, WinStreak);
            PlayerPrefs.SetInt(PrefTowerStep, SeatTowerStep);
            PlayerPrefs.SetInt(PrefTowerPlayers, SeatTowerRemainingPlayers);
            PlayerPrefs.SetInt(PrefRaceScore, LevelRacePlayerScore);
            PlayerPrefs.SetInt(PrefClueHuntIdx, ClueHuntIndex);

            foreach (var kv in _boosterCounts)
            {
                PlayerPrefs.SetInt("WMS_Boost_" + kv.Key, kv.Value);
            }
            PlayerPrefs.Save();
            OnResourcesChanged?.Invoke();
        }

        public int GetBoosterCount(BoostType type)
        {
            return _boosterCounts.TryGetValue(type, out int c) ? c : 0;
        }

        public void AddBooster(BoostType type, int amount)
        {
            _boosterCounts[type] = GetBoosterCount(type) + amount;
            SavePlayerState();
        }

        public bool TrySpendBoosterOrCoins(BoostType type, int fallbackCoinPrice)
        {
            int count = GetBoosterCount(type);
            if (count > 0)
            {
                _boosterCounts[type] = count - 1;
                SavePlayerState();
                return true;
            }

            if (Coins >= fallbackCoinPrice)
            {
                Coins -= fallbackCoinPrice;
                SavePlayerState();
                return true;
            }
            return false;
        }

        public void AddCoins(int amount)
        {
            Coins = Mathf.Max(0, Coins + amount);
            SavePlayerState();
        }

        public bool TrySpendCoins(int amount)
        {
            if (Coins < amount) return false;
            Coins -= amount;
            SavePlayerState();
            return true;
        }

        public void AddTickets(int amount)
        {
            Tickets = Mathf.Clamp(Tickets + amount, 0, MaxTickets);
            SavePlayerState();
        }

        public void SetCurrentLevel(int levelNumber)
        {
            CurrentLevel = Mathf.Clamp(levelNumber, 1, 259);
            SavePlayerState();
        }

        public void RecordLevelVictory(int baseCoinReward, int multiplier)
        {
            int totalEarned = baseCoinReward * Mathf.Max(1, multiplier);
            Coins += totalEarned;
            Tickets = Mathf.Clamp(Tickets + 1, 0, MaxTickets);
            WinStreak++;
            CurrentLevel = Mathf.Clamp(CurrentLevel + 1, 1, 259);

            // Advance Seat Tower
            int totalSteps = SeatTowerConfig != null ? SeatTowerConfig.totalSteps : 7;
            SeatTowerStep++;
            if (SeatTowerConfig != null && SeatTowerConfig.playerReductionRanges != null && SeatTowerStep - 1 < SeatTowerConfig.playerReductionRanges.Count)
            {
                var r = SeatTowerConfig.playerReductionRanges[SeatTowerStep - 1];
                SeatTowerRemainingPlayers = UnityEngine.Random.Range(r.minPlayers, r.maxPlayers + 1);
            }
            else
            {
                SeatTowerRemainingPlayers = Mathf.Max(5, SeatTowerRemainingPlayers - UnityEngine.Random.Range(8, 16));
            }

            if (SeatTowerStep >= totalSteps)
            {
                int grandPrize = SeatTowerConfig != null ? SeatTowerConfig.grandPrizeCoins : 1000;
                int share = Mathf.Max(150, grandPrize / Mathf.Max(1, SeatTowerRemainingPlayers));
                Coins += share;
                SeatTowerStep = 0;
                SeatTowerRemainingPlayers = SeatTowerConfig != null ? SeatTowerConfig.initialPlayerCount : 100;
            }

            // Advance Level Race
            int raceTarget = SeatRaceConfig != null ? SeatRaceConfig.NumberLevelTarget : 10;
            LevelRacePlayerScore++;
            for (int i = 0; i < LevelRaceBotScores.Length; i++)
            {
                if (UnityEngine.Random.value < 0.45f)
                    LevelRaceBotScores[i] = Mathf.Min(raceTarget - 1, LevelRaceBotScores[i] + 1);
            }
            if (LevelRacePlayerScore >= raceTarget)
            {
                Coins += 500;
                AddBooster(BoostType.Booster_Hint, 1);
                AddBooster(BoostType.Booster_SpecialItem, 1);
                LevelRacePlayerScore = 0;
                for (int i = 0; i < LevelRaceBotScores.Length; i++)
                    LevelRaceBotScores[i] = UnityEngine.Random.Range(0, 3);
            }

            SavePlayerState();
        }

        public void RecordLevelDefeat()
        {
            WinStreak = 0;
            SeatTowerStep = 0;
            SeatTowerRemainingPlayers = SeatTowerConfig != null ? SeatTowerConfig.initialPlayerCount : 100;
            Tickets = Mathf.Max(1, Tickets - 1);
            SavePlayerState();
        }

        public void CompleteCurrentClueHunt()
        {
            Coins += 150;
            AddBooster(BoostType.Booster_Hint, 1);
            if (ClueHuntLevels.Count > 0)
            {
                ClueHuntIndex = (ClueHuntIndex + 1) % ClueHuntLevels.Count;
            }
            SavePlayerState();
        }
    }
}
