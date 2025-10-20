using UnityEngine;
using UnityEngine.UI;

public class BubbleFoodItemUI : MonoBehaviour
{
    [SerializeField] private Image imageDish;
    public void SetFood(Sprite foodSprite)
    {
        imageDish.sprite = foodSprite;
    }
}