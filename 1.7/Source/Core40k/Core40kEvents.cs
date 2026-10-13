using System;
using Verse;

namespace Core40k;

/// <summary>
/// Game events the framework re-broadcasts so several systems can share one Harmony patch.
/// </summary>
public static class Core40kEvents
{
    //(killed, killer). Raised from RecordsUtility.Notify_PawnKilled, so it fires exactly when vanilla credits a kill.
    public static event Action<Pawn, Pawn> PawnKilled;

    public static void Notify_PawnKilled(Pawn killed, Pawn killer)
    {
        PawnKilled?.Invoke(killed, killer);
    }
}
