using System;
using RimWorld.Planet;
using Verse;

namespace Core40k;

public class Ability_ShootProjectileWithCharges : VEF.Abilities.Ability_ShootProjectile
{
    private int chargesRemaining = -1;
    private int nextChargeTick = -1;

    public int MaxCharges => Math.Max(1, def.GetModExtension<DefModExtension_AbilityCharges>()?.maxCharges ?? 1);

    public int ChargesRemaining
    {
        get
        {
            RefreshCharges();
            return chargesRemaining;
        }
    }

    private int RechargeTime => Math.Max(1, GetCooldownForPawn());

    /// <summary>
    /// Grants any charges the elapsed time has earned and mirrors the result onto the base cooldown field,
    /// so the ability reads as off cooldown while charges remain and as recharging once empty.
    /// </summary>
    private void RefreshCharges()
    {
        var maxCharges = MaxCharges;

        if (chargesRemaining < 0)
        {
            chargesRemaining = maxCharges;
            nextChargeTick = -1;
        }
        else if (chargesRemaining > maxCharges)
        {
            chargesRemaining = maxCharges;
        }

        var currentTick = Find.TickManager.TicksGame;

        while (chargesRemaining < maxCharges && nextChargeTick >= 0 && currentTick >= nextChargeTick)
        {
            chargesRemaining++;
            nextChargeTick += RechargeTime;
        }

        if (chargesRemaining >= maxCharges)
        {
            nextChargeTick = -1;
        }
        else if (nextChargeTick < 0)
        {
            nextChargeTick = currentTick + RechargeTime;
        }

        cooldown = chargesRemaining > 0 ? 0 : nextChargeTick;
    }

    public override bool IsEnabledForPawn(out string reason)
    {
        RefreshCharges();
        return base.IsEnabledForPawn(out reason);
    }

    public override Gizmo GetGizmo()
    {
        RefreshCharges();
        return base.GetGizmo();
    }

    public override string GetDescriptionForPawn()
    {
        var text = base.GetDescriptionForPawn();

        if (MaxCharges <= 1)
        {
            return text;
        }

        RefreshCharges();
        return text + "\n" + "BEWH.Framework.Ability.Charges".Translate(chargesRemaining, MaxCharges).Colorize(ColorLibrary.LightBlue);
    }

    public override void Cast(params GlobalTargetInfo[] targets)
    {
        RefreshCharges();

        if (nextChargeTick < 0)
        {
            nextChargeTick = Find.TickManager.TicksGame + RechargeTime;
        }

        chargesRemaining = Math.Max(0, chargesRemaining - 1);

        base.Cast(targets);

        RefreshCharges();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref chargesRemaining, "chargesRemaining", -1);
        Scribe_Values.Look(ref nextChargeTick, "nextChargeTick", -1);
    }
}
