using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Core40k;

public class CompProperties_PhysicalShield : CompProperties
{
    public float frontArcDegrees = 90f;
    public float backArcDegrees = 90f;

    public float frontBlockChance = 0.40f;
    public float sideBlockChance = 0.15f;
    public float backBlockChance = 0f;

    public float frontDamageFactor = 1f;
    public float sideDamageFactor = 1f;
    public float backDamageFactor = 1f;

    public bool shieldOnLeft = true;
    public float offSideFactor = 1f;

    public float meleeFactor = 1f;
    public float rangedFactor = 1f;
    public bool blocksExplosions = false;
    public List<DamageDef> neverBlocks;

    public float shieldArmorRating = 0.4f;
    public float maxBlockChance = 0.9f;
    public float undraftedFactor = 0.5f;
    public int blockCooldownTicks = 20;

    public float hitPointLossFactor = 0.25f;
    public int minHitPointLoss = 1;

    public bool requiresManipulation = true;
    public bool hideWhenInactive = false;

    public float blockSkillXp = 50f;

    public EffecterDef blockEffecter;
    public bool showBlockText = true;

    public CompProperties_PhysicalShield()
    {
        compClass = typeof(Comp_PhysicalShield);
    }

    /// <summary>
    /// Zone for the signed angle between the wearer's facing and the direction of the attacker (-180..180).
    /// </summary>
    public ShieldZone ZoneFor(float delta)
    {
        var abs = Mathf.Abs(delta);
        if (abs <= frontArcDegrees / 2f)
        {
            return ShieldZone.Front;
        }

        if (abs >= 180f - backArcDegrees / 2f)
        {
            return ShieldZone.Back;
        }

        return ShieldZone.Side;
    }

    /// <summary>
    /// Whether a negative delta (the wearer's left) or a positive one is the side without the shield.
    /// </summary>
    public bool IsWeaponArmSide(ShieldZone zone, float delta)
    {
        if (zone != ShieldZone.Side)
        {
            return false;
        }

        return shieldOnLeft ? delta > 0f : delta < 0f;
    }

    public float BlockChanceFor(ShieldZone zone, bool weaponArmSide)
    {
        var chance = zone switch
        {
            ShieldZone.Front => frontBlockChance,
            ShieldZone.Side => sideBlockChance,
            _ => backBlockChance
        };

        return weaponArmSide ? chance * offSideFactor : chance;
    }

    public float DamageFactorFor(ShieldZone zone, bool weaponArmSide)
    {
        var factor = zone switch
        {
            ShieldZone.Front => frontDamageFactor,
            ShieldZone.Side => sideDamageFactor,
            _ => backDamageFactor
        };

        return weaponArmSide ? 1f - (1f - factor) * offSideFactor : factor;
    }

    public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
    {
        foreach (var error in base.ConfigErrors(parentDef))
        {
            yield return error;
        }

        if (parentDef.apparel == null)
        {
            yield return $"{nameof(CompProperties_PhysicalShield)} on {parentDef.defName}, which is not apparel.";
        }
        else if (parentDef.apparel.layers.NullOrEmpty() || !parentDef.apparel.layers.Any(layer => layer?.defName == "BEWH_Shield"))
        {
            yield return $"{nameof(CompProperties_PhysicalShield)} on {parentDef.defName}, which is not on the BEWH_Shield apparel layer.";
        }

        if (frontArcDegrees < 0f || backArcDegrees < 0f || frontArcDegrees + backArcDegrees > 360f)
        {
            yield return $"{nameof(CompProperties_PhysicalShield)} on {parentDef.defName} has invalid arcs: front and back must be non-negative and add up to at most 360.";
        }

        if (frontBlockChance is < 0f or > 1f || sideBlockChance is < 0f or > 1f || backBlockChance is < 0f or > 1f)
        {
            yield return $"{nameof(CompProperties_PhysicalShield)} on {parentDef.defName} has a block chance outside 0-1.";
        }
    }
}
