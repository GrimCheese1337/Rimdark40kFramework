using System.Collections.Generic;
using Verse;

namespace Core40k;

public class DefModExtension_AddRandomGeneByWeight : DefModExtension
{
    public Dictionary<GeneDef, float> possibleGenesToGive = new();

    public IntRange amountToGive = IntRange.One;
    
    public int chanceToGrantGene = 100;

    public bool skipIfAnyAlreadyExistsOnPawn = false;
    
    public bool addAsXenogene = true;
}