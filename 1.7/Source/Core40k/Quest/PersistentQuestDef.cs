using System.Collections.Generic;
using Verse;

namespace Core40k;

/// <summary>
/// Registers a persistent quest for every storyteller, so the mod declaring it does not have to
/// patch storyteller defs and custom storytellers are covered as well.
/// </summary>
public class PersistentQuestDef : Def
{
    public StorytellerCompProperties_PersistentQuest quest;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in base.ConfigErrors())
        {
            yield return error;
        }

        if (quest == null)
        {
            yield return "PersistentQuestDef has no quest set.";
            yield break;
        }

        foreach (var error in quest.ConfigErrors(null))
        {
            yield return error;
        }
    }
}
