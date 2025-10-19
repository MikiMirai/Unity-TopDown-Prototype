using UnityEngine;

[ExecuteAlways, ImageEffectAllowedInSceneView]
public class CameraMSAA : MonoBehaviour
{
#if UNITY_EDITOR
    [ImageEffectUsesCommandBuffer]
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination);
    }
#endif
}
