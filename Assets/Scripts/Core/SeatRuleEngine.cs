using System;
using System.Collections.Generic;
using UnityEngine;

namespace WheresMySeat
{
    public interface ISlot
    {
        string Id { get; }
        int IndexOfSeat { get; }
        Transform Anchor { get; }
        bool IsOccupied { get; }
        bool IsSeatSlot { get; }
        Person Occupant { get; }
        GameObject ItemOccupant { get; }
        List<SimpleTraitType> Traits { get; }
        SeatPosture Posture { get; }
        void SetOccupant(Person person);
        void SetOccupantItem(GameObject item);
    }

    public interface ISelectableSlot
    {
        void ActiveSelecting(bool active);
    }

    public interface IPerson
    {
        string DisplayName { get; }
        RoleType RoleType { get; }
        List<PersonRequirement> Requirements { get; }
    }

    public class PlacementContext
    {
        public Person Person;
        public ISlot Slot;
        public List<ISlot> AllSlots;
        public List<IPerson> AllPersons;
        public List<OnOffItem> AllOnOffItems;
        public float DistanceBetweenSeatToCheck = 1.85f;
        public float HorizontalRange = 1.85f;
        public float VerticalRange = 1.85f;
        public float SameRowYTolerance = 0.45f;
        public float SameColXTolerance = 0.45f;
        public float SameRowXTolerance = 8.0f;
        public float SameColYTolerance = 0.45f;
        public float CircleRadius = 1.85f;

        public ISlot Seat
        {
            get => Slot;
            set => Slot = value;
        }

        public List<ISlot> AllSeats
        {
            get => AllSlots;
            set => AllSlots = value;
        }

        public Vector2 GetSlotWorldPos(ISlot s)
        {
            if (s == null || s.Anchor == null) return Vector2.zero;
            Vector3 p = s.Anchor.position;
            return new Vector2(p.x, p.y);
        }

