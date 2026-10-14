using HarmonyLib;
using RimWorld;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(ResurrectionUtility), "TryResurrect")]
public class RankLimitResurrectionReAddPatch
{
    public static void Postfix(ref bool __result, Pawn pawn)
    {
        if (!__result || pawn == null || !pawn.HasComp<CompRankInfo>())
        {
            return;
        }

        pawn.GetComp<CompRankInfo>().GameComponentRankInfo.Notify_RankHoldersChanged();
        RankUtils.StripRanksOverLimit(pawn);
    }
}
