using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Core40k;

public class ColourPresetDef : Def
{
    public Color primaryColour = Color.white;
    public Color secondaryColour = Color.white;
    public Color? tertiaryColour = null;
    
    public int colorAmount = 1;

    public PresetType appliesToKind = PresetType.All;
    
    public List<string> appliesTo = [];
}

public enum PresetType
{
    None,
    All,
    Armor,
    Weapon,
}