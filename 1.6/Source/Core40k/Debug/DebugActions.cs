using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using RimWorld;
using Verse;

namespace Core40k;

public static class DebugActions
{
    [DebugAction("RimDark", "Increment all rank days by 1", false, false, false, false, false,0, false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = -1000)]
    private static void IncrementRankDays(Pawn p)
    {
        if (!p.HasComp<CompRankInfo>())
        {
            return;
        }

        p.GetComp<CompRankInfo>().IncreaseDaysForAllRank();
    }
    
    [DebugAction("RimDark", "Print days as ranks", false, false, false, false, false,0, false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = -1000)]
    private static void PrintDaysAsRanks(Pawn p)
    {
        if (!p.HasComp<CompRankInfo>())
        {
            return;
        }

        var comp = p.GetComp<CompRankInfo>();
        foreach (var rankDef in comp.UnlockedRanks)
        {
            Log.Message(rankDef.label + " : " + comp.GetDaysAsRank(rankDef));
        }
    }

        
    [DebugAction("RimDark", "Reset all ranks", false, false, false, false, false, 0, false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = -1000)]
    private static void ResetRank(Pawn p)
    {
        if (!p.HasComp<CompRankInfo>())
        {
            return;
        }
        
        Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
            "Reset every rank on " + p.LabelShortCap + "?",
            () => p.GetComp<CompRankInfo>().ResetRanks(null),
            destructive: true));
    }

    private static CompArtifactWeapon ArtifactCompOf(Thing thing)
    {
        if (thing is Pawn pawn)
        {
            thing = pawn.equipment?.Primary;
        }
        return ArtifactWeaponUtility.ArtifactOf(thing);
    }

    private static List<DebugActionNode> ArtifactTargets(System.Action<CompArtifactWeapon> action)
    {
        var nodes = new List<DebugActionNode>();
        var seen = new HashSet<CompArtifactWeapon>();
        foreach (var thing in Find.Selector.SelectedObjects.OfType<Thing>())
        {
            var comp = ArtifactCompOf(thing);
            if (comp == null || !seen.Add(comp))
            {
                continue;
            }
            nodes.Add(new DebugActionNode(comp.parent.LabelCap, DebugActionType.Action, () => action(comp)));
        }
        return nodes;
    }

    [DebugAction("RimDark", "Artifact: make...", false, false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = -900)]
    private static List<DebugActionNode> ArtifactMake()
    {
        var nodes = new List<DebugActionNode>();
        foreach (var thing in Find.Selector.SelectedObjects.OfType<Thing>())
        {
            var comp = ArtifactCompOf(thing);
            if (comp == null || comp.IsArtifact)
            {
                continue;
            }
            var weaponNode = new DebugActionNode(comp.parent.LabelCap);
            foreach (var kind in comp.Props.kinds)
            {
                if (kind == null)
                {
                    continue;
                }
                weaponNode.AddChild(new DebugActionNode(kind.defName, DebugActionType.Action, () => comp.MakeArtifact(kind)));
            }
            nodes.Add(weaponNode);
        }
        return nodes;
    }

    [DebugAction("RimDark", "Artifact: add trait...", false, false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = -899)]
    private static List<DebugActionNode> ArtifactAddTrait()
    {
        var nodes = new List<DebugActionNode>();
        foreach (var thing in Find.Selector.SelectedObjects.OfType<Thing>())
        {
            var comp = ArtifactCompOf(thing);
            if (comp == null || !comp.IsArtifact)
            {
                continue;
            }
            var weaponNode = new DebugActionNode(comp.parent.LabelCap);
            foreach (var trait in DefDatabase<ArtifactTraitDef>.AllDefsListForReading)
            {
                if (comp.CanAddTrait(trait))
                {
                    weaponNode.AddChild(new DebugActionNode(trait.defName, DebugActionType.Action, () => comp.AddTrait(trait)));
                }
            }
            nodes.Add(weaponNode);
        }
        return nodes;
    }

    [DebugAction("RimDark", "Artifact: remove trait...", false, false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = -898)]
    private static List<DebugActionNode> ArtifactRemoveTrait()
    {
        var nodes = new List<DebugActionNode>();
        foreach (var thing in Find.Selector.SelectedObjects.OfType<Thing>())
        {
            var comp = ArtifactCompOf(thing);
            if (comp == null || !comp.IsArtifact)
            {
                continue;
            }
            var weaponNode = new DebugActionNode(comp.parent.LabelCap);
            foreach (var trait in comp.Traits.ToList())
            {
                weaponNode.AddChild(new DebugActionNode(trait.defName, DebugActionType.Action, () => comp.RemoveTrait(trait)));
            }
            nodes.Add(weaponNode);
        }
        return nodes;
    }

    [DebugAction("RimDark", "Artifact: add 10 kills", false, false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = -897)]
    private static List<DebugActionNode> ArtifactAddKills()
    {
        return ArtifactTargets(comp =>
        {
            var wielder = comp.Wielder;
            for (var i = 0; i < 10; i++)
            {
                comp.Notify_KilledPawn(wielder, wielder);
            }
        });
    }

    [DebugAction("RimDark", "Artifact: advance stage", false, false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = -896)]
    private static List<DebugActionNode> ArtifactAdvanceStage()
    {
        return ArtifactTargets(comp =>
        {
            if (!comp.TryAdvanceStage(comp.Wielder))
            {
                Messages.Message("Conditions for the next stage are not met.", MessageTypeDefOf.RejectInput, false);
            }
        });
    }

    [DebugAction("RimDark", "Artifact: strip", false, false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = -895)]
    private static List<DebugActionNode> ArtifactStrip()
    {
        return ArtifactTargets(comp => comp.Strip());
    }
}
