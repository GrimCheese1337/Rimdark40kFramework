using HarmonyLib;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(Pawn), nameof(Pawn.PreApplyDamage))]
public static class HarmonyPatch_ShieldPreApplyDamage
{
    public static void Postfix(Pawn __instance, ref DamageInfo dinfo, ref bool absorbed)
    {
        if (absorbed)
        {
            return;
        }

        ShieldUtils.TryApplyShield(__instance, ref dinfo, ref absorbed);
    }
}
