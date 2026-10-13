using RimWorld;
using Verse;

namespace Core40k;

/// <summary>
/// One directional opinion entry, read from the holder's side: a pawn whose dominant faith or creed carries this
/// entry feels the resulting value toward a pawn whose dominant faith or creed matches the target. Exactly one of
/// faith or creed is set. Entries on a faith may only target a faith.
/// </summary>
public class DevotionOpinion
{
    public DevotionFaithDef faith;

    public DevotionCreedDef creed;

    public float opinion = 0f;

    public float opinionPerHolderTier = 0f;

    public float opinionPerTargetTier = 0f;

    public ThoughtDef thought;

    [MustTranslate]
    public string label;

    public bool HasTarget => faith != null || creed != null;

    public ThoughtDef Thought => thought ?? Core40kDefOf.BEWH_DevotionOpinion;

    public float ValueFor(int holderTier, int targetTier)
    {
        return opinion + (opinionPerHolderTier * (holderTier + 1)) + (opinionPerTargetTier * (targetTier + 1));
    }
}
