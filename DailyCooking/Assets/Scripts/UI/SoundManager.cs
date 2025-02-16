using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
    private const string PLAYER_SOUND_EFFECTS_VOLUME = "SoundEffectVolume";

    public static SoundManager Instance { get; private set; }


    [SerializeField] private AudioClipRefsSO AudioClipRefsSO;
    private float footstepTimer;
    private float footstepTimerMax = 0.1f;
    private float volume = 1f;
    private void Awake()
    {
        if(Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        volume = PlayerPrefs.GetFloat(PLAYER_SOUND_EFFECTS_VOLUME, 1f);
    }
    private void Start()
    {
        SceneManager.sceneLoaded += SceneManager_sceneLoaded;
    }

    private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        if (KitchenGameManager.Instance != null)
        {
            Debug.Log("on start");
            DeliveryManager.Instance.OnRecipeSuccess += DeliveryManager_OnRecipeSuccess;
            DeliveryManager.Instance.OnRecipeFailed += DeliveryManager_OnRecipeFailed;
            CuttingCounterController.OnAnyCut += CuttingCounter_OnAnyCut;
            PlayerStateMachine.Instance.OnPickedSomething += Player_OnPickedSomething;
            BaseCounterController.OnAnyObjectPlacedHere += BaseCounter_OnAnyObjectPlacedHere;
            TrashCounterController.OnAnyObjectTrashed += TrashCounter_OnAnyObjectTrashed;
        }
    }

    private void TrashCounter_OnAnyObjectTrashed(object sender, System.EventArgs e)
    {
        TrashCounterController trashCounter = sender as TrashCounterController;
        PlaySound(AudioClipRefsSO.objectDrop, trashCounter.TrashCounterView.transform.position);
    }

    private void BaseCounter_OnAnyObjectPlacedHere(object sender, KitchenObjectSO e)
    {
        BaseCounterController baseCounter = sender as BaseCounterController;
        PlaySound(AudioClipRefsSO.objectDrop, baseCounter.BaseCounterView.transform.position);
    }

    private void Player_OnPickedSomething(object sender, System.EventArgs e)
    {
        PlaySound(AudioClipRefsSO.objectPickup, PlayerStateMachine.Instance.transform.position);
    }

    private void CuttingCounter_OnAnyCut(object sender, System.EventArgs e)
    {
        CuttingCounterController cuttingCounter = sender as CuttingCounterController;
        PlaySound(AudioClipRefsSO.chop, cuttingCounter.BaseCounterView.transform.position);
    }

    private void DeliveryManager_OnRecipeFailed(object sender, System.EventArgs e)
    {
        DeliveryCounterController deliveryCounter = DeliveryCounterController.Instance;
        PlaySound(AudioClipRefsSO.deliveryFailed, deliveryCounter.BaseCounterView.transform.position);
    }

    private void DeliveryManager_OnRecipeSuccess(object sender, System.EventArgs e)
    {
        DeliveryCounterController deliveryCounter = DeliveryCounterController.Instance;
        PlaySound(AudioClipRefsSO.deliverySuccess, deliveryCounter.BaseCounterView.transform.position);
    }

    private void PlaySound(AudioClip audioClip, Vector3 position, float volume = 1f)
    {
        AudioSource.PlayClipAtPoint(audioClip, position, volume);
    }
    private void PlaySound(AudioClip[] audioClipArray, Vector3 position, float volumeMultiplier = 1f)
    {
        PlaySound(audioClipArray[Random.Range(0, audioClipArray.Length)], position, volumeMultiplier * volume);
    }

    public void PlayFootStepSound(Vector3 position, float volume)
    {
        footstepTimer -= Time.deltaTime;
        if (footstepTimer < 0)
        {
            footstepTimer = footstepTimerMax;

            PlaySound(AudioClipRefsSO.footstep, position, volume);
        }
    }
    public void PlayCountdownSound()
    {
        PlaySound(AudioClipRefsSO.warning, Vector3.zero);

    }
    public void PlayWarningSound(Vector3 position)
    {
        PlaySound(AudioClipRefsSO.warning, position);

    }
    public void ChangeVolume()
    {
        volume += .1f;
        if (volume > 1f)
        {
            volume = 0f;
        }
        PlayerPrefs.SetFloat(PLAYER_SOUND_EFFECTS_VOLUME, volume);
        PlayerPrefs.Save();
    }
    public float GetVolume()
    {
        return volume;
    }
}