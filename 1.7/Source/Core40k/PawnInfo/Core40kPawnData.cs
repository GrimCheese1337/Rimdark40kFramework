using System.Runtime.CompilerServices;
using Verse;

namespace Core40k;

public static class Core40kPawnData
{
    private sealed class Holder
    {
        public CompRankInfo comp;
    }

    private static readonly ConditionalWeakTable<Pawn, Holder> table = new();

    /// <summary>
    /// Cached CompRankInfo lookup. Caches the null result too, so pawns without the comp are not
    /// rescanned. Entries are collected along with the pawn.
    /// Not for use in pawn generation patches that run before comps are set up; use GetComp there.
    /// </summary>
    public static CompRankInfo Of(Pawn pawn)
    {
        if (pawn == null)
        {
            return null;
        }

        return table.GetValue(pawn, static p => new Holder { comp = p.GetComp<CompRankInfo>() }).comp;
    }
}
