using System.Collections.Generic;
using Verse;

namespace Core40k;

public class CompProperties_AmmoChanger : CompProperties
{
    public List<ThingDef> availableProjectiles = [];
    
    public CompProperties_AmmoChanger()
    {
        compClass = typeof(Comp_AmmoChanger);
    }
}