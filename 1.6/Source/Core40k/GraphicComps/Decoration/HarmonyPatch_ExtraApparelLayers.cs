using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.Wear))]
public static class HarmonyPatch_ExtraApparelLayersWear
{
    public static void Postfix(Pawn_ApparelTracker __instance, Apparel newApparel)
    {
        if (!ApparelLayerConflictUtility.Active)
        {
            return;
        }

        ApparelLayerConflictUtility.ResolveAfterWear(__instance.pawn, newApparel);
    }
}

[HarmonyPatch(typeof(JobGiver_OptimizeApparel), nameof(JobGiver_OptimizeApparel.ApparelScoreGain))]
public static class HarmonyPatch_ExtraApparelLayersScoreGain
{
    public static void Postfix(Pawn pawn, Apparel ap, List<float> wornScoresCache, ref float __result)
    {
        if (!ApparelLayerConflictUtility.Active || __result <= -1000f || pawn?.apparel == null || ap == null)
        {
            return;
        }

        var body = pawn.RaceProps.body;
        var worn = pawn.apparel.WornApparel;
        var vanillaConflict = false;
        var extraConflict = false;
        var extraScore = 0f;

        for (var i = 0; i < worn.Count; i++)
        {
            if (!ApparelUtility.CanWearTogether(worn[i].def, ap.def, body))
            {
                vanillaConflict = true;
                continue;
            }

            if (!ApparelLayerConflictUtility.ConflictsViaExtraLayers(worn[i], ap, body))
            {
                continue;
            }

            if (pawn.outfits?.forcedHandler?.AllowedToAutomaticallyDrop(worn[i]) == false || pawn.apparel.IsLocked(worn[i]))
            {
                __result = -1000f;
                return;
            }

            extraConflict = true;
            if (wornScoresCache != null && i < wornScoresCache.Count)
            {
                extraScore += wornScoresCache[i];
            }
        }

        if (!extraConflict)
        {
            return;
        }

        if (!vanillaConflict)
        {
            __result /= 10f;
        }
        __result -= extraScore;
    }
}

[HarmonyPatch(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.WouldReplaceLockedApparel))]
public static class HarmonyPatch_ExtraApparelLayersWouldReplaceLocked
{
    public static void Postfix(Pawn_ApparelTracker __instance, Apparel newApparel, ref bool __result)
    {
        if (__result || !ApparelLayerConflictUtility.Active || !__instance.AnyApparelLocked)
        {
            return;
        }

        var body = __instance.pawn.RaceProps.body;
        foreach (var worn in __instance.WornApparel)
        {
            if (__instance.IsLocked(worn) && ApparelLayerConflictUtility.ConflictsViaExtraLayers(newApparel, worn, body))
            {
                __result = true;
                return;
            }
        }
    }
}

[HarmonyPatch(typeof(ApparelUtility), nameof(ApparelUtility.GetApparelReplacedByNewApparel))]
public static class HarmonyPatch_ExtraApparelLayersReplacedBy
{
    public static void Postfix(Pawn pawn, Apparel newApparel, ref Apparel __result)
    {
        if (__result != null || !ApparelLayerConflictUtility.Active || pawn?.apparel == null)
        {
            return;
        }

        var body = pawn.RaceProps.body;
        foreach (var worn in pawn.apparel.WornApparel)
        {
            if (ApparelLayerConflictUtility.ConflictsViaExtraLayers(newApparel, worn, body))
            {
                __result = worn;
                return;
            }
        }
    }
}

[HarmonyPatch]
public static class HarmonyPatch_ExtraApparelLayersStandAccepts
{
    public static MethodBase TargetMethod()
    {
        var map = typeof(Building_OutfitStand).GetInterfaceMap(typeof(IHaulDestination));
        for (var i = 0; i < map.InterfaceMethods.Length; i++)
        {
            if (map.InterfaceMethods[i].Name == nameof(IHaulDestination.Accepts))
            {
                return map.TargetMethods[i];
            }
        }

        return null;
    }

    public static void Postfix(Building_OutfitStand __instance, Thing t, ref bool __result)
    {
        if (!__result || !ApparelLayerConflictUtility.Active || t is not Apparel apparel)
        {
            return;
        }

        var held = __instance.HeldItems;
        for (var i = 0; i < held.Count; i++)
        {
            if (held[i] == t)
            {
                return;
            }
        }

        if (ApparelLayerConflictUtility.StandConflict(__instance, apparel) != null)
        {
            __result = false;
        }
    }
}

