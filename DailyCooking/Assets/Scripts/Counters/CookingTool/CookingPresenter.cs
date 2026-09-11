using System.Collections.Generic;
using UnityEngine;

// UI + sound + option menu. Plain C# (not a MonoBehaviour) so the prefab is unchanged;
// the CookingTool facade constructs it with serialized refs.
public sealed class CookingPresenter
{
    private const float UiEpsilon = 0.005f;
    private const string OptionTitle = "Select way to process:";

    private ProgressBarUI _progress;
    private BurnWarningUI _burnUI;
    private CombineDetailUI _combineUI;
    private float _burnThreshold = 0.5f;

    private AudioSource _cookingSound;
    private AudioSource _warningSound;
    private float _lastShown = -1f;
    private Vector3 _cachedPos;
    private bool _posCached;

    public void Init(ProgressBarUI progress, BurnWarningUI burnUI, CombineDetailUI combineUI, float burnThreshold)
    {
        _progress = progress;
        _burnUI = burnUI;
        _combineUI = combineUI;
        _burnThreshold = burnThreshold;
    }

    public void Reset()
    {
        _lastShown = -1f;
        _posCached = false;
    }

    public void HideAll(ProgressBarUI progress, BurnWarningUI burnUI)
    {
        progress?.Hide();
        burnUI?.Hide();
    }

    public void OnCombineResolved(CombineRecipeSO recipe)
    {
        if (_combineUI != null)
            _combineUI.InitUI(recipe);
    }

    public void OnStateChanged(CookingTool.State state, Vector3 pos)
    {
        _cachedPos = pos;
        _posCached = true;
        _lastShown = -1f;
        if (state == CookingTool.State.Idle || state == CookingTool.State.Burned)
        {
            SoundManager.Instance.StopSound(_cookingSound);
            SoundManager.Instance.StopSound(_warningSound);
        }
        if (state == CookingTool.State.Cooking || state == CookingTool.State.Cooked)
        {
            if (_cookingSound == null || !_cookingSound.isPlaying)
                _cookingSound = SoundManager.Instance.PlayCookingSound(pos);
        }
    }

    public void Draw(CookingTool.State state, CookingClock clock, RecipeResolver resolver,
        IHasProgress progressSender, ProgressBarUI progress, BurnWarningUI burnUI, Vector3 pos)
    {
        if (!_posCached)
        {
            _cachedPos = pos;
            _posCached = true;
        }
        switch (state)
        {
            case CookingTool.State.Idle:
                break;
            case CookingTool.State.Cooking:
                if (resolver.CookingTimeMax > 0f)
                {
                    float p = Mathf.Clamp01(clock.LocalCook / resolver.CookingTimeMax);
                    if (Mathf.Abs(p - _lastShown) >= UiEpsilon)
                    {
                        _lastShown = p;
                        progress?.OnProgressChanged(p);
                    }
                }
                break;
            case CookingTool.State.Cooked:
                if (resolver.BurningRecipe == null)
                {
                    if (_lastShown < 1f)
                    {
                        _lastShown = 1f;
                        progress?.OnProgressChanged(1f);
                    }
                    burnUI?.Hide();
                    break;
                }
                float burn = resolver.BurningTimeMax > 0f
                    ? Mathf.Clamp01(clock.LocalBurn / resolver.BurningTimeMax)
                    : 1f;
                // BurnWarningUI needs IHasProgress; pass null-safe via facade wrapper set by caller.
                // Caller passes `this` through DrawWithSender; kept simple here.
                if (Mathf.Abs(burn - _lastShown) >= UiEpsilon)
                {
                    _lastShown = burn;
                    progress?.OnProgressChanged(burn);
                    if (progressSender != null)
                        burnUI?.OnProgressChanged(progressSender, burn);
                }
                if (burn >= _burnThreshold && (_warningSound == null || !_warningSound.isPlaying))
                    _warningSound = SoundManager.Instance.PlayWarningSound(_cachedPos);
                break;
            case CookingTool.State.Burned:
                progress?.Hide();
                burnUI?.Hide();
                break;
        }
    }

    public void ShowOptionMenu(KitchenObjectSO input, List<KitchenObjectSO> options, IHasOptionalSO sender)
    {
        if (input == null || options == null || options.Count == 0 || sender == null)
            return;
        UIPopupManager.Instance.ShowPopup(
            UIPopupType.UIOptionMenuPopup,
            new UIOptionMenuPopup.Param
            {
                sender = sender,
                optionalList = options,
                Title = OptionTitle
            });
    }
}
