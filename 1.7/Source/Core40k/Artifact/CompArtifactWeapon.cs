using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace Core40k;

public class CompArtifactWeapon : ThingComp, IPawnStatSource, IDecorationLockSource
{
    public CompProperties_ArtifactWeapon Props => (CompProperties_ArtifactWeapon)props;

    private ArtifactKindDef kind;
    private List<ArtifactTraitDef> traits = [];
    private int stage = -1;
    private string name;
    private ArtifactKillRecord kills = new();
    private List<ArtifactBearerRecord> bearers = [];
    private List<int> stageTicks = [];
    private Dictionary<string, float> state = new();
    //Decorations this comp forced on, so stripping removes exactly those and never a player-fitted one.
    private List<DecorationDef> forcedHistory = [];

    //Runtime only
    private bool activeForWielder = true;
    private List<ArtifactTraitDef> cachedActiveTraits;
    private readonly List<ArtifactTraitDef> appliedTraits = [];
    private Dictionary<StatDef, float> cachedStatOffset = new();
    private Dictionary<StatDef, float> cachedStatFactor = new();
    private Dictionary<StatDef, float> cachedWielderStatOffset = new();
    private Dictionary<StatDef, float> cachedWielderStatFactor = new();
    private bool verbModifiersCached;
    private int cachedAdditionalBurstShotCount;
    private float cachedAdditionalRange;
    private float cachedAdditionalWarmupTime;
    private List<Tool> cachedTools;
    private List<Tool> cachedSourceTools;
    private int toolsVersion;
    private int cachedToolsVersion = -1;

    public bool IsArtifact => kind != null;
    public ArtifactKindDef Kind => kind;
    public List<ArtifactTraitDef> Traits => traits;
    public int Stage => stage;
    public string Name => name;
    public ArtifactKillRecord Kills => kills;
    public List<ArtifactBearerRecord> Bearers => bearers;
    public List<int> StageTicks => stageTicks;
    public bool WielderMeetsRequirements => activeForWielder;

    public Pawn Wielder => parent.ParentHolder is Pawn_EquipmentTracker { pawn: not null } tracker ? tracker.pawn : null;

    public ArtifactStage NextStage => kind?.StageAt(stage + 1);

    //State bag
    public float GetState(string key, float defaultValue = 0f)
    {
        return !key.NullOrEmpty() && state.TryGetValue(key, out var value) ? value : defaultValue;
    }

    public void SetState(string key, float value)
    {
        if (!key.NullOrEmpty())
        {
            state[key] = value;
        }
    }

    public float AddState(string key, float delta)
    {
        var value = GetState(key) + delta;
        SetState(key, value);
        return value;
    }

    //Active traits
    public List<ArtifactTraitDef> ActiveTraits
    {
        get
        {
            if (cachedActiveTraits != null)
            {
                return cachedActiveTraits;
            }

            cachedActiveTraits = [];
            if (!IsArtifact || !activeForWielder)
            {
                return cachedActiveTraits;
            }

            var wielder = Wielder;
            foreach (var trait in traits)
            {
                if (trait != null && trait.Worker.IsActiveFor(this, wielder))
                {
                    cachedActiveTraits.Add(trait);
                }
            }

            return cachedActiveTraits;
        }
    }

    public bool TraitActive(ArtifactTraitDef trait)
    {
        return trait != null && ActiveTraits.Contains(trait);
    }

    public void InvalidateCaches()
    {
        cachedActiveTraits = null;
        cachedStatOffset = new Dictionary<StatDef, float>();
        cachedStatFactor = new Dictionary<StatDef, float>();
        cachedWielderStatOffset = new Dictionary<StatDef, float>();
        cachedWielderStatFactor = new Dictionary<StatDef, float>();
        verbModifiersCached = false;
        toolsVersion++;

        var equippable = parent.GetComp<CompEquippable>();
        if (equippable?.verbTracker == null)
        {
            return;
        }

        equippable.verbTracker.VerbsNeedReinitOnLoad();
        var wielder = Wielder;
        if (wielder == null)
        {
            return;
        }
        foreach (var verb in equippable.AllVerbs)
        {
            verb.caster = wielder;
        }
    }

    /// <summary>
    /// Re-evaluates the kind and trait requirements against the wielder. Caches (and the verb list) are only dropped
    /// when the active set actually changed, so a kill mid-burst does not rebuild the verbs.
    /// </summary>
    private void RefreshWielder(Pawn wielder)
    {
        if (!IsArtifact)
        {
            activeForWielder = true;
            cachedActiveTraits = null;
            return;
        }

        var wasActive = activeForWielder;
        var previous = cachedActiveTraits;

        activeForWielder = RequirementData.IsNullOrEmpty(kind.wielderRequirements)
                           || wielder == null
                           || kind.wielderRequirements.Meets(wielder);
        cachedActiveTraits = null;
        var current = ActiveTraits;

        if (wasActive != activeForWielder || previous == null || !previous.SequenceEqual(current))
        {
            InvalidateCaches();
        }
    }