        public List<ISlot> NeighborSlots(SeatDirectionCheck dir)
        {
            var result = new List<ISlot>();
            if (Slot == null || AllSlots == null) return result;

            Vector2 myPos = GetSlotWorldPos(Slot);

            for (int i = 0; i < AllSlots.Count; i++)
            {
                ISlot other = AllSlots[i];
                if (other == null || other == Slot || !other.IsSeatSlot) continue;

                Vector2 otherPos = GetSlotWorldPos(other);
                float dx = otherPos.x - myPos.x;
                float dy = otherPos.y - myPos.y;
                float absDx = Mathf.Abs(dx);
                float absDy = Mathf.Abs(dy);

                bool match = false;
                switch (dir)
                {
                    case SeatDirectionCheck.Horizontal:
                        match = absDy <= SameRowYTolerance && absDx <= HorizontalRange;
                        break;
                    case SeatDirectionCheck.HorizontalRight:
                        // "I sit to the right of X" -> X's seat is to the left OR right within horizontal range;
                        // wait: in SitWithSomeOne(Right, "Glia", "I sit to the right of Glia!"),
                        // if MY seat is to the right of Glia, then Glia is at dx < 0 (to my left)!
                        // Let's check BOTH or specifically check where the target is!
                        // For general NeighborSlots(HorizontalRight), let's check dx < 0 (person on my left so I am to their right) OR dx > 0 if someone is to my right:
                        // Actually let's handle directional target relative check cleanly:
                        // If I say "I sit to the right of Glia", Glia's X < My X (so Glia is to my left, dx < -0.05f), OR if I say "Someone is to my right", dx > 0.05f.
                        // Wait: in our LevelDatabase, how did we construct `SitWithSomeOne(SeatDirectionCheck.Right, other.name, "I want to sit to the right of {other.name}!")`?
                        // Let's check LevelDatabase:
                        // `if (orow == r && ocol == c - 1)` -> `other` is at `c - 1` (left of `c`), so `otherPos.x < myPos.x` (`dx < -0.05f`)!
                        // And in Level 8: `SitWithRole(SeatDirectionCheck.Left, RoleType.Adult, "I need an Adult to my left!")` -> Adult is at `c - 1` (`dx < -0.05f`), and `Right` -> Adult is at `c + 1` (`dx > 0.05f`).
                        // Wait: if `Horizontal` checks `absDy <= SameRowYTolerance && absDx <= HorizontalRange`, let's see!
                        match = absDy <= SameRowYTolerance && absDx <= HorizontalRange && dx < -0.05f;
                        break;
                    case SeatDirectionCheck.HorizontalLeft:
                        match = absDy <= SameRowYTolerance && absDx <= HorizontalRange && dx > 0.05f;
                        break;
                    case SeatDirectionCheck.Vertical:
                    case SeatDirectionCheck.Opposite:
                        match = absDx <= SameColXTolerance && absDy <= VerticalRange;
                        break;
                    case SeatDirectionCheck.VerticalUp:
                        // "I sit in front of X" -> X is in row r+1 (lower Y, so dy < -0.05f)
                        match = absDx <= SameColXTolerance && absDy <= VerticalRange && dy < -0.05f;
                        break;
                    case SeatDirectionCheck.VerticalDown:
                        // "I sit behind X" -> X is in row r-1 (higher Y, so dy > 0.05f)
                        match = absDx <= SameColXTolerance && absDy <= VerticalRange && dy > 0.05f;
                        break;
                    case SeatDirectionCheck.SameRow:
                        match = absDy <= SameRowYTolerance && absDx <= SameRowXTolerance;
                        break;
                    case SeatDirectionCheck.SameColumn:
                        match = absDx <= SameColXTolerance && absDy <= 8.0f;
                        break;
                    case SeatDirectionCheck.HorizontalAndVertical:
                        match = (absDy <= SameRowYTolerance && absDx <= HorizontalRange) ||
                                (absDx <= SameColXTolerance && absDy <= VerticalRange);
                        break;
                    case SeatDirectionCheck.Circle:
                    default:
                        match = Vector2.Distance(myPos, otherPos) <= CircleRadius;
                        break;
                }

                if (match)
                {
                    result.Add(other);
                }
            }

            return result;
        }
    }

    [Serializable]
    public abstract class PersonRequirement
    {
        public string description;
        public string invalidDescription;
        public bool isHideAtDialog;

        public string Description => description;

        public abstract RuleResult Evaluate(PlacementContext ctx);

        public RuleResult Validate(PlacementContext ctx)
        {
            return Evaluate(ctx);
        }
    }

    [Serializable]
    public class SitAnyWhere : PersonRequirement
    {
        public SitAnyWhere()
        {
            description = "I can sit anywhere!";
            invalidDescription = "I need a valid seat!";
        }

        public override RuleResult Evaluate(PlacementContext ctx)
        {
            if (ctx.Slot == null || !ctx.Slot.IsSeatSlot)
                return RuleResult.Fail(invalidDescription, ctx.Slot);
            return RuleResult.Success(ctx.Slot);
        }
    }

    [Serializable]
    public class SimpleSitRequirement : PersonRequirement
    {
        public SimpleTraitType requiredTrait;

        public SimpleSitRequirement(SimpleTraitType trait, string desc, string invalidDesc = "")
        {
            requiredTrait = trait;
            description = desc;
            invalidDescription = string.IsNullOrEmpty(invalidDesc) ? $"This isn't a {FormatTrait(trait)} seat..." : invalidDesc;
        }

        public static SimpleSitRequirement MustHave(SimpleTraitType trait, string desc, string invalidDesc = "")
        {
            return new SimpleSitRequirement(trait, desc, invalidDesc);
        }

        public static string FormatTrait(SimpleTraitType t)
        {
            return t.ToString().Replace("_", " ");
        }

