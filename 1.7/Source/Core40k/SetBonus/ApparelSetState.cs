using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using VEF.Abilities;
using Verse;
using AbilityDef = RimWorld.AbilityDef;

namespace Core40k;

/// <summary>
/// Which apparel sets a pawn currently has active. Rebuilt from worn apparel, equipped weapons and
/// fitted decorations whenever they change and never saved; held by CompRankInfo.
/// </summary>
public class ApparelSetState
{
    private readonly CompRankInfo parent;

    private bool dirty = true;

    private readonly HashSet<ThingDef> wornDefs = [];
    private readonly HashSet<ThingDef> heldDefs = [];
    private readonly HashSet<DecorationDef> fittedDecorations = [];
    private readonly HashSet<SetWeaponPiece> matchedWeaponPieces = [];
    private readonly Dictionary<ApparelSetDef, int> counts = new();
    private readonly List<ApparelSetDef> activeSets = [];
    private readonly List<ApparelSetTier> activeTiers = [];
    private readonly List<ApparelSetTier> previousTiers = [];
    private readonly Dictionary<ThingDef, VerbModifier> verbModifiers = new();

    private readonly Dictionary<StatDef, float> cachedStatOffset = new();
    private readonly Dictionary<StatDef, float> cachedStatFactor = new();

    public ApparelSetState(CompRankInfo parent)
    {
        this.parent = parent;
    }

    private Pawn Pawn => parent?.ParentPawn;

    //Caches

    private void EnsureCaches()
    {
        if (!dirty)
        {
            return;
        }

        wornDefs.Clear();
        heldDefs.Clear();
        fittedDecorations.Clear();
        matchedWeaponPieces.Clear();
        counts.Clear();
        activeSets.Clear();
        activeTiers.Clear();
        verbModifiers.Clear();

        var pawn = Pawn;
        var worn = pawn?.apparel?.WornApparel;
        if (worn != null)
        {
            foreach (var apparel in worn)
            {
                if (apparel?.def == null)
                {
                    continue;
                }

                wornDefs.Add(apparel.def);
                CollectDecorations(apparel);
            }
        }

        var equipment = pawn?.equipment?.AllEquipmentListForReading;
        if (equipment != null)
        {
            foreach (var eq in equipment)
            {
                if (eq?.def == null)
                {
                    continue;
                }

                heldDefs.Add(eq.def);
                CollectDecorations(eq);
            }
        }

        var candidates = new List<ApparelSetDef>();
        foreach (var thingDef in wornDefs)
        {
            AddCandidates(candidates, ApparelSetIndex.SetsFor(thingDef));
        }
        foreach (var thingDef in heldDefs)
        {
            AddCandidates(candidates, ApparelSetIndex.SetsFor(thingDef));
        }
        foreach (var decoration in fittedDecorations)
        {
            AddCandidates(candidates, ApparelSetIndex.SetsFor(decoration));
        }

        foreach (var set in candidates)
        {
            var count = set.CountIn(wornDefs, heldDefs, fittedDecorations, out var weaponMatched);
            counts[set] = count;

            foreach (var weaponPiece in set.weaponPieces)
            {
                foreach (var thingDef in heldDefs)
                {
                    if (weaponPiece.Matches(thingDef))
                    {
                        matchedWeaponPieces.Add(weaponPiece);
                        break;
                    }
                }
            }

            ApparelSetTier highest = null;
            var anyTier = false;
            foreach (var tier in set.tiers)
            {
                if (!TierRequirementsMet(tier, count, weaponMatched))
                {
                    continue;
                }

                if (set.cumulativeTiers)
                {
                    activeTiers.Add(tier);
                    anyTier = true;
                }
                else
                {
                    highest = tier;
                }
            }

            if (highest != null)
            {
                activeTiers.Add(highest);
                anyTier = true;
            }

            if (anyTier)
            {
                activeSets.Add(set);
            }
        }

        activeSets.SortBy(set => set.sortOrder);
        BuildVerbModifiers();
        dirty = false;
    }

    private void CollectDecorations(ThingWithComps thing)
    {
        if (!ApparelSetIndex.AnyDecorationPieces)
        {
            return;
        }

        var comps = thing.AllComps;
        for (var i = 0; i < comps.Count; i++)
        {
            if (comps[i] is not CompDecorativeBase decorative)
            {
                continue;
            }

            foreach (var decoration in decorative.Decorations.Keys)
            {
                if (ApparelSetIndex.IsSetDecoration(decoration))
                {
                    fittedDecorations.Add(decoration);
                }
            }
        }
    }

    private static void AddCandidates(List<ApparelSetDef> candidates, List<ApparelSetDef> sets)
    {
        foreach (var set in sets)
        {
            if (!candidates.Contains(set))
            {
                candidates.Add(set);
            }
        }
    }