    /// <summary>
    /// Brings the hediffs and abilities on the wielder in line with the traits that are active right now, so a
    /// trait whose requirements were met after equipping still gets applied, and one that stopped applying is removed.
    /// </summary>
    private void SyncAppliedTraits(Pawn wielder)
    {
        if (wielder == null)
        {
            return;
        }

        var active = ActiveTraits;
        for (var i = appliedTraits.Count - 1; i >= 0; i--)
        {
            var trait = appliedTraits[i];
            if (trait == null || !active.Contains(trait))
            {
                trait?.Worker.Notify_Unequipped(this, wielder);
                appliedTraits.RemoveAt(i);
            }
        }

        foreach (var trait in active)
        {
            if (!appliedTraits.Contains(trait))
            {
                trait.Worker.Notify_Equipped(this, wielder);
                appliedTraits.Add(trait);
            }
        }

        ArtifactWeaponUtility.UpdateTicking(this);
    }

    //Becoming an artifact
    public bool CanBecome(ArtifactKindDef newKind, out string reason)
    {
        reason = null;
        if (newKind == null)
        {
            reason = "no kind";
            return false;
        }
        if (IsArtifact)
        {
            reason = parent.LabelCap + " is already an artifact";
            return false;
        }
        if (Props.kinds.NullOrEmpty() || !Props.kinds.Contains(newKind))
        {
            reason = parent.def.defName + " does not list " + newKind.defName + " in its artifact kinds";
            return false;
        }
        if (!newKind.HasStages)
        {
            reason = newKind.defName + " has no stages";
            return false;
        }
        if (!newKind.AllowsWeapon(parent.def))
        {
            reason = parent.def.defName + " has none of the weaponTags of " + newKind.defName;
            return false;
        }
        if (!newKind.Worker.CanBecome(parent))
        {
            reason = newKind.defName + " refused " + parent.LabelCap;
            return false;
        }
        return true;
    }

    public void MakeArtifact(ArtifactKindDef newKind, List<ArtifactTraitDef> curatedTraits = null, bool silent = false)
    {
        if (!CanBecome(newKind, out var reason))
        {
            Log.Error("[RimDark] Cannot make artifact: " + reason);
            return;
        }

        kind = newKind;
        stage = -1;
        var wielder = Wielder;
        RefreshWielder(wielder);
        ApplyStage(0, curatedTraits, wielder, silent);
    }

    /// <summary>
    /// Advances one stage when the next stage's conditions are met. For a weapon that is not yet an artifact this is
    /// the awakening check across every kind the def allows, in list order.
    /// </summary>
    public bool TryAdvanceStage(Pawn wielder, bool silent = false)
    {
        if (!IsArtifact)
        {
            return TryAwaken(wielder, silent);
        }

        if (!activeForWielder)
        {
            return false;
        }

        var next = NextStage;
        if (next == null || !next.IsMet(this, wielder))
        {
            return false;
        }

        ApplyStage(stage + 1, null, wielder, silent);
        return true;
    }

    private bool TryAwaken(Pawn wielder, bool silent)
    {
        if (Props.kinds.NullOrEmpty())
        {
            return false;
        }

        foreach (var candidate in Props.kinds)
        {
            var first = candidate?.StageAt(0);
            if (first == null || !first.HasConditions)
            {
                continue;
            }
            if (!RequirementData.IsNullOrEmpty(candidate.wielderRequirements) && (wielder == null || !candidate.wielderRequirements.Meets(wielder)))
            {
                continue;
            }
            if (!candidate.AllowsWeapon(parent.def) || !candidate.Worker.CanBecome(parent) || !first.IsMet(this, wielder))
            {
                continue;
            }

            MakeArtifact(candidate, null, silent);
            return true;
        }

        return false;
    }

