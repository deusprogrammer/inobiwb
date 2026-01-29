using UnityEngine;

[CreateAssetMenu(fileName = "ClutterBlockFadingState", menuName = "States/ClutterBlock Fading State")]
public class ClutterBlockFadingState : ClutterBlockState
{
    private float fadeProgress = 0f;
    private Vector3 originalScale;

    public override void OnEvent(string eventName, GameObjectStateController controller)
    {
        // Handle events if needed
    }

    public override void EnterState(GameObjectStateController controller)
    {
        ClutterBlockStateController blockController = (ClutterBlockStateController)controller;
        fadeProgress = 0f;
        originalScale = blockController.transform.localScale;
        
        // Try to set up material for transparency
        Renderer renderer = blockController.GetComponent<Renderer>();
        if (renderer != null && renderer.material.HasProperty("_Color"))
        {
            // Make sure we're using a material instance
            renderer.material = new Material(renderer.material);
            
            // Try to enable transparency on Standard shader
            if (renderer.material.shader.name == "Standard")
            {
                renderer.material.SetFloat("_Mode", 3);
                renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                renderer.material.SetInt("_ZWrite", 0);
                renderer.material.DisableKeyword("_ALPHATEST_ON");
                renderer.material.EnableKeyword("_ALPHABLEND_ON");
                renderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                renderer.material.renderQueue = 3000;
            }
        }
    }

    public override void Tick(float deltaTime, GameObjectStateController controller)
    {
        ClutterBlockStateController blockController = (ClutterBlockStateController)controller;
        
        // Fade out the block
        fadeProgress += deltaTime * 2f; // Fade speed
        float alpha = Mathf.Lerp(1f, 0f, fadeProgress);
        
        // Fade alpha if material supports it
        Renderer renderer = blockController.GetComponent<Renderer>();
        if (renderer != null && renderer.material.HasProperty("_Color"))
        {
            Color color = renderer.material.color;
            color.a = alpha;
            renderer.material.color = color;
        }
        
        // Also shrink the object for visual feedback
        blockController.transform.localScale = originalScale * alpha;
        
        if (fadeProgress >= 1f)
        {
            // Fade complete, destroy the object
            Object.Destroy(blockController.gameObject);
        }
    }
}
