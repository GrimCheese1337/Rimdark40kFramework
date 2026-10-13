using RimWorld;
using Verse;

namespace Core40k;

/// <summary>
/// Activates on the ThoughtDef the resolved opinion entry names (BEWH_DevotionOpinion by default), so a pair of
/// pawns never carries more than one devotion opinion thought. The value itself comes from Thought_DevotionOpinion.
/// </summary>
public class ThoughtWorker_DevotionOpinion : ThoughtWorker
{
    protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn otherPawn)
    {
        if (!DevotionUtils.AnyCreedsLoaded || !p.RaceProps.Humanlike || !otherPawn.RaceProps.Humanlike)
        {
            return ThoughtState.Inactive;
        }

        var entry = DevotionUtils.OpinionBetween(p, otherPawn, out _, out _);
        if (entry == null || entry.Thought != def)
        {
            return ThoughtState.Inactive;
        }

        return ThoughtState.ActiveAtStage(0);
    }
}
