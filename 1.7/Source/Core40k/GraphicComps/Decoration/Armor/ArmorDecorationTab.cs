using System.Linq;
using Verse;

namespace Core40k;

public class ArmorDecorationTab : DecorationBaseTab
{
    protected override void SetupHook()
    {
        var apparels = selPawn.apparel.WornApparel.Where(a => a.HasComp<CompDecorative>()).ToList();
        foreach (var apparel in apparels)
        {
            decorativeComps.Add(apparel.GetComp<CompDecorativeBase>());
        }
    }
}