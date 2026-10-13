using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Core40k;

public class GameComponent_Devotion : GameComponent
{
    private const int TicksPerDay = 60000;
    private const int TickInterval = 2500;

    private Dictionary<DevotionCreedDef, float> colonyStanding = new Dictionary<DevotionCreedDef, float>();

    private Dictionary<ConsequenceTrackDef, float> consequenceTracks = new Dictionary<ConsequenceTrackDef, float>();

    private List<DevotionRecord> records = [];

    private List<DevotionCreedDef> colonyStandingKeys;
    private List<float> colonyStandingValues;
    private List<ConsequenceTrackDef> consequenceTrackKeys;
    private List<float> consequenceTrackValues;

    private Dictionary<DevotionCreedDef, List<Pawn>> creedIndex;

    public List<DevotionRecord> Records => records ??= [];

    public GameComponent_Devotion(Game game)
    {
    }

    public float ColonyStanding(DevotionCreedDef creed)
    {
        if (creed == null)
        {
            return 0f;
        }

        return colonyStanding.TryGetValue(creed, out var value) ? value : 0f;
    }

    public float ConsequenceLevel(ConsequenceTrackDef track)
    {
        if (track == null)
        {
            return 0f;
        }

        return consequenceTracks.TryGetValue(track, out var value) ? value : 0f;
    }

    public void AddConsequence(ConsequenceTrackDef track, float amount)
    {
        if (track == null)
        {
            return;
        }

        var level = Math.Clamp(ConsequenceLevel(track) + amount, 0f, track.max);
        if (level <= 0f)
        {
            consequenceTracks.Remove(track);
        }
        else
        {
            consequenceTracks[track] = level;
        }
    }

    public void AddRecord(DevotionRecord record)
    {
        if (record == null)
        {
            return;
        }

        Records.Add(record);
    }

    /// <summary>
    /// Total standing and focus for a faith. Focus is 0 when spread evenly across the faith's
    /// creeds and 1 when concentrated in one, and drives cross-creed tolerance.
    /// </summary>
    public (float total, float focus) FaithSummary(DevotionFaithDef faith)
    {
        if (faith == null)
        {
            return (0f, 0f);
        }

        var total = 0f;
        var highest = 0f;
        foreach (var creed in faith.Creeds)
        {
            var value = ColonyStanding(creed);
            total += value;
            if (value > highest)
            {
                highest = value;
            }
        }

        var focus = total <= 0f ? 0f : highest / total;
        return (total, focus);
    }

    public float ToleranceFor(DevotionFaithDef faith)
    {
        if (faith == null)
        {
            return 0f;
        }

        var (total, focus) = FaithSummary(faith);
        if (total <= 0f)
        {
            return 0f;
        }

        return Math.Clamp((1f - focus) * faith.focusToleranceCurveMax, 0f, 1f);
    }

    public List<Pawn> PawnsOfCreed(DevotionCreedDef creed)
    {
        if (creed == null)
        {
            return [];
        }

        creedIndex ??= BuildCreedIndex();
        return creedIndex.TryGetValue(creed, out var list) ? list : [];
    }

    public void Notify_PawnDevotionChanged(Pawn pawn)
    {
        creedIndex = null;
    }

    public override void GameComponentTick()
    {
        base.GameComponentTick();

        if (!DevotionUtils.AnyCreedsLoaded)
        {
            return;
        }

        if (Find.TickManager.TicksGame % TickInterval != 0)
        {
            return;
        }

        var pawns = PlayerPawns();

        foreach (var pawn in pawns)
        {
            Core40kPawnData.Of(pawn)?.TickDevotionDecay(TickInterval);
        }

        RecomputeColonyStanding(pawns);
        TickConsequenceTracks();
    }

    private void RecomputeColonyStanding(List<Pawn> pawns)
    {
        var target = new Dictionary<DevotionCreedDef, float>();

        foreach (var pawn in pawns)
        {
            var comp = Core40kPawnData.Of(pawn);
            if (comp == null || !comp.HasAnyDevotion)
            {
                continue;
            }

            foreach (var creed in comp.ActiveCreeds)
            {
                if (creed == null)
                {
                    continue;
                }

                var contribution = comp.GetStandingPct(creed) * creed.colonyWeightPerPawn;
                target[creed] = (target.TryGetValue(creed, out var existing) ? existing : 0f) + contribution;
            }
        }

        foreach (var creed in colonyStanding.Keys)
        {
            if (!target.ContainsKey(creed))
            {
                target.Add(creed, 0f);
            }
        }

        foreach (var pair in target)
        {
            var smoothing = pair.Key.faith?.colonySmoothingPerDay ?? 0.15f;
            var step = smoothing * (TickInterval / (float)TicksPerDay);
            var current = ColonyStanding(pair.Key);
            var moved = Waylith.Math.Mathf.MoveTowards(current, pair.Value, Math.Max(step, 0.0001f));

            if (moved <= 0f && Waylith.Math.Mathf.Approximately(pair.Value, 0f))
            {
                colonyStanding.Remove(pair.Key);
            }
            else
            {
                colonyStanding[pair.Key] = moved;
            }
        }
    }

