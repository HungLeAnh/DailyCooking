using UnityEngine;

public class StoveCounterSound : MonoBehaviour
{
    [SerializeField] private StoveCounter stoveCounter;
    private AudioSource audioSource;
    private float warningSoundTimer;
    private bool playWarningSound;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        stoveCounter.CookingTool.OnStageChanged += CookingTool_OnStageChanged;
        stoveCounter.CookingTool.OnProgressChanged += StoveCounter_OnProgressChanged;
    }

    private void CookingTool_OnStageChanged(object sender, CookingTool.OnStageChangeEventArgs e)
    {        
        bool playSound = e.state == CookingTool.State.Cooking || e.state == CookingTool.State.Cooked;
        if (playSound)
        {
            audioSource.Play();
        }
        else
        {
            audioSource.Pause();
        }
    }

    private void StoveCounter_OnProgressChanged(object sender, IHasProgress.OnProgressChangedEventArgs e)
    {

        float burnShowProgressAmount = .5f;

        playWarningSound = stoveCounter.CookingTool.IsDone() && e.progressNormalized >= burnShowProgressAmount;


    }

    private void Update()
    {
        if (playWarningSound)
        {
            warningSoundTimer -= Time.deltaTime;
            if (warningSoundTimer <= 0f)
            {
                float warningSoundTimerMax = .2f;
                warningSoundTimer = warningSoundTimerMax;
                SoundManager.Instance.PlayWarningSound(stoveCounter.transform.position);
            }
        }

    }
}
