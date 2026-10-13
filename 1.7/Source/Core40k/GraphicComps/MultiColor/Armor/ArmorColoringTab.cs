using System.Linq;
using Verse;

namespace Core40k;

public class ArmorColoringTab : ColoringBaseTab
{
    protected override void SetupHook(Pawn pawn)
    {
        foreach (var item in pawn.apparel.WornApparel.Where(a => a.HasComp<CompMultiColor>()))
        {
            var multiColor = item.GetComp<CompMultiColor>();
            multiColorComps.Add(multiColor);
        }
    }
}