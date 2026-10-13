using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Core40k;

public class DevotionFaithDef : Def
{
    public DevotionTolerance tolerance = DevotionTolerance.Exclusive;

    public DevotionBudget budget = DevotionBudget.Independent;

    public DevotionPolarity polarity = DevotionPolarity.Consensual;

    public float colonySmoothingPerDay = 0.15f;

    public float focusToleranceCurveMax = 1f;

    public List<DevotionOpinion> opinions = [];

    [NoTranslate]
    public string iconPath;

    private List<DevotionCreedDef> cachedCreeds;

    public List<DevotionCreedDef> Creeds
    {
        get
        {
            return cachedCreeds ??= DefDatabase<DevotionCreedDef>.AllDefsListForReading
                .Where(creed => creed.faith == this)
                .ToList();
        }
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in base.ConfigErrors())
        {
            yield return error;
        }

        if (opinions.NullOrEmpty())
        {
            yield break;
        }

        foreach (var entry in opinions)
        {
            if (entry == null)
            {
                continue;
            }

            if (entry.creed != null)
            {
                yield return "DevotionFaithDef opinions may only target a faith, not a creed (" + entry.creed.defName + ").";
            }

            if (entry.faith == this)
            {
                yield return "DevotionFaithDef has an opinion entry targeting itself.";
            }
        }
    }
}
