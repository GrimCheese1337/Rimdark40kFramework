using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Core40k;

[HarmonyPatch(typeof(Pawn), nameof(Pawn.TryStartAttack))]
public static class HybridWeaponMeleePatch
{
    public static bool Prefix(Pawn __instance, LocalTargetInfo targ, ref bool __result)
    {
        var hybrid = __instance.equipment?.Primary?.def.GetModExtension<DefModExtension_HybridWeapon>();
        if (hybrid == null || !__instance.Spawned || __instance.meleeVerbs == null || !__instance.kindDef.canMeleeAttack
            || __instance.stances.FullBodyBusy || __instance.WorkTagIsDisabled(WorkTags.Violent))
        {
            return true;
        }

        var meleeTarget = FindMeleeTarget(__instance, targ, hybrid);
        if (meleeTarget == null)
        {
            return true;
        }

        var verb = __instance.meleeVerbs.TryGetMeleeVerb(meleeTarget);
        if (verb == null || !verb.CanHitTarget(meleeTarget) || !verb.TryStartCastOn(meleeTarget))
        {
            return true;
        }

        __result = true;
        return false;
    }

    /// <summary>
    /// Returns an adjacent active threat (preferring the ordered target), otherwise the ordered target if it is within melee reach.
    /// </summary>
    private static Thing FindMeleeTarget(Pawn pawn, LocalTargetInfo targ, DefModExtension_HybridWeapon hybrid)
    {
        var ordered = targ.Thing;
        var map = pawn.Map;

        if (hybrid.meleeAdjacentThreatsFirst)
        {
            Pawn threat = null;
            for (var i = 0; i < 8; i++)
            {
                var cell = pawn.Position + GenAdj.AdjacentCells[i];
                if (!cell.InBounds(map))
                {
                    continue;
                }

                var things = cell.GetThingList(map);
                for (var j = 0; j < things.Count; j++)
                {
                    if (things[j] is not Pawn other || !IsAdjacentThreat(pawn, other))
                    {
                        continue;
                    }

                    if (other == ordered)
                    {
                        return other;
                    }

                    threat ??= other;
                }
            }

            if (threat != null)
            {
                return threat;
            }
        }

        if (hybrid.meleeOrderedTargetWhenAdjacent && ordered is { Spawned: true } && ordered.Map == map
            && pawn.CanReachImmediate(ordered, PathEndMode.Touch))
        {
            return ordered;
        }

        return null;
    }

    /// <summary>
    /// Same adjacency threat check vanilla uses in JobDriver_Wait.CheckForAutoAttack.
    /// </summary>
    private static bool IsAdjacentThreat(Pawn pawn, Pawn other)
    {
        if (other.ThreatDisabled(pawn) || !pawn.HostileTo(other) || other.IsPsychologicallyInvisible())
        {
            return false;
        }

        var activity = other.GetComp<CompActivity>();
        if (activity != null && !activity.IsActive)
        {
            return false;
        }

        return !pawn.ThreatDisabledBecauseNonAggressiveRoamer(other) && GenHostility.IsActiveThreatTo(other, pawn.Faction, ignoreHives: false);
    }
}
