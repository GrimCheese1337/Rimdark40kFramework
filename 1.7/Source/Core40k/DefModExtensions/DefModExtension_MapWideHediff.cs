using Verse;

namespace Core40k;

public class DefModExtension_MapWideHediff : DefModExtension
{
    public bool affectPlayerColonists = false;
    public bool affectEnemies = false;
    public bool affectCaster = false;

    public HediffDef hediffDef = null;
}