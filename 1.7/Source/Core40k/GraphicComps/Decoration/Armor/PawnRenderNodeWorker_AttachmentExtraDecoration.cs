using UnityEngine;
using Verse;

namespace Core40k;

public class PawnRenderNodeWorker_AttachmentExtraDecoration : PawnRenderNodeWorker
{
    public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
    {
        var offset = base.OffsetFor(node, parms, out pivot);
        
        if (node is not PawnRenderNode_AttachmentExtraDecoration pawnRenderNode)
        {
            return offset;
        }

        offset += pawnRenderNode.decorativeComp.GetAdditionalOffsetForRot(parms.facing, pawnRenderNode.decorationDef);
        
        return offset;
    }
    
    public override float LayerFor(PawnRenderNode node, PawnDrawParms parms)
    {
        var layer = base.LayerFor(node, parms);

        if (node is not PawnRenderNode_AttachmentExtraDecoration pawnRenderNode)
        {
            return layer;
        }
        
        layer += pawnRenderNode.decorativeComp.GetAdditionalLayerForRot(parms.facing, pawnRenderNode.decorationDef);
        
        return layer;
    }
    
    public override Vector3 ScaleFor(PawnRenderNode node, PawnDrawParms parms)
    {
        var scale = base.ScaleFor(node, parms);
        
        if (node is not PawnRenderNode_AttachmentExtraDecoration pawnRenderNode)
        {
            return scale;
        }
        
        var additionalScale = pawnRenderNode.decorativeComp.GetAdditionalScaleForRot(parms.facing, pawnRenderNode.decorationDef);

        scale.x *= additionalScale.x;
        scale.z *= additionalScale.z;
        
        return scale;
    }
}