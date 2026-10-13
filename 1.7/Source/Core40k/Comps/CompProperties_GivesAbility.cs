using RimWorld;
using Verse;

namespace Core40k
{
    public class CompProperties_GivesAbility : CompProperties
    {
        public AbilityDef ability;

        public CompProperties_GivesAbility()
        {
            compClass = typeof(Comp_GivesAbility);
        }
    }
}