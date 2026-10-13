using System.Collections.Generic;
using Verse;

namespace Core40k;

public class DecorationPresetDef : Def
{
    public List<ThingDef> appliesTo = [];
    public List<PresetData> presetData = [];
}