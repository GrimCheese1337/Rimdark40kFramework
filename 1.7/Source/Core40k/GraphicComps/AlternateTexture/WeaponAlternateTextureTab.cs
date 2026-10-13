using Verse;

namespace Core40k;

public class WeaponAlternateTextureTab : AlternateTextureBaseTab
{
    protected override void SetupHook(Pawn pawn)
    {
        var alternateComp = pawn?.equipment?.Primary?.GetComp<CompAlternateTexture>();
        if (alternateComp != null)
        {
            alternateComps.Add(alternateComp);
        }
    }
}