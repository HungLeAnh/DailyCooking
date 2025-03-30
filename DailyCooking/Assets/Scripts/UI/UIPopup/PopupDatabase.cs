using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "PopupSystem/PopupDatabase", fileName = "PopupDatabase")]
public class PopupDatabase : ScriptableObject
{
    [SerializeField] private List<PopupData> popups;

    public List<PopupData> Popups { get => popups; set => popups = value; }

    public PopupData GetPopup(string popupName)
    {
        foreach (var popup in Popups)
        {
            if (popup.popupName == popupName)
            {
                return popup;
            }
        }
        return null;
    }

}