    private void ApplyStage(int stageIndex, List<ArtifactTraitDef> curatedTraits, Pawn wielder, bool silent)
    {
        var artifactStage = kind.StageAt(stageIndex);
        if (artifactStage == null)
        {
            return;
        }

        stage = stageIndex;
        stageTicks.Add(Find.TickManager?.TicksGame ?? 0);

        var added = new List<ArtifactTraitDef>();
        if (!curatedTraits.NullOrEmpty())
        {
            foreach (var trait in curatedTraits)
            {
                if (AddTraitInternal(trait))
                {
                    added.Add(trait);
                }
            }
        }
        else
        {
            RollTraits(artifactStage.traitCount.RandomInRange, false, added);
            RollTraits(artifactStage.drawbackCount.RandomInRange, true, added);
        }

        if (stageIndex == 0)
        {
            ApplyQuality();
            ApplyForcedColours();
            name = kind.Worker.GenerateName(this, wielder);
            var art = parent.TryGetComp<CompArt>();
            if (art != null && !name.NullOrEmpty())
            {
                art.Title = name;
            }
        }

        ApplyForcedDecorations();
        InvalidateCaches();
        NotifyAppearanceChanged();

        foreach (var trait in added)
        {
            trait.Worker.Notify_Gained(this, stageIndex);
        }
        kind.Worker.Notify_StageReached(this, stageIndex, wielder);
        SyncAppliedTraits(wielder);

        if (!silent)
        {
            SendStageLetter(artifactStage, wielder);
        }
    }

    private void RollTraits(int count, bool drawbacks, List<ArtifactTraitDef> added)
    {
        for (var i = 0; i < count; i++)
        {
            var candidates = DefDatabase<ArtifactTraitDef>.AllDefsListForReading
                .Where(trait => trait.isDrawback == drawbacks && CanAddTrait(trait))
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var picked = candidates.RandomElementByWeight(trait => Math.Max(trait.commonality, 0.0001f));
            if (AddTraitInternal(picked))
            {
                added.Add(picked);
            }
        }
    }

    public bool CanAddTrait(ArtifactTraitDef trait)
    {
        if (trait == null || !IsArtifact || traits.Contains(trait))
        {
            return false;
        }
        if (!kind.AllowsTrait(trait) || !trait.AppliesTo(parent.def))
        {
            return false;
        }
        if (traits.Count == 0 && !trait.canGenerateAlone)
        {
            return false;
        }
        foreach (var existing in traits)
        {
            if (existing != null && trait.Overlaps(existing))
            {
                return false;
            }
        }
        return true;
    }

    private bool AddTraitInternal(ArtifactTraitDef trait)
    {
        if (!CanAddTrait(trait))
        {
            return false;
        }
        traits.Add(trait);
        return true;
    }

    public bool AddTrait(ArtifactTraitDef trait)
    {
        if (!IsArtifact)
        {
            Log.Error("[RimDark] AddTrait on " + parent.LabelCap + " which is not an artifact.");
            return false;
        }
        if (!AddTraitInternal(trait))
        {
            return false;
        }

        ApplyForcedDecorations();
        if (trait.forcedColours != null)
        {
            ApplyColours(trait.forcedColours);
        }
        InvalidateCaches();
        NotifyAppearanceChanged();
        trait.Worker.Notify_Gained(this, stage);
        SyncAppliedTraits(Wielder);
        return true;
    }

    public bool RemoveTrait(ArtifactTraitDef trait)
    {
        if (trait == null || !traits.Remove(trait))
        {
            return false;
        }

        var wielder = Wielder;
        if (appliedTraits.Remove(trait))
        {
            trait.Worker.Notify_Unequipped(this, wielder);
        }
        trait.Worker.Notify_Lost(this);
        InvalidateCaches();
        RemoveUnlockedForcedDecorations();
        NotifyAppearanceChanged();
        SyncAppliedTraits(wielder);
        return true;
    }

    public void Strip()
    {
        if (!IsArtifact)
        {
            return;
        }

        var wielder = Wielder;
        foreach (var trait in appliedTraits)
        {
            trait?.Worker.Notify_Unequipped(this, wielder);
        }
        appliedTraits.Clear();
        foreach (var trait in traits)
        {
            trait?.Worker.Notify_Lost(this);
        }
        kind.Worker.Notify_Stripped(this);

        kind = null;
        traits = [];
        stage = -1;
        name = null;
        stageTicks = [];
        activeForWielder = true;

        InvalidateCaches();
        RemoveUnlockedForcedDecorations();
        NotifyAppearanceChanged();
        ArtifactWeaponUtility.UpdateTicking(this);
    }

    private void ApplyQuality()
    {
        if (kind.qualityOnCreate == null)
        {
            return;
        }

        var quality = parent.TryGetComp<CompQuality>();
        if (quality != null && quality.Quality < kind.qualityOnCreate.Value)
        {
            quality.SetQuality(kind.qualityOnCreate.Value, ArtGenerationContext.Colony);
        }
    }

    private void ApplyForcedColours()
    {
        if (kind.forcedColours != null)
        {
            ApplyColours(kind.forcedColours);
        }
        foreach (var trait in traits)
        {
            if (trait?.forcedColours != null)
            {
                ApplyColours(trait.forcedColours);
            }
        }
    }

    private void ApplyColours(ColourPresetDef preset)
    {
        var multiColor = parent.GetComp<CompMultiColor>();
        if (multiColor == null)
        {
            return;
        }

        multiColor.SetColors(preset);
        multiColor.SetOriginals();
    }

