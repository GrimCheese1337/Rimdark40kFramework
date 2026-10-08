using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Core40k;

//Startup index of which apparel belongs to which sets and everything apparel sets can grant, so gear
//changes never have to scan the def database.
[StaticConstructorOnStartup]
public static class ApparelSetIndex
{
    private static Dictionary<ThingDef, List<ApparelSetDef>> setsByThing;
    private static HashSet<HediffDef> grantableHediffs;
    private static HashSet<AbilityDef> grantableAbilities;
    private static HashSet<VEF.Abilities.AbilityDef> grantableVFEAbilities;

    private static readonly List<ApparelSetDef> EmptySets = [];

    static ApparelSetIndex()
    {
        Build();
    }

    private static void EnsureBuilt()
    {
        if (setsByThing == null)
        {
            Build();
        }
    }

    public static void Build()
    {
        setsByThing = new Dictionary<ThingDef, List<ApparelSetDef>>();
        grantableHediffs = [];
        grantableAbilities = [];
        grantableVFEAbilities = [];

        foreach (var set in DefDatabase<ApparelSetDef>.AllDefsListForReading)
        {
            foreach (var piece in set.pieces)
            {
                if (piece == null)
                {
                    continue;
                }

                if (!setsByThing.TryGetValue(piece, out var list))
                {
                    list = [];
                    setsByThing.Add(piece, list);
                }

                if (!list.Contains(set))
                {
                    list.Add(set);
                }
            }

            foreach (var tier in set.tiers)
            {
                if (!tier.givesHediffs.NullOrEmpty())
                {
                    grantableHediffs.AddRange(tier.givesHediffs);
                }
                if (!tier.givesAbilities.NullOrEmpty())
                {
                    grantableAbilities.AddRange(tier.givesAbilities);
                }
                if (!tier.givesVFEAbilities.NullOrEmpty())
                {
                    grantableVFEAbilities.AddRange(tier.givesVFEAbilities);
                }
            }
        }

        foreach (var list in setsByThing.Values)
        {
            list.SortBy(def => def.sortOrder);
        }

        WarnSharedGrants();
    }

    private static void WarnSharedGrants()
    {
        if (grantableHediffs.Count == 0 && grantableAbilities.Count == 0 && grantableVFEAbilities.Count == 0)
        {
            return;
        }

        var reported = new HashSet<Def>();

        void Check(List<HediffDef> hediffs, List<AbilityDef> abilities, List<VEF.Abilities.AbilityDef> vfeAbilities, string source)
        {
            if (!hediffs.NullOrEmpty())
            {
                foreach (var def in hediffs)
                {
                    if (def != null && grantableHediffs.Contains(def) && reported.Add(def))
                    {
                        Log.Warning("[RimDark] " + def.defName + " is granted by an apparel set and by " + source + ". Each system removes it when its own bonus ends, so give set bonuses their own defs.");
                    }
                }
            }

            if (!abilities.NullOrEmpty())
            {
                foreach (var def in abilities)
                {
                    if (def != null && grantableAbilities.Contains(def) && reported.Add(def))
                    {
                        Log.Warning("[RimDark] " + def.defName + " is granted by an apparel set and by " + source + ". Each system removes it when its own bonus ends, so give set bonuses their own defs.");
                    }
                }
            }

            if (!vfeAbilities.NullOrEmpty())
            {
                foreach (var def in vfeAbilities)
                {
                    if (def != null && grantableVFEAbilities.Contains(def) && reported.Add(def))
                    {
                        Log.Warning("[RimDark] " + def.defName + " is granted by an apparel set and by " + source + ". Each system removes it when its own bonus ends, so give set bonuses their own defs.");
                    }
                }
            }
        }

        foreach (var variant in DefDatabase<GeneVariantDef>.AllDefsListForReading)
        {
            Check(variant.givesHediffs, variant.givesAbilities, variant.givesVFEAbilities, variant.defName);
        }

        foreach (var set in DefDatabase<GeneVariantSetDef>.AllDefsListForReading)
        {
            foreach (var tier in set.tiers)
            {
                Check(tier.givesHediffs, tier.givesAbilities, tier.givesVFEAbilities, set.defName);
            }
        }

        foreach (var decoration in DefDatabase<DecorationDef>.AllDefsListForReading)
        {
            Check(decoration.givesHediffs, decoration.givesAbilities, decoration.givesVFEAbilities, decoration.defName);
        }
    }

    public static bool AnySets
    {
        get
        {
            EnsureBuilt();
            return setsByThing.Count != 0;
        }
    }

    public static bool AnyGrants
    {
        get
        {
            EnsureBuilt();
            return grantableHediffs.Count != 0 || grantableAbilities.Count != 0 || grantableVFEAbilities.Count != 0;
        }
    }

    public static List<ApparelSetDef> SetsFor(ThingDef thingDef)
    {
        EnsureBuilt();
        return thingDef != null && setsByThing.TryGetValue(thingDef, out var list) ? list : EmptySets;
    }

    public static bool IsPiece(ThingDef thingDef)
    {
        EnsureBuilt();
        return thingDef != null && setsByThing.ContainsKey(thingDef);
    }

    public static bool IsGrantable(HediffDef hediffDef)
    {
        EnsureBuilt();
        return hediffDef != null && grantableHediffs.Contains(hediffDef);
    }

    public static bool IsGrantable(AbilityDef abilityDef)
    {
        EnsureBuilt();
        return abilityDef != null && grantableAbilities.Contains(abilityDef);
    }

    public static bool IsGrantable(VEF.Abilities.AbilityDef abilityDef)
    {
        EnsureBuilt();
        return abilityDef != null && grantableVFEAbilities.Contains(abilityDef);
    }
}
