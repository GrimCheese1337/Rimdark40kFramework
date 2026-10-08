using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace Core40k;

/// <summary>
/// A group of specific apparel that grants extra rewards when enough pieces are worn at once. Tiers are
/// cumulative unless cumulativeTiers is false, in which case only the highest reached tier applies.
/// </summary>
public class ApparelSetDef : Def
{
    public float sortOrder = 0f;

    public List<ThingDef> pieces = [];

    public bool cumulativeTiers = true;

    public List<SetBonusTier> tiers = [];

    public int CountIn(ICollection<ThingDef> worn)
    {
        var count = 0;
        foreach (var piece in pieces)
        {
            if (piece != null && worn.Contains(piece))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Info card text: description, the piece list (ticked when worn) and every tier with its rewards.
    /// </summary>
    public string ReportText(ApparelSetState state)
    {
        var builder = new StringBuilder();
        if (!description.NullOrEmpty())
        {
            builder.AppendLine(description);
            builder.AppendLine();
        }

        builder.AppendLine("BEWH.Framework.GeneCustomization.SetMembers".Translate());
        foreach (var piece in pieces)
        {
            if (piece == null)
            {
                continue;
            }

            var worn = state != null && state.IsWorn(piece);
            builder.AppendLine("  - " + piece.LabelCap + (worn ? " ✓" : string.Empty));
        }

        builder.AppendLine();
        builder.AppendLine("BEWH.Framework.ApparelSet.Bonuses".Translate());
        if (!cumulativeTiers)
        {
            builder.AppendLine("BEWH.Framework.ApparelSet.HighestTierOnly".Translate());
        }

        foreach (var tier in tiers)
        {
            var active = state != null && state.IsTierActive(tier);
            var header = "BEWH.Framework.GeneCustomization.TierRequires".Translate(tier.requiredCount).ToString();
            if (!tier.label.NullOrEmpty())
            {
                header = tier.label.CapitalizeFirst() + " - " + header;
            }
            if (active)
            {
                header += " " + "BEWH.Framework.ApparelSet.Active".Translate();
            }

            builder.AppendLine();
            builder.AppendLine(header);

            var tierDescription = tier.Description();
            if (!tierDescription.NullOrEmpty())
            {
                foreach (var line in tierDescription.Split('\n'))
                {
                    builder.AppendLine("    " + line.TrimEnd('\r'));
                }
            }
        }

        return builder.ToString().TrimEndNewlines();
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var configError in base.ConfigErrors())
        {
            yield return configError;
        }

        if (pieces.NullOrEmpty())
        {
            yield return "set has no pieces";
            yield break;
        }

        if (pieces.Any(piece => piece == null))
        {
            yield return "set has a null piece";
        }

        foreach (var piece in pieces)
        {
            if (piece != null && !piece.IsApparel)
            {
                yield return piece.defName + " is not apparel";
            }
        }

        if (pieces.Where(piece => piece != null).Distinct().Count() != pieces.Count(piece => piece != null))
        {
            yield return "set lists the same piece more than once";
        }

        foreach (var tier in tiers)
        {
            if (tier == null)
            {
                continue;
            }

            if (tier.requiredCount < 1)
            {
                yield return "tier requiredCount must be at least 1";
            }

            if (tier.requiredCount > pieces.Count)
            {
                yield return "tier requires " + tier.requiredCount + " pieces but the set only has " + pieces.Count;
            }
        }
    }

    public override void ResolveReferences()
    {
        base.ResolveReferences();

        tiers ??= [];
        tiers.RemoveAll(tier => tier == null);
        if (tiers.Count == 0)
        {
            tiers.Add(new SetBonusTier { requiredCount = pieces.Count });
        }

        tiers.SortBy(tier => tier.requiredCount);
    }
}
