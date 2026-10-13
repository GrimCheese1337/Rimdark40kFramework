using RimWorld;

namespace Core40k;

/// <summary>
/// A comp that adds to the stats of the pawn wearing or holding its parent, read by
/// GetValueUnfinalizedFromVariousFactorPatch through GameComponent_CoreUtils.cachedDecoratives.
/// </summary>
public interface IPawnStatSource
{
    float GetPawnStatOffset(StatDef stat);

    float GetPawnStatFactor(StatDef stat);
}
