using RimWorld;
using Verse;

namespace Core40k;

/// <summary>
/// Adds every PersistentQuestDef's storyteller comp to each storyteller that does not already
/// carry one for the same incident.
/// </summary>
[StaticConstructorOnStartup]
public static class PersistentQuestInjector
{
    static PersistentQuestInjector()
    {
        var quests = DefDatabase<PersistentQuestDef>.AllDefsListForReading;
        var storytellers = DefDatabase<StorytellerDef>.AllDefsListForReading;

        for (var i = 0; i < quests.Count; i++)
        {
            var questProps = quests[i].quest;
            if (questProps?.incident == null)
            {
                continue;
            }

            for (var j = 0; j < storytellers.Count; j++)
            {
                var storyteller = storytellers[j];
                storyteller.comps ??= [];

                if (!Carries(storyteller, questProps.incident))
                {
                    storyteller.comps.Add(questProps);
                }
            }
        }
    }

    private static bool Carries(StorytellerDef storyteller, IncidentDef incident)
    {
        var comps = storyteller.comps;

        for (var i = 0; i < comps.Count; i++)
        {
            if (comps[i] is StorytellerCompProperties_PersistentQuest existing && existing.incident == incident)
            {
                return true;
            }
        }

        return false;
    }
}
