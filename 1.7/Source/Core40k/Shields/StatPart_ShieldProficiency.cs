using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Core40k;

/// <summary>
/// Explains on the proficiency stat card which shield is worn and whether it can currently be used.
/// Does not change the value.
/// </summary>
public class StatPart_ShieldProficiency : StatPart
{
    public override void TransformValue(StatRequest req, ref float val)
    {
    }

    public override string ExplanationPart(StatRequest req)
    {
        if (!req.HasThing || req.Thing is not Pawn pawn || pawn.apparel == null)
        {
            return null;
        }

        var shield = ShieldUtils.GetWornShield(pawn);
        if (shield == null)
        {
            return "BEWH.Framework.Shield.Proficiency.NoShield".Translate();
        }

        var line = "BEWH.Framework.Shield.Proficiency.Worn".Translate(shield.parent.LabelCap);
        if (!ShieldUtils.IsActive(pawn, shield, out var reasonKey))
        {
            line += "\n" + "BEWH.Framework.Shield.Proficiency.Inactive".Translate(reasonKey.Translate());
        }

        return line;
    }

    public override IEnumerable<Dialog_InfoCard.Hyperlink> GetInfoCardHyperlinks(StatRequest req)
    {
        if (req.HasThing && req.Thing is Pawn pawn && ShieldUtils.GetWornShield(pawn) is { } shield)
        {
            yield return new Dialog_InfoCard.Hyperlink(shield.parent);
        }
    }
}