    private IEnumerable<ForcedDecoration> AllForcedDecorations()
    {
        if (kind != null)
        {
            foreach (var forced in kind.forcedDecorations)
            {
                if (forced?.def != null)
                {
                    yield return forced;
                }
            }
        }
        foreach (var trait in traits)
        {
            if (trait == null)
            {
                continue;
            }
            foreach (var forced in trait.forcedDecorations)
            {
                if (forced?.def != null)
                {
                    yield return forced;
                }
            }
        }
    }

    private void ApplyForcedDecorations()
    {
        var decoration = parent.GetComp<CompWeaponDecoration>();
        if (decoration == null)
        {
            if (AllForcedDecorations().Any())
            {
                Log.WarningOnce("[RimDark] " + parent.def.defName + " has forced artifact decorations but no CompWeaponDecoration; they are skipped.", parent.def.GetHashCode() ^ 0x4A7);
            }
            return;
        }

        foreach (var forced in AllForcedDecorations())
        {
            if (decoration.Decorations.ContainsKey(forced.def))
            {
                continue;
            }
            if (decoration.ForceAddDecoration(forced.def, forced.colour, forced.colourTwo, forced.colourThree, forced.mask, forced.flipped) && !forcedHistory.Contains(forced.def))
            {
                forcedHistory.Add(forced.def);
            }
        }
    }

    private void RemoveUnlockedForcedDecorations()
    {
        var decoration = parent.GetComp<CompWeaponDecoration>();
        if (decoration == null)
        {
            return;
        }

        for (var i = forcedHistory.Count - 1; i >= 0; i--)
        {
            var def = forcedHistory[i];
            if (def == null || decoration.IsDecorationLocked(def, out _))
            {
                continue;
            }
            decoration.RemoveForcedDecoration(def);
            forcedHistory.RemoveAt(i);
        }
    }

    //IDecorationLockSource
    public bool LocksDecoration(DecorationDef def)
    {
        if (def == null || !IsArtifact)
        {
            return false;
        }
        foreach (var forced in AllForcedDecorations())
        {
            if (forced.def == def)
            {
                return true;
            }
        }
        return false;
    }

    public string LockReason(DecorationDef def)
    {
        return "BEWH.Framework.Artifact.BoundToArtifact".Translate(kind?.label ?? "artifact");
    }

    public bool LocksColours
    {
        get
        {
            if (!IsArtifact)
            {
                return false;
            }
            if (kind.lockColours)
            {
                return true;
            }
            foreach (var trait in traits)
            {
                if (trait is { forcedColours: not null, lockColours: true })
                {
                    return true;
                }
            }
            return false;
        }
    }

    public string ColourLockReason => "BEWH.Framework.Artifact.BoundToArtifact".Translate(kind?.label ?? "artifact");

    public bool LocksDecorationColours(DecorationDef def)
    {
        foreach (var forced in AllForcedDecorations())
        {
            if (forced.def == def && forced.lockColours)
            {
                return true;
            }
        }
        return false;
    }

    private void NotifyAppearanceChanged()
    {
        parent.GetComp<CompWeaponDecoration>()?.Notify_GraphicChanged();
        parent.Notify_ColorChanged();
    }

    //Bearers and kills
    private ArtifactBearerRecord CurrentBearer => bearers.Count > 0 ? bearers[bearers.Count - 1] : null;

    private void OpenBearerRecord(Pawn pawn)
    {
        var pawnName = ArtifactBearerRecord.NameOf(pawn);
        var current = CurrentBearer;
        if (current != null && current.pawnName == pawnName)
        {
            current.lastTick = Find.TickManager?.TicksGame ?? current.lastTick;
            return;
        }
        bearers.Add(new ArtifactBearerRecord(pawn));
    }

    private void CloseBearerRecord()
    {
        var current = CurrentBearer;
        if (current != null)
        {
            current.lastTick = Find.TickManager?.TicksGame ?? current.lastTick;
        }
    }

    public void Notify_KilledPawn(Pawn wielder, Pawn victim)
    {
        kills.Record(victim);
        var current = CurrentBearer;
        if (current != null)
        {
            current.kills++;
            current.lastTick = Find.TickManager?.TicksGame ?? current.lastTick;
        }

        if (!IsArtifact)
        {
            TryAdvanceStage(wielder);
            return;
        }

        RefreshWielder(wielder);
        SyncAppliedTraits(wielder);
        if (!activeForWielder)
        {
            return;
        }

        kind.Worker.Notify_KilledPawn(this, wielder, victim);
        foreach (var trait in ActiveTraits.ToList())
        {
            trait.Worker.Notify_KilledPawn(this, wielder, victim);
        }

        TryAdvanceStage(wielder);
    }