[HarmonyPatch(typeof(Building_OutfitStand), nameof(Building_OutfitStand.AddApparel))]
public static class HarmonyPatch_ExtraApparelLayersStandAdd
{
    public static void Prefix(Building_OutfitStand __instance, Apparel apparel)
    {
        if (!ApparelLayerConflictUtility.Active || !__instance.Spawned)
        {
            return;
        }

        var conflict = ApparelLayerConflictUtility.StandConflict(__instance, apparel);
        while (conflict != null)
        {
            if (!__instance.TryDrop(conflict, __instance.Position, ThingPlaceMode.Near, 1, out _))
            {
                return;
            }
            conflict = ApparelLayerConflictUtility.StandConflict(__instance, apparel);
        }
    }
}

[HarmonyPatch(typeof(JobDriver_UseOutfitStand), nameof(JobDriver_UseOutfitStand.Notify_Starting))]
public static class HarmonyPatch_ExtraApparelLayersUseStand
{
    private static readonly AccessTools.FieldRef<JobDriver_UseOutfitStand, HashSet<Apparel>> WornToStand =
        AccessTools.FieldRefAccess<JobDriver_UseOutfitStand, HashSet<Apparel>>("wornApparelToTransferToStand");

    private static readonly AccessTools.FieldRef<JobDriver_UseOutfitStand, HashSet<Apparel>> StandToPawn =
        AccessTools.FieldRefAccess<JobDriver_UseOutfitStand, HashSet<Apparel>>("standApparelToTransferToPawn");

    private static readonly AccessTools.FieldRef<JobDriver_UseOutfitStand, int> Duration =
        AccessTools.FieldRefAccess<JobDriver_UseOutfitStand, int>("duration");

    public static void Postfix(JobDriver_UseOutfitStand __instance)
    {
        if (!ApparelLayerConflictUtility.Active)
        {
            return;
        }

        var pawn = __instance.pawn;
        var standToPawn = StandToPawn(__instance);
        var wornToStand = WornToStand(__instance);
        if (pawn?.apparel == null || standToPawn.NullOrEmpty() || wornToStand == null)
        {
            return;
        }

        var body = pawn.RaceProps.body;
        ref var duration = ref Duration(__instance);
        List<Apparel> blocked = null;
        var accepted = new List<Apparel>();

        foreach (var standApparel in standToPawn)
        {
            var locked = false;
            foreach (var other in accepted)
            {
                if (ApparelLayerConflictUtility.ConflictsViaExtraLayers(standApparel, other, body))
                {
                    locked = true;
                    break;
                }
            }

            List<Apparel> displaced = null;
            if (!locked)
            {
                foreach (var worn in pawn.apparel.WornApparel)
                {
                    if (wornToStand.Contains(worn) || !ApparelLayerConflictUtility.ConflictsViaExtraLayers(standApparel, worn, body))
                    {
                        continue;
                    }

                    if (pawn.apparel.IsLocked(worn))
                    {
                        locked = true;
                        break;
                    }

                    displaced ??= [];
                    displaced.Add(worn);
                }
            }

            if (locked)
            {
                blocked ??= [];
                blocked.Add(standApparel);
                continue;
            }

            accepted.Add(standApparel);
            if (displaced == null)
            {
                continue;
            }

            foreach (var worn in displaced)
            {
                if (wornToStand.Add(worn))
                {
                    duration += (int)(worn.GetStatValue(StatDefOf.EquipDelay) * 60f);
                }
            }
        }

        if (blocked == null)
        {
            return;
        }

        foreach (var apparel in blocked)
        {
            if (standToPawn.Remove(apparel))
            {
                duration -= (int)(apparel.GetStatValue(StatDefOf.EquipDelay) * 60f);
            }
        }

        List<Apparel> keepWorn = null;
        foreach (var worn in wornToStand)
        {
            var stillReplaced = false;
            foreach (var standApparel in standToPawn)
            {
                if (!ApparelLayerConflictUtility.CanWearTogether(standApparel, worn, body))
                {
                    stillReplaced = true;
                    break;
                }
            }

            if (!stillReplaced)
            {
                keepWorn ??= [];
                keepWorn.Add(worn);
            }
        }

        if (keepWorn == null)
        {
            return;
        }

        foreach (var worn in keepWorn)
        {
            wornToStand.Remove(worn);
            duration -= (int)(worn.GetStatValue(StatDefOf.EquipDelay) * 60f);
        }
    }
}
