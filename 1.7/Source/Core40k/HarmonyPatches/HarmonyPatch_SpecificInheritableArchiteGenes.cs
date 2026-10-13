using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Core40k;

[StaticConstructorOnStartup]
public static class SpecificInheritableArchiteGenes
{
    static SpecificInheritableArchiteGenes()
    {
        Core40kMod.harmony.Patch(AccessTools.Method(typeof(PregnancyUtility), "ApplyBirthOutcome"), null, new HarmonyMethod(AccessTools.Method(typeof(SpecificInheritableArchiteGenes), "Postfix")));
    }

    public static void Postfix(ref Thing __result, Pawn geneticMother, Pawn father)
    {
        if (__result is not Pawn pawn)
        {
            return;
        }

        if (geneticMother?.genes == null || father?.genes == null)
        {
            return;
        }

        var inheritedArchiteGenes = GetInheriteArchiteGenes(geneticMother.genes, father.genes);

        if (inheritedArchiteGenes.NullOrEmpty())
        {
            return;
        }

        foreach (var geneDef in inheritedArchiteGenes.Where(geneDef => !pawn.genes.HasActiveGene(geneDef)))
        {
            pawn.genes.AddGene(geneDef, false);
        }
    }


    private static List<GeneDef> GetInheriteArchiteGenes(Pawn_GeneTracker mother, Pawn_GeneTracker father)
    {
        var geneSetMother = mother.Endogenes.Select((Gene x) => x.def).ToList();
        var geneSetFather = father.Endogenes.Select((Gene x) => x.def).ToList();

        var finalGenes = new List<GeneDef>();


        foreach (var geneDef in geneSetMother)
        {
            if (finalGenes.Contains(geneDef))
            {
                continue;
            }

            if (geneDef.biostatArc <= 0 || !geneDef.HasModExtension<DefModExtension_InheritableArchite>())
            {
                continue;
            }
                
            if (!geneDef.GetModExtension<DefModExtension_InheritableArchite>().presentOnBothParentsRequired || geneSetFather.Contains(geneDef))
            {
                finalGenes.Add(geneDef);
            }
        }

        foreach (var geneDef in geneSetFather)
        {
            if (finalGenes.Contains(geneDef))
            {
                continue;
            }

            if (geneDef.biostatArc <= 0 || !geneDef.HasModExtension<DefModExtension_InheritableArchite>())
            {
                continue;
            }
                
            if (!geneDef.GetModExtension<DefModExtension_InheritableArchite>().presentOnBothParentsRequired)
            {
                finalGenes.Add(geneDef);
            }
        }

        return finalGenes;
    }
}