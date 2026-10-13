using RimWorld;
using Verse;

namespace Core40k;

public class CompProperties_ConversionField : CompProperties_Shield
{
    public bool scaleWithVisualSize = true;

    public CompProperties_ConversionField()
    {
        compClass = typeof(Comp_ConversionField);
        blocksRangedWeapons = false;
        minDrawSize = 1.35f;
        maxDrawSize = 1.75f;
    }
}
