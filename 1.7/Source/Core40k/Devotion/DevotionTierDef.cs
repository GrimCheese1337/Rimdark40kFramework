using System.Collections.Generic;
using RimWorld;
using Verse;
using AbilityDef = RimWorld.AbilityDef;

namespace Core40k;

public class DevotionTierDef : Def
{
    public float threshold = 0f;

    public bool reversible = true;

    public float decayPerDayOverride = -1f;

    public List<HediffData> grantHediffs = [];

    public List<StatModifier> statOffsets = [];

    public List<StatModifier> statFactors = [];

    public List<AbilityDef> grantAbilities = [];

    public List<VEF.Abilities.AbilityDef> grantVFEAbilities = [];

    public List<TraitData> grantTraits = [];

    public List<ThoughtDef> memoriesOnReach = [];

    public List<DevotionTierEffect> extraEffects = [];

    public LetterDef letterOnReach;

    [MustTranslate]
    public string letterLabel;

    [MustTranslate]
    public string letterText;

    public virtual void Apply(CompRankInfo comp, DevotionCreedDef creed)
    {
        var pawn = comp?.ParentPawn;
        if (pawn == null)
        {
            return;
        }

        pawn.AddAbilities(grantAbilities, grantVFEAbilities);

        if (!grantHediffs.NullOrEmpty() && pawn.health != null)
        {
            foreach (var hediffData in grantHediffs)
            {
                if (hediffData?.hediffDef == null || pawn.health.hediffSet.HasHediff(hediffData.hediffDef))
                {
                    continue;
                }

                var hediff = HediffMaker.MakeHediff(hediffData.hediffDef, pawn, pawn.health.hediffSet.GetBodyPartRecord(hediffData.bodyPartDef));
                hediff.Severity = hediffData.initialSeverity;
                pawn.health.AddHediff(hediff);
            }
        }

        if (!grantTraits.NullOrEmpty() && pawn.story?.traits != null)
        {
            foreach (var traitData in grantTraits)
            {
                if (traitData?.traitDef == null || pawn.story.traits.HasTrait(traitData.traitDef, traitData.degree))
                {
                    continue;
                }

                pawn.story.traits.GainTrait(new Trait(traitData.traitDef, traitData.degree));
            }
        }

        if (!memoriesOnReach.NullOrEmpty() && pawn.needs?.mood?.thoughts?.memories != null)
        {
            foreach (var memory in memoriesOnReach)
            {
                if (memory != null)
                {
                    pawn.needs.mood.thoughts.memories.TryGainMemory(memory);
                }
            }
        }

        foreach (var effect in extraEffects)
        {
            effect?.Apply(pawn, creed, this);
        }
    }

    public virtual void Remove(CompRankInfo comp, DevotionCreedDef creed)
    {
        var pawn = comp?.ParentPawn;
        if (pawn == null)
        {
            return;
        }

        pawn.RemoveAbilities(grantAbilities, grantVFEAbilities);

        if (!grantHediffs.NullOrEmpty() && pawn.health != null)
        {
            foreach (var hediffData in grantHediffs)
            {
                if (hediffData?.hediffDef == null)
                {
                    continue;
                }

                var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffData.hediffDef);
                if (hediff != null)
                {
                    pawn.health.RemoveHediff(hediff);
                }
            }
        }

        if (!grantTraits.NullOrEmpty() && pawn.story?.traits != null)
        {
            foreach (var traitData in grantTraits)
            {
                if (traitData?.traitDef == null)
                {
                    continue;
                }

                var trait = pawn.story.traits.GetTrait(traitData.traitDef, traitData.degree);
                if (trait != null)
                {
                    pawn.story.traits.RemoveTrait(trait);
                }
            }
        }

        if (!memoriesOnReach.NullOrEmpty() && pawn.needs?.mood?.thoughts?.memories != null)
        {
            foreach (var memory in memoriesOnReach)
            {
                if (memory != null)
                {
                    pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(memory);
                }
            }
        }

        foreach (var effect in extraEffects)
        {
            effect?.Remove(pawn, creed, this);
        }
    }
}
