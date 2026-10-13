using RimWorld;
using Verse;

namespace Core40k;

public class Thought_DevotionOpinion : Thought_SituationalSocial
{
    public override float OpinionOffset()
    {
        if (ThoughtUtility.ThoughtNullified(pawn, def))
        {
            return 0f;
        }

        return DevotionUtils.OpinionValue(pawn, otherPawn);
    }

    public override string LabelCap
    {
        get
        {
            var entry = DevotionUtils.OpinionBetween(pawn, otherPawn, out _, out _);
            if (entry == null || entry.label.NullOrEmpty())
            {
                return base.LabelCap;
            }

            return entry.label.Formatted(pawn.Named("PAWN"), otherPawn.Named("OTHERPAWN")).CapitalizeFirst();
        }
    }
}
