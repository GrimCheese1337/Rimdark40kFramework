using Verse;

namespace Core40k;

/// <summary>
/// A snapshot of something that must outlive the pawn. Holds no pawn or def references, so it survives the pawn
/// being discarded and the creed's content mod being removed.
/// </summary>
public class DevotionRecord : IExposable
{
    public string pawnName;

    [NoTranslate]
    public string creedDefName;

    public string creedLabel;

    public int tierReached;

    public int tick;

    public string outcome;

    public DevotionCreedDef Creed => creedDefName.NullOrEmpty()
        ? null
        : GenDefDatabase.GetDefSilentFail(typeof(DevotionCreedDef), creedDefName) as DevotionCreedDef;

    public string CreedLabel
    {
        get
        {
            var creed = Creed;
            if (creed != null)
            {
                return creed.LabelCap;
            }

            return creedLabel ?? creedDefName;
        }
    }

    public DevotionRecord()
    {
    }

    public DevotionRecord(Pawn pawn, DevotionCreedDef creed, int tierReached, string outcome)
    {
        pawnName = pawn?.Name?.ToStringShort ?? pawn?.LabelShortCap;
        creedDefName = creed?.defName;
        creedLabel = creed == null ? null : creed.LabelCap.ToString();
        this.tierReached = tierReached;
        this.outcome = outcome;
        tick = Find.TickManager?.TicksGame ?? 0;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref pawnName, "pawnName");
        Scribe_Values.Look(ref creedDefName, "creedDefName");
        Scribe_Values.Look(ref creedLabel, "creedLabel");
        Scribe_Values.Look(ref tierReached, "tierReached");
        Scribe_Values.Look(ref tick, "tick");
        Scribe_Values.Look(ref outcome, "outcome");
    }
}
