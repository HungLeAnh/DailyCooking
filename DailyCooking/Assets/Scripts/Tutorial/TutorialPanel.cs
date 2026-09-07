using Coffee.UIExtensions;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Video;


[System.Serializable]
public class TutorialStep
{
    [SerializeField] private int id;
    [SerializeField] private string content;
    [SerializeField] private RectTransform elementToHighlight;
    [SerializeField] private bool isImportant;
    [SerializeField] private bool isHidePanel;
    [SerializeField] private UnityEvent onStepStart;
    [SerializeField] private VideoClip clip;

    public int Id { get => id; set => id = value; }
    public string Content { get => content; set => content = value; }
    public RectTransform ElementToHighlight { get => elementToHighlight; set => elementToHighlight = value; }
    public bool IsImportant { get => isImportant; set => isImportant = value; }
    public bool IsHidePanel { get => isHidePanel; set => isHidePanel = value; }
    public UnityEvent OnStepStart { get => onStepStart; set => onStepStart = value; }
    public VideoClip Clip { get => clip; set => clip = value; }
}
public class TutorialPanel : MonoBehaviour
{
    public event EventHandler OnTutorialClosed;

    [SerializeField] protected Canvas parentCanvas;
    [SerializeField] protected GameObject panelObject;
    [SerializeField] protected TutorialType panelType;
    [SerializeField] protected TextMeshProUGUI contentText;
    [SerializeField] protected Button nextButton;
    [SerializeField] protected Button previousButton;
    [SerializeField] protected Button closeButton;

    [SerializeField] protected Unmask highlightMask;
    [SerializeField] protected GameObject highlightObject;
    [SerializeField] protected Button hightLightButton;

    [SerializeField] protected RectTransform panelRectTransform;

    [Header("Tutorial step")]
    [SerializeField] protected List<TutorialStep> tutorialSteps = new List<TutorialStep>();

    protected int currentStepIndex = -1;

    protected virtual void Awake()
    {
        // Set up button listeners
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }

        if (previousButton != null)
            previousButton.onClick.AddListener(OnPreviousButtonClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseButtonClicked);

        highlightObject.SetActive(false);
    }

    public void HighlightElement(RectTransform elementToHighlight,bool isActive = false)
    {
        // Create highlight around the specified element
        highlightObject.SetActive(isActive);
        if (highlightMask != null && elementToHighlight != null)
        {
            highlightMask.FitTo(elementToHighlight);
            var highLightRect = highlightMask.transform as RectTransform;
            highLightRect.anchorMax = highLightRect.anchorMin = Vector2.zero;
            highLightRect.pivot = new Vector2(0.5f, 0.5f);

            var highLightButtonRect = hightLightButton.transform as RectTransform;
            highLightButtonRect.position = highLightRect.position;
            highLightButtonRect.sizeDelta = highLightRect.sizeDelta;
        }
    }

    protected void ShowCurrentStep()
    {
        TutorialStep step = tutorialSteps[currentStepIndex];
        step.OnStepStart?.Invoke();
        // Set panel content
        if(step.IsHidePanel)
        {
            panelObject.SetActive(false);
        }
        else
        {
            panelObject.SetActive(true);
        }
        SetContent(step.Content);
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
