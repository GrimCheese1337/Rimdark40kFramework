using HarmonyLib;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(Thing), "TakeDamage")]
public class CriticalHitPatch
{
    public static void Prefix(ref DamageInfo dinfo)
    {
        var criticalHit = dinfo.Weapon?.GetModExtension<DefModExtension_CriticalHit>();
        if (criticalHit == null)
        {
            return;
        }

        if (Rand.Chance(criticalHit.criticalHitChance))
        {
            var critDamage = dinfo.Amount * criticalHit.criticalHitDamageMultiplier;
            dinfo.SetAmount(critDamage);
        }
    }
}