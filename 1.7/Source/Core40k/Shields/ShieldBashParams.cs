using Verse;

namespace Core40k;

public struct ShieldBashParams
{
    public DamageDef damageDef;
    public float damageAmount;
    public float armorPenetration;
    public int stunTicks;
    public int knockbackCells;
    public float maxKnockbackBodySize;
    public float impactDamage;
    public FleckDef impactFleck;
    public SoundDef impactSound;
    public ThingDef weapon;
}
