using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Core40k;

public class DevotionCreedDef : Def
{
    public DevotionFaithDef faith;

    public List<DevotionTierDef> tiers = [];

    public float maxStanding = 100f;

    public float decayPerDay = 0f;

    public float decayDelayDays = 0f;

    public float colonyWeightPerPawn = 1f;

    public List<DevotionBleed> bleed = [];

    public List<DevotionOpinion> opinions = [];

    public Color drawColor = Color.white;

    [NoTranslate]
    public string iconPath;

    private Texture2D cachedIcon;

    public Texture2D Icon
    {
        get
        {
            if (cachedIcon != null)
            {
                return cachedIcon;
            }

            cachedIcon = iconPath.NullOrEmpty() ? BaseContent.BadTex : ContentFinder<Texture2D>.Get(iconPath);
            return cachedIcon;
        }
    }

    public override void ResolveReferences()
    {
        base.ResolveReferences();

        if (tiers.NullOrEmpty())
        {
            return;
        }

        tiers.RemoveAll(tier => tier == null);
        tiers.SortBy(tier => tier.threshold);
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in base.ConfigErrors())
        {
            yield return error;
        }

        if (faith == null)
        {
            yield return "DevotionCreedDef has no faith.";
        }

        if (tiers.NullOrEmpty())
        {
            yield return "DevotionCreedDef has no tiers.";
        }
        else
        {
            if (tiers[0] != null && tiers[0].threshold <= 0f)
            {
                yield return "DevotionCreedDef first tier must have a threshold above 0.";
            }

            for (var i = 1; i < tiers.Count; i++)
            {
                if (tiers[i] != null && tiers[i - 1] != null && tiers[i].threshold <= tiers[i - 1].threshold)
                {
                    yield return "DevotionCreedDef tier thresholds must be strictly ascending (" + tiers[i].defName + ").";
                }
            }
        }

        if (!bleed.NullOrEmpty())
        {
            foreach (var entry in bleed)
            {
                if (entry?.creed == null)
                {
                    continue;
                }

                if (entry.creed == this)
                {
                    yield return "DevotionCreedDef bleeds into itself.";
                }
                else if (faith != null && entry.creed.faith != faith)
                {
                    yield return "DevotionCreedDef bleed target " + entry.creed.defName + " belongs to another faith.";
                }
            }
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

            if (entry.faith != null && entry.creed != null)
            {
                yield return "DevotionCreedDef opinion entry sets both faith and creed.";
            }

            if (entry.creed == this)
            {
                yield return "DevotionCreedDef has an opinion entry targeting itself.";
            }
        }
    }
}