    private bool TierRequirementsMet(ApparelSetTier tier, int count, bool weaponMatched)
    {
        if (count < tier.requiredCount)
        {
            return false;
        }

        if (tier.requiresWeapon && !weaponMatched)
        {
            return false;
        }

        if (!tier.requiredDecorations.NullOrEmpty())
        {
            foreach (var decoration in tier.requiredDecorations)
            {
                if (decoration != null && !fittedDecorations.Contains(decoration))
                {
                    return false;
                }
            }
        }

        return true;
    }

    //Built eagerly for every equipped weapon so the verb getters only ever read.
    private void BuildVerbModifiers()
    {
        if (activeTiers.Count == 0 || !ApparelSetIndex.AnyVerbModifiers)
        {
            return;
        }

        foreach (var weaponDef in heldDefs)
        {
            if (!ApparelSetIndex.HasVerbModifier(weaponDef))
            {
                continue;
            }

            VerbModifier sum = null;
            foreach (var set in activeSets)
            {
                if (!set.MatchesWeapon(weaponDef))
                {
                    continue;
                }

                foreach (var tier in set.tiers)
                {
                    if (!tier.HasVerbModifier || !activeTiers.Contains(tier))
                    {
                        continue;
                    }

                    sum ??= new VerbModifier();
                    sum.additionalBurstShotCount += tier.verbModifier.additionalBurstShotCount;
                    sum.additionalRange += tier.verbModifier.additionalRange;
                    sum.additionalWarmupTime += tier.verbModifier.additionalWarmupTime;
                }
            }

            if (sum != null)
            {
                verbModifiers[weaponDef] = sum;
            }
        }
    }

    //Queries

    public bool AnyActive
    {
        get
        {
            EnsureCaches();
            return activeTiers.Count != 0;
        }
    }

    public List<ApparelSetDef> ActiveSets
    {
        get
        {
            EnsureCaches();
            return activeSets;
        }
    }

    public int CountOf(ApparelSetDef set)
    {
        EnsureCaches();
        return set != null && counts.TryGetValue(set, out var count) ? count : 0;
    }

    public bool IsWorn(ThingDef thingDef)
    {
        EnsureCaches();
        return thingDef != null && wornDefs.Contains(thingDef);
    }

    public bool IsWeaponPieceMatched(SetWeaponPiece weaponPiece)
    {
        EnsureCaches();
        return weaponPiece != null && matchedWeaponPieces.Contains(weaponPiece);
    }

    public bool IsFitted(DecorationDef decorationDef)
    {
        EnsureCaches();
        return decorationDef != null && fittedDecorations.Contains(decorationDef);
    }

    public bool IsTierActive(ApparelSetTier tier)
    {
        EnsureCaches();
        return tier != null && activeTiers.Contains(tier);
    }

    /// <summary>
    /// The summed verb modifier of every active tier whose set has a weapon piece matching this weapon, or
    /// null when there is none.
    /// </summary>
    public VerbModifier VerbModifierFor(ThingDef weaponDef)
    {
        EnsureCaches();
        return weaponDef != null && verbModifiers.TryGetValue(weaponDef, out var modifier) ? modifier : null;
    }

    //Changes

    /// <summary>
    /// Rebuilds the active sets from what is worn and equipped now, brings granted hediffs and abilities
    /// in line, and optionally tells the player about tiers that just became active.
    /// </summary>
    public void Notify_GearChanged(bool notify)
    {
        var hadState = !dirty;
        previousTiers.Clear();
        if (hadState)
        {
            previousTiers.AddRange(activeTiers);
        }

        dirty = true;
        cachedStatOffset.Clear();
        cachedStatFactor.Clear();

        EnsureCaches();
        ReconcileGrants();

        if (notify && hadState)
        {
            SendTierMessages();
        }
    }

    private void SendTierMessages()
    {
        var pawn = Pawn;
        if (pawn?.Faction is not { IsPlayer: true } || !PawnUtility.ShouldSendNotificationAbout(pawn))
        {
            return;
        }

        foreach (var set in activeSets)
        {
            ApparelSetTier gained = null;
            foreach (var tier in set.tiers)
            {
                if (activeTiers.Contains(tier) && !previousTiers.Contains(tier))
                {
                    gained = tier;
                }
            }

            if (gained == null)
            {
                continue;
            }

            Messages.Message("BEWH.Framework.ApparelSet.TierGained".Translate(pawn.LabelShortCap, set.label, gained.LabelOrCount), pawn, MessageTypeDefOf.PositiveEvent, false);
        }
    }