        public override RuleResult Evaluate(PlacementContext ctx)
        {
            if (ctx.Slot == null || !ctx.Slot.IsSeatSlot)
                return RuleResult.Fail(invalidDescription, ctx.Slot);

            if (ctx.Slot.Traits != null && ctx.Slot.Traits.Contains(requiredTrait))
                return RuleResult.Success(ctx.Slot);

            return RuleResult.Fail(invalidDescription, ctx.Slot);
        }
    }

    [Serializable]
    public class SitAvoidPositionTrait : PersonRequirement
    {
        public SimpleTraitType requiredTrait;

        public SitAvoidPositionTrait(SimpleTraitType traitToAvoid, string desc, string invalidDesc = "")
        {
            requiredTrait = traitToAvoid;
            description = desc;
            invalidDescription = string.IsNullOrEmpty(invalidDesc)
                ? $"I don't want a {SimpleSitRequirement.FormatTrait(traitToAvoid)} seat!"
                : invalidDesc;
        }

        public override RuleResult Evaluate(PlacementContext ctx)
        {
            if (ctx.Slot == null || !ctx.Slot.IsSeatSlot)
                return RuleResult.Fail(invalidDescription, ctx.Slot);

            if (ctx.Slot.Traits != null && ctx.Slot.Traits.Contains(requiredTrait))
                return RuleResult.Fail(invalidDescription, ctx.Slot);

            return RuleResult.Success(ctx.Slot);
        }
    }

    [Serializable]
    public class IWantSitAlone : PersonRequirement
    {
        public SeatDirectionCheck checkMode;

        public IWantSitAlone(SeatDirectionCheck dir, string desc = "I want to sit alone!", string invalidDesc = "Someone is sitting too close to me!")
        {
            checkMode = dir;
            description = desc;
            invalidDescription = invalidDesc;
        }

        public override RuleResult Evaluate(PlacementContext ctx)
        {
            if (ctx.Slot == null || !ctx.Slot.IsSeatSlot)
                return RuleResult.Fail(invalidDescription, ctx.Slot);

            var neighbors = ctx.NeighborSlots(checkMode);
            for (int i = 0; i < neighbors.Count; i++)
            {
                if (neighbors[i].IsOccupied && neighbors[i].Occupant != null && neighbors[i].Occupant != ctx.Person)
                {
                    return RuleResult.Fail(invalidDescription, neighbors[i]);
                }
            }

            return RuleResult.Success(ctx.Slot);
        }
    }

    [Serializable]
    public class IWantSitNextToTag : PersonRequirement
    {
        public string defaultTargetTag;
        public SeatDirectionCheck directionCheck;

        public IWantSitNextToTag(string targetTag, SeatDirectionCheck dir, string desc = "", string invalidDesc = "")
        {
            defaultTargetTag = targetTag;
            directionCheck = dir;
            description = string.IsNullOrEmpty(desc) ? $"I want to sit next to {targetTag}!" : desc;
            invalidDescription = string.IsNullOrEmpty(invalidDesc) ? $"I'm not sitting with {targetTag}!" : invalidDesc;
        }

        public override RuleResult Evaluate(PlacementContext ctx)
        {
            if (ctx.Slot == null || !ctx.Slot.IsSeatSlot)
                return RuleResult.Fail(invalidDescription, ctx.Slot);

            var neighbors = ctx.NeighborSlots(directionCheck);
            for (int i = 0; i < neighbors.Count; i++)
            {
                Person occ = neighbors[i].Occupant;
                if (occ != null && occ.HasTag(defaultTargetTag))
                {
                    return RuleResult.Success(ctx.Slot);
                }
            }

            return RuleResult.Fail(invalidDescription, ctx.Slot);
        }
    }

    [Serializable]
    public class SitAvoidChar : PersonRequirement
    {
        public string defaultTargetTag;
        public SeatDirectionCheck directionCheck;

