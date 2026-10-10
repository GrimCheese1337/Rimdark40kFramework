using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace Core40k;

/// <summary>
/// A group of specific apparel, weapons and decorations that grants extra rewards when enough of them are
/// worn, equipped or fitted at once. Tiers are cumulative unless cumulativeTiers is false, in which case
/// only the highest reached tier applies.
/// </summary>
public class ApparelSetDef : Def
{
    public float sortOrder = 0f;

    public List<ThingDef> pieces = [];

    public List<SetWeaponPiece> weaponPieces = [];

    public List<DecorationDef> decorations = [];

    public bool cumulativeTiers = true;

    public List<ApparelSetTier> tiers = [];

    public int TotalPieces => pieces.Count + weaponPieces.Count + decorations.Count;

    public bool HasWeaponPieces => !weaponPieces.NullOrEmpty();

    public bool MatchesWeapon(ThingDef thingDef)
    {
        if (thingDef == null || weaponPieces.NullOrEmpty())
        {
            return false;
        }

        foreach (var weaponPiece in weaponPieces)
        {
            if (weaponPiece.Matches(thingDef))
            {
                return true;
            }
        }

        return false;
    }

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
    /// Counts every apparel piece worn, weapon piece matched by an equipped weapon and decoration fitted.
    /// </summary>
    public int CountIn(ICollection<ThingDef> worn, ICollection<ThingDef> held, ICollection<DecorationDef> fitted, out bool weaponMatched)
    {
        var count = CountIn(worn);

        weaponMatched = false;
        foreach (var weaponPiece in weaponPieces)
        {
            foreach (var thingDef in held)
            {
                if (weaponPiece.Matches(thingDef))
                {
                    count++;
                    weaponMatched = true;
                    break;
                }
            }
        }

        foreach (var decoration in decorations)
        {
            if (decoration != null && fitted.Contains(decoration))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Info card text: description, the member lists (ticked when counted) and every tier with its rewards.
    /// </summary>
    public string ReportText(ApparelSetState state)
    {
        var builder = new StringBuilder();
        if (!description.NullOrEmpty())
        {
            builder.AppendLine(description);
            builder.AppendLine();
        }

        if (!pieces.NullOrEmpty())
        {
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
        }

        if (!weaponPieces.NullOrEmpty())
        {
            if (!pieces.NullOrEmpty())
            {
                builder.AppendLine();
            }
            builder.AppendLine("BEWH.Framework.ApparelSet.WeaponMembers".Translate());
            foreach (var weaponPiece in weaponPieces)
            {
                var held = state != null && state.IsWeaponPieceMatched(weaponPiece);
                builder.AppendLine("  - " + weaponPiece.LabelCap + (held ? " ✓" : string.Empty));
            }
        }

        if (!decorations.NullOrEmpty())
        {
            if (!pieces.NullOrEmpty() || !weaponPieces.NullOrEmpty())
            {
                builder.AppendLine();
            }
            builder.AppendLine("BEWH.Framework.ApparelSet.DecorationMembers".Translate());
            foreach (var decoration in decorations)
            {
                if (decoration == null)
                {
                    continue;
                }

                var fitted = state != null && state.IsFitted(decoration);
                builder.AppendLine("  - " + decoration.LabelCap + (fitted ? " ✓" : string.Empty));
            }
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

            AppendIndented(builder, tier.ExtraDescription());
            AppendIndented(builder, tier.Description());
        }

        return builder.ToString().TrimEndNewlines();
    }

    private static void AppendIndented(StringBuilder builder, string text)
    {
        if (text.NullOrEmpty())
        {
            return;
        }

        foreach (var line in text.Split('\n'))
        {
            builder.AppendLine("    " + line.TrimEnd('\r'));
        }
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var configError in base.ConfigErrors())
        {
            yield return configError;
        }

        pieces ??= [];
        weaponPieces ??= [];
        decorations ??= [];

        if (TotalPieces == 0)
        {
            yield return "set has no pieces, weaponPieces or decorations";
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

        foreach (var weaponPiece in weaponPieces)
        {
            if (weaponPiece == null)
            {
                yield return "set has a null weapon piece";
                continue;
            }

            if (weaponPiece.weapons.NullOrEmpty() && weaponPiece.weaponTags.NullOrEmpty())
            {
                yield return "weapon piece " + weaponPiece.label + " has neither weapons nor weaponTags";
            }

            if (weaponPiece.weapons == null)
            {
                continue;
            }

            foreach (var weapon in weaponPiece.weapons)
            {
                if (weapon == null)
                {
                    yield return "weapon piece " + weaponPiece.label + " has a null weapon";
                }
                else if (!weapon.IsWeapon)
                {
                    yield return weapon.defName + " in weapon piece " + weaponPiece.label + " is not a weapon";
                }
            }
        }

        if (decorations.Any(decoration => decoration == null))
        {
            yield return "set has a null decoration";
        }

        if (decorations.Where(decoration => decoration != null).Distinct().Count() != decorations.Count(decoration => decoration != null))
        {
            yield return "set lists the same decoration more than once";
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

            if (tier.requiredCount > TotalPieces)
            {
                yield return "tier requires " + tier.requiredCount + " pieces but the set only has " + TotalPieces;
            }

            if (!HasWeaponPieces && (tier.requiresWeapon || tier.verbModifier != null))
            {
                yield return "tier uses requiresWeapon or verbModifier but the set has no weaponPieces";
            }

            if (tier.requiredDecorations == null)
            {
                continue;
            }

            foreach (var required in tier.requiredDecorations)
            {
                if (required == null)
                {
                    yield return "tier has a null requiredDecorations entry";
                }
                else if (!decorations.Contains(required))
                {
                    yield return "tier requires decoration " + required.defName + " which is not listed in the set's decorations";
                }
            }
        }
    }

    public override void ResolveReferences()
    {
        base.ResolveReferences();

        pieces ??= [];
        weaponPieces ??= [];
        decorations ??= [];
        weaponPieces.RemoveAll(piece => piece == null);

        tiers ??= [];
        tiers.RemoveAll(tier => tier == null);
        if (tiers.Count == 0)
        {
            tiers.Add(new ApparelSetTier { requiredCount = TotalPieces });
        }

        tiers.SortBy(tier => tier.requiredCount);
    }
}
