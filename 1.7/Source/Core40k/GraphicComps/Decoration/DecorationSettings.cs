using UnityEngine;
using Verse;

namespace Core40k;

public class DecorationSettings : IExposable
{
    public Color Color = Color.white;
    public Color ColorTwo = Color.white;
    public Color ColorThree = Color.white;
    public bool Flipped = false;
    public MaskDef maskDef = null;

    public DecorationSettings()
    {
        
    }

    public DecorationSettings(DecorationSettings decorationSettings)
    {
        Flipped = decorationSettings.Flipped;
        Color = decorationSettings.Color;
        ColorTwo = decorationSettings.ColorTwo;
        ColorThree = decorationSettings.ColorThree;
        maskDef = decorationSettings.maskDef;
    }
    
    public virtual void ExposeData()
    {
        Scribe_Values.Look(ref Color, "Color");
        Scribe_Values.Look(ref ColorTwo, "ColorTwo");
        Scribe_Values.Look(ref ColorThree, "ColorThree");
        Scribe_Values.Look(ref Flipped, "Flipped");
        Scribe_Defs.Look(ref maskDef, "maskDef");
    }
}