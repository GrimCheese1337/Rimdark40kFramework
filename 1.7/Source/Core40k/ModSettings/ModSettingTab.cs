using UnityEngine;
using Verse;

namespace Core40k;

public abstract class ModSettingTab
{
    protected Vector2 scrollPos;
    protected float scrollViewHeight = 0f;
    protected const float ListingHeightIncrease = 24f;
    protected const float ListingHeightIncreaseGap = 36f;
    
    public abstract void DrawTab(Rect inRect, ModSettings settings);
}