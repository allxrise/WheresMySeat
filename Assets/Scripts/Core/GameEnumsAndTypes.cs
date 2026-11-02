using System;
using System.Collections.Generic;
using UnityEngine;

namespace WheresMySeat
{
    public enum SceneType
    {
        Game = 0
    }

    public enum LevelDifficulty
    {
        Easy = 0,
        Hard = 1,
        VeryHard = 2
    }

    public enum MapID
    {
        USA = 0,
        England = 1,
        France = 2,
        Brazil = 3,
        India = 4,
        Japan = 5,
        Italy = 6,
        Germany = 7,
        Australia = 8
    }

    public enum GameState
    {
        Playing = 0,
        Paused = 1,
        GameOver = 2,
        Victory = 3,
        Lose = GameOver,
        Win = Victory
    }

    public enum GameMode
    {
        TimeCount = 0,
        Moves = 1,
        Timer = TimeCount
    }

    public enum GamePlayMode
    {
        Normal = 0,
        DailyChallenge = 1
    }

    public enum BoostType
    {
        Booster_Undo = 0,
        Booster_HintFindPos = 1,
        Booster_SpecialItem = 2,
        Booster_Freeze = 3,
        Booster_Skip = 4,
        Booster_Mega_AutoSeat = 5,
        Booster_Mega_ExtraMove = 6,
        Booster_Mega_Add_Time = 7,
        Revive = 8,
        Shield = 9,
        Booster_Hint = Booster_HintFindPos,
        Booster_SkipLevel = Booster_Skip,
        Booster_PreLevelHint = Booster_Mega_AutoSeat,
        Booster_PreLevelAddMoves = Booster_Mega_ExtraMove
    }

    public enum SimpleTraitType
    {
        Window = 0,
        Same_Direction = 1,
        Opposite_Direction = 2,
        Row_1 = 3,
        Row_2 = 4,
        Row_3 = 5,
        Row_4 = 6,
        Center = 7,
        Dirty = 8,
        Driver = 9,
        Front = 10,
        Back = 11,
        Stand = 12,
        Aisle = 13,
        Floor_1 = 14,
        Floor_2 = 15,
        Stool = 16,
        Table = 17,
        Outside = 18,
        Single = 19,
        Water = 20,
        View_River = 21,
        Pollen = 22,
        Cold = 23,
        Hot = 24,
        Chef = 25,
        Private = 26,
        GeneralCondition_1 = 27,
        GeneralCondition_2 = 28,
        GeneralCondition_3 = 29,
        GeneralCondition_4 = 30,
        King = 31,
        Left = 32,
        Right = 33,
        End = 34,
        Inside = 35,
        Middle = 36,
        Row_5 = 37
    }

    public enum SeatDirectionCheck
    {
        Circle = 0,
        Horizontal = 1,
        Vertical = 2,
        HorizontalRight = 3,
        HorizontalLeft = 4,
        VerticalUp = 5,
        VerticalDown = 6,
        HorizontalAndVertical = 7,
        SameRow = 8,
        SameColumn = 9,
        Opposite = 10,
        Around = Circle,
        NextTo_Horizontal = Horizontal,
        Right = HorizontalRight,
        Left = HorizontalLeft,
        Front = VerticalUp,
        Behind = VerticalDown
    }

    public enum SeatPosture
    {
        Sitting = 0,
        Standing = 1,
        Unknown = 2
    }

    public enum RoleType
    {
        Adult = 0,
        Kid = 1,
        Item = 2
    }

    public enum PersonFaceAnimation
    {
        Unknown = 0,
        Face_normal = 1,
        Face_happy = 2,
        Face_sad = 3,
        Face_angry = 4,
        Face_drag = 5,
        Face_confuse = 6,
        Face_wow = 7,
        Face_Sing_happy = 8,
        Face_sleep = 9,
        Face_talking = 10,
        Face_Phone_talk = 11
    }

