using Coffee.UIExtensions;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public enum TutorialPanelPosition
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
    Center
}
[System.Serializable]
public class TutorialStep
{
    public int id;
    public string content;
    public TutorialPanelPosition panelPosition;
    public Vector2 panelOffset;
    public RectTransform elementToHighlight;
    public bool isImportant;

}
public class TutorialPanel : MonoBehaviour
{
    public event EventHandler OnTutorialClosed;

    [SerializeField] private TutorialType panelType;
    [SerializeField] private TextMeshProUGUI contentText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Unmask highlightMask;

    [SerializeField] private RectTransform panelRectTransform;

    [Header("Tutorial step")]
    [SerializeField] private List<TutorialStep> tutorialSteps = new List<TutorialStep>();

    private int currentStepIndex = -1;

    private void Awake()
    {
        // Set up button listeners
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextButtonClicked);

        if (previousButton != null)
            previousButton.onClick.AddListener(OnPreviousButtonClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseButtonClicked);
    }

    public void HighlightElement(RectTransform elementToHighlight)
    {
        // Create highlight around the specified element
        if (highlightMask != null && elementToHighlight != null)
        {
            highlightMask.fitTarget = elementToHighlight;
        }
    }

    public void SetPosition(TutorialPanelPosition position, Vector2 offset)
    {
        // Position the panel based on the specified position
        switch (position)
        {
            case TutorialPanelPosition.TopLeft:
                panelRectTransform.anchorMin = new Vector2(0, 1);
                panelRectTransform.anchorMax = new Vector2(0, 1);
                panelRectTransform.pivot = new Vector2(0, 1);
                break;
            case TutorialPanelPosition.TopRight:
                panelRectTransform.anchorMin = new Vector2(1, 1);
                panelRectTransform.anchorMax = new Vector2(1, 1);
                panelRectTransform.pivot = new Vector2(1, 1);
                break;
            case TutorialPanelPosition.BottomLeft:
                panelRectTransform.anchorMin = new Vector2(0, 0);
                panelRectTransform.anchorMax = new Vector2(1, 0);
                panelRectTransform.pivot = new Vector2(0.5f, 0);
                break;
            case TutorialPanelPosition.BottomRight:
                panelRectTransform.anchorMin = new Vector2(1, 0);
                panelRectTransform.anchorMax = new Vector2(1, 0);
                panelRectTransform.pivot = new Vector2(1, 0);
                break;
            case TutorialPanelPosition.Center:
                panelRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                panelRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                panelRectTransform.pivot = new Vector2(0.5f, 0.5f);
                break;
        }

        panelRectTransform.anchoredPosition = offset;
    }
    private void ShowCurrentStep()
    {
        TutorialStep step = tutorialSteps[currentStepIndex];
        // Set panel content
        SetContent(step.content);

        // Position the panel
        SetPosition(step.panelPosition, step.panelOffset);

        // Highlight element if specified
        if (step.elementToHighlight != null)
        {
            HighlightElement(step.elementToHighlight);
        }
    }
    public void SetContent(string content)
    {
        if (contentText != null)
            contentText.text = content;
    }

    public void NextStep()
    {

        currentStepIndex++;

        if (currentStepIndex < tutorialSteps.Count)
        {
            ShowCurrentStep();
        }
        else
        {
            CloseTutorial();
        }
    }

    public void PreviousStep()
    {
        currentStepIndex--;

        if (currentStepIndex >= 0)
        {
            ShowCurrentStep();
        }
        else
        {
            currentStepIndex = 0;
            ShowCurrentStep();
        }
    }
    public void StartTutorial()
    {
        gameObject.SetActive(true);
        currentStepIndex = -1;

        NextStep();
    }

    public void CloseTutorial()
    {
        gameObject.SetActive(false);
        currentStepIndex = -1;
        OnTutorialClosed?.Invoke(this, EventArgs.Empty);

    }
    private void OnNextButtonClicked()
    {
        NextStep();
    }

    private void OnPreviousButtonClicked()
    {
        PreviousStep();
    }

    private void OnCloseButtonClicked()
    {
        CloseTutorial();
    }

    public TutorialType GetPanelType()
    {
        return panelType;
    }
}
