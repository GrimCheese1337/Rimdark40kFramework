using Verse;

namespace Core40k;

public enum ArtifactKillCategory
{
    Humanlike,
    Animal,
    Mechanoid,
    Entity,
    Other,
}

public class ArtifactKillRecord : IExposable
{
    public int total;
    public int humanlike;
    public int animal;
    public int mechanoid;
    public int entity;
    public int other;

    /// <summary>
    /// Sorts a victim the same way RecordsUtility.Notify_PawnKilled does for the vanilla kill records.
    /// </summary>
    public static ArtifactKillCategory CategoryOf(Pawn victim)
    {
        var raceProps = victim?.RaceProps;
        if (raceProps == null)
        {
            return ArtifactKillCategory.Other;
        }
        if (ModsConfig.AnomalyActive && (victim.IsSubhuman || raceProps.IsAnomalyEntity))
        {
            return ArtifactKillCategory.Entity;
        }
        if (raceProps.Humanlike)
        {
            return ArtifactKillCategory.Humanlike;
        }
        if (raceProps.IsMechanoid)
        {
            return ArtifactKillCategory.Mechanoid;
        }
        if (raceProps.Animal)
        {
            return ArtifactKillCategory.Animal;
        }
        return ArtifactKillCategory.Other;
    }

    public void Record(Pawn victim)
    {
        total++;
        switch (CategoryOf(victim))
        {
            case ArtifactKillCategory.Humanlike:
                humanlike++;
                break;
            case ArtifactKillCategory.Animal:
                animal++;
                break;
            case ArtifactKillCategory.Mechanoid:
                mechanoid++;
                break;
            case ArtifactKillCategory.Entity:
                entity++;
                break;
            default:
                other++;
                break;
        }
    }

    public int CountOf(ArtifactKillCategory category)
    {
        return category switch
        {
            ArtifactKillCategory.Humanlike => humanlike,
            ArtifactKillCategory.Animal => animal,
            ArtifactKillCategory.Mechanoid => mechanoid,
            ArtifactKillCategory.Entity => entity,
            _ => other,
        };
    }

    public static string LabelOf(ArtifactKillCategory category)
    {
        return ("BEWH.Framework.Artifact.KillCategory." + category).Translate();
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref total, "total", 0);
        Scribe_Values.Look(ref humanlike, "humanlike", 0);
        Scribe_Values.Look(ref animal, "animal", 0);
        Scribe_Values.Look(ref mechanoid, "mechanoid", 0);
        Scribe_Values.Look(ref entity, "entity", 0);
        Scribe_Values.Look(ref other, "other", 0);
    }
}