    public enum PersonBodyAnimation
    {
        Body_down = 0,
        Body_drag = 1,
        Body_happy = 2,
        Body_stand_idle = 3,
        Body_sit_idle = 4,
        Body_sitdown = 5,
        Body_stand_up = 6
    }

    [Serializable]
    public struct RuleResult
    {
        public bool Ok;
        public string Reason;
        public ISlot slot;

        public bool IsValid => Ok;

        public static RuleResult Success(ISlot s = null)
        {
            return new RuleResult { Ok = true, Reason = string.Empty, slot = s };
        }

        public static RuleResult Pass(ISlot s = null)
        {
            return Success(s);
        }

        public static RuleResult Fail(string reason, ISlot s = null)
        {
            return new RuleResult { Ok = false, Reason = reason, slot = s };
        }
    }

    [Serializable]
    public class HintEntry
    {
        public string personName;
        public int seatNamesOrdered;
    }

    [Serializable]
    public class HintSeatMapping
    {
        public List<HintEntry> hintEntry = new List<HintEntry>();
    }

    [Serializable]
    public class ExtractedLevelInfo
    {
        public string name;
        public int levelNumber;
        public int difficulty;
        public float maxCameraSize = 6.5f;
        public float distanceBetweenSeatToCheck = 1.2f;
        public float horizontalRange = 1.6f;
        public float verticalRange = 1.6f;
        public float sameRowXTolerance = 0.25f;
        public float sameColYTolerance = 0.25f;
        public int gameMode = 1;
        public int valueGameMode = 15;
        public List<HintEntry> hintEntries = new List<HintEntry>();
    }

    [Serializable]
    public class ExtractedLevelsWrapper
    {
        public List<ExtractedLevelInfo> levels = new List<ExtractedLevelInfo>();
    }

    [Serializable]
    public class ExtractedLevelDatabase : ExtractedLevelsWrapper
    {
    }

    [Serializable]
    public class LevelMapingItem
    {
        public int fromLevel;
        public int toLevel;
        public MapID mapID;
        public string mapName;
    }

    [Serializable]
    public class GameFlowData
    {
        public bool active_banner = true;
        public int banner_first_level = 2;
        public int inter_first_level = 9;
        public int show_rating_level = 14;
        public int unlock_booster_hint_level = 5;
        public int unlock_booster_chair_level = 11;
        public int unlock_booster_moremove_level = 12;
        public int unlock_seat_race_level = 12;
        public int unlock_treasure_climb_level = 16;
        public int show_profile_level = 9;
    }

    [Serializable]
    public class InitialResource
    {
        public string id;
        public int default_number;
    }

    [Serializable]
    public class InitialResourcesWrapper
    {
        public List<InitialResource> resources = new List<InitialResource>();
    }

    [Serializable]
    public class CoinPriceEntry
    {
        public string key;
        public int first_price;
        public int normal_price;
    }

    [Serializable]
    public class CoinPricesData
    {
        public List<CoinPriceEntry> items = new List<CoinPriceEntry>();
    }

    [Serializable]
    public class PlayerReductionRange
    {
        public int step;
        public int minPlayers;
        public int maxPlayers;
    }

    [Serializable]
    public class SeatTowerTierConfig
    {
        public int tier;
        public int required_levels;
        public int reward_total;
        public int final_survivors_min;
        public int final_survivors_max;
    }

    [Serializable]
    public class SeatTowerConfig
    {
        public bool seat_tower_enabled = true;
        public int level_unlock = 10;
        public int seat_tower_expire_hours = 24;
        public int seat_tower_total_players = 100;
        public int initialPlayerCount = 100;
        public int totalSteps = 7;
        public int grandPrizeCoins = 1000;
        public List<PlayerReductionRange> playerReductionRanges = new List<PlayerReductionRange>();
        public List<SeatTowerTierConfig> tiers = new List<SeatTowerTierConfig>();
    }

