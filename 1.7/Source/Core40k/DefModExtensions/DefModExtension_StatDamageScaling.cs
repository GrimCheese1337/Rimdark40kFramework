using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Core40k;

public enum StatDamageSource
{
    Wielder,
    Target,
}

public enum StatDamageMode
{
    Factor,
    Offset,
}

public class StatDamageScaling
{
    public StatDef stat;
    public StatDamageSource source = StatDamageSource.Wielder;
    public StatDamageMode mode = StatDamageMode.Factor;

    public SimpleCurve curve;
    public float baseline;
    public float perPoint;

    public bool pawnsOnly = true;

    /// <summary>
    /// Factor mode returns a multiplier, Offset mode a flat damage amount. A curve, if set, is used as-is.
    /// </summary>
    public float Evaluate(float statValue)
    {
        if (curve != null)
        {
            return curve.Evaluate(statValue);
        }

        var delta = (statValue - baseline) * perPoint;
        return mode == StatDamageMode.Factor ? 1f + delta : delta;
    }

    public bool CanReadFrom(Thing thing)
    {
        return thing != null && stat != null && (!pawnsOnly || thing is Pawn);
    }
}

public class DefModExtension_StatDamageScaling : DefModExtension
{
    public List<StatDamageScaling> scalings = new();
    public List<DamageDef> damageDefs;
    public float minFactor = 0f;
    public float maxFactor = 10f;

    public bool AppliesTo(DamageDef damageDef)
    {
        return damageDefs.NullOrEmpty() || damageDefs.Contains(damageDef);
    }

    /// <summary>
    /// Returns the damage amount after applying every scaling entry, clamped to minFactor/maxFactor of the input amount.
    /// </summary>
    public float ScaleDamage(float amount, Thing wielder, Thing target)
    {
        if (amount <= 0f || scalings.NullOrEmpty())
        {
            return amount;
        }

        var offset = 0f;
        var factor = 1f;
        foreach (var entry in scalings)
        {
            var source = entry.source == StatDamageSource.Wielder ? wielder : target;
            if (!entry.CanReadFrom(source))
            {
                continue;
            }

            var value = entry.Evaluate(source.GetStatValue(entry.stat));
            if (entry.mode == StatDamageMode.Factor)
            {
                factor *= value;
            }
            else
            {
                offset += value;
            }
        }

        var scaled = Math.Clamp((amount + offset) * factor, amount * minFactor, amount * maxFactor);
        return Math.Max(0f, scaled);
    }

    /// <summary>
    /// Factor and flat offset coming from wielder-side entries only, used for the info card.
    /// </summary>
    public void WielderContribution(Pawn wielder, out float factor, out float offset)
    {
        factor = 1f;
        offset = 0f;
        if (scalings.NullOrEmpty())
        {
            return;
        }

        foreach (var entry in scalings)
        {
            if (entry.source != StatDamageSource.Wielder || !entry.CanReadFrom(wielder))
            {
                continue;
            }

            var value = entry.Evaluate(wielder.GetStatValue(entry.stat));
            if (entry.mode == StatDamageMode.Factor)
            {
                factor *= value;
            }
            else
            {
                offset += value;
            }
        }
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in base.ConfigErrors())
        {
            yield return error;
        }

        if (scalings.NullOrEmpty())
        {
            yield return "DefModExtension_StatDamageScaling has no scalings.";
            yield break;
        }

        foreach (var scaling in scalings)
        {
            if (scaling.stat == null)
            {
                yield return "DefModExtension_StatDamageScaling entry has no stat.";
            }
            else if (scaling.curve == null && scaling.perPoint == 0f)
            {
                yield return $"DefModExtension_StatDamageScaling entry for {scaling.stat.defName} has neither curve nor perPoint.";
            }
        }

        if (maxFactor < minFactor)
        {
            yield return "DefModExtension_StatDamageScaling has maxFactor lower than minFactor.";
        }
    }
}
