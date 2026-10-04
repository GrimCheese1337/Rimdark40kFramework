using System.Collections.Generic;
using Verse;

namespace Core40k;

public class ArtifactStage
{
    public List<ArtifactCondition> conditions = [];
    public IntRange traitCount = new(1, 1);
    public IntRange drawbackCount = new(0, 0);

    [MustTranslate]
    public string letterLabel;

    [MustTranslate]
    public string letterText;

    public LetterDef letterDef;

    public bool HasConditions => !conditions.NullOrEmpty();

    public bool IsMet(CompArtifactWeapon comp, Pawn wielder)
    {
        if (conditions.NullOrEmpty())
        {
            return false;
        }

        foreach (var condition in conditions)
        {
            if (condition == null || !condition.IsMet(comp, wielder))
            {
                return false;
            }
        }

        return true;
    }
}
