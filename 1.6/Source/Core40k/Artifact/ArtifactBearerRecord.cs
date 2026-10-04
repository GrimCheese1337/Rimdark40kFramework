using Verse;

namespace Core40k;

/// <summary>
/// One stretch of a pawn carrying the weapon. Name only, so the record outlives the pawn.
/// </summary>
public class ArtifactBearerRecord : IExposable
{
    public string pawnName;
    public int firstTick;
    public int lastTick;
    public int kills;
    public bool colonist;

    public ArtifactBearerRecord()
    {
    }

    public ArtifactBearerRecord(Pawn pawn)
    {
        pawnName = NameOf(pawn);
        firstTick = Find.TickManager?.TicksGame ?? 0;
        lastTick = firstTick;
        colonist = pawn?.IsColonist ?? false;
    }

    public static string NameOf(Pawn pawn)
    {
        return pawn?.Name?.ToStringShort ?? pawn?.LabelShortCap;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref pawnName, "pawnName");
        Scribe_Values.Look(ref firstTick, "firstTick", 0);
        Scribe_Values.Look(ref lastTick, "lastTick", 0);
        Scribe_Values.Look(ref kills, "kills", 0);
        Scribe_Values.Look(ref colonist, "colonist", false);
    }
}