    /// <summary>
    /// Feeds each track from its faith's colony total, applies track decay, then rolls the highest threshold
    /// reached for one of its incidents on a player home map.
    /// </summary>
    private void TickConsequenceTracks()
    {
        var tracks = DefDatabase<ConsequenceTrackDef>.AllDefsListForReading;
        if (tracks.Count == 0)
        {
            return;
        }

        var dayFraction = TickInterval / (float)TicksPerDay;

        for (var i = 0; i < tracks.Count; i++)
        {
            var track = tracks[i];
            if (track.faith == null)
            {
                continue;
            }

            var (total, _) = FaithSummary(track.faith);
            var delta = 0f;
            if (total > 0f && track.gainPerColonyStandingPerDay > 0f)
            {
                delta += track.gainPerColonyStandingPerDay * total * dayFraction;
            }

            if (track.decayPerDay > 0f)
            {
                delta -= track.decayPerDay * dayFraction;
            }

            if (!Waylith.Math.Mathf.Approximately(delta, 0f))
            {
                AddConsequence(track, delta);
            }

            var level = ConsequenceLevel(track);
            if (level <= 0f)
            {
                continue;
            }

            var threshold = track.ActiveThresholdAt(level);
            if (threshold == null || threshold.incidents.NullOrEmpty() || threshold.mtbDays <= 0f)
            {
                continue;
            }

            if (!Rand.MTBEventOccurs(threshold.mtbDays, TicksPerDay, TickInterval))
            {
                continue;
            }

            TryFireIncident(threshold);
        }
    }

    private static void TryFireIncident(ConsequenceThreshold threshold)
    {
        var map = Find.AnyPlayerHomeMap;
        if (map == null)
        {
            return;
        }

        var candidates = new List<(IncidentDef incident, IncidentParms parms)>();
        foreach (var incident in threshold.incidents)
        {
            if (incident == null)
            {
                continue;
            }

            var parms = StorytellerUtility.DefaultParmsNow(incident.category, map);
            if (incident.Worker.CanFireNow(parms))
            {
                candidates.Add((incident, parms));
            }
        }

        if (candidates.Count == 0)
        {
            return;
        }

        var chosen = candidates.RandomElement();
        chosen.incident.Worker.TryExecute(chosen.parms);
    }

    private Dictionary<DevotionCreedDef, List<Pawn>> BuildCreedIndex()
    {
        var index = new Dictionary<DevotionCreedDef, List<Pawn>>();

        foreach (var pawn in PlayerPawns())
        {
            var comp = Core40kPawnData.Of(pawn);
            if (comp == null || !comp.HasAnyDevotion)
            {
                continue;
            }

            foreach (var creed in comp.ActiveCreeds)
            {
                if (creed == null)
                {
                    continue;
                }

                if (!index.TryGetValue(creed, out var list))
                {
                    list = [];
                    index.Add(creed, list);
                }

                list.Add(pawn);
            }
        }

        return index;
    }

    private static List<Pawn> PlayerPawns()
    {
        var result = new List<Pawn>();

        var maps = Find.Maps;
        for (var i = 0; i < maps.Count; i++)
        {
            result.AddRange(maps[i].mapPawns.FreeColonists);
        }

        var caravans = Find.WorldObjects.Caravans;
        for (var i = 0; i < caravans.Count; i++)
        {
            var caravan = caravans[i];
            if (!caravan.IsPlayerControlled)
            {
                continue;
            }

            var pawns = caravan.PawnsListForReading;
            for (var j = 0; j < pawns.Count; j++)
            {
                if (pawns[j].Faction is { IsPlayer: true })
                {
                    result.Add(pawns[j]);
                }
            }
        }

        return result;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref colonyStanding, "colonyStanding", LookMode.Def, LookMode.Value, ref colonyStandingKeys, ref colonyStandingValues, false);
        Scribe_Collections.Look(ref consequenceTracks, "consequenceTracks", LookMode.Def, LookMode.Value, ref consequenceTrackKeys, ref consequenceTrackValues, false);
        Scribe_Collections.Look(ref records, "records", LookMode.Deep);

        if (Scribe.mode != LoadSaveMode.PostLoadInit)
        {
            return;
        }

        colonyStanding ??= new Dictionary<DevotionCreedDef, float>();
        consequenceTracks ??= new Dictionary<ConsequenceTrackDef, float>();
        records ??= [];
        records.RemoveAll(record => record == null);
        creedIndex = null;
    }
}
