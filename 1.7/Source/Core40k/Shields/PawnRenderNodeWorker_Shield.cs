using RimWorld;
using Verse;

namespace Core40k;

/// <summary>
/// Render worker for worn shields. Same visibility rules as a backpack, shown on all four rotations,
/// hidden outside combat unless the always-show setting is on, and hidden while the wearer is downed
/// or asleep when the shield sets hideWhenInactive. Only field reads here, since this can run in the
/// parallel render phase.
/// </summary>
public class PawnRenderNodeWorker_Shield : PawnRenderNodeWorker
{
    public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
    {
        var pawn = parms.pawn;
        if (pawn == null)
        {
            return true;
        }

        if (parms.Portrait)
        {
            if ((parms.flags & PawnRenderFlags.Clothes) != PawnRenderFlags.Clothes)
            {
                return false;
            }
        }
        else
        {
            if (!IsVisibleInPosture(pawn, parms))
            {
                return false;
            }

            var apparel = node.apparel;
            if (apparel != null && Core40kUtils.DefHasComp<Comp_PhysicalShield>(apparel.def))
            {
                var shield = apparel.GetComp<Comp_PhysicalShield>();
                if (shield != null)
                {
                    if (!shield.Visible)
                    {
                        return false;
                    }

                    if (shield.Props.hideWhenInactive && (pawn.Downed || pawn.jobs?.curDriver?.asleep == true))
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private static bool IsVisibleInPosture(Pawn pawn, PawnDrawParms parms)
    {
        if (parms.posture is PawnPosture.Standing or PawnPosture.LayingOnGroundNormal or PawnPosture.LayingOnGroundFaceUp)
        {
            return true;
        }

        var drawBodyOverride = pawn.mindState?.duty?.def?.drawBodyOverride;
        if (drawBodyOverride.HasValue)
        {
            return drawBodyOverride.Value;
        }

        if (parms.bed != null && pawn.RaceProps.Humanlike)
        {
            return parms.bed.def.building.bed_showSleeperBody;
        }

        return true;
    }
}
