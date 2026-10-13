using HarmonyLib;
using RimWorld;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(RecordsUtility), nameof(RecordsUtility.Notify_PawnKilled))]
public static class PawnKilledEventPatch
{
    public static void Postfix(Pawn killed, Pawn killer)
    {
        if (killed == null || killer == null)
        {
            return;
        }

        Core40kEvents.Notify_PawnKilled(killed, killer);
    }
}
