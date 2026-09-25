using UnityEngine;

// Toggles the outline ("_IsActive") on the last material slot of a set of renderers.
// Reading Renderer.materials copies every material each call, and highlights toggle every
// frame, so the per-renderer copies are made once, reused, and destroyed with the owner.
public class HighlightMaterials
{
    private static readonly int IsActiveId = Shader.PropertyToID("_IsActive");

    private readonly Renderer[] renderers;
    private Material[] highlightMaterials;
    private bool? isActive;

    public HighlightMaterials(Renderer[] renderers)
    {
        this.renderers = renderers;
    }

    public void SetActive(bool active)
    {
        if (isActive == active)
            return;
        isActive = active;
        if (highlightMaterials == null)
            CacheMaterials();

        float value = active ? 1f : 0f;
        foreach (Material material in highlightMaterials)
        {
            if (material != null)
                material.SetFloat(IsActiveId, value);
        }
    }

    // Call from the owner's OnDestroy: the copies made by Renderer.materials are not freed otherwise.
    public void Release()
    {
        if (highlightMaterials == null)
            return;
        if (renderers != null)
        {
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null)
                        Object.Destroy(material);
                }
            }
        }
        highlightMaterials = null;
    }

    private void CacheMaterials()
    {
        int count = renderers != null ? renderers.Length : 0;
        highlightMaterials = new Material[count];
        for (int i = 0; i < count; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer.sharedMaterials == null || renderer.sharedMaterials.Length == 0)
                continue;
            Material[] materials = renderer.materials;
            highlightMaterials[i] = materials[materials.Length - 1];
        }
    }
}