    private void ReconcileGrants()
    {
        var pawn = Pawn;
        if (pawn == null || pawn.Dead || !ApparelSetIndex.AnyGrants)
        {
            return;
        }

        var expectedHediffs = new HashSet<HediffDef>();
        var expectedAbilities = new HashSet<AbilityDef>();
        var expectedVFEAbilities = new HashSet<VEF.Abilities.AbilityDef>();

        foreach (var tier in activeTiers)
        {
            expectedHediffs.AddRange(tier.givesHediffs);
            expectedAbilities.AddRange(tier.givesAbilities);
            expectedVFEAbilities.AddRange(tier.givesVFEAbilities);
        }

        if (pawn.health?.hediffSet != null)
        {
            var hediffs = pawn.health.hediffSet.hediffs;
            for (var i = hediffs.Count - 1; i >= 0; i--)
            {
                var hediff = hediffs[i];
                if (ApparelSetIndex.IsGrantable(hediff.def) && !expectedHediffs.Contains(hediff.def))
                {
                    pawn.health.RemoveHediff(hediff);
                }
            }

            foreach (var hediffDef in expectedHediffs)
            {
                if (!pawn.health.hediffSet.HasHediff(hediffDef))
                {
                    pawn.health.AddHediff(hediffDef);
                }
            }
        }

        if (pawn.abilities != null)
        {
            List<AbilityDef> toRemove = null;
            foreach (var ability in pawn.abilities.abilities)
            {
                if (ApparelSetIndex.IsGrantable(ability.def) && !expectedAbilities.Contains(ability.def))
                {
                    toRemove ??= [];
                    toRemove.Add(ability.def);
                }
            }

            List<AbilityDef> toAdd = null;
            foreach (var abilityDef in expectedAbilities)
            {
                if (pawn.abilities.GetAbility(abilityDef, false) == null)
                {
                    toAdd ??= [];
                    toAdd.Add(abilityDef);
                }
            }

            if (toRemove != null)
            {
                pawn.RemoveAbilities(toRemove, null);
            }
            if (toAdd != null)
            {
                pawn.AddAbilities(toAdd, null);
            }
        }

        var vefComp = pawn.GetComp<CompAbilities>();
        if (vefComp != null)
        {
            List<VEF.Abilities.AbilityDef> toRemove = null;
            foreach (var ability in vefComp.LearnedAbilities)
            {
                if (ApparelSetIndex.IsGrantable(ability.def) && !expectedVFEAbilities.Contains(ability.def))
                {
                    toRemove ??= [];
                    toRemove.Add(ability.def);
                }
            }

            List<VEF.Abilities.AbilityDef> toAdd = null;
            foreach (var abilityDef in expectedVFEAbilities)
            {
                if (!vefComp.LearnedAbilities.Any(ability => ability.def == abilityDef))
                {
                    toAdd ??= [];
                    toAdd.Add(abilityDef);
                }
            }

            if (toRemove != null)
            {
                pawn.RemoveAbilities(null, toRemove);
            }
            if (toAdd != null)
            {
                pawn.AddAbilities(null, toAdd);
            }
        }
    }

    //Stats

    public float GetStatOffset(StatDef stat)
    {
        if (!AnyActive)
        {
            return 0f;
        }

        if (cachedStatOffset.TryGetValue(stat, out var cached))
        {
            return cached;
        }

        var num = 0f;
        foreach (var tier in activeTiers)
        {
            if (!tier.statOffsets.NullOrEmpty())
            {
                num += tier.statOffsets.GetStatOffsetFromList(stat);
            }
        }

        cachedStatOffset[stat] = num;
        return num;
    }

    public float GetStatFactor(StatDef stat)
    {
        if (!AnyActive)
        {
            return 1f;
        }

        if (cachedStatFactor.TryGetValue(stat, out var cached))
        {
            return cached;
        }

        var num = 1f;
        foreach (var tier in activeTiers)
        {
            if (!tier.statFactors.NullOrEmpty())
            {
                num *= tier.statFactors.GetStatFactorFromList(stat);
            }
        }

        cachedStatFactor[stat] = num;
        return num;
    }

    public void AppendStatsExplanation(StatDef stat, StringBuilder sb, string whitespace = "")
    {
        if (!AnyActive)
        {
            return;
        }

        var stringBuilder = new StringBuilder();
        foreach (var set in activeSets)
        {
            foreach (var tier in set.tiers)
            {
                if (!activeTiers.Contains(tier))
                {
                    continue;
                }

                var label = set.LabelCap + " (" + tier.LabelOrCount + ")";
                var offset = tier.statOffsets.NullOrEmpty() ? 0f : tier.statOffsets.GetStatOffsetFromList(stat);
                if (!Waylith.Math.Mathf.Approximately(offset, 0f))
                {
                    stringBuilder.AppendLine(whitespace + "    " + label + ": " + stat.Worker.ValueToString(offset, finalized: false, ToStringNumberSense.Offset));
                }

                var factor = tier.statFactors.NullOrEmpty() ? 1f : tier.statFactors.GetStatFactorFromList(stat);
                if (!Waylith.Math.Mathf.Approximately(factor, 1f))
                {
                    stringBuilder.AppendLine(whitespace + "    " + label + ": " + stat.Worker.ValueToString(factor, finalized: false, ToStringNumberSense.Factor));
                }
            }
        }

        if (stringBuilder.Length != 0)
        {
            sb.AppendLine(whitespace + "BEWH.Framework.StatReport.ApparelSet".Translate() + ":");
            sb.Append(stringBuilder);
        }
    }
}
