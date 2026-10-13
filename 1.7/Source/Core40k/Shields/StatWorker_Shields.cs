using RimWorld;
using Verse;

namespace Core40k;

/// <summary>
/// Visibility worker for the BEWH_Shields category: same shape as <see cref="StatWorker_Voidfaring"/>,
/// shown on pawns only.
/// </summary>
public class StatWorker_Shields : StatWorker
{
    public override bool ShouldShowFor(StatRequest req)
    {
        return req.Def is ThingDef { category: ThingCategory.Pawn } && base.ShouldShowFor(req);
    }
}