    [Serializable]
    public class SeatRaceRewardItem
    {
        public string id;
        public int count;
    }

    [Serializable]
    public class SeatRace
    {
        public int seat_race_unlock_level = 12;
        public int seat_race_total_level = 10;
        public int seat_race_duration_minutes = 60;
        public int seat_race_play_option = 1;
        public List<SeatRaceRewardItem> seat_race_reward_top1 = new List<SeatRaceRewardItem>();
        public List<SeatRaceRewardItem> seat_race_reward_top2 = new List<SeatRaceRewardItem>();
        public List<SeatRaceRewardItem> seat_race_reward_top3 = new List<SeatRaceRewardItem>();
    }

    [Serializable]
    public class SeatRaceData
    {
        public SeatRace seat_race = new SeatRace();
        public int NumberLevelTarget => seat_race != null && seat_race.seat_race_total_level > 0 ? seat_race.seat_race_total_level : 10;
    }

    [Serializable]
    public class ClueHuntSuspect
    {
        public string id;
        public string portrait;
        public List<string> tags = new List<string>();

        public bool HasTag(string tag)
        {
            return tags != null && tags.Contains(tag);
        }
    }

    [Serializable]
    public class ClueHuntClue
    {
        public string id;
        public string tag;
        public int cost = 1;
    }

    [Serializable]
    public class ClueHuntReward
    {
        public string type;
        public string id;
        public int amount;
    }

    [Serializable]
    public class ClueHuntLevel
    {
        public int levelId;
        public ClueHuntReward reward;
        public List<ClueHuntSuspect> suspects = new List<ClueHuntSuspect>();
        public List<ClueHuntClue> clues = new List<ClueHuntClue>();
        public string answerId;

        public int id => levelId > 0 ? levelId : 1;
        public string title => $"Mystery #{id}";
        public string answer => answerId;
        public string question => "Which suspect matches the exact witness clues above?";

        public List<string> choices
        {
            get
            {
                var list = new List<string>();
                if (suspects != null)
                {
                    foreach (var s in suspects)
                    {
                        if (s != null && !string.IsNullOrEmpty(s.id))
                            list.Add(s.id);
                    }
                }
                return list;
            }
        }

        public string description
        {
            get
            {
                ClueHuntSuspect culprit = null;
                if (suspects != null)
                {
                    foreach (var s in suspects)
                    {
                        if (s != null && string.Equals(s.id, answerId, StringComparison.OrdinalIgnoreCase))
                        {
                            culprit = s;
                            break;
                        }
                    }
                }

                var lines = new List<string>();
                lines.Add("<b>Suspects on Scene:</b>");
                if (suspects != null)
                {
                    foreach (var s in suspects)
                    {
                        if (s == null) continue;
                        string tagList = (s.tags != null && s.tags.Count > 0) ? string.Join(", ", s.tags) : "none";
                        lines.Add($"• <b>{s.id}</b> — traits: [{tagList}]");
                    }
                }

                lines.Add("\n<b>Witness Clues about the Culprit:</b>");
                if (clues != null && culprit != null)
                {
                    foreach (var c in clues)
                    {
                        if (c == null) continue;
                        bool hasIt = culprit.HasTag(c.tag);
                        string status = hasIt ? "<color=#44EE77>YES (Has trait)</color>" : "<color=#FF6677>NO (Does NOT have trait)</color>";
                        lines.Add($"• Trait <b>{c.tag}</b>: {status}");
                    }
                }
                return string.Join("\n", lines);
            }
        }

        public string explanation => $"Suspect '{answerId}' is the only suspect whose traits match every single YES/NO witness clue!";
    }

    [Serializable]
    public class ClueHuntConfigInner
    {
        public List<ClueHuntLevel> levels = new List<ClueHuntLevel>();
    }

    [Serializable]
    public class ClueHuntRootWrapper
    {
        public ClueHuntConfigInner clue_hunt_config = new ClueHuntConfigInner();
    }
}