    private const int RequirementRefreshInterval = 2500;

    /// <summary>
    /// Any wielder gate on the kind or a held trait. Such artifacts are re-checked on a slow tick so a rank or gene
    /// gained while the weapon is held wakes its dormant traits without waiting for the next kill.
    /// </summary>
    private bool HasRequirementGates
    {
        get
        {
            if (!IsArtifact)
            {
                return false;
            }
            if (!RequirementData.IsNullOrEmpty(kind.wielderRequirements))
            {
                return true;
            }
            foreach (var trait in traits)
            {
                if (trait != null && !RequirementData.IsNullOrEmpty(trait.requirements))
                {
                    return true;
                }
            }
            return false;
        }
    }

    public void TickWorkers(Pawn wielder, int ticksGame)
    {
        if (ticksGame % RequirementRefreshInterval == 0 && HasRequirementGates)
        {
            RefreshWielder(wielder);
            SyncAppliedTraits(wielder);
        }

        foreach (var trait in ActiveTraits)
        {
            var interval = trait.Worker.TickInterval;
            if (interval > 0 && ticksGame % interval == 0)
            {
                trait.Worker.Tick(this, wielder);
            }
        }
    }

    public bool WantsTicking
    {
        get
        {
            if (Wielder == null || !IsArtifact)
            {
                return false;
            }
            if (HasRequirementGates)
            {
                return true;
            }
            foreach (var trait in traits)
            {
                if (trait != null && trait.Worker.TickInterval > 0)
                {
                    return true;
                }
            }
            return false;
        }
    }

    //Equipment life cycle
    public override void Notify_Equipped(Pawn pawn)
    {
        base.Notify_Equipped(pawn);
        RefreshWielder(pawn);
        OpenBearerRecord(pawn);
        RegisterStatSource(pawn);
        SyncAppliedTraits(pawn);
        TryAdvanceStage(pawn);
    }

    public override void Notify_Unequipped(Pawn pawn)
    {
        base.Notify_Unequipped(pawn);
        foreach (var trait in appliedTraits)
        {
            trait?.Worker.Notify_Unequipped(this, pawn);
        }
        appliedTraits.Clear();
        CloseBearerRecord();
        UnregisterStatSource(pawn);
        activeForWielder = true;
        InvalidateCaches();
        ArtifactWeaponUtility.UpdateTicking(this);
    }

    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
        base.PostDestroy(mode, previousMap);
        ArtifactWeaponUtility.Unregister(this);

        if (!IsArtifact || kind.destroyedThought == null || mode == DestroyMode.Vanish)
        {
            return;
        }

        var last = CurrentBearer;
        if (last == null || !last.colonist)
        {
            return;
        }

