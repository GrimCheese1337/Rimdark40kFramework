using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Core40k;

public static class ApparelLayerConflictUtility
{
    private static readonly List<ApparelLayerDef> NoLayers = [];

    private static bool? active;

    /// <summary>
    /// True when any loaded decoration grants extra apparel layers. Every patch exits on false.
    /// </summary>
    public static bool Active => active ??= DefDatabase<DecorationDef>.AllDefsListForReading.Any(def => !def.extraApparelLayers.NullOrEmpty());

    /// <summary>
    /// The layers this apparel occupies through its fitted decorations, beyond the layers of its def.
    /// </summary>
    public static List<ApparelLayerDef> ExtraLayersOf(Apparel apparel)
    {
        if (!Active || apparel == null)
        {
            return NoLayers;
        }

        var comp = apparel.GetComp<CompDecorativeBase>();
        return comp == null ? NoLayers : comp.ExtraApparelLayers;
    }

    /// <summary>
    /// True when the two pieces can be worn together by the def rules, but not once their decoration-granted layers are counted.
    /// </summary>
    public static bool ConflictsViaExtraLayers(Apparel a, Apparel b, BodyDef body)
    {
        if (!Active || a == null || b == null || a == b)
        {
            return false;
        }

        return ConflictsViaExtraLayers(a.def, ExtraLayersOf(a), b.def, ExtraLayersOf(b), body);
    }

    /// <summary>
    /// Same as the apparel overload, for layer sets that are not fitted yet.
    /// </summary>
    public static bool ConflictsViaExtraLayers(ThingDef aDef, List<ApparelLayerDef> extraA, ThingDef bDef, List<ApparelLayerDef> extraB, BodyDef body)
    {
        if (extraA.NullOrEmpty() && extraB.NullOrEmpty())
        {
            return false;
        }

        var layersA = aDef.apparel.layers;
        var layersB = bDef.apparel.layers;

        if (SharesLayer(layersA, layersB))
        {
            return false;
        }

        if (!SharesLayer(extraA, layersB) && !SharesLayer(layersA, extraB) && !SharesLayer(extraA, extraB))
        {
            return false;
        }

        return BodyPartGroupsInterfere(aDef, bDef, body);
    }

    /// <summary>
    /// ApparelUtility.CanWearTogether, with decoration-granted layers counted.
    /// </summary>
    public static bool CanWearTogether(Apparel a, Apparel b, BodyDef body)
    {
        return ApparelUtility.CanWearTogether(a.def, b.def, body) && !ConflictsViaExtraLayers(a, b, body);
    }

    /// <summary>
    /// After a piece has been put on, takes off whatever it now shares a decoration-granted layer with. The new piece wins,
    /// as in vanilla, except while the pawn is being generated, where a piece occupying extra layers is never displaced by
    /// one without.
    /// </summary>
    public static void ResolveAfterWear(Pawn pawn, Apparel newApparel)
    {
        if (!Active || pawn?.apparel == null || newApparel == null || newApparel.Wearer != pawn)
        {
            return;
        }

        var body = pawn.RaceProps.body;
        var generating = PawnGenerator.IsBeingGenerated(pawn);
        var newHasExtras = ExtraLayersOf(newApparel).Count > 0;

        List<Apparel> displaced = null;
        foreach (var worn in pawn.apparel.WornApparel)
        {
            if (!ConflictsViaExtraLayers(newApparel, worn, body))
            {
                continue;
            }

            if (generating && !newHasExtras && ExtraLayersOf(worn).Count > 0)
            {
                RemoveDisplaced(pawn, newApparel);
                return;
            }

            displaced ??= [];
            displaced.Add(worn);
        }

        if (displaced == null)
        {
            return;
        }

        foreach (var apparel in displaced)
        {
            RemoveDisplaced(pawn, apparel);
        }
    }

    /// <summary>
    /// Takes off every worn piece that shares a decoration-granted layer with another worn piece, never a locked one: when
    /// the other piece is locked, the piece holding the extra layer comes off instead. Called when decorations are
    /// committed to what the pawn is already wearing.
    /// </summary>
    public static void ResolveWornConflicts(Pawn pawn)
    {
        if (!Active || pawn?.apparel == null)
        {
            return;
        }

        var body = pawn.RaceProps.body;
        var worn = pawn.apparel.WornApparel;
        List<Apparel> displaced = null;

        for (var i = 0; i < worn.Count; i++)
        {
            var holder = worn[i];
            if (ExtraLayersOf(holder).Count == 0 || (displaced != null && displaced.Contains(holder)))
            {
                continue;
            }

            for (var j = 0; j < worn.Count; j++)
            {
                var other = worn[j];
                if (other == holder || (displaced != null && displaced.Contains(other)))
                {
                    continue;
                }

                if (!ConflictsViaExtraLayers(holder, other, body))
                {
                    continue;
                }

                displaced ??= [];
                if (pawn.apparel.IsLocked(other))
                {
                    if (!pawn.apparel.IsLocked(holder))
                    {
                        displaced.Add(holder);
                        break;
                    }
                    continue;
                }

                displaced.Add(other);
            }
        }

        if (displaced == null)
        {
            return;
        }

        foreach (var apparel in displaced)
        {
            RemoveDisplaced(pawn, apparel);
        }
    }

