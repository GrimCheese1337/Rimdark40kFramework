using RimWorld;
using Verse;

namespace Core40k;

/// <summary>
/// One stateless instance per ArtifactTraitDef. Per-weapon state belongs in the comp's state bag.
/// </summary>
public class ArtifactTraitWorker
{
    public ArtifactTraitDef def;

    /// <summary>
    /// Whether the trait contributes anything right now. A null wielder means the weapon is not held; a trait with
    /// wielder requirements is dormant then, since there is nobody to judge.
    /// </summary>
    public virtual bool IsActiveFor(CompArtifactWeapon comp, Pawn wielder)
    {
        if (RequirementData.IsNullOrEmpty(def.requirements))
        {
            return true;
        }
        return wielder != null && def.requirements.Meets(wielder);
    }

    public virtual void Notify_Equipped(CompArtifactWeapon comp, Pawn wielder)
    {
        if (wielder == null)
        {
            return;
        }

        if (!def.equippedHediffs.NullOrEmpty() && wielder.health != null)
        {
            foreach (var hediffDef in def.equippedHediffs)
            {
                if (wielder.health.hediffSet.GetFirstHediffOfDef(hediffDef) == null)
                {
                    wielder.health.AddHediff(hediffDef, wielder.health.hediffSet.GetBrain());
                }
            }
        }

        wielder.AddAbilities(def.givesAbilities, def.givesVFEAbilities);
    }

    public virtual void Notify_Unequipped(CompArtifactWeapon comp, Pawn wielder)
    {
        if (wielder == null)
        {
            return;
        }

        if (!def.equippedHediffs.NullOrEmpty() && wielder.health?.hediffSet != null)
        {
            foreach (var hediffDef in def.equippedHediffs)
            {
                var hediff = wielder.health.hediffSet.GetFirstHediffOfDef(hediffDef);
                if (hediff != null)
                {
                    wielder.health.RemoveHediff(hediff);
                }
            }
        }

        wielder.RemoveAbilities(def.givesAbilities, def.givesVFEAbilities);
    }

    public virtual void Notify_KilledPawn(CompArtifactWeapon comp, Pawn wielder, Pawn victim)
    {
        if (def.killThought != null && wielder?.needs?.mood != null)
        {
            var thought = ThoughtMaker.MakeThought(def.killThought);
            if (thought is Thought_WeaponTrait weaponTraitThought)
            {
                weaponTraitThought.weapon = comp.parent;
            }
            if (thought is Thought_Memory memory)
            {
                wielder.needs.mood.thoughts.memories.TryGainMemory(memory);
            }
        }

        if (def.devotionOnKill?.creed != null)
        {
            DevotionUtils.AddStanding(wielder, def.devotionOnKill.creed, def.devotionOnKill.amount);
        }
    }

    public virtual void Notify_Gained(CompArtifactWeapon comp, int stage)
    {
    }

    public virtual void Notify_Lost(CompArtifactWeapon comp)
    {
    }

    public virtual int TickInterval => 0;

    public virtual void Tick(CompArtifactWeapon comp, Pawn wielder)
    {
    }

    public virtual string ExtraDescription(CompArtifactWeapon comp)
    {
        return null;
    }
}
