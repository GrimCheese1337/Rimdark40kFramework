using System;
using System.Collections.Generic;
using Verse;

namespace Core40k;

public static class DevotionUtils
{
    private static bool? anyCreedsLoaded;

    private static readonly Dictionary<(DevotionCreedDef, DevotionCreedDef), DevotionOpinion> opinionCache = new();

    /// <summary>
    /// Raised after a pawn's applied tier for a creed changes: (pawn, creed, fromTier, toTier). Not raised during
    /// load reconciliation.
    /// </summary>
    public static event Action<Pawn, DevotionCreedDef, int, int> TierChanged;

    public static bool AnyCreedsLoaded
    {
        get
        {
            anyCreedsLoaded ??= DefDatabase<DevotionCreedDef>.AllDefsListForReading.Count > 0;
            return anyCreedsLoaded.Value;
        }
    }

    public static float AddStanding(Pawn pawn, DevotionCreedDef creed, float amount)
    {
        if (!AnyCreedsLoaded || pawn == null || creed == null)
        {
            return 0f;
        }

        if (creed.faith is { polarity: DevotionPolarity.Consensual } && amount > 0f && !CanGainConsensually(pawn, creed))
        {
            return 0f;
        }

        return Core40kPawnData.Of(pawn)?.AddStanding(creed, amount) ?? 0f;
    }

    public static float GetStanding(Pawn pawn, DevotionCreedDef creed)
    {
        if (!AnyCreedsLoaded || pawn == null || creed == null)
        {
            return 0f;
        }

        return Core40kPawnData.Of(pawn)?.GetStanding(creed) ?? 0f;
    }

    public static int TierIndexOf(Pawn pawn, DevotionCreedDef creed)
    {
        if (!AnyCreedsLoaded || pawn == null || creed == null)
        {
            return -1;
        }

        return Core40kPawnData.Of(pawn)?.TierIndexOf(creed) ?? -1;
    }

    public static DevotionCreedDef DominantCreedOf(Pawn pawn)
    {
        if (!AnyCreedsLoaded || pawn == null)
        {
            return null;
        }

        return Core40kPawnData.Of(pawn)?.DominantCreed;
    }

    /// <summary>
    /// Consensual faiths only accrue on a pawn who already holds standing in that creed, or who
    /// has none at all in a conflicting one. Prevents proximity alone converting anybody.
    /// </summary>
    private static bool CanGainConsensually(Pawn pawn, DevotionCreedDef creed)
    {
        var comp = Core40kPawnData.Of(pawn);
        if (comp == null)
        {
            return false;
        }

        if (comp.GetStanding(creed) > 0f)
        {
            return true;
        }

        if (creed.faith == null || creed.faith.tolerance != DevotionTolerance.Exclusive)
        {
            return true;
        }

        foreach (var other in comp.ActiveCreeds)
        {
            if (other != null && other != creed && other.faith == creed.faith)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Most specific opinion entry the holder's creed or faith declares toward the target's creed or faith:
    /// creed→creed, creed→faith, then faith→faith. Null when nothing applies. Cached per creed pair.
    /// </summary>
    public static DevotionOpinion ResolveOpinion(DevotionCreedDef holder, DevotionCreedDef target)
    {
        if (holder == null || target == null || holder == target)
        {
            return null;
        }

        var key = (holder, target);
        if (opinionCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        DevotionOpinion result = null;

        if (!holder.opinions.NullOrEmpty())
        {
            foreach (var entry in holder.opinions)
            {
                if (entry != null && entry.creed == target)
                {
                    result = entry;
                    break;
                }
            }

            if (result == null && target.faith != null)
            {
                foreach (var entry in holder.opinions)
                {
                    if (entry != null && entry.creed == null && entry.faith == target.faith)
                    {
                        result = entry;
                        break;
                    }
                }
            }
        }

        if (result == null && holder.faith != null && target.faith != null && !holder.faith.opinions.NullOrEmpty())
        {
            foreach (var entry in holder.faith.opinions)
            {
                if (entry != null && entry.creed == null && entry.faith == target.faith)
                {
                    result = entry;
                    break;
                }
            }
        }

        opinionCache.Add(key, result);
        return result;
    }

    /// <summary>
    /// Resolves the opinion entry between two pawns from their dominant creeds. Returns null when either pawn has
    /// no devotion, they share a dominant creed, or nothing is declared.
    /// </summary>
    public static DevotionOpinion OpinionBetween(Pawn holder, Pawn target, out DevotionCreedDef holderCreed, out DevotionCreedDef targetCreed)
    {
        holderCreed = null;
        targetCreed = null;

        if (!AnyCreedsLoaded || holder == null || target == null || holder == target)
        {
            return null;
        }

        var holderComp = Core40kPawnData.Of(holder);
        if (holderComp == null || !holderComp.HasAnyDevotion)
        {
            return null;
        }

        var targetComp = Core40kPawnData.Of(target);
        if (targetComp == null || !targetComp.HasAnyDevotion)
        {
            return null;
        }

        holderCreed = holderComp.DominantCreed;
        targetCreed = targetComp.DominantCreed;
        return ResolveOpinion(holderCreed, targetCreed);
    }

    /// <summary>
    /// Final opinion value between two pawns: the entry's value at both tiers, scaled by colony tolerance only
    /// when both creeds share a faith.
    /// </summary>
    public static float OpinionValue(Pawn holder, Pawn target)
    {
        var entry = OpinionBetween(holder, target, out var holderCreed, out var targetCreed);
        if (entry == null)
        {
            return 0f;
        }

        var value = entry.ValueFor(TierIndexOf(holder, holderCreed), TierIndexOf(target, targetCreed));

        if (holderCreed.faith != null && holderCreed.faith == targetCreed.faith)
        {
            var tolerance = Current.Game?.GetComponent<GameComponent_Devotion>()?.ToleranceFor(holderCreed.faith) ?? 0f;
            value *= 1f - tolerance;
        }

        return value;
    }

    public static void Notify_TierChanged(Pawn pawn, DevotionCreedDef creed, int from, int to)
    {
        if (pawn == null)
        {
            return;
        }

        Notify_DevotionChanged(pawn);
        TierChanged?.Invoke(pawn, creed, from, to);
    }

    public static void Notify_DevotionChanged(Pawn pawn)
    {
        if (pawn == null)
        {
            return;
        }

        Current.Game?.GetComponent<GameComponent_Devotion>()?.Notify_PawnDevotionChanged(pawn);
    }
}
