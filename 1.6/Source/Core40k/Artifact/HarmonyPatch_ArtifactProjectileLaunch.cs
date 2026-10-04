using HarmonyLib;
using UnityEngine;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(Projectile), nameof(Projectile.Launch), [
    typeof(Thing),
    typeof(Vector3),
    typeof(LocalTargetInfo),
    typeof(LocalTargetInfo),
    typeof(ProjectileHitFlags),
    typeof(bool),
    typeof(Thing),
    typeof(ThingDef)
])]
public static class ArtifactProjectileLaunchPatch
{
    public static void Postfix(Projectile __instance, Thing equipment)
    {
        if (equipment is not ThingWithComps weapon || !Core40kUtils.DefHasComp<CompArtifactWeapon>(weapon.def))
        {
            return;
        }

        weapon.GetComp<CompArtifactWeapon>()?.ApplyToProjectile(__instance);
    }
}
