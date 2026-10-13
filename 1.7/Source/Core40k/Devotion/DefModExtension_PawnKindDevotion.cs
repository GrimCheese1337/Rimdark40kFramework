using System;
using System.Collections.Generic;
using Verse;

namespace Core40k;

public class DefModExtension_PawnKindDevotion : DefModExtension
{
    public List<PawnKindDevotionEntry> devotions = [];
}

public class PawnKindDevotionEntry
{
    public DevotionCreedDef creed;

    public FloatRange standingRange = new FloatRange(0f, 0f);

    public IntRange tierRange = new IntRange(-1, -1);

    public float chance = 1f;

    /// <summary>
    /// Standing to grant. tierRange wins when set, so content can ask for "Marked to Chosen"
    /// without knowing the thresholds.
    /// </summary>
    public float ResolveStanding()
    {
        if (creed == null)
        {
            return 0f;
        }

        if (tierRange.min < 0 && tierRange.max < 0)
        {
            return standingRange.RandomInRange;
        }

        if (creed.tiers.NullOrEmpty())
        {
            return 0f;
        }

        var min = Math.Clamp(tierRange.min, 0, creed.tiers.Count - 1);
        var max = Math.Clamp(tierRange.max, min, creed.tiers.Count - 1);
        var tier = Rand.RangeInclusive(min, max);

        var floor = creed.tiers[tier].threshold;
        var ceiling = tier + 1 < creed.tiers.Count ? creed.tiers[tier + 1].threshold : creed.maxStanding;

        return Rand.Range(floor, Math.Max(floor, ceiling - 0.01f));
    }
}