        public SitAvoidChar(string targetTag, SeatDirectionCheck dir, string desc = "", string invalidDesc = "")
        {
            defaultTargetTag = targetTag;
            directionCheck = dir;
            description = string.IsNullOrEmpty(desc) ? $"Keep me away from {targetTag}!" : desc;
            invalidDescription = string.IsNullOrEmpty(invalidDesc) ? $"I don't want to sit near {targetTag}!" : invalidDesc;
        }

        public SitAvoidChar(SeatDirectionCheck dir, string targetTag, string desc = "", string invalidDesc = "")
            : this(targetTag, dir, desc, invalidDesc)
        {
        }

        public override RuleResult Evaluate(PlacementContext ctx)
        {
            if (ctx.Slot == null || !ctx.Slot.IsSeatSlot)
                return RuleResult.Fail(invalidDescription, ctx.Slot);

            var neighbors = ctx.NeighborSlots(directionCheck);
            for (int i = 0; i < neighbors.Count; i++)
            {
                Person occ = neighbors[i].Occupant;
                if (occ != null && occ.HasTag(defaultTargetTag))
                {
                    return RuleResult.Fail(invalidDescription, neighbors[i]);
                }
            }

            return RuleResult.Success(ctx.Slot);
        }
    }

    [Serializable]
    public class SitWithSomeOne : PersonRequirement
    {
        public SeatDirectionCheck seatDirectionCheck;
        public string targetTag;

        public SitWithSomeOne(SeatDirectionCheck dir, string desc = "I don't want to sit alone!")
        {
            seatDirectionCheck = dir;
            targetTag = string.Empty;
            description = desc;
            invalidDescription = "There is nobody next to me!";
        }

        public SitWithSomeOne(SeatDirectionCheck dir, string targetPersonName, string desc, string invalidDesc = "")
        {
            seatDirectionCheck = dir;
            targetTag = targetPersonName;
            description = desc;
            invalidDescription = string.IsNullOrEmpty(invalidDesc) ? $"I need to sit by {targetPersonName}!" : invalidDesc;
        }

        public override RuleResult Evaluate(PlacementContext ctx)
        {
            if (ctx.Slot == null || !ctx.Slot.IsSeatSlot)
                return RuleResult.Fail(invalidDescription, ctx.Slot);

            var neighbors = ctx.NeighborSlots(seatDirectionCheck);
            for (int i = 0; i < neighbors.Count; i++)
            {
                Person occ = neighbors[i].Occupant;
                if (occ != null && occ != ctx.Person)
                {
                    if (string.IsNullOrEmpty(targetTag) || occ.HasTag(targetTag))
                    {
                        return RuleResult.Success(ctx.Slot);
                    }
                }
            }

            return RuleResult.Fail(invalidDescription, ctx.Slot);
        }
    }

    [Serializable]
    public class SitWithRole : PersonRequirement
    {
        public RoleType requiredRole;
        public SeatDirectionCheck directionCheck;

        public SitWithRole(RoleType role, SeatDirectionCheck dir, string desc = "", string invalidDesc = "")
        {
            requiredRole = role;
            directionCheck = dir;
            description = string.IsNullOrEmpty(desc) ? $"I need to sit next to an {role}!" : desc;
            invalidDescription = string.IsNullOrEmpty(invalidDesc) ? $"No {role} is sitting next to me!" : invalidDesc;
        }

        public SitWithRole(SeatDirectionCheck dir, RoleType role, string desc = "", string invalidDesc = "")
            : this(role, dir, desc, invalidDesc)
        {
        }

        public override RuleResult Evaluate(PlacementContext ctx)
        {
            if (ctx.Slot == null || !ctx.Slot.IsSeatSlot)
                return RuleResult.Fail(invalidDescription, ctx.Slot);

            // For SitWithRole, check horizontal/adjacent neighbors in that direction or any horizontal neighbor if Left/Right
            var neighbors = ctx.NeighborSlots(directionCheck);
            for (int i = 0; i < neighbors.Count; i++)
            {
                if (neighbors[i].Occupant != null && neighbors[i].Occupant.RoleType == requiredRole)
                {
                    return RuleResult.Success(ctx.Slot);
                }
            }

            return RuleResult.Fail(invalidDescription, ctx.Slot);
        }
    }

