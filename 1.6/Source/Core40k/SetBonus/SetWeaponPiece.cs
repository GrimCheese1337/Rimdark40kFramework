using System.Collections.Generic;
using Verse;

namespace Core40k;

/// <summary>
/// One weapon slot of an apparel set. Counts as a single piece while the pawn has any matching weapon
/// equipped, matched by def or by weaponTags.
/// </summary>
public class SetWeaponPiece
{
    public string label;

    public List<ThingDef> weapons = [];

    public List<string> weaponTags = [];

    [Unsaved]
    public HashSet<ThingDef> resolved = [];

    public bool Matches(ThingDef thingDef)
    {
        return thingDef != null && resolved != null && resolved.Contains(thingDef);
    }

    public string LabelCap
    {
        get
        {
            if (!label.NullOrEmpty())
            {
                return label.CapitalizeFirst();
            }

            if (!weapons.NullOrEmpty() && weapons[0] != null)
            {
                return weapons[0].LabelCap;
            }

            if (resolved != null)
            {
                foreach (var thingDef in resolved)
                {
                    return thingDef.LabelCap;
                }
            }

            return weaponTags.NullOrEmpty() ? "?" : weaponTags[0];
        }
    }

    /// <summary>
    /// Fills resolved from the explicit weapons and every ThingDef carrying one of the weaponTags.
    /// </summary>
    public void Resolve(Dictionary<string, List<ThingDef>> defsByWeaponTag)
    {
        resolved = [];
        if (weapons != null)
        {
            foreach (var weapon in weapons)
            {
                if (weapon != null)
                {
                    resolved.Add(weapon);
                }
            }
        }

        if (weaponTags == null)
        {
            return;
        }

        foreach (var tag in weaponTags)
        {
            if (tag != null && defsByWeaponTag.TryGetValue(tag, out var defs))
            {
                resolved.AddRange(defs);
            }
        }
    }
}
