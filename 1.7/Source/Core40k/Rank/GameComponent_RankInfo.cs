using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Core40k;

public class GameComponent_RankInfo : GameComponent
{
    public Dictionary<RankDef, int> rankLimits = new Dictionary<RankDef, int>();
    
    private bool eligibilityBaselineDone;
    
    private int cachedColonistCount = -1;
    private int cachedColonistCountTick = -1;

    private const int CountRefreshIntervalTicks = 2500;

    private readonly Dictionary<RankDef, int> heldRankCounts = new Dictionary<RankDef, int>();

    private readonly HashSet<Pawn> countedPawns = [];

    private bool heldRankCountsDirty = true;

    public GameComponent_RankInfo(Game game)
    {
    }

    public override void GameComponentTick()
    {
        base.GameComponentTick();
        RankEligibilityNotifier.Tick();

        if (Find.TickManager.TicksGame % CountRefreshIntervalTicks == 0)
        {
            heldRankCountsDirty = true;
        }
    }

    public override void StartedNewGame()
    {
        base.StartedNewGame();
        RecountRankLimits();
        eligibilityBaselineDone = true;
    }

    /// <summary>
    /// Marks the held rank counts for rebuilding on next use.
    /// </summary>
    public void RecountRankLimits()
    {
        heldRankCountsDirty = true;
        cachedColonistCountTick = -1;
    }

    /// <summary>
    /// Marks the held rank counts for rebuilding on next use.
    /// </summary>
    public void Notify_RankHoldersChanged()
    {
        heldRankCountsDirty = true;
    }

    private void EnsureHeldRankCounts()
    {
        if (!heldRankCountsDirty)
        {
            return;
        }

        heldRankCountsDirty = false;
        heldRankCounts.Clear();
        countedPawns.Clear();

        foreach (var pawn in PawnsFinder.AllMapsWorldAndTemporary_Alive)
        {
            if (!RankUtils.CountsTowardsRankLimits(pawn) || !countedPawns.Add(pawn))
            {
                continue;
            }

            var comp = pawn.GetComp<CompRankInfo>();
            if (comp == null)
            {
                continue;
            }

            foreach (var rank in comp.UnlockedRanks)
            {
                if (rank == null || !RankUtils.IsLimited(rank))
                {
                    continue;
                }

                heldRankCounts[rank] = heldRankCounts.TryGetValue(rank, out var count) ? count + 1 : 1;
            }
        }

        countedPawns.Clear();
    }

    public override void LoadedGame()
    {
        base.LoadedGame();
        heldRankCountsDirty = true;

        if (eligibilityBaselineDone)
        {
            return;
        }

        RankEligibilityNotifier.SeedBaseline();
        eligibilityBaselineDone = true;
    }

    public void PawnResetRanks(List<RankDef> unlockedRanks)
    {
        heldRankCountsDirty = true;
    }

    public void PawnLostRank(RankDef rankDef)
    {
        heldRankCountsDirty = true;
    }

    public void PawnGainedRank(RankDef rankDef)
    {
        heldRankCountsDirty = true;
    }
        
    public bool CanHaveMoreOfRank(RankDef rankDef)
    {
        return AllowedAmountOfRank(rankDef) > CurrentAmountOfRank(rankDef);
    }
        
    public (bool allowed, int allowedAmount, int currentAmount) CanHaveMoreOfRankWithInfo(RankDef rankDef)
    {
        var allowedAmount = AllowedAmountOfRank(rankDef);
        var currentAmount = CurrentAmountOfRank(rankDef);

        return (allowedAmount > currentAmount, allowedAmount, currentAmount);
    }
        
    public int AllowedAmountOfRank(RankDef rankDef)
    {
        var playerPawnAmount = GetColonistForCounting();
                
        var allowedAmount = rankDef.colonyLimitOfRank.y > 0 ? rankDef.colonyLimitOfRank.x + Math.Floor(playerPawnAmount/rankDef.colonyLimitOfRank.y) : rankDef.colonyLimitOfRank.x;

        return (int)allowedAmount;
    }

    public int CurrentAmountOfRank(RankDef rankDef)
    {
        EnsureHeldRankCounts();
        return heldRankCounts.TryGetValue(rankDef, out var count) ? count : 0;
    }
        
    private int GetColonistForCounting()
    {
        var tickManager = Find.TickManager;
        var caching = tickManager != null && !tickManager.Paused;

        if (caching && cachedColonistCountTick == tickManager.TicksGame)
        {
            return cachedColonistCount;
        }

        var playerPawnAmount = 0;

        var maps = Find.Maps;
        for (var i = 0; i < maps.Count; i++)
        {
            playerPawnAmount += maps[i].mapPawns.ColonistCount;
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
                    playerPawnAmount++;
                }
            }
        }

        if (caching)
        {
            cachedColonistCount = playerPawnAmount;
            cachedColonistCountTick = tickManager.TicksGame;
        }

        return playerPawnAmount;
    }
        
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref rankLimits, "rankLimits");
        Scribe_Values.Look(ref eligibilityBaselineDone, "eligibilityBaselineDone", false);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            rankLimits ??= new Dictionary<RankDef, int>();
            heldRankCountsDirty = true;
        }
    }
}
