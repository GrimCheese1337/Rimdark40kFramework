using HarmonyLib;
using System.Collections.Generic;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(GenRecipe), "MakeRecipeProducts")]
public class DontPlaceProductPatch
{
    public static bool Prefix(ref IEnumerable<Thing> __result, RecipeDef recipeDef)
    {
        if (!recipeDef.HasModExtension<DefModExtension_DontPlaceProduct>())
        {
            return true;
        }
            
        __result = new List<Thing>();
        return false;
    }
}