using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace Core40k;

/// <summary>
/// An apparel set tier. On top of the shared tier rewards it can require a set weapon or specific set
/// decorations, and can modify the verbs of the set's own weapons.
/// </summary>
public class ApparelSetTier : SetBonusTier
{
    public bool requiresWeapon = false;

    public List<DecorationDef> requiredDecorations = [];

    public VerbModifier verbModifier;

    public bool HasVerbModifier => verbModifier != null
                                   && (verbModifier.additionalBurstShotCount != 0
                                       || verbModifier.additionalRange != 0f
                                       || verbModifier.additionalWarmupTime != 0f);

    /// <summary>
    /// Requirement and verb lines shown under the shared tier description.
    /// </summary>
    public string ExtraDescription()
    {
        var builder = new StringBuilder();

        if (requiresWeapon)
        {
            builder.AppendLine("BEWH.Framework.ApparelSet.RequiresWeapon".Translate());
        }

        if (!requiredDecorations.NullOrEmpty())
        {
            var labels = requiredDecorations.Where(def => def != null).Select(def => def.LabelCap.ToString());
            builder.AppendLine("BEWH.Framework.ApparelSet.RequiresDecorations".Translate(string.Join(", ", labels)));
        }

        if (HasVerbModifier)
        {
            if (verbModifier.additionalBurstShotCount != 0)
            {
                builder.AppendLine("BEWH.Framework.Artifact.BurstShots".Translate(verbModifier.additionalBurstShotCount.ToStringWithSign()));
            }
            if (verbModifier.additionalRange != 0f)
            {
                builder.AppendLine("BEWH.Framework.Artifact.Range".Translate(verbModifier.additionalRange.ToStringWithSign("0.#")));
            }
            if (verbModifier.additionalWarmupTime != 0f)
            {
                builder.AppendLine("BEWH.Framework.Artifact.Warmup".Translate(verbModifier.additionalWarmupTime.ToStringWithSign("0.##")));
            }
        }

        return builder.ToString().TrimEndNewlines();
    }
}
