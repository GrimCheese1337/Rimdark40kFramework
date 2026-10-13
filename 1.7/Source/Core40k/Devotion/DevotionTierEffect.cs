using Verse;

namespace Core40k;

/// <summary>
/// Content-side hook for tier payloads the declarative fields cannot express.
/// Listed under DevotionTierDef.extraEffects as li Class="YourMod.YourEffect".
/// Remove also runs from ClearAllDevotion and load reconciliation, so it must be safe on a tier that was never applied.
/// </summary>
public abstract class DevotionTierEffect
{
    public abstract void Apply(Pawn pawn, DevotionCreedDef creed, DevotionTierDef tier);

    public abstract void Remove(Pawn pawn, DevotionCreedDef creed, DevotionTierDef tier);

    public virtual string ExplanationFor(DevotionCreedDef creed, DevotionTierDef tier)
    {
        return null;
    }
}
