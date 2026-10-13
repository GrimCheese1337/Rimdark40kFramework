using Verse;

namespace Core40k;

public class DefModExtension_HolyDamageExtension : DefModExtension
{
    public int chanceToIgnite = 20;
    public int minHitAmount = 2;
    public int maxHitAmount = 5;
    public float healPercentOfDamageToAllies = 0.5f;
}