using Verse;

namespace Core40k;

public class HediffCompProperties_RemoveMentalStateOnHediffEnd : HediffCompProperties
{
    public MentalStateDef specificMentalState;

    public HediffCompProperties_RemoveMentalStateOnHediffEnd()
    {
        compClass = typeof(Hediff_RemoveMentalStateOnHediffEnd);
    }
}