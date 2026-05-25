using UnityEngine;
using UnityEngine.EventSystems;

public class UICharacterPreview : MonoBehaviour, IDragHandler
{
    [SerializeField] private GameObject characterObject;
    public void RotateCharacter(float deltaX)
    {
        characterObject.transform.eulerAngles += new Vector3(0, -deltaX, 0);
    }   
    public void OnDrag(PointerEventData eventData)
    {
        RotateCharacter(-eventData.delta.x);
    }
}