public interface IUIPopupManager
{
    void HidePopup(UIPopupType popupType, object param = null);
    void ShowPopup(UIPopupType popupType, object param = null);
}
