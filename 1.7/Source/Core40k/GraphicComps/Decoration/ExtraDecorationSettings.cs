namespace Core40k;

public class ExtraDecorationSettings : DecorationSettings
{
    public ExtraDecorationSettings()
    {
        
    }

    public ExtraDecorationSettings(DecorationSettings decorationSettings)
    {
        Flipped = decorationSettings.Flipped;
        Color = decorationSettings.Color;
        ColorTwo = decorationSettings.ColorTwo;
        ColorThree = decorationSettings.ColorThree;
        maskDef = decorationSettings.maskDef;
    }
}