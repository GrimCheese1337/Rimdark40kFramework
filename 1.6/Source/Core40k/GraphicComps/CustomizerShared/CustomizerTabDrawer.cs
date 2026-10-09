using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Core40k;

public class CustomizerTabDrawer
{
    public virtual void Setup(Pawn pawn)
    {

    }

    public virtual void DrawTab(Rect rect, Pawn pawn, ref Vector2 scrollPosition)
    {

    }

    /// <summary>
    /// Whether the tab is currently shown in the dialog. Re-checked every frame, so it can follow edits made in other tabs.
    /// </summary>
    public virtual bool IsVisible(Pawn pawn)
    {
        return true;
    }

    public virtual void OnClose(Pawn pawn, bool closeOnCancel, bool closeOnClickedOutside)
    {

    }

    public virtual void OnReset(Pawn pawn)
    {

    }

    //Comps this drawer edits, so the dialog can capture and commit them exactly once.
    public virtual IEnumerable<CompGraphicParent> Comps => [];
}
