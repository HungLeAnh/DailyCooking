using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Draws 1-3 small stars above nearby food that went through a gesture step, and above plates by
// their dish stars. One screen-space overlay with pooled icons instead of a widget on every food prefab.
public class QualityStarsOverlay : MonoBehaviour
{
    private const int MaxStars = 3;

    [SerializeField] private Sprite filledStar;
    [SerializeField] private Sprite emptyStar;
    [SerializeField] private float maxDistance = 8f;
    [SerializeField] private float heightOffset = 0.35f;
    [SerializeField] private float starSize = 34f;

    private readonly List<Image[]> pool = new List<Image[]>();

    private void LateUpdate()
    {
        Camera camera = Camera.main;
        int used = 0;
        if (camera != null)
        {
            Vector3 cameraPosition = camera.transform.position;
            foreach (KitchenObject food in KitchenObject.Spawned)
            {
                if (food == null)
                    continue;
                Vector3 world = food.transform.position + Vector3.up * heightOffset;
                if ((world - cameraPosition).sqrMagnitude > maxDistance * maxDistance)
                    continue;
                int stars = food is TablewareKitchenObject plate ? plate.DishStars : food.Quality.Stars;
                if (stars <= 0)
                    continue;
                Vector3 screen = camera.WorldToScreenPoint(world);
                if (screen.z <= 0f)
                    continue;
                Show(Get(used++), screen, stars);
            }
        }
        for (int i = used; i < pool.Count; i++)
            SetActive(pool[i], false);
    }

    private Image[] Get(int index)
    {
        while (pool.Count <= index)
        {
            var icons = new Image[MaxStars];
            for (int i = 0; i < MaxStars; i++)
            {
                var go = new GameObject("QualityStar", typeof(RectTransform), typeof(Image));
                go.layer = gameObject.layer;
                var rect = (RectTransform)go.transform;
                rect.SetParent(transform, false);
                rect.sizeDelta = new Vector2(starSize, starSize);
                icons[i] = go.GetComponent<Image>();
                icons[i].raycastTarget = false;
                icons[i].preserveAspect = true;
            }
            pool.Add(icons);
        }
        return pool[index];
    }

    private void Show(Image[] icons, Vector3 screen, int stars)
    {
        for (int i = 0; i < MaxStars; i++)
        {
            Image icon = icons[i];
            icon.gameObject.SetActive(true);
            icon.sprite = i < stars ? filledStar : emptyStar;
            icon.rectTransform.position = new Vector3(screen.x + (i - 1) * starSize, screen.y, 0f);
        }
    }

    private static void SetActive(Image[] icons, bool active)
    {
        foreach (Image icon in icons)
            if (icon.gameObject.activeSelf != active)
                icon.gameObject.SetActive(active);
    }
}
