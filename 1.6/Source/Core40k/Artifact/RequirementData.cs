using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Core40k;

/// <summary>
/// The six requirement lists RequirementUtility understands, as one XML node so kinds and traits do not repeat them.
/// </summary>
public class RequirementData
{
    public List<RankDef> mustHaveRank = null;
    public List<GeneDef> mustHaveGene = null;
    public List<GeneDef> mustHaveGeneOneAmong = null;
    public List<TraitData> mustHaveTrait = null;
    public List<HediffDef> mustHaveHediff = null;
    public List<ResearchProjectDef> mustHaveResearch = null;

    public bool IsEmpty =>
        mustHaveRank.NullOrEmpty()
        && mustHaveGene.NullOrEmpty()
        && mustHaveGeneOneAmong.NullOrEmpty()
        && mustHaveTrait.NullOrEmpty()
        && mustHaveHediff.NullOrEmpty()
        && mustHaveResearch.NullOrEmpty();

    public bool Meets(Pawn pawn)
    {
        return IsEmpty || RequirementUtility.MeetsRequirements(pawn, mustHaveRank, mustHaveGene, mustHaveTrait, mustHaveHediff, mustHaveResearch, mustHaveGeneOneAmong);
    }

    public bool Has(Pawn pawn, out string lockedReason)
    {
        if (IsEmpty)
        {
            lockedReason = null;
            return true;
        }

        return RequirementUtility.HasRequirements(pawn, mustHaveRank, mustHaveGene, mustHaveTrait, mustHaveHediff, mustHaveResearch, out lockedReason, mustHaveGeneOneAmong);
    }

    public static bool IsNullOrEmpty(RequirementData data)
    {
        return data == null || data.IsEmpty;
    }
}
