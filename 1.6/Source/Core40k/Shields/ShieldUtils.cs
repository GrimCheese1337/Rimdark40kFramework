using RimWorld;
using UnityEngine;
using Verse;

namespace Core40k;

public static class ShieldUtils
{
    public const int AttackedVisibleTicks = 600;

    /// <summary>
    /// The first worn apparel carrying <see cref="Comp_PhysicalShield"/>, or null.
    /// </summary>
    public static Comp_PhysicalShield GetWornShield(Pawn pawn)
    {
        var worn = pawn?.apparel?.WornApparel;
        if (worn == null)
        {
            return null;
        }

        for (var i = 0; i < worn.Count; i++)
        {
            if (Core40kUtils.DefHasComp<Comp_PhysicalShield>(worn[i].def))
            {
                return worn[i].GetComp<Comp_PhysicalShield>();
            }
        }

        return null;
    }

    /// <summary>
    /// Whether the wearer can use the shield at all. inactiveReasonKey is a translation key when false.
    /// </summary>
    public static bool IsActive(Pawn pawn, Comp_PhysicalShield shield, out string inactiveReasonKey)
    {
        inactiveReasonKey = null;
        if (pawn.Downed)
        {
            inactiveReasonKey = "BEWH.Framework.Shield.Inactive.Downed";
        }
        else if (!pawn.Awake())
        {
            inactiveReasonKey = "BEWH.Framework.Shield.Inactive.Asleep";
        }
        else if (shield.Props.requiresManipulation && !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
        {
            inactiveReasonKey = "BEWH.Framework.Shield.Inactive.NoManipulation";
        }

        return inactiveReasonKey == null;
    }

    /// <summary>
    /// Drafted, fighting on its own, or not player-controlled. Anything else gets the shield's undrafted factor.
    /// </summary>
    public static bool IsReady(Pawn pawn)
    {
        if (pawn.drafter == null || pawn.Drafted)
        {
            return true;
        }

        var job = pawn.CurJobDef;
        return job == JobDefOf.AttackMelee || job == JobDefOf.AttackStatic;
    }

    /// <summary>
    /// Whether a worn shield should be drawn: always when the setting says so, otherwise while drafted, fighting or recently attacked.
    /// </summary>
    public static bool ShouldShowShield(Pawn pawn, Comp_PhysicalShield shield)
    {
        if (Core40kUtils.ModSettings.alwaysShowShields || pawn.Drafted || shield.AttackedRecently)
        {
            return true;
        }

        var job = pawn.CurJobDef;
        if (job == JobDefOf.AttackMelee || job == JobDefOf.AttackStatic)
        {
            return true;
        }

        return PawnRenderUtility.CarryWeaponOpenly(pawn);
    }

    /// <summary>
    /// Makes the wearer's render tree re-check the shield's node and marks the cached pawn texture for re-rendering.
    /// </summary>
    public static void RefreshShieldGraphic(Pawn pawn, Apparel shield)
    {
        var tree = pawn.Drawer?.renderer?.renderTree;
        if (tree == null || !tree.Resolved)
        {
            return;
        }

        var node = FindApparelNode(tree.rootNode, shield);
        if (node != null)
        {
            node.requestRecache = true;
        }

        GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(pawn);
    }

    private static PawnRenderNode FindApparelNode(PawnRenderNode node, Apparel apparel)
    {
        if (node == null || apparel == null)
        {
            return null;
        }

        if (node.apparel == apparel)
        {
            return node;
        }

        if (node.children == null)
        {
            return null;
        }

        for (var i = 0; i < node.children.Length; i++)
        {
            var found = FindApparelNode(node.children[i], apparel);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>
    /// Signed angle (-180..180) from the pawn's facing to the attack's origin. Negative is the pawn's left.
    /// False when the origin cannot be known.
    /// </summary>
    public static bool TryGetAttackDelta(Pawn pawn, DamageInfo dinfo, out float delta)
    {
        delta = 0f;
        float angleToAttacker;

        var instigator = dinfo.Instigator;
        if (dinfo.Def.isExplosive)
        {
            angleToAttacker = dinfo.Angle + 180f;
        }
        else if (instigator != null && instigator.SpawnedOrAnyParentSpawned && instigator.MapHeld == pawn.MapHeld)
        {
            var offset = instigator.PositionHeld - pawn.PositionHeld;
            if (offset == IntVec3.Zero)
            {
                return true;
            }

            angleToAttacker = offset.ToVector3().AngleFlat();
        }
        else if (instigator != null)
        {
            angleToAttacker = dinfo.Angle + 180f;
        }
        else
        {
            return false;
        }

        delta = Mathf.DeltaAngle(pawn.Rotation.AsAngle, angleToAttacker);
        return true;
    }

    /// <summary>
    /// Final block chance for one hit, after proficiency, readiness, armour penetration and the shield's cap.
    /// </summary>
    public static float BlockChance(Pawn pawn, Comp_PhysicalShield shield, DamageInfo dinfo, ShieldZone zone, bool weaponArmSide)
    {
        var props = shield.Props;
        var chance = props.BlockChanceFor(zone, weaponArmSide);
        if (chance <= 0f)
        {
            return 0f;
        }

        chance *= dinfo.Def.isRanged || dinfo.Def.isExplosive ? props.rangedFactor : props.meleeFactor;
        chance *= pawn.GetStatValue(Core40kDefOf.BEWH_ShieldProficiency);
        if (!IsReady(pawn))
        {
            chance *= props.undraftedFactor;
        }

        chance -= Mathf.Max(0f, dinfo.ArmorPenetrationInt - props.shieldArmorRating);
        return Mathf.Clamp(chance, 0f, props.maxBlockChance);
    }

    /// <summary>
    /// Called from Pawn.PreApplyDamage after vanilla absorption. Blocks the hit outright, or scales it by the
    /// zone's damage factor.
    /// </summary>
    public static void TryApplyShield(Pawn pawn, ref DamageInfo dinfo, ref bool absorbed)
    {
        var damageDef = dinfo.Def;
        if (damageDef == null || dinfo.IgnoreArmor || damageDef.ignoreShields)
        {
            return;
        }

        if (dinfo.Category == DamageInfo.SourceCategory.Collapse || !damageDef.ExternalViolenceFor(pawn))
        {
            return;
        }

        if (pawn.apparel == null || pawn.apparel.WornApparelCount == 0)
        {
            return;
        }

        var shield = GetWornShield(pawn);
        if (shield == null)
        {
            return;
        }

        shield.Notify_Attacked(pawn);

        var props = shield.Props;
        if (damageDef.isExplosive && !props.blocksExplosions)
        {
            return;
        }

        if (props.neverBlocks != null && props.neverBlocks.Contains(damageDef))
        {
            return;
        }

        if (!IsActive(pawn, shield, out _) || !TryGetAttackDelta(pawn, dinfo, out var delta))
        {
            return;
        }

        var zone = props.ZoneFor(delta);
        var weaponArmSide = props.IsWeaponArmSide(zone, delta);

        if (!shield.OnCooldown)
        {
            var chance = BlockChance(pawn, shield, dinfo, zone, weaponArmSide);
            if (chance > 0f && Rand.Chance(chance))
            {
                absorbed = true;
                shield.Notify_Blocked(pawn, dinfo);
                return;
            }
        }

        var factor = props.DamageFactorFor(zone, weaponArmSide);
        if (factor < 1f)
        {
            dinfo.SetAmount(dinfo.Amount * factor);
        }
    }
}