        foreach (var colonist in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists)
        {
            colonist.needs?.mood?.thoughts?.memories?.TryGainMemory(kind.destroyedThought);
        }
    }

    private void RegisterStatSource(Pawn pawn)
    {
        var coreUtils = ArtifactWeaponUtility.CoreUtils;
        if (pawn == null || coreUtils == null)
        {
            return;
        }

        if (!coreUtils.cachedDecoratives.TryGetValue(pawn, out var decoratives))
        {
            decoratives = new GameComponent_CoreUtils.CachedDecoratives();
            coreUtils.cachedDecoratives.Add(pawn, decoratives);
        }
        decoratives.Add(this);
    }

    private void UnregisterStatSource(Pawn pawn)
    {
        var coreUtils = ArtifactWeaponUtility.CoreUtils;
        if (pawn == null || coreUtils == null || !coreUtils.cachedDecoratives.TryGetValue(pawn, out var decoratives))
        {
            return;
        }

        decoratives.Remove(this);
        if (decoratives.IsEmpty)
        {
            coreUtils.cachedDecoratives.Remove(pawn);
        }
    }

    //Stats
    public override float GetStatOffset(StatDef stat)
    {
        if (stat == null || !IsArtifact)
        {
            return 0f;
        }
        if (cachedStatOffset.TryGetValue(stat, out var cached))
        {
            return cached;
        }

        var result = 0f;
        foreach (var trait in ActiveTraits)
        {
            result += trait.statOffsets.GetStatOffsetFromList(stat);
        }
        cachedStatOffset[stat] = result;
        return result;
    }

    public override float GetStatFactor(StatDef stat)
    {
        if (stat == null || !IsArtifact)
        {
            return 1f;
        }
        if (cachedStatFactor.TryGetValue(stat, out var cached))
        {
            return cached;
        }

        var result = 1f;
        foreach (var trait in ActiveTraits)
        {
            result *= trait.statFactors.GetStatFactorFromList(stat);
        }
        cachedStatFactor[stat] = result;
        return result;
    }

    public float GetWielderStatOffset(StatDef stat)
    {
        if (stat == null || !IsArtifact)
        {
            return 0f;
        }
        if (cachedWielderStatOffset.TryGetValue(stat, out var cached))
        {
            return cached;
        }

        var result = 0f;
        foreach (var trait in ActiveTraits)
        {
            result += trait.wielderStatOffsets.GetStatOffsetFromList(stat);
        }
        cachedWielderStatOffset[stat] = result;
        return result;
    }

    public float GetPawnStatOffset(StatDef stat)
    {
        return 0f;
    }

    public float GetPawnStatFactor(StatDef stat)
    {
        if (stat == null || !IsArtifact)
        {
            return 1f;
        }
        if (cachedWielderStatFactor.TryGetValue(stat, out var cached))
        {
            return cached;
        }

        var result = 1f;
        foreach (var trait in ActiveTraits)
        {
            result *= trait.wielderStatFactors.GetStatFactorFromList(stat);
        }
        cachedWielderStatFactor[stat] = result;
        return result;
    }

    public override void GetStatsExplanation(StatDef stat, StringBuilder sb, string whitespace = "")
    {
        if (!IsArtifact)
        {
            return;
        }

        var lines = new StringBuilder();
        foreach (var trait in ActiveTraits)
        {
            var offset = trait.statOffsets.GetStatOffsetFromList(stat);
            if (!Waylith.Math.Mathf.Approximately(offset, 0f))
            {
                lines.AppendLine(whitespace + "    " + trait.LabelCap + ": " + stat.Worker.ValueToString(offset, finalized: false, ToStringNumberSense.Offset));
            }
            var factor = trait.statFactors.GetStatFactorFromList(stat);
            if (!Waylith.Math.Mathf.Approximately(factor, 1f))
            {
                lines.AppendLine(whitespace + "    " + trait.LabelCap + ": " + stat.Worker.ValueToString(factor, finalized: false, ToStringNumberSense.Factor));
            }
        }

        if (lines.Length != 0)
        {
            sb.AppendLine(whitespace + "BEWH.Framework.StatReport.ArtifactTraits".Translate() + ":");
            sb.Append(lines);
        }
    }

    //Verbs
    private void RecacheVerbModifiers()
    {
        if (verbModifiersCached)
        {
            return;
        }

        verbModifiersCached = true;
        cachedAdditionalBurstShotCount = 0;
        cachedAdditionalRange = 0f;
        cachedAdditionalWarmupTime = 0f;

        foreach (var trait in ActiveTraits)
        {
            if (trait.verbModifier == null)
            {
                continue;
            }
            cachedAdditionalBurstShotCount += trait.verbModifier.additionalBurstShotCount;
            cachedAdditionalRange += trait.verbModifier.additionalRange;
            cachedAdditionalWarmupTime += trait.verbModifier.additionalWarmupTime;
        }
    }

    public int AdditionalBurstShotCount
    {
        get
        {
            RecacheVerbModifiers();
            return cachedAdditionalBurstShotCount;
        }
    }

    public float AdditionalRange
    {
        get
        {
            RecacheVerbModifiers();
            return cachedAdditionalRange;
        }
    }

    public float AdditionalWarmupTime
    {
        get
        {
            RecacheVerbModifiers();
            return cachedAdditionalWarmupTime;
        }
    }

    public void ApplyToProjectile(Projectile projectile)
    {
        if (projectile == null || !IsArtifact)
        {
            return;
        }

        foreach (var trait in ActiveTraits)
        {
            if (trait.damageDefOverride != null)
            {
                projectile.damageDefOverride = trait.damageDefOverride;
            }
            if (!trait.extraDamages.NullOrEmpty())
            {
                projectile.extraDamages ??= [];
                projectile.extraDamages.AddRange(trait.extraDamages);
            }
            projectile.stoppingPower += trait.additionalStoppingPower;
        }
    }

    /// <summary>
    /// Copies the melee tools with the active traits' extra damages appended, so def-level tools are never touched.
    /// Returns the source list untouched when no active trait adds melee damage.
    /// </summary>
    public List<Tool> ApplyExtraDamage(List<Tool> sourceTools)
    {
        if (sourceTools.NullOrEmpty() || !IsArtifact)
        {
            return sourceTools;
        }

        var active = ActiveTraits;
        var anyExtra = false;
        foreach (var trait in active)
        {
            if (!trait.extraDamages.NullOrEmpty())
            {
                anyExtra = true;
                break;
            }
        }
        if (!anyExtra)
        {
            return sourceTools;
        }

        if (cachedTools != null && ReferenceEquals(cachedSourceTools, sourceTools) && cachedToolsVersion == toolsVersion)
        {
            return cachedTools;
        }

        var tools = new List<Tool>(sourceTools.Count);
        foreach (var tool in sourceTools)
        {
            if (tool == null)
            {
                tools.Add(tool);
                continue;
            }

            var copy = WeaponDecorationVerbUtility.CopyTool(tool);
            copy.extraMeleeDamages ??= [];
            copy.extraMeleeDamages.RemoveWhere(damage => damage is NamedExtraDamage named && named.name != null && named.name.StartsWith(ExtraDamagePrefix));
            foreach (var trait in active)
            {
                if (trait.extraDamages.NullOrEmpty())
                {
                    continue;
                }
                foreach (var extraDamage in trait.extraDamages)
                {
                    copy.extraMeleeDamages.Add(new NamedExtraDamage
                    {
                        def = extraDamage.def,
                        amount = extraDamage.amount,
                        armorPenetration = extraDamage.armorPenetration,
                        chance = extraDamage.chance,
                        name = ExtraDamagePrefix + trait.defName,
                    });
                }
            }
            tools.Add(copy);
        }

        cachedSourceTools = sourceTools;
        cachedToolsVersion = toolsVersion;
        cachedTools = tools;
        return tools;
    }

    private const string ExtraDamagePrefix = "Artifact:";

    //Letters and UI
    private void SendStageLetter(ArtifactStage artifactStage, Pawn wielder)
    {
        if (artifactStage.letterLabel.NullOrEmpty() && artifactStage.letterText.NullOrEmpty())
        {
            return;
        }
        if (Current.ProgramState != ProgramState.Playing || Find.LetterStack == null)
        {
            return;
        }
        if (wielder != null && wielder.Faction != Faction.OfPlayer)
        {
            return;
        }

        string weaponName = parent.LabelCap;
        var pawnName = wielder?.LabelShortCap ?? "";
        TaggedString label = (artifactStage.letterLabel ?? "").Formatted(weaponName.Named("WEAPON"), pawnName.Named("PAWN"));
        TaggedString text = (artifactStage.letterText ?? "").Formatted(weaponName.Named("WEAPON"), pawnName.Named("PAWN"));
        if (label.RawText.NullOrEmpty())
        {
            label = weaponName;
        }

        Find.LetterStack.ReceiveLetter(label, text, artifactStage.letterDef ?? LetterDefOf.PositiveEvent, wielder != null ? new LookTargets(wielder) : new LookTargets(parent));
    }

    public override string TransformLabel(string label)
    {
        if (parent.compStyleable?.SourcePrecept?.Label != null)
        {
            return label;
        }
        return name.NullOrEmpty() ? label : name;
    }

    public override string CompInspectStringExtra()
    {
        if (!IsArtifact)
        {
            return null;
        }

        var traitList = traits.Where(trait => trait != null).Select(trait => trait.label).ToCommaList().CapitalizeFirst();
        return kind.LabelCap + (traitList.NullOrEmpty() ? "" : ": " + traitList);
    }

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
    {
        if (!IsArtifact)
        {
            yield break;
        }

        var report = new StringBuilder();
        report.AppendLine(kind.description ?? kind.LabelCap);
        report.AppendLine();

        var wielder = Wielder;
        var active = ActiveTraits;
        for (var i = 0; i < traits.Count; i++)
        {
            var trait = traits[i];
            if (trait == null)
            {
                continue;
            }

            var header = trait.LabelCap.Colorize(trait.isDrawback ? ColorLibrary.RedReadable : ColorLibrary.Yellow);
            if (!active.Contains(trait))
            {
                string reason = null;
                if (!activeForWielder && wielder != null)
                {
                    kind.wielderRequirements?.Has(wielder, out reason);
                }
                else if (wielder != null)
                {
                    trait.requirements?.Has(wielder, out reason);
                }
                header += " " + "BEWH.Framework.Artifact.Dormant".Translate(reason ?? "");
            }
            report.AppendLine(header);
            report.AppendLine(trait.description);

            foreach (var modifier in trait.statOffsets)
            {
                report.AppendLine(" - " + modifier.stat.LabelCap + " " + modifier.stat.Worker.ValueToString(modifier.value, finalized: false, ToStringNumberSense.Offset));
            }
            foreach (var modifier in trait.statFactors)
            {
                report.AppendLine(" - " + modifier.stat.LabelCap + " " + modifier.stat.Worker.ValueToString(modifier.value, finalized: false, ToStringNumberSense.Factor));
            }
            foreach (var modifier in trait.wielderStatOffsets)
            {
                report.AppendLine(" - " + "BEWH.Framework.Artifact.Wielder".Translate() + " " + modifier.stat.LabelCap + " " + modifier.stat.Worker.ValueToString(modifier.value, finalized: false, ToStringNumberSense.Offset));
            }
            foreach (var modifier in trait.wielderStatFactors)
            {
                report.AppendLine(" - " + "BEWH.Framework.Artifact.Wielder".Translate() + " " + modifier.stat.LabelCap + " " + modifier.stat.Worker.ValueToString(modifier.value, finalized: false, ToStringNumberSense.Factor));
            }
            if (trait.verbModifier != null)
            {
                if (trait.verbModifier.additionalBurstShotCount != 0)
                {
                    report.AppendLine(" - " + "BEWH.Framework.Artifact.BurstShots".Translate(trait.verbModifier.additionalBurstShotCount.ToStringWithSign()));
                }
                if (trait.verbModifier.additionalRange != 0f)
                {
                    report.AppendLine(" - " + "BEWH.Framework.Artifact.Range".Translate(trait.verbModifier.additionalRange.ToStringWithSign("0.#")));
                }
                if (trait.verbModifier.additionalWarmupTime != 0f)
                {
                    report.AppendLine(" - " + "BEWH.Framework.Artifact.Warmup".Translate(trait.verbModifier.additionalWarmupTime.ToStringWithSign("0.##")));
                }
            }
            if (!trait.extraDamages.NullOrEmpty())
            {
                foreach (var extraDamage in trait.extraDamages)
                {
                    report.AppendLine(" - " + "BEWH.Framework.Artifact.ExtraDamage".Translate(extraDamage.def?.LabelCap ?? "", extraDamage.amount.ToString("0.#")));
                }
            }
            if (trait.damageDefOverride != null)
            {
                report.AppendLine(" - " + "BEWH.Framework.Artifact.DamageOverride".Translate(trait.damageDefOverride.LabelCap));
            }
            if (trait.additionalStoppingPower != 0f)
            {
                report.AppendLine(" - " + "StatsReport_AdditionalStoppingPower".Translate() + " " + trait.additionalStoppingPower.ToStringByStyle(ToStringStyle.FloatOne, ToStringNumberSense.Offset));
            }
            foreach (var hediff in trait.equippedHediffs)
            {
                report.AppendLine(" - " + hediff.LabelCap);
            }

            var extra = trait.Worker.ExtraDescription(this);
            if (!extra.NullOrEmpty())
            {
                report.AppendLine(extra);
            }
            if (i < traits.Count - 1)
            {
                report.AppendLine();
            }
        }

        yield return new StatDrawEntry(
            parent.def.IsMeleeWeapon ? StatCategoryDefOf.Weapon_Melee : StatCategoryDefOf.Weapon_Ranged,
            "BEWH.Framework.Artifact.TraitsStat".Translate(kind.LabelCap),
            traits.Where(trait => trait != null).Select(trait => trait.label).ToCommaList().CapitalizeFirst(),
            report.ToString().TrimEndNewlines(),
            1104);
    }

    //Saving
    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Defs.Look(ref kind, "kind");
        Scribe_Collections.Look(ref traits, "traits", LookMode.Def);
        Scribe_Values.Look(ref stage, "stage", -1);
        Scribe_Values.Look(ref name, "name");
        Scribe_Deep.Look(ref kills, "kills");
        Scribe_Collections.Look(ref bearers, "bearers", LookMode.Deep);
        Scribe_Collections.Look(ref stageTicks, "stageTicks", LookMode.Value);
        Scribe_Collections.Look(ref state, "state", LookMode.Value, LookMode.Value);
        Scribe_Collections.Look(ref forcedHistory, "forcedHistory", LookMode.Def);

        if (Scribe.mode != LoadSaveMode.PostLoadInit)
        {
            return;
        }

        traits ??= [];
        traits.RemoveAll(trait => trait == null);
        kills ??= new ArtifactKillRecord();
        bearers ??= [];
        bearers.RemoveAll(bearer => bearer == null);
        stageTicks ??= [];
        state ??= new Dictionary<string, float>();
        forcedHistory ??= [];
        forcedHistory.RemoveAll(def => def == null);

        if (kind == null && stage >= 0)
        {
            Log.Warning("[RimDark] " + parent.def.defName + " lost its artifact kind (content mod removed); it is a plain weapon again.");
            traits = [];
            stage = -1;
            name = null;
            stageTicks = [];
        }
        else if (kind != null)
        {
            if (stage >= kind.stages.Count)
            {
                stage = kind.stages.Count - 1;
            }
            //Deferred so a forced decoration added by an update is fitted once the pawn is fully loaded.
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                if (IsArtifact && !parent.Destroyed)
                {
                    ApplyForcedDecorations();
                }
            });
        }

        var wielder = Wielder;
        RefreshWielder(wielder);
        if (wielder != null)
        {
            RegisterStatSource(wielder);
            appliedTraits.Clear();
            appliedTraits.AddRange(ActiveTraits);
            ArtifactWeaponUtility.UpdateTicking(this);
        }
    }

}
