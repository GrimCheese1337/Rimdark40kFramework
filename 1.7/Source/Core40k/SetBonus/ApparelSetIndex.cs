using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Core40k;

//Startup index of which apparel, weapons and decorations belong to which sets and everything apparel
//sets can grant, so gear changes never have to scan the def database.
[StaticConstructorOnStartup]
public static class ApparelSetIndex
{
    private static Dictionary<ThingDef, List<ApparelSetDef>> setsByThing;
    private static Dictionary<DecorationDef, List<ApparelSetDef>> setsByDecoration;
    private static HashSet<ThingDef> verbModifiedWeapons;
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
        setsByDecoration = new Dictionary<DecorationDef, List<ApparelSetDef>>();
        verbModifiedWeapons = [];
        grantableHediffs = [];
        grantableAbilities = [];
        grantableVFEAbilities = [];

        var sets = DefDatabase<ApparelSetDef>.AllDefsListForReading;
        var defsByWeaponTag = BuildWeaponTagLookup(sets);

        foreach (var set in sets)
        {
            foreach (var piece in set.pieces)
            {
                AddTo(setsByThing, piece, set);
            }

            var anyVerbModifier = false;
            foreach (var tier in set.tiers)
            {
                if (tier.HasVerbModifier)
                {
                    anyVerbModifier = true;
                }
            }

            foreach (var weaponPiece in set.weaponPieces)
            {
                weaponPiece.Resolve(defsByWeaponTag);
                if (weaponPiece.resolved.Count == 0)
                {
                    Log.Warning("[RimDark] Apparel set " + set.defName + " weapon piece " + weaponPiece.LabelCap + " matches no loaded weapon.");
                    continue;
                }

                foreach (var weaponDef in weaponPiece.resolved)
                {
                    AddTo(setsByThing, weaponDef, set);
                    if (anyVerbModifier)
                    {
                        verbModifiedWeapons.Add(weaponDef);
                    }
                }
            }

            foreach (var decoration in set.decorations)
            {
                AddTo(setsByDecoration, decoration, set);
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
        foreach (var list in setsByDecoration.Values)
        {
            list.SortBy(def => def.sortOrder);
        }

        WarnSharedGrants();
    }

    private static Dictionary<string, List<ThingDef>> BuildWeaponTagLookup(List<ApparelSetDef> sets)
    {
        var lookup = new Dictionary<string, List<ThingDef>>();

        var wanted = new HashSet<string>();
        foreach (var set in sets)
        {
            foreach (var weaponPiece in set.weaponPieces)
            {
                if (weaponPiece.weaponTags != null)
                {
                    wanted.AddRange(weaponPiece.weaponTags);
                }
            }
        }

        if (wanted.Count == 0)
        {
            return lookup;
        }

        foreach (var thingDef in DefDatabase<ThingDef>.AllDefsListForReading)
        {
            if (thingDef.weaponTags.NullOrEmpty())
            {
                continue;
            }

            foreach (var tag in thingDef.weaponTags)
            {
                if (tag == null || !wanted.Contains(tag))
                {
                    continue;
                }

                if (!lookup.TryGetValue(tag, out var list))
                {
                    list = [];
                    lookup.Add(tag, list);
                }
                list.Add(thingDef);
            }
        }

        return lookup;
    }

    private static void AddTo<TKey>(Dictionary<TKey, List<ApparelSetDef>> dict, TKey key, ApparelSetDef set) where TKey : class
    {
        if (key == null)
        {
            return;
        }

        if (!dict.TryGetValue(key, out var list))
        {
            list = [];
            dict.Add(key, list);
        }

        if (!list.Contains(set))
        {
            list.Add(set);
        }
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
            return setsByThing.Count != 0 || setsByDecoration.Count != 0;
        }
    }

    public static bool AnyDecorationPieces
    {
        get
        {
            EnsureBuilt();
            return setsByDecoration.Count != 0;
        }
    }

    public static bool AnyVerbModifiers
    {
        get
        {
            EnsureBuilt();
            return verbModifiedWeapons.Count != 0;
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

    public static List<ApparelSetDef> SetsFor(DecorationDef decorationDef)
    {
        EnsureBuilt();
        return decorationDef != null && setsByDecoration.TryGetValue(decorationDef, out var list) ? list : EmptySets;
    }

    public static bool IsPiece(ThingDef thingDef)
    {
        EnsureBuilt();
        return thingDef != null && setsByThing.ContainsKey(thingDef);
    }

    public static bool IsSetDecoration(DecorationDef decorationDef)
    {
        EnsureBuilt();
        return decorationDef != null && setsByDecoration.ContainsKey(decorationDef);
    }

    public static bool HasVerbModifier(ThingDef weaponDef)
    {
        EnsureBuilt();
        return weaponDef != null && verbModifiedWeapons.Contains(weaponDef);
    }

    /// <summary>
    /// Whether any decoration fitted to this thing is a set decoration.
    /// </summary>
    public static bool CarriesSetDecoration(Thing thing)
    {
        if (thing is not ThingWithComps thingWithComps || !AnyDecorationPieces)
        {
            return false;
        }

        var comps = thingWithComps.AllComps;
        for (var i = 0; i < comps.Count; i++)
        {
            if (comps[i] is not CompDecorativeBase decorative)
            {
                continue;
            }

            foreach (var decoration in decorative.Decorations.Keys)
            {
                if (setsByDecoration.ContainsKey(decoration))
                {
                    return true;
                }
            }
        }

        return false;
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
