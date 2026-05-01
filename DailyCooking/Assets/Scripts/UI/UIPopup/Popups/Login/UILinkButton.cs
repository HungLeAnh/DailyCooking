using System;
using UnityEngine;
using UnityEngine.UI;

public class UILinkButton : MonoBehaviour
{
    [SerializeField] private Button linkButton;
    [SerializeField] private Image linkButtonImage;
    [SerializeField] private Sprite linkedSprite;
    [SerializeField] private Sprite unlinkedSprite;
    private AuthenticationType authenticationType;
    public void SetLinkedState(AuthenticationType type, Action linkAction, Action unlinkAction = null)
    {
        this.authenticationType = type;
        linkButtonImage.sprite = SessionManager.Instance.HasIDOfType(authenticationType) ? linkedSprite : unlinkedSprite;
        linkButton.onClick.AddListener(() => {
            if (SessionManager.Instance.HasIDOfType(authenticationType))
            {
                unlinkAction?.Invoke();
            }
            else
            {
                linkAction.Invoke();
            }
        });

    }
    public void OnLinkOrUnlink()
    {
        linkButtonImage.sprite = SessionManager.Instance.HasIDOfType(authenticationType) ? linkedSprite : unlinkedSprite;
    }
}