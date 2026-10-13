using RimWorld;
using Verse;

namespace Core40k;

public class CompProperties_AbilityWaveAttack : CompProperties_AbilityEffectWithDuration
{
    public float range;

    public float lineWidthEnd;

    public int stunTicks;

    public EffecterDef effecterDef;

    public HediffDef hediffDef;
        
    public bool replaceExisting;

    public float severity = -1f;
        
    public bool onlyBrain;


    public CompProperties_AbilityWaveAttack()
    {
        compClass = typeof(CompAbilityEffect_WaveAttack);
    }
}