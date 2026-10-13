using System.Xml;
using RimWorld;
using Verse;

namespace Core40k;

public class TraitData
{
    public TraitDef traitDef;

    public int degree = 0;

    public TraitData()
    {
    }

    public TraitData(TraitDef traitDef, int degree)
    {
        this.traitDef = traitDef;
        this.degree = degree;
    }

    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "traitDef", xmlRoot.Name, null, null, typeof(TraitDef));
        if (xmlRoot.FirstChild != null)
        {
            degree = ParseHelper.FromString<int>(xmlRoot.FirstChild.Value);
        }
    }
}