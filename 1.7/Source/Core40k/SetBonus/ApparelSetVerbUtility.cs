using System;
using Verse;

namespace Core40k;

/// <summary>
/// Applies apparel set tier verb modifiers to the verbs of the set's own weapons. Runs after the ammo
/// changer, weapon decoration and artifact adjustments.
/// </summary>
public static class ApparelSetVerbUtility
{
    private static VerbModifier ModifierFor(Verb verb)
    {
        if (!ApparelSetIndex.AnyVerbModifiers)
        {
            return null;
        }

        var weapon = verb.EquipmentSource;
        if (weapon == null || !ApparelSetIndex.HasVerbModifier(weapon.def))
        {
            return null;
        }

        return verb.CasterPawn?.GetComp<CompRankInfo>()?.ApparelSetsIfAny?.VerbModifierFor(weapon.def);
    }

    public static void ApplyBurst(Verb verb, ref int result)
    {
        var modifier = ModifierFor(verb);
        if (modifier == null || modifier.additionalBurstShotCount == 0)
        {
            return;
        }

        result = Math.Max(1, result + modifier.additionalBurstShotCount);
    }

    public static void ApplyRange(Verb verb, ref float result)
    {
        var modifier = ModifierFor(verb);
        if (modifier == null || modifier.additionalRange == 0f)
        {
            return;
        }

        result = Math.Max(0f, result + modifier.additionalRange);
    }

    public static void ApplyWarmup(Verb verb, ref float result)
    {
        var modifier = ModifierFor(verb);
        if (modifier == null || modifier.additionalWarmupTime == 0f)
        {
            return;
        }

        result = Math.Max(0f, result + modifier.additionalWarmupTime);
    }
}
