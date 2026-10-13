using HarmonyLib;
using RimWorld;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(PawnCapacityWorker_BloodPumping), "CalculateCapacityLevel")]
public class LostHeartSurvivalPatch
{
    public static float Postfix(float originalResult, HediffSet __0)
    {
        if (originalResult > 0)
        {
            return originalResult;
        }
        
        var hediffSet = __0;

        var pawn = hediffSet.pawn;
        if (pawn.DestroyedOrNull() || pawn.genes == null || pawn.Dead)
        {
            return originalResult;
        }
        
        foreach (var gene in pawn.genes.GenesListForReading)
        {
            if (!gene.def.HasModExtension<DefModExtension_LostHeartSurvival>() || !gene.Active)
            {
                continue;
            }

            var capacityMod = gene.def.capMods.FirstOrFallback(mod => mod.capacity == PawnCapacityDefOf.BloodPumping);
            if (capacityMod == null)
            {
                continue;
            }
            
            var result = capacityMod.offset * capacityMod.postFactor;
            return result;
        }
        
        return originalResult;
    }
}