using HarmonyLib;
using RimWorld;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(Pawn), nameof(Pawn.SetFaction))]
public static class RankLimitOnFactionChangePatch
{
    public static void Prefix(Pawn __instance, out bool __state)
    {
        __state = RankUtils.CountsTowardsRankLimits(__instance);
    }

    public static void Postfix(Pawn __instance, bool __state)
    {
        RankLimitMembershipUtility.Notify_MembershipMaybeChanged(__instance, __state);
    }
}

[HarmonyPatch(typeof(Pawn_GuestTracker), nameof(Pawn_GuestTracker.SetGuestStatus))]
public static class RankLimitOnGuestStatusChangePatch
{
    public static void Prefix(Pawn ___pawn, out bool __state)
    {
        __state = RankUtils.CountsTowardsRankLimits(___pawn);
    }

    public static void Postfix(Pawn ___pawn, bool __state)
    {
        RankLimitMembershipUtility.Notify_MembershipMaybeChanged(___pawn, __state);
    }
}

public static class RankLimitMembershipUtility
{
    /// <summary>
    /// Refreshes rank counts when a pawn starts or stops counting towards the player's rank limits, and strips ranks a returning pawn has no room for.
    /// </summary>
    public static void Notify_MembershipMaybeChanged(Pawn pawn, bool countedBefore)
    {
        if (pawn == null || Current.Game == null || !pawn.HasComp<CompRankInfo>())
        {
            return;
        }

        var countsNow = RankUtils.CountsTowardsRankLimits(pawn);
        if (countedBefore == countsNow)
        {
            return;
        }

        pawn.GetComp<CompRankInfo>().GameComponentRankInfo.Notify_RankHoldersChanged();

        if (countsNow)
        {
            RankUtils.StripRanksOverLimit(pawn);
        }
    }
}
