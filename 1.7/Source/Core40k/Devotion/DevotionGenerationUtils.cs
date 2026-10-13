using Verse;

namespace Core40k;

public static class DevotionGenerationUtils
{
    /// <summary>
    /// Grants pawnkind-defined devotion. Standing is written directly rather than through
    /// DevotionUtils.AddStanding so a consensual faith's proximity gate does not reject it, and silently so
    /// generation sends no tier letters.
    /// </summary>
    public static void TryGivePawnKindDevotion(Pawn pawn)
    {
        if (!DevotionUtils.AnyCreedsLoaded || pawn?.kindDef == null || Current.Game == null)
        {
            return;
        }

        var ext = pawn.kindDef.GetModExtension<DefModExtension_PawnKindDevotion>();
        if (ext == null || ext.devotions.NullOrEmpty())
        {
            return;
        }

        var comp = pawn.GetComp<CompRankInfo>();
        if (comp == null)
        {
            return;
        }

        foreach (var entry in ext.devotions)
        {
            if (entry?.creed == null)
            {
                continue;
            }

            if (entry.chance < 1f && !Rand.Chance(entry.chance))
            {
                continue;
            }

            var amount = entry.ResolveStanding();
            if (amount <= 0f)
            {
                continue;
            }

            comp.SetStanding(entry.creed, amount, true);
        }
    }
}
