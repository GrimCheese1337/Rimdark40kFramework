using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Core40k;

public class ConsequenceTrackDef : Def
{
    public DevotionFaithDef faith;

    public float max = 100f;

    public float gainPerColonyStandingPerDay = 0f;

    public float decayPerDay = 0f;

    public List<ConsequenceThreshold> thresholds = [];

    public override void ResolveReferences()
    {
        base.ResolveReferences();

        if (!thresholds.NullOrEmpty())
        {
            thresholds.RemoveAll(threshold => threshold == null);
            thresholds.SortBy(threshold => threshold.at);
        }
    }

    public ConsequenceThreshold ActiveThresholdAt(float level)
    {
        if (thresholds.NullOrEmpty())
        {
            return null;
        }

        ConsequenceThreshold result = null;
        for (var i = 0; i < thresholds.Count; i++)
        {
            if (thresholds[i].at > level)
            {
                break;
            }

            result = thresholds[i];
        }

        return result;
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in base.ConfigErrors())
        {
            yield return error;
        }

        if (faith == null)
        {
            yield return "ConsequenceTrackDef has no faith.";
        }
    }
}

public class ConsequenceThreshold
{
    public float at = 0f;

    public List<IncidentDef> incidents = [];

    public float mtbDays = 0f;

    [MustTranslate]
    public string description;
}
