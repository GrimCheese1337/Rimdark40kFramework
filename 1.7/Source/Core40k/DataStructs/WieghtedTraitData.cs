using RimWorld;

namespace Core40k;

public class WeightedTraitData
{
    public TraitDef traitDef;

    public int degree = 0;

    public int weight = 100;

    public WeightedTraitData()
    {
    }

    public WeightedTraitData(TraitDef traitDef, int degree, int weight)
    {
        this.traitDef = traitDef;
        this.degree = degree;
        this.weight = weight;
    }
}