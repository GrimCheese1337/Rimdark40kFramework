using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace Core40k;

/// <summary>
/// One threshold of a set: the rewards granted once enough pieces of the set are active.
/// Shared by gene variant sets and apparel sets.
/// </summary>
public class SetBonusTier
{
    public int requiredCount = 1;

    public string label;

    public List<StatModifier> statOffsets = [];
    public List<StatModifier> statFactors = [];

    public List<AbilityDef> givesAbilities = [];
    public List<VEF.Abilities.AbilityDef> givesVFEAbilities = [];
    public List<HediffDef> givesHediffs = [];

    public string LabelOrCount => label.NullOrEmpty() ? "BEWH.Framework.GeneCustomization.TierPieces".Translate(requiredCount).ToString() : label;

    public string Description()
    {
        var builder = new StringBuilder();

        if (!statOffsets.NullOrEmpty())
        {
            foreach (var statOffset in statOffsets)
            {
                builder.AppendLine(statOffset.stat.LabelCap + ": " + statOffset.ValueToStringAsOffset);
            }
        }

        if (!statFactors.NullOrEmpty())
        {
            foreach (var statFactor in statFactors)
            {
                builder.AppendLine(statFactor.stat.LabelCap + ": " + statFactor.ToStringAsFactor);
            }
        }

        foreach (var ability in givesAbilities)
        {
            builder.AppendLine("BEWH.Framework.GeneCustomization.GrantsLine".Translate(ability.LabelCap));
        }

        foreach (var ability in givesVFEAbilities)
        {
            builder.AppendLine("BEWH.Framework.GeneCustomization.GrantsLine".Translate(ability.LabelCap));
        }

        foreach (var hediff in givesHediffs)
        {
            builder.AppendLine("BEWH.Framework.GeneCustomization.GrantsLine".Translate(hediff.LabelCap));
        }

        return builder.ToString().TrimEndNewlines();
    }
}
