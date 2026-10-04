using System.Collections.Generic;
using Verse;

namespace Core40k;

public class CompProperties_ArtifactWeapon : CompProperties
{
    //The kinds this weapon def may become. MakeArtifact refuses any other kind.
    public List<ArtifactKindDef> kinds = [];

    public CompProperties_ArtifactWeapon()
    {
        compClass = typeof(CompArtifactWeapon);
    }

    public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
    {
        foreach (var configError in base.ConfigErrors(parentDef))
        {
            yield return configError;
        }

        if (!parentDef.IsWeapon)
        {
            yield return "CompProperties_ArtifactWeapon on " + parentDef.defName + " which is not a weapon";
        }
    }
}
