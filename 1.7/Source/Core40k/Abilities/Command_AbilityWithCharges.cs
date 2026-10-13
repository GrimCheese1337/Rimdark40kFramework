using UnityEngine;
using Verse;

namespace Core40k;

public class Command_AbilityWithCharges : VEF.Abilities.Command_Ability
{
    public Command_AbilityWithCharges(Pawn pawn, VEF.Abilities.Ability ability)
        : base(pawn, ability)
    {
    }

    protected override GizmoResult GizmoOnGUIInt(Rect butRect, GizmoRenderParms parms)
    {
        var result = base.GizmoOnGUIInt(butRect, parms);
        
        if (ability is not Ability_ShootProjectileWithCharges chargedAbility || chargedAbility.MaxCharges <= 1)
        {
            return result;
        }

        var anchor = Text.Anchor;
        var font = Text.Font;

        Text.Font = GameFont.Tiny;
        Text.Anchor = TextAnchor.LowerLeft;
        Widgets.Label(butRect.ContractedBy(2f), chargedAbility.ChargesRemaining + " / " + chargedAbility.MaxCharges);
        Text.Anchor = anchor;
        Text.Font = font;

        return result;
    }
}
