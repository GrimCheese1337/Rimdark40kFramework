using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using VEF.Abilities;
using Verse;
using AbilityDef = RimWorld.AbilityDef;

namespace Core40k;

/// <summary>
/// Which apparel sets a pawn currently has active. Rebuilt from worn apparel whenever it changes and
/// never saved; held by CompRankInfo.
/// </summary>
public class ApparelSetState
{
    private readonly CompRankInfo parent;

    private bool dirty = true;

    private readonly HashSet<ThingDef> wornDefs = [];
    private readonly Dictionary<ApparelSetDef, int> counts = new();
    private readonly List<ApparelSetDef> activeSets = [];
    private readonly List<SetBonusTier> activeTiers = [];
    private readonly List<SetBonusTier> previousTiers = [];

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
        counts.Clear();
        activeSets.Clear();
        activeTiers.Clear();

        var worn = Pawn?.apparel?.WornApparel;
        if (worn != null)
        {
            foreach (var apparel in worn)
            {
                if (apparel?.def != null)
                {
                    wornDefs.Add(apparel.def);
                }
            }
        }

        foreach (var thingDef in wornDefs)
        {
            foreach (var set in ApparelSetIndex.SetsFor(thingDef))
            {
                if (counts.ContainsKey(set))
                {
                    continue;
                }

                var count = set.CountIn(wornDefs);
                counts[set] = count;

                SetBonusTier highest = null;
                var anyTier = false;
                foreach (var tier in set.tiers)
                {
                    if (count < tier.requiredCount)
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
        }

        activeSets.SortBy(set => set.sortOrder);
        dirty = false;
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

    public bool IsTierActive(SetBonusTier tier)
    {
        EnsureCaches();
        return tier != null && activeTiers.Contains(tier);
    }

    //Changes

    /// <summary>
    /// Rebuilds the active sets from what is worn now, brings granted hediffs and abilities in line,
    /// and optionally tells the player about tiers that just became active.
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
            SetBonusTier gained = null;
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
                if (!Mathf.Approximately(offset, 0f))
                {
                    stringBuilder.AppendLine(whitespace + "    " + label + ": " + stat.Worker.ValueToString(offset, finalized: false, ToStringNumberSense.Offset));
                }

                var factor = tier.statFactors.NullOrEmpty() ? 1f : tier.statFactors.GetStatFactorFromList(stat);
                if (!Mathf.Approximately(factor, 1f))
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
