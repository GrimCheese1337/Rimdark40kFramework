using RimWorld;
using Verse;

namespace Core40k;

public class CompProperties_AbilityShieldBash : CompProperties_AbilityEffect
{
    public DamageDef damageDef;
    public float damageAmount = 8f;
    public float armorPenetration = -1f;
    public StatDef scaleStat;
    public float scaleFactor = 1f;

    public int stunTicks = 90;
    public int knockbackCells = 1;
    public float maxKnockbackBodySize = 1.5f;
    public float impactDamage;

    public FleckDef impactFleck;
    public SoundDef impactSound;

    public CompProperties_AbilityShieldBash()
    {
        compClass = typeof(CompAbilityEffect_ShieldBash);
    }
}
