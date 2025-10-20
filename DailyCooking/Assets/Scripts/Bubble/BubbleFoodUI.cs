using UnityEngine;
using UnityEngine.UI;

public class BubbleFoodUI: MonoBehaviour
{
    [SerializeField] private GameObject dishPrefab;
    [SerializeField] private Transform dishContainerTransform;

    private void Awake()
    {
        dishPrefab.SetActive(false);
    }

    public void SetOrder(Sprite[] foodSprites)
    {
        foreach (Transform child in dishContainerTransform)
        {
            Destroy(child.gameObject);
        }
        foreach (var foodSprite in foodSprites)
        {
            GameObject dishGO = Instantiate(dishPrefab, dishContainerTransform);
            dishGO.SetActive(true);
            BubbleFoodItemUI dishUI = dishGO.GetComponent<BubbleFoodItemUI>();
            dishUI.SetFood(foodSprite);
        }
    }
}
