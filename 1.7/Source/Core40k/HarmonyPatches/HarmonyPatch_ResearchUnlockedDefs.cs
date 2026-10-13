using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(ResearchProjectDef), nameof(ResearchProjectDef.UnlockedDefs), MethodType.Getter)]
public static class HarmonyPatch_ResearchUnlockedDefs
{
    private static Dictionary<ResearchProjectDef, List<Def>> extraUnlocks;
    private static readonly Dictionary<ResearchProjectDef, List<Def>> injectedInto = new();

    public static void Postfix(ResearchProjectDef __instance, List<Def> __result)
    {
        if (__result == null || (injectedInto.TryGetValue(__instance, out var injected) && injected == __result))
        {
            return;
        }
        injectedInto[__instance] = __result;

        extraUnlocks ??= BuildExtraUnlocks();
        if (!extraUnlocks.TryGetValue(__instance, out var defs))
        {
            return;
        }

        var added = false;
        foreach (var def in defs)
        {
            if (__result.Contains(def))
            {
                continue;
            }
            __result.Add(def);
            added = true;
        }

        if (!added)
        {
            return;
        }

        var sorted = __result.OrderBy(def => def.label).ToList();
        __result.Clear();
        __result.AddRange(sorted);
    }

    /// <summary>
    /// Maps each research project to the framework content it gates that vanilla does not list by itself.
    /// </summary>
    private static Dictionary<ResearchProjectDef, List<Def>> BuildExtraUnlocks()
    {
        var result = new Dictionary<ResearchProjectDef, List<Def>>();
        foreach (var decoration in DefDatabase<DecorationDef>.AllDefsListForReading)
        {
            if (decoration.mustHaveResearch == null)
            {
                continue;
            }
            foreach (var project in decoration.mustHaveResearch)
            {
                if (project != null)
                {
                    Add(result, project, decoration);
                }
            }
        }
        foreach (var thingDef in DefDatabase<ThingDef>.AllDefsListForReading)
        {
            var project = thingDef.GetModExtension<DefModExtension_AmmoChanger>()?.unlockedBy;
            if (project != null)
            {
                Add(result, project, thingDef);
            }
        }
        return result;
    }

    private static void Add(Dictionary<ResearchProjectDef, List<Def>> map, ResearchProjectDef project, Def def)
    {
        if (!map.TryGetValue(project, out var list))
        {
            list = [];
            map.Add(project, list);
        }
        if (!list.Contains(def))
        {
            list.Add(def);
        }
    }
}

[HarmonyPatch(typeof(Widgets), nameof(Widgets.DefIcon))]
public static class HarmonyPatch_DecorationDefIcon
{
    public static bool Prefix(Rect rect, Def def, float scale, Material material, float alpha)
    {
        if (def is not DecorationDef decoration)
        {
            return true;
        }
        Widgets.DrawTextureFitted(rect, decoration.Icon, scale, material, alpha);
        return false;
    }
}
