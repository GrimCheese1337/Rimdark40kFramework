using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.Notify_ApparelChanged))]
public static class HarmonyPatch_ApparelSetGearChanged
{
    public static void Postfix(Pawn_ApparelTracker __instance)
    {
        if (!ApparelSetIndex.AnySets)
        {
            return;
        }

        __instance.pawn?.GetComp<CompRankInfo>()?.Notify_GearChanged();
    }
}

[HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.Notify_EquipmentAdded))]
public static class HarmonyPatch_ApparelSetEquipmentAdded
{
    public static void Postfix(Pawn_EquipmentTracker __instance, ThingWithComps eq)
    {
        ApparelSetEquipmentUtility.Notify_EquipmentChanged(__instance.pawn, eq);
    }
}

[HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.Notify_EquipmentRemoved))]
public static class HarmonyPatch_ApparelSetEquipmentRemoved
{
    public static void Postfix(Pawn_EquipmentTracker __instance, ThingWithComps eq)
    {
        ApparelSetEquipmentUtility.Notify_EquipmentChanged(__instance.pawn, eq);
    }
}

public static class ApparelSetEquipmentUtility
{
    /// <summary>
    /// Rebuilds the pawn's sets when the equipment that changed is a set weapon or carries a set decoration.
    /// </summary>
    public static void Notify_EquipmentChanged(Pawn pawn, ThingWithComps eq)
    {
        if (pawn == null || eq == null || !ApparelSetIndex.AnySets)
        {
            return;
        }

        if (!ApparelSetIndex.IsPiece(eq.def) && !ApparelSetIndex.CarriesSetDecoration(eq))
        {
            return;
        }

        pawn.GetComp<CompRankInfo>()?.Notify_GearChanged();
    }
}

[HarmonyPatch(typeof(ThingDef), nameof(ThingDef.SpecialDisplayStats))]
public static class HarmonyPatch_ApparelSetDisplayStats
{
    public static IEnumerable<StatDrawEntry> Postfix(IEnumerable<StatDrawEntry> values, ThingDef __instance, StatRequest req)
    {
        foreach (var value in values)
        {
            yield return value;
        }

        if (!ApparelSetIndex.AnySets)
        {
            yield break;
        }

        if (req.Thing is Pawn pawn)
        {
            var pawnState = pawn.GetComp<CompRankInfo>()?.ApparelSetsIfAny;
            if (pawnState == null || !pawnState.AnyActive)
            {
                yield break;
            }

            foreach (var set in pawnState.ActiveSets)
            {
                yield return Entry(set, pawnState, StatCategoryDefOf.BasicsPawn);
            }

            yield break;
        }

        var sets = ApparelSetIndex.SetsFor(__instance);
        if (sets.Count == 0)
        {
            yield break;
        }

        var owner = req.Thing?.ParentHolder switch
        {
            Pawn_ApparelTracker apparelTracker => apparelTracker.pawn,
            Pawn_EquipmentTracker equipmentTracker => equipmentTracker.pawn,
            _ => null,
        };
        var state = owner?.GetComp<CompRankInfo>()?.ApparelSetsIfAny;
        var category = __instance.IsApparel ? StatCategoryDefOf.Apparel : StatCategoryDefOf.Weapon;
        foreach (var set in sets)
        {
            yield return Entry(set, state, category);
        }
    }

    private static StatDrawEntry Entry(ApparelSetDef set, ApparelSetState state, StatCategoryDef category)
    {
        var value = state != null
            ? state.CountOf(set) + "/" + set.TotalPieces
            : "BEWH.Framework.GeneCustomization.TierPieces".Translate(set.TotalPieces).ToString();

        var hyperlinks = new List<Dialog_InfoCard.Hyperlink>();
        foreach (var piece in set.pieces)
        {
            if (piece != null)
            {
                hyperlinks.Add(new Dialog_InfoCard.Hyperlink(piece));
            }
        }
        foreach (var weaponPiece in set.weaponPieces)
        {
            foreach (var weapon in weaponPiece.resolved)
            {
                hyperlinks.Add(new Dialog_InfoCard.Hyperlink(weapon));
            }
        }

        return new StatDrawEntry(category, "BEWH.Framework.ApparelSet.StatLabel".Translate(set.LabelCap), value, set.ReportText(state), 5000, set.LabelCap, hyperlinks);
    }
}
