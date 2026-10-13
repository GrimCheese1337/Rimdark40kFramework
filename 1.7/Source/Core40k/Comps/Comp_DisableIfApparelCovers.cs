using RimWorld;
using Verse;

namespace Core40k;

public class Comp_DisableIfApparelCovers : CompAbilityEffect
{
    private new CompProperties_DisableIfApparelCovers Props => (CompProperties_DisableIfApparelCovers)props;

    public override bool CanCast => !TryGetBlockingApparel(out _, out _);

    /// <summary>
    /// Finds the first worn apparel that covers one of the body part groups listed in the comp props.
    /// </summary>
    private bool TryGetBlockingApparel(out Apparel blockingApparel, out BodyPartGroupDef coveredBodyPart)
    {
        blockingApparel = null;
        coveredBodyPart = null;

        if (parent.pawn?.apparel == null)
        {
            return false;
        }

        foreach (var apparel in parent.pawn.apparel.WornApparel)
        {
            foreach (var bodyPart in Props.disabledIfCovered)
            {
                if (!apparel.def.apparel.bodyPartGroups.Contains(bodyPart))
                {
                    continue;
                }

                blockingApparel = apparel;
                coveredBodyPart = bodyPart;
                return true;
            }
        }

        return false;
    }

    public override bool GizmoDisabled(out string reason)
    {
        if (TryGetBlockingApparel(out var blockingApparel, out var coveredBodyPart))
        {
            reason = "BEWH.Framework.Comp.AbilityDisabledByCoveredPart".Translate(parent.def.LabelCap, coveredBodyPart.LabelCap, blockingApparel.Label);
            return true;
        }

        return base.GizmoDisabled(out reason);
    }
}