    /// <summary>
    /// The worn pieces, other than the apparel itself, that fitting the decoration to it would take off.
    /// </summary>
    public static List<Apparel> WornDisplacedBy(Pawn pawn, Apparel apparel, DecorationDef decoration)
    {
        if (!Active || pawn?.apparel == null || apparel == null || decoration == null || decoration.extraApparelLayers.NullOrEmpty() || apparel.Wearer != pawn)
        {
            return null;
        }

        var extras = new List<ApparelLayerDef>(ExtraLayersOf(apparel));
        foreach (var layer in decoration.extraApparelLayers)
        {
            if (layer != null && !extras.Contains(layer) && !apparel.def.apparel.layers.Contains(layer))
            {
                extras.Add(layer);
            }
        }

        var body = pawn.RaceProps.body;
        List<Apparel> result = null;
        foreach (var worn in pawn.apparel.WornApparel)
        {
            if (worn == apparel || !ConflictsViaExtraLayers(apparel.def, extras, worn.def, ExtraLayersOf(worn), body))
            {
                continue;
            }

            result ??= [];
            result.Add(worn);
        }

        return result;
    }

    /// <summary>
    /// The first held apparel on the stand that shares a decoration-granted layer with the given apparel, if any.
    /// </summary>
    public static Apparel StandConflict(Building_OutfitStand stand, Apparel apparel)
    {
        if (!Active || stand == null || apparel == null)
        {
            return null;
        }

        var held = stand.HeldItems;
        for (var i = 0; i < held.Count; i++)
        {
            if (held[i] is Apparel other && ConflictsViaExtraLayers(apparel, other, BodyDefOf.Human))
            {
                return other;
            }
        }

        return null;
    }

    /// <summary>
    /// Destroyed while the pawn is being generated, dropped when the pawn is on a map, otherwise moved to its inventory.
    /// </summary>
    public static void RemoveDisplaced(Pawn pawn, Apparel apparel)
    {
        var tracker = pawn.apparel;
        if (!tracker.WornApparel.Contains(apparel))
        {
            return;
        }

        if (PawnGenerator.IsBeingGenerated(pawn))
        {
            tracker.Remove(apparel);
            apparel.Destroy(DestroyMode.Vanish);
            return;
        }

        if (pawn.MapHeld != null)
        {
            var forbid = pawn.Faction != null && pawn.Faction.HostileTo(Faction.OfPlayer);
            if (tracker.TryDrop(apparel, out _, pawn.PositionHeld, forbid))
            {
                return;
            }
        }

        if (pawn.inventory != null && tracker.TryMoveToInventory(apparel))
        {
            return;
        }

        if (tracker.WornApparel.Contains(apparel))
        {
            tracker.Remove(apparel);
        }

        if (apparel.ParentHolder == null && !apparel.Destroyed)
        {
            apparel.Destroy(DestroyMode.Vanish);
        }
    }

    public static string LayersLabel(List<ApparelLayerDef> layers)
    {
        return layers.Where(layer => layer != null).Select(layer => layer.label).ToCommaList().CapitalizeFirst();
    }

    private static bool SharesLayer(List<ApparelLayerDef> a, List<ApparelLayerDef> b)
    {
        if (a.NullOrEmpty() || b.NullOrEmpty())
        {
            return false;
        }

        for (var i = 0; i < a.Count; i++)
        {
            if (b.Contains(a[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool BodyPartGroupsInterfere(ThingDef aDef, ThingDef bDef, BodyDef body)
    {
        var groupsA = aDef.apparel.bodyPartGroups;
        var groupsB = bDef.apparel.bodyPartGroups;
        var interferingA = aDef.apparel.GetInterferingBodyPartGroups(body);
        var interferingB = bDef.apparel.GetInterferingBodyPartGroups(body);

        for (var i = 0; i < groupsA.Count; i++)
        {
            if (interferingB.Contains(groupsA[i]))
            {
                return true;
            }
        }

        for (var i = 0; i < groupsB.Count; i++)
        {
            if (interferingA.Contains(groupsB[i]))
            {
                return true;
            }
        }

        return false;
    }
}
