using UnityEngine;

public class StoveCounterView : BaseCounterView
{
    [SerializeField] private ProgressBarUI progressBarUI;
    [SerializeField] private BurnWarningUI burnWarningUI;
    [SerializeField] private GameObject[] visualGameObjectArray;

    private StoveCounterController _stoveCounterController;

    private void Start()
    {
        _stoveCounterController = GetComponent<StoveCounterController>();
        _stoveCounterController.OnProgressChanged += StoveCounterController_OnProgressChanged;
        _stoveCounterController.OnStateChanged += StoveCounterController_OnStateChanged;
    }

    private void StoveCounterController_OnProgressChanged(object sender, float progress)
    {
        progressBarUI.OnProgressChanged(progress);
        burnWarningUI.OnProgressChanged(_stoveCounterController, progress);
    }

    private void StoveCounterController_OnStateChanged(object sender, StoveCounterService.State state)
    {
        bool showVisual = state == StoveCounterService.State.Cooking || state == StoveCounterService.State.Cooked;
        foreach (var visualGameObject in visualGameObjectArray)
        {
            visualGameObject.SetActive(showVisual);
        }
    }
}