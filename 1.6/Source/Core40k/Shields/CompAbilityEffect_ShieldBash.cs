using RimWorld;
using Verse;

namespace Core40k;

public class CompAbilityEffect_ShieldBash : CompAbilityEffect
{
    public new CompProperties_AbilityShieldBash Props => (CompProperties_AbilityShieldBash)props;

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);

        var caster = parent.pawn;
        if (caster == null || target.Pawn is not { Spawned: true } victim)
        {
            return;
        }

        var damage = Props.damageAmount;
        if (Props.scaleStat != null)
        {
            damage *= caster.GetStatValue(Props.scaleStat) * Props.scaleFactor;
        }

        ShieldBashUtility.Apply(caster, victim, new ShieldBashParams
        {
            damageDef = Props.damageDef,
            damageAmount = damage,
            armorPenetration = Props.armorPenetration,
            stunTicks = Props.stunTicks,
            knockbackCells = Props.knockbackCells,
            maxKnockbackBodySize = Props.maxKnockbackBodySize,
            impactDamage = Props.impactDamage,
            impactFleck = Props.impactFleck,
            impactSound = Props.impactSound,
            weapon = ShieldUtils.GetWornShield(caster)?.parent.def
        });
    }

    public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
    {
        if (target.Pawn == null)
        {
            return false;
        }

        if (!HasUsableShield(out var reason))
        {
            if (throwMessages)
            {
                Messages.Message(reason, parent.pawn, MessageTypeDefOf.RejectInput, false);
            }

            return false;
        }

        return base.Valid(target, throwMessages);
    }

    public override bool GizmoDisabled(out string reason)
    {
        if (!HasUsableShield(out reason))
        {
            return true;
        }

        return base.GizmoDisabled(out reason);
    }

    private bool HasUsableShield(out string reason)
    {
        reason = null;
        var pawn = parent.pawn;
        var shield = ShieldUtils.GetWornShield(pawn);
        if (shield == null)
        {
            reason = "BEWH.Framework.Shield.Bash.NoShield".Translate();
            return false;
        }

        if (!ShieldUtils.IsActive(pawn, shield, out var reasonKey))
        {
            reason = "BEWH.Framework.Shield.Bash.Inactive".Translate(reasonKey.Translate());
            return false;
        }

        return true;
    }
}
