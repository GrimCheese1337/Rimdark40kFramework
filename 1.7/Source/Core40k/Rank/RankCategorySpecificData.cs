using System.Collections.Generic;
using UnityEngine;

namespace Core40k;

public class RankCategorySpecificData
{
    public RankDef rankDef;
    public Vector2 displayPosition;
    public List<RankData> rankRequirements = [];
    public List<RankData> rankRequirementsOneAmong = [];
}