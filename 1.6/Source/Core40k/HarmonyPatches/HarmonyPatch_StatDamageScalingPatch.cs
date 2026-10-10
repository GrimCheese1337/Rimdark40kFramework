using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
[HarmonyPriority(Priority.High)]
public static class StatDamageScalingPatch
{
    public static void Prefix(Thing __instance, ref DamageInfo dinfo)
    {
        var weaponScaling = dinfo.Weapon?.GetModExtension<DefModExtension_StatDamageScaling>();
        var damageScaling = dinfo.Def?.GetModExtension<DefModExtension_StatDamageScaling>();
        if (weaponScaling == null && damageScaling == null || dinfo.Amount <= 0f)
        {
            return;
        }

        var amount = dinfo.Amount;
        if (weaponScaling != null && weaponScaling.AppliesTo(dinfo.Def))
        {
            amount = weaponScaling.ScaleDamage(amount, dinfo.Instigator, __instance);
        }

        if (damageScaling != null && damageScaling != weaponScaling && damageScaling.AppliesTo(dinfo.Def))
        {
            amount = damageScaling.ScaleDamage(amount, dinfo.Instigator, __instance);
        }

        if (amount != dinfo.Amount)
        {
            dinfo.SetAmount(amount);
        }
    }
}

[HarmonyPatch(typeof(ThingDef), nameof(ThingDef.SpecialDisplayStats))]
public static class StatDamageScalingDisplayStatsPatch
{
    public static IEnumerable<StatDrawEntry> Postfix(IEnumerable<StatDrawEntry> values, ThingDef __instance, StatRequest req)
    {
        foreach (var value in values)
        {
            yield return value;
        }

        if (!__instance.IsWeapon)
        {
            yield break;
        }

        var scaling = __instance.GetModExtension<DefModExtension_StatDamageScaling>();
        if (scaling == null || scaling.scalings.NullOrEmpty())
        {
            yield break;
        }

        var wielder = (req.Thing?.ParentHolder as Pawn_EquipmentTracker)?.pawn;
        yield return Entry(scaling, wielder);
    }

    /// <summary>
    /// Builds the "Damage scaling" info card entry for a weapon carrying DefModExtension_StatDamageScaling.
    /// </summary>
    private static StatDrawEntry Entry(DefModExtension_StatDamageScaling scaling, Pawn wielder)
    {
        var valid = scaling.scalings.Where(s => s?.stat != null).ToList();
        var value = valid.Select(s => s.stat.LabelCap.ToString()).Distinct().ToCommaList();

        var report = new StringBuilder();
        report.AppendLine("BEWH.Framework.StatDamageScaling.Desc".Translate());

        foreach (var entry in valid)
        {
            report.AppendLine();
            var sourceLabel = entry.source == StatDamageSource.Wielder
                ? "BEWH.Framework.StatDamageScaling.Wielder".Translate()
                : "BEWH.Framework.StatDamageScaling.Target".Translate();
            report.AppendLine("BEWH.Framework.StatDamageScaling.EntryHeader".Translate(entry.stat.LabelCap, sourceLabel));

            if (entry.curve != null)
            {
                foreach (var point in entry.curve.Points)
                {
                    var effect = entry.mode == StatDamageMode.Factor
                        ? "x" + point.y.ToString("0.##")
                        : point.y.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Offset);
                    report.AppendLine("  - " + "BEWH.Framework.StatDamageScaling.CurvePoint".Translate(entry.stat.ValueToString(point.x), effect));
                }
            }
            else
            {
                var perPoint = entry.mode == StatDamageMode.Factor
                    ? entry.perPoint.ToStringByStyle(ToStringStyle.PercentZero, ToStringNumberSense.Offset)
                    : entry.perPoint.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Offset);
                report.AppendLine("  - " + "BEWH.Framework.StatDamageScaling.PerPoint".Translate(perPoint, entry.stat.ValueToString(entry.baseline)));
            }

            if (!entry.pawnsOnly)
            {
                report.AppendLine("  - " + "BEWH.Framework.StatDamageScaling.NonPawns".Translate());
            }
        }

        report.AppendLine();
        if (!scaling.damageDefs.NullOrEmpty())
        {
            report.AppendLine("BEWH.Framework.StatDamageScaling.DamageTypes".Translate(scaling.damageDefs.Where(d => d != null).Select(d => d.label).ToCommaList()));
        }
        report.AppendLine("BEWH.Framework.StatDamageScaling.Limit".Translate(scaling.minFactor.ToString("0.##"), scaling.maxFactor.ToString("0.##")));

        if (wielder != null && valid.Any(s => s.source == StatDamageSource.Wielder))
        {
            scaling.WielderContribution(wielder, out var factor, out var offset);
            var current = "x" + factor.ToString("0.##");
            if (offset != 0f)
            {
                current += ", " + offset.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Offset);
            }
            report.AppendLine("BEWH.Framework.StatDamageScaling.Current".Translate(wielder.LabelShort, current));
        }

        if (valid.Any(s => s.source == StatDamageSource.Target))
        {
            report.AppendLine("BEWH.Framework.StatDamageScaling.TargetDependent".Translate());
        }

        return new StatDrawEntry(StatCategoryDefOf.Weapon, "BEWH.Framework.StatDamageScaling.StatLabel".Translate(), value, report.ToString().TrimEndNewlines(), 4990);
    }
}
