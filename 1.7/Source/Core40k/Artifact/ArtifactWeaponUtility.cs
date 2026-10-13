using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Core40k;

[StaticConstructorOnStartup]
public static class ArtifactWeaponUtility
{
    private static Game cachedGame;
    private static GameComponent_CoreUtils coreUtils;

    public static GameComponent_CoreUtils CoreUtils
    {
        get
        {
            if (coreUtils != null && cachedGame == Current.Game)
            {
                return coreUtils;
            }

            cachedGame = Current.Game;
            coreUtils = cachedGame?.GetComponent<GameComponent_CoreUtils>();
            return coreUtils;
        }
    }

    //Held artifacts whose active traits tick. Rebuilt from PostExposeData on load, so never saved.
    private static readonly List<CompArtifactWeapon> ticking = [];

    static ArtifactWeaponUtility()
    {
        Core40kEvents.PawnKilled += OnPawnKilled;
        InjectInspectTabs();
    }

    /// <summary>
    /// Turns the weapon into an artifact of the given kind at stage 0. Traits are rolled unless a curated list is
    /// given. Refuses, with an error, when the weapon def does not list the kind or is already an artifact.
    /// </summary>
    public static bool MakeArtifact(ThingWithComps weapon, ArtifactKindDef kind, List<ArtifactTraitDef> traits = null, bool silent = false)
    {
        var comp = weapon?.GetComp<CompArtifactWeapon>();
        if (comp == null)
        {
            Log.Error("[RimDark] MakeArtifact: " + (weapon?.def.defName ?? "null") + " has no CompArtifactWeapon.");
            return false;
        }
        if (!comp.CanBecome(kind, out var reason))
        {
            Log.Error("[RimDark] MakeArtifact: " + reason);
            return false;
        }

        comp.MakeArtifact(kind, traits, silent);
        return comp.IsArtifact;
    }

    public static CompArtifactWeapon ArtifactOf(Thing thing)
    {
        return thing is ThingWithComps withComps && Core40kUtils.DefHasComp<CompArtifactWeapon>(withComps.def)
            ? withComps.GetComp<CompArtifactWeapon>()
            : null;
    }

    private static void OnPawnKilled(Pawn killed, Pawn killer)
    {
        var primary = killer?.equipment?.Primary;
        if (primary == null)
        {
            return;
        }

        ArtifactOf(primary)?.Notify_KilledPawn(killer, killed);
    }

    public static void UpdateTicking(CompArtifactWeapon comp)
    {
        if (comp == null)
        {
            return;
        }

        if (comp.WantsTicking)
        {
            if (!ticking.Contains(comp))
            {
                ticking.Add(comp);
            }
        }
        else
        {
            ticking.Remove(comp);
        }
    }

    public static void Unregister(CompArtifactWeapon comp)
    {
        ticking.Remove(comp);
    }

    public static void ClearTicking()
    {
        ticking.Clear();
    }

    private static readonly List<CompArtifactWeapon> tickBuffer = [];

    public static void Tick()
    {
        if (ticking.Count == 0)
        {
            return;
        }

        var ticksGame = Find.TickManager.TicksGame;
        tickBuffer.Clear();
        tickBuffer.AddRange(ticking);
        foreach (var comp in tickBuffer)
        {
            if (comp?.parent == null || comp.parent.Destroyed)
            {
                ticking.Remove(comp);
                continue;
            }

            var wielder = comp.Wielder;
            if (wielder == null || wielder.Dead)
            {
                ticking.Remove(comp);
                continue;
            }

            comp.TickWorkers(wielder, ticksGame);
        }
        tickBuffer.Clear();
    }

    private static void InjectInspectTabs()
    {
        var tab = InspectTabManager.GetSharedInstance(typeof(ITab_Artifact));
        foreach (var thingDef in DefDatabase<ThingDef>.AllDefsListForReading)
        {
            if (!Core40kUtils.DefHasComp<CompArtifactWeapon>(thingDef))
            {
                continue;
            }

            thingDef.inspectorTabsResolved ??= [];
            if (!thingDef.inspectorTabsResolved.Contains(tab))
            {
                thingDef.inspectorTabsResolved.Add(tab);
            }
        }
    }
}
