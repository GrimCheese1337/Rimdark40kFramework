using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Core40k;

public class Comp_PhysicalShield : ThingComp
{
    private int lastBlockTick = -99999;

    private int lastAttackedTick = -99999;

    public bool Visible;

    public CompProperties_PhysicalShield Props => (CompProperties_PhysicalShield)props;

    public Pawn Wearer => (parent as Apparel)?.Wearer;

    public bool OnCooldown => Find.TickManager.TicksGame - lastBlockTick < Props.blockCooldownTicks;

    public bool AttackedRecently => Find.TickManager.TicksGame - lastAttackedTick < ShieldUtils.AttackedVisibleTicks;

    /// <summary>
    /// Records that the wearer was attacked, so the shield is drawn when shields are only shown in combat.
    /// </summary>
    public void Notify_Attacked(Pawn pawn)
    {
        lastAttackedTick = Find.TickManager.TicksGame;
        UpdateVisibility(pawn);
    }

    /// <summary>
    /// Re-evaluates whether the shield should be drawn and refreshes the wearer's graphics when that changes.
    /// </summary>
    public void UpdateVisibility(Pawn pawn)
    {
        var visible = ShieldUtils.ShouldShowShield(pawn, this);
        if (visible == Visible)
        {
            return;
        }

        Visible = visible;
        ShieldUtils.RefreshShieldGraphic(pawn, parent as Apparel);
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref lastBlockTick, "lastBlockTick", -99999);
    }

    /// <summary>
    /// Records a successful block: cooldown, melee training, feedback and wear on the shield.
    /// </summary>
    public void Notify_Blocked(Pawn pawn, DamageInfo dinfo)
    {
        lastBlockTick = Find.TickManager.TicksGame;

        if (Props.blockSkillXp > 0f)
        {
            pawn.skills?.Learn(SkillDefOf.Melee, Props.blockSkillXp);
        }

        PlayBlockEffects(pawn, dinfo);

        if (!parent.def.useHitPoints)
        {
            return;
        }

        var loss = Mathf.Max(Props.minHitPointLoss, GenMath.RoundRandom(dinfo.Amount * Props.hitPointLossFactor));
        if (loss > 0)
        {
            parent.TakeDamage(new DamageInfo(dinfo.Def, loss));
        }
    }

    private void PlayBlockEffects(Pawn pawn, DamageInfo dinfo)
    {
        if (!pawn.Spawned)
        {
            return;
        }

        var map = pawn.Map;
        var effecterDef = Props.blockEffecter ?? DefaultBlockEffecter(dinfo);
        if (effecterDef != null)
        {
            var health = pawn.health;
            if (health.deflectionEffecter == null || health.deflectionEffecter.def != effecterDef)
            {
                health.deflectionEffecter?.Cleanup();
                health.deflectionEffecter = effecterDef.Spawn();
            }

            var target = new TargetInfo(pawn.Position, map);
            health.deflectionEffecter.Trigger(target, dinfo.Instigator != null ? (TargetInfo)dinfo.Instigator : target);
        }

        pawn.Drawer.Notify_DamageDeflected(dinfo);

        if (Props.showBlockText)
        {
            MoteMaker.ThrowText(pawn.DrawPos, map, "BEWH.Framework.Shield.Blocked".Translate());
        }
    }

    private EffecterDef DefaultBlockEffecter(DamageInfo dinfo)
    {
        var bullet = dinfo.Def == DamageDefOf.Bullet;
        if (parent.def.apparel?.useDeflectMetalEffect == true && dinfo.Def.canUseDeflectMetalEffect)
        {
            return bullet ? EffecterDefOf.Deflect_Metal_Bullet : EffecterDefOf.Deflect_Metal;
        }

        return bullet ? EffecterDefOf.Deflect_General_Bullet : EffecterDefOf.Deflect_General;
    }

    public override void CompDrawWornExtras()
    {
        base.CompDrawWornExtras();

        var pawn = Wearer;
        if (pawn != null)
        {
            UpdateVisibility(pawn);
        }

        if (!Prefs.DevMode || !DebugSettings.godMode)
        {
            return;
        }

        if (pawn is not { Spawned: true } || !Find.Selector.IsSelected(pawn))
        {
            return;
        }

        var center = pawn.DrawPos;
        center.y = AltitudeLayer.MetaOverlays.AltitudeFor();
        var facing = pawn.Rotation.AsAngle;
        var frontHalf = Props.frontArcDegrees / 2f;
        var backEdge = 180f - Props.backArcDegrees / 2f;

        DrawZoneEdge(center, facing - frontHalf, SimpleColor.Green);
        DrawZoneEdge(center, facing + frontHalf, SimpleColor.Green);
        DrawZoneEdge(center, facing - backEdge, SimpleColor.Red);
        DrawZoneEdge(center, facing + backEdge, SimpleColor.Red);
        DrawZoneEdge(center, facing + (Props.shieldOnLeft ? -90f : 90f), SimpleColor.Cyan);
    }

    private static void DrawZoneEdge(Vector3 center, float angle, SimpleColor color)
    {
        GenDraw.DrawLineBetween(center, center + Vector3Utility.HorizontalVectorFromAngle(angle) * 1.5f, color, 0.05f);
    }

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
    {
        foreach (var entry in base.SpecialDisplayStats())
        {
            yield return entry;
        }

        var category = Core40kDefOf.BEWH_Shields;
        var p = Props;
        var priority = 5000;

        yield return Entry(category, "FrontBlock", p.frontBlockChance.ToStringPercent(), priority--);
        yield return Entry(category, "SideBlock", p.sideBlockChance.ToStringPercent(), priority--);
        yield return Entry(category, "BackBlock", p.backBlockChance.ToStringPercent(), priority--);

        if (p.frontDamageFactor < 1f || p.sideDamageFactor < 1f || p.backDamageFactor < 1f)
        {
            yield return Entry(category, "FrontReduction", (1f - p.frontDamageFactor).ToStringPercent(), priority--);
            yield return Entry(category, "SideReduction", (1f - p.sideDamageFactor).ToStringPercent(), priority--);
            yield return Entry(category, "BackReduction", (1f - p.backDamageFactor).ToStringPercent(), priority--);
        }
        else
        {
            priority -= 3;
        }

        yield return Entry(category, "FrontArc", p.frontArcDegrees.ToString("F0") + "°", priority--);
        yield return Entry(category, "BackArc", p.backArcDegrees.ToString("F0") + "°", priority--);

        if (p.offSideFactor != 1f)
        {
            yield return Entry(category, "OffSide", "x" + p.offSideFactor.ToStringPercent(), priority--);
        }

        if (p.meleeFactor != 1f)
        {
            yield return Entry(category, "Melee", "x" + p.meleeFactor.ToStringPercent(), priority--);
        }

        if (p.rangedFactor != 1f)
        {
            yield return Entry(category, "Ranged", "x" + p.rangedFactor.ToStringPercent(), priority--);
        }

        yield return Entry(category, "ArmorRating", p.shieldArmorRating.ToStringPercent(), priority--);
        yield return Entry(category, "MaxBlock", p.maxBlockChance.ToStringPercent(), priority--);
        yield return Entry(category, "Explosions", p.blocksExplosions ? "Yes".Translate() : "No".Translate(), priority);
    }

    private static StatDrawEntry Entry(StatCategoryDef category, string key, string value, int priority)
    {
        return new StatDrawEntry(category,
            ("BEWH.Framework.Shield.Stat." + key).Translate(),
            value,
            ("BEWH.Framework.Shield.Stat." + key + "Desc").Translate(),
            priority);
    }
}
