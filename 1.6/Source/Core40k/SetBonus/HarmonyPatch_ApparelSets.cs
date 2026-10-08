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

        __instance.pawn?.GetComp<CompRankInfo>()?.Notify_ApparelChanged();
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

        var wearer = (req.Thing as Apparel)?.Wearer;
        var state = wearer?.GetComp<CompRankInfo>()?.ApparelSetsIfAny;
        foreach (var set in sets)
        {
            yield return Entry(set, state, StatCategoryDefOf.Apparel);
        }
    }

    private static StatDrawEntry Entry(ApparelSetDef set, ApparelSetState state, StatCategoryDef category)
    {
        var value = state != null
            ? state.CountOf(set) + "/" + set.pieces.Count
            : "BEWH.Framework.GeneCustomization.TierPieces".Translate(set.pieces.Count).ToString();

        var hyperlinks = new List<Dialog_InfoCard.Hyperlink>();
        foreach (var piece in set.pieces)
        {
            if (piece != null)
            {
                hyperlinks.Add(new Dialog_InfoCard.Hyperlink(piece));
            }
        }

        return new StatDrawEntry(category, "BEWH.Framework.ApparelSet.StatLabel".Translate(set.LabelCap), value, set.ReportText(state), 5000, set.LabelCap, hyperlinks);
    }
}
