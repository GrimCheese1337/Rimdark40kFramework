namespace Core40k;

/// <summary>
/// When the owning creed gains standing under a Shared faith budget, the listed creed loses gain times factor.
/// Directional and same faith only.
/// </summary>
public class DevotionBleed
{
    public DevotionCreedDef creed;

    public float factor = 0f;
}