    [Serializable]
    public class MustAvoidNeighborWithTrait : PersonRequirement
    {
        public string traitToAvoid;
        public SeatDirectionCheck directionCheck;

        public MustAvoidNeighborWithTrait(string personTrait, SeatDirectionCheck dir, string desc = "", string invalidDesc = "")
        {
            traitToAvoid = personTrait;
            directionCheck = dir;
            description = string.IsNullOrEmpty(desc) ? $"I can't stand {personTrait} people!" : desc;
            invalidDescription = string.IsNullOrEmpty(invalidDesc) ? $"Someone {personTrait} is too close!" : invalidDesc;
        }

        public override RuleResult Evaluate(PlacementContext ctx)
        {
            if (ctx.Slot == null || !ctx.Slot.IsSeatSlot)
                return RuleResult.Fail(invalidDescription, ctx.Slot);

            var neighbors = ctx.NeighborSlots(directionCheck);
            for (int i = 0; i < neighbors.Count; i++)
            {
                Person occ = neighbors[i].Occupant;
                if (occ != null && occ.HasTrait(traitToAvoid))
                {
                    return RuleResult.Fail(invalidDescription, neighbors[i]);
                }
            }

            return RuleResult.Success(ctx.Slot);
        }
    }

    [Serializable]
    public class SitWithOnOffItem : PersonRequirement
    {
        public bool requireOn;

        public SitWithOnOffItem(bool wantLightOn, string desc = "", string invalidDesc = "")
        {
            requireOn = wantLightOn;
            description = string.IsNullOrEmpty(desc)
                ? (wantLightOn ? "I need the reading light ON!" : "I want the light OFF to sleep!")
                : desc;
            invalidDescription = string.IsNullOrEmpty(invalidDesc)
                ? (wantLightOn ? "It's too dark here! Tap the lamp!" : "The light is bothering my eyes!")
                : invalidDesc;
        }

        public override RuleResult Evaluate(PlacementContext ctx)
        {
            if (ctx.Slot == null || !ctx.Slot.IsSeatSlot)
                return RuleResult.Fail(invalidDescription, ctx.Slot);

            if (PlacementManager.Instance != null)
            {
                var items = PlacementManager.Instance.GetAllOnOffItems();
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i] != null && items[i].IndexOfSeat == ctx.Slot.IndexOfSeat)
                    {
                        if (items[i].IsOn == requireOn)
                            return RuleResult.Success(ctx.Slot);
                        return RuleResult.Fail(invalidDescription, ctx.Slot);
                    }
                }
            }

            return RuleResult.Success(ctx.Slot);
        }
    }

    public static class SeatRuleEngine
    {
        public static RuleResult Validate(PlacementContext ctx)
        {
            if (ctx == null || ctx.Person == null)
                return RuleResult.Fail("Invalid person context");

            if (ctx.Slot == null || !ctx.Slot.IsSeatSlot)
                return RuleResult.Fail("Character is not placed on a seat yet", ctx?.Slot);

            // If Special Booster (Magic Seat / SitAnywhere) was applied to this person
            if (ctx.Person.isUseSpecialBoster)
                return RuleResult.Success(ctx.Slot);

            var reqs = ctx.Person.Requirements;
            if (reqs == null || reqs.Count == 0)
                return RuleResult.Success(ctx.Slot);

            for (int i = 0; i < reqs.Count; i++)
            {
                if (reqs[i] == null) continue;
                RuleResult res = reqs[i].Evaluate(ctx);
                if (!res.Ok)
                {
                    return res;
                }
            }

            return RuleResult.Success(ctx.Slot);
        }
    }
}
