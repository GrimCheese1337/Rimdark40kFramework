using HarmonyLib;
using RimWorld;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(StatWorker), "GetValueUnfinalized")]
public static class GetValueUnfinalizedFromVariousFactorPatch
{
    private static Game cachedGameForCoreUtils;
    private static GameComponent_CoreUtils coreUtils;

    private static GameComponent_CoreUtils CoreUtils => CoreUtilsFor();

    private static GameComponent_CoreUtils CoreUtilsFor()
    {
        if (coreUtils != null && cachedGameForCoreUtils == Current.Game)
        {
            return coreUtils;
        }

        cachedGameForCoreUtils = Current.Game;
        coreUtils = cachedGameForCoreUtils?.GetComponent<GameComponent_CoreUtils>();

        return coreUtils;
    }
    
    public static void Postfix(ref float __result, StatWorker __instance, StatRequest req)
    {
        if (req.Thing is not Pawn pawn)
        {
            return;
        }

        var stat = __instance.stat;
        if (!DecorationIndex.AffectsPawnStat(stat))
        {
            return;
        }

        var coreUtilsComp = CoreUtils;
        if (coreUtilsComp == null)
        {
            return;
        }

        var offset = 0f;
        var factor = 1f;

        if (coreUtilsComp.cachedDecoratives.TryGetValue(pawn, out var cachedDecoratives))
        {
            Accumulate(cachedDecoratives, stat, ref offset, ref factor);
        }

        if (coreUtilsComp.cachedAlternateTexture.TryGetValue(pawn, out var cachedAlternateTexture))
        {
            Accumulate(cachedAlternateTexture, stat, ref offset, ref factor);
        }

        if (offset == 0f && factor == 1f)
        {
            return;
        }

        __result = (__result + offset) * factor;
    }

    /// <summary>
    /// Single pass over the cached comps collecting both the pawn stat offset and factor.
    /// </summary>
    private static void Accumulate(GameComponent_CoreUtils.CachedDecoratives cached, StatDef stat, ref float offset, ref float factor)
    {
        var apparelComps = cached.apparelComps;
        for (var i = 0; i < apparelComps.Count; i++)
        {
            if (apparelComps[i] is IPawnStatSource source)
            {
                offset += source.GetPawnStatOffset(stat);
                factor *= source.GetPawnStatFactor(stat);
            }
        }

        var weaponComps = cached.weaponComps;
        for (var i = 0; i < weaponComps.Count; i++)
        {
            if (weaponComps[i] is IPawnStatSource source)
            {
                offset += source.GetPawnStatOffset(stat);
                factor *= source.GetPawnStatFactor(stat);
            }
        }
    }

    public static float GetStatOffsetForX(Pawn pawn, StatDef stat)
    {
        var num = 0f;

        var coreUtilsComp = CoreUtils;
        if (coreUtilsComp == null)
        {
            return num;
        }

        if (coreUtilsComp.cachedDecoratives.TryGetValue(pawn, out var cachedDecoratives))
        {
            num += OffsetFrom(cachedDecoratives, stat);
        }

        if (coreUtilsComp.cachedAlternateTexture.TryGetValue(pawn, out var cachedAlternateTexture))
        {
            num += OffsetFrom(cachedAlternateTexture, stat);
        }

        return num;
    }

    public static float GetStatFactorForX(Pawn pawn, StatDef stat)
    {
        var num = 1f;

        var coreUtilsComp = CoreUtils;
        if (coreUtilsComp == null)
        {
            return num;
        }

        if (coreUtilsComp.cachedDecoratives.TryGetValue(pawn, out var cachedDecoratives))
        {
            num *= FactorFrom(cachedDecoratives, stat);
        }

        if (coreUtilsComp.cachedAlternateTexture.TryGetValue(pawn, out var cachedAlternateTexture))
        {
            num *= FactorFrom(cachedAlternateTexture, stat);
        }
        
        return num;
    }

    private static float OffsetFrom(GameComponent_CoreUtils.CachedDecoratives cached, StatDef stat)
    {
        var num = 0f;

        var apparelComps = cached.apparelComps;
        for (var i = 0; i < apparelComps.Count; i++)
        {
            if (apparelComps[i] is IPawnStatSource source)
            {
                num += source.GetPawnStatOffset(stat);
            }
        }

        var weaponComps = cached.weaponComps;
        for (var i = 0; i < weaponComps.Count; i++)
        {
            if (weaponComps[i] is IPawnStatSource source)
            {
                num += source.GetPawnStatOffset(stat);
            }
        }

        return num;
    }

    private static float FactorFrom(GameComponent_CoreUtils.CachedDecoratives cached, StatDef stat)
    {
        var num = 1f;

        var apparelComps = cached.apparelComps;
        for (var i = 0; i < apparelComps.Count; i++)
        {
            if (apparelComps[i] is IPawnStatSource source)
            {
                num *= source.GetPawnStatFactor(stat);
            }
        }

        var weaponComps = cached.weaponComps;
        for (var i = 0; i < weaponComps.Count; i++)
        {
            if (weaponComps[i] is IPawnStatSource source)
            {
                num *= source.GetPawnStatFactor(stat);
            }
        }

        return num;
    }
}
