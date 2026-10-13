using Verse;

namespace Core40k;

public class WeaponColoringTab : ColoringBaseTab
{
    protected override void SetupHook(Pawn pawn)
    {
        var multiColor = pawn?.equipment?.Primary?.GetComp<CompMultiColor>();
        if (multiColor != null)
        {
            multiColorComps.Add(multiColor);
        }
    }
}