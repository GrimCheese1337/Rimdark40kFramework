using UnityEngine;
using Verse;

namespace Core40k;

public class Dialog_ConfirmColorPresetOverride : Window
{
    public override Vector2 InitialSize => new Vector2(300f, 120f);

    private ColourPreset colourPreset;
    private Color colorOne;
    private Color colorTwo;
    private Color colorThree;
        
    private static Core40kModSettings modSettings = null;

    public static Core40kModSettings ModSettings => modSettings ??= LoadedModManager.GetMod<Core40kMod>().GetSettings<Core40kModSettings>();
        
    public Dialog_ConfirmColorPresetOverride(ColourPreset colourPreset, Color colorOne, Color colorTwo, Color colorThree)
    {
        this.colourPreset = colourPreset;
        this.colorOne = colorOne;
        this.colorTwo = colorTwo;
        this.colorThree = colorThree;
    }
        
    public override void DoWindowContents(Rect inRect)
    {
        var viewRect = inRect.ContractedBy(5f);

        var labelRect = new Rect(viewRect)
        {
            height = viewRect.height * 0.5f
        };
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(labelRect, "BEWH.Framework.Customization.ConfirmOverride".Translate());
        Text.Anchor = TextAnchor.UpperLeft;
            
        var closeRect = new Rect(labelRect)
        {
            height = viewRect.height * 0.3f,
            width = labelRect.width / 4,
        };
        closeRect.y = viewRect.yMax - closeRect.height;
        if (Widgets.ButtonText(closeRect, "Close".Translate()))
        {
            Close();
        }
            
        var acceptRect = new Rect(closeRect)
        {
            xMin = viewRect.xMax - closeRect.width,
            width = closeRect.width,
        };
        if (Widgets.ButtonText(acceptRect, "Accept".Translate()))
        {
            ModSettings.UpdatePreset(colourPreset, colorOne, colorTwo, colorThree);
            Close();
        }
    }
}