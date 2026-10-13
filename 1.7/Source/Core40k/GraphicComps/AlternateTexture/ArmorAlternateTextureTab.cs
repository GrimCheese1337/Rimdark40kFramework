using System.Linq;
using Verse;

namespace Core40k;

public class ArmorAlternateTextureTab : AlternateTextureBaseTab
{
    protected override void SetupHook(Pawn pawn)
    {
        foreach (var item in pawn.apparel.WornApparel.Where(a => a.HasComp<CompAlternateTexture>()))
        {
            var alternateComp = item.GetComp<CompAlternateTexture>();
            alternateComps.Add(alternateComp);
        }
    }
}