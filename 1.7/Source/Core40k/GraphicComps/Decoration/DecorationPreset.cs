using System.Collections.Generic;
using Verse;

namespace Core40k;

public class DecorationPreset : IExposable
{
    public string name = "decoration preset";
    public string appliesTo = null;
    public List<DecorationPresetParts> decorationPresetParts = [];
    
    public DecorationPreset()
    {
            
    }
    
    public DecorationPreset(string name, List<DecorationPresetParts> decorationPresetParts, ThingDef appliesTo)
    {
        this.name = name;
        this.appliesTo = appliesTo.defName;
        this.decorationPresetParts = decorationPresetParts;
    }
    
    public void ExposeData()
    {
        Scribe_Collections.Look(ref decorationPresetParts, "extraDecorationPreset");
        Scribe_Values.Look(ref appliesTo, "appliesTo");
        Scribe_Values.Look(ref name, "name");
    }
}