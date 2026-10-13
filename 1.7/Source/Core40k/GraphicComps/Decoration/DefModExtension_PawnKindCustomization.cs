using System.Collections.Generic;
using Verse;

namespace Core40k;

public class DefModExtension_PawnKindCustomization : DefModExtension
{
    public Dictionary<ColourPresetDef, ColorSelectionType> defaultColorSelection = [];
    
    public Dictionary<ThingDef, List<ExtraDecorationDef>> extraDecorations = [];
    
    public Dictionary<ThingDef, DecorationPresetDef> extraDecorationPreset = [];
}

public enum ColorSelectionType
{
    Fail = 0,
    Default = 1,
    TryMatch = 2,
}