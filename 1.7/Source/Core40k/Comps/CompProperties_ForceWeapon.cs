using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Core40k;

public class CompProperties_ForceWeapon : CompProperties
{
    public DamageDef damageDef;

    public List<ToolCapacityDef> capacitiesToApplyOn;

    public StatDef scalingStat;
    public float damageScalingFactor = 1;
    public float minValueToWork = 0;

    public bool scalesPen = false;
    public float penScaleFactor = 1;
    public float flatPen = 1;

    public CompProperties_ForceWeapon()
    {
        compClass = typeof(Comp_ForceWeapon);
    }
}