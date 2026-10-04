using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Grammar;

namespace Core40k;

/// <summary>
/// One stateless instance per ArtifactKindDef, for behaviour the XML cannot express.
/// </summary>
public class ArtifactKindWorker
{
    public ArtifactKindDef def;

    public virtual bool CanBecome(ThingWithComps weapon)
    {
        return true;
    }

    public virtual void Notify_StageReached(CompArtifactWeapon comp, int stage, Pawn wielder)
    {
    }

    public virtual void Notify_KilledPawn(CompArtifactWeapon comp, Pawn wielder, Pawn victim)
    {
    }

    public virtual void Notify_Stripped(CompArtifactWeapon comp)
    {
    }

    public virtual string GenerateName(CompArtifactWeapon comp, Pawn bearer)
    {
        var adjectives = new List<string>();
        foreach (var trait in comp.Traits)
        {
            if (!trait.traitAdjectives.NullOrEmpty())
            {
                adjectives.AddRange(trait.traitAdjectives);
            }
        }

        var weaponType = def.namerLabels.NullOrEmpty() ? comp.parent.def.label : def.namerLabels.RandomElement();
        var adjective = adjectives.NullOrEmpty() ? def.label : adjectives.RandomElement();

        //A random name from the shuffled bank rather than TaleData_Pawn.GenerateRandom, which generates a whole pawn.
        var bank = PawnNameDatabaseShuffled.BankOf(PawnNameCategory.HumanStandard);
        var gender = Rand.Bool ? Gender.Male : Gender.Female;
        var anyPawnFirst = bank.GetName(PawnNameSlot.First, gender, checkIfAlreadyUsed: false);
        var anyPawnLast = bank.GetName(PawnNameSlot.Last, Gender.None, checkIfAlreadyUsed: false);

        var request = new GrammarRequest();
        request.Rules.Add(new Rule_String("weapon_type", weaponType));
        request.Rules.Add(new Rule_String("trait_adjective", adjective));
        request.Rules.Add(new Rule_String("kind", def.label));
        request.Rules.Add(new Rule_String("ANYPAWN_nameDef", anyPawnFirst));
        request.Rules.Add(new Rule_String("ANYPAWN_nameFull", anyPawnFirst + " " + anyPawnLast));
        request.Rules.Add(new Rule_String("ANYPAWN_nameLast", anyPawnLast));
        if (bearer != null)
        {
            foreach (var rule in GrammarUtility.RulesForPawn("BEARER", bearer, request.Constants))
            {
                request.Rules.Add(rule);
            }
        }
        request.Includes.Add(def.nameRulePack ?? Core40kDefOf.BEWH_NamerArtifactWeapon);

        var name = NameGenerator.GenerateName(request, null, appendNumberIfNameUsed: false, "r_artifact_name");
        return name.NullOrEmpty() ? null : name.StripTags();
    }
}
