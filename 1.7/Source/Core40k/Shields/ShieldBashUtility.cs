using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.Sound;

namespace Core40k;

public static class ShieldBashUtility
{
    private static List<(Pawn caster, Pawn target, ShieldBashParams bash)> pending = [];
    private static List<(Pawn caster, Pawn target, ShieldBashParams bash)> processing = [];

    public static bool ApplyingQueued { get; private set; }

    public static void ClearQueue()
    {
        pending.Clear();
        processing.Clear();
        ApplyingQueued = false;
    }

    /// <summary>
    /// Queues a bash for the next tick, for bashes triggered from inside another pawn's damage handling.
    /// </summary>
    public static void Queue(Pawn caster, Pawn target, ShieldBashParams bash)
    {
        pending.Add((caster, target, bash));
    }

    public static void Tick()
    {
        if (pending.Count == 0)
        {
            return;
        }

        (pending, processing) = (processing, pending);
        ApplyingQueued = true;
        try
        {
            for (var i = 0; i < processing.Count; i++)
            {
                var (caster, target, bash) = processing[i];
                if (caster is not { Spawned: true, Dead: false } || target is not { Spawned: true, Dead: false } || caster.Map != target.Map)
                {
                    continue;
                }

                Apply(caster, target, bash);
            }
        }
        finally
        {
            processing.Clear();
            ApplyingQueued = false;
        }
    }

    /// <summary>
    /// Damages, knocks back and stuns the target, in that order.
    /// </summary>
    public static void Apply(Pawn caster, Pawn target, ShieldBashParams bash)
    {
        if (caster == null || target is not { Spawned: true })
        {
            return;
        }

        var map = target.Map;

        if (bash.damageAmount > 0f)
        {
            var damageDef = bash.damageDef ?? DamageDefOf.Blunt;
            var armorPenetration = bash.armorPenetration >= 0f ? bash.armorPenetration : bash.damageAmount * 0.015f;
            var dinfo = new DamageInfo(damageDef, bash.damageAmount, armorPenetration, (target.Position - caster.Position).AngleFlat, caster, weapon: bash.weapon);
            target.TakeDamage(dinfo);
        }

        if (target.Dead || !target.Spawned)
        {
            return;
        }

        if (bash.knockbackCells > 0 && target.BodySize <= bash.maxKnockbackBodySize && target.CurrentBed() == null)
        {
            Knockback(caster, target, bash);
        }

        if (target.Dead || !target.Spawned)
        {
            return;
        }

        if (bash.stunTicks > 0)
        {
            target.stances?.stunner?.StunFor(bash.stunTicks, caster, true, true, false);
        }

        if (bash.impactFleck != null)
        {
            FleckMaker.Static(target.Position, map, bash.impactFleck);
        }

        bash.impactSound?.PlayOneShot(new TargetInfo(target.Position, map));
    }

    private static void Knockback(Pawn caster, Pawn target, ShieldBashParams bash)
    {
        var map = target.Map;
        var offset = target.Position - caster.Position;
        var step = offset == IntVec3.Zero ? caster.Rotation.FacingCell : new IntVec3(Math.Sign(offset.x), 0, Math.Sign(offset.z));

        var dest = target.Position;
        var blocked = false;
        for (var i = 0; i < bash.knockbackCells; i++)
        {
            var next = dest + step;
            if (!CanEnter(dest, next, step, map))
            {
                blocked = true;
                break;
            }

            dest = next;
        }

        if (dest != target.Position)
        {
            target.Position = dest;
            target.Notify_Teleported(true, false);
        }

        if (blocked && bash.impactDamage > 0f && !target.Dead)
        {
            target.TakeDamage(new DamageInfo(DamageDefOf.Blunt, bash.impactDamage, 0f, -1f, caster));
        }
    }

    private static bool CanEnter(IntVec3 from, IntVec3 next, IntVec3 step, Map map)
    {
        if (!next.InBounds(map) || !next.Standable(map) || next.GetFirstPawn(map) != null)
        {
            return false;
        }

        if (step.x != 0 && step.z != 0)
        {
            var sideX = from + new IntVec3(step.x, 0, 0);
            var sideZ = from + new IntVec3(0, 0, step.z);
            if (!sideX.Standable(map) || !sideZ.Standable(map))
            {
                return false;
            }
        }

        return true;
    }
}
