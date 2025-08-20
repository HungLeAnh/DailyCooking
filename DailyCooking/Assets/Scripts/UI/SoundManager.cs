using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public enum SoundType
{
    Footstep,
    Warning,
    Cooking,
    ObjectPickup,
    ObjectDrop,
    DeliverySuccess,
    DeliveryFailed,
    Chop,
    CountDown,
    None
}
public class SoundManager : PersistentSingleton<SoundManager>   
{
    private const string PLAYER_SOUND_EFFECTS_VOLUME = "SoundEffectVolume";

    [SerializeField] private AudioClipRefsSO AudioClipRefsSO;
    private float footstepTimer;
    private float footstepTimerMax = 0.1f;
    private float volume = 1f;
    private Dictionary<SoundType, AudioSource> audioSourceDictionary;
    protected override void Awake()
    {
        base.Awake();
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
            DeliveryManager.Instance.OnRecipeSuccess += DeliveryManager_OnRecipeSuccess;
            DeliveryManager.Instance.OnRecipeFailed += DeliveryManager_OnRecipeFailed;
            //CuttingCounterController.OnAnyCut += CuttingCounter_OnAnyCut;
            //PlayerStateMachine.Instance.OnPickedSomething += Player_OnPickedSomething;
            //BaseCounterController.OnAnyObjectPlacedHere += BaseCounter_OnAnyObjectPlacedHere;
            //TrashCounterController.OnAnyObjectTrashed += TrashCounter_OnAnyObjectTrashed;
        }
    }

    private void TrashCounter_OnAnyObjectTrashed(object sender, System.EventArgs e)
    {
        TrashCounterController trashCounter = sender as TrashCounterController;
        PlaySound(AudioClipRefsSO.objectDrop, trashCounter.BaseCounterView.transform.position,
            soundType: SoundType.ObjectDrop);
    }

    private void BaseCounter_OnAnyObjectPlacedHere(object sender, KitchenObjectSO e)
    {
        BaseCounterController baseCounter = sender as BaseCounterController;
        PlaySound(AudioClipRefsSO.objectDrop, baseCounter.BaseCounterView.transform.position,
            soundType: SoundType.ObjectDrop);
    }

    private void Player_OnPickedSomething(object sender, System.EventArgs e)
    {
        PlaySound(AudioClipRefsSO.objectPickup, PlayerStateMachine.Instance.transform.position,
            soundType: SoundType.ObjectPickup);
    }

    private void CuttingCounter_OnAnyCut(object sender, System.EventArgs e)
    {
        CuttingCounterController cuttingCounter = sender as CuttingCounterController;
        PlaySound(AudioClipRefsSO.chop, cuttingCounter.BaseCounterView.transform.position,
            soundType: SoundType.Chop);
    }

    private void DeliveryManager_OnRecipeFailed(object sender, System.EventArgs e)
    {
        DeliveryCounterController deliveryCounter = DeliveryCounterController.Instance;
        PlaySound(AudioClipRefsSO.deliveryFailed, 
            deliveryCounter.BaseCounterView.transform.position, soundType: SoundType.DeliveryFailed);
    }

    private void DeliveryManager_OnRecipeSuccess(object sender, System.EventArgs e)
    {
        DeliveryCounterController deliveryCounter = DeliveryCounterController.Instance;
        PlaySound(AudioClipRefsSO.deliverySuccess, 
            deliveryCounter.BaseCounterView.transform.position, soundType : SoundType.DeliverySuccess);
    }
    private void PlaySoundAtPosition(AudioClip clip, Vector3 position, 
        float volume = 1f, bool destroyOnEnd = true, SoundType soundType = SoundType.None)
    {
        if(audioSourceDictionary.ContainsKey(soundType))
        {
            audioSourceDictionary[soundType].Stop();
            audioSourceDictionary[soundType].clip = clip;
            audioSourceDictionary[soundType].spatialBlend = 0f;
            audioSourceDictionary[soundType].volume = volume;
            audioSourceDictionary[soundType].Play();
        }
        else
        {
            GameObject tempGO = new GameObject("TempAudio");
            tempGO.transform.position = position;
            AudioSource audioSource = tempGO.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.spatialBlend = 0f;
            audioSource.volume = volume; 
            audioSource.Play();
            if(!destroyOnEnd)
            {
                Destroy(tempGO, clip.length);
            }
            else
            {
                audioSourceDictionary.Add(soundType,audioSource);
            }
        }
           
    }

    private void PlaySound(AudioClip audioClip, Vector3 position,
        float volume = 1f, bool destroyOnEnd = true ,SoundType soundType = SoundType.None)
    {
        PlaySoundAtPosition(audioClip, position, volume, destroyOnEnd, soundType);
    }
    public void StopPlaySound(SoundType soundType)
    {
        audioSourceDictionary[soundType].Stop();
    }
    private void PlaySound(AudioClip[] audioClipArray, Vector3 position, 
        float volumeMultiplier = 1f, bool destroyOnEnd = true, SoundType soundType = SoundType.None)
    {
        PlaySound(audioClipArray[Random.Range(0, audioClipArray.Length)], position, 
            volumeMultiplier * volume, destroyOnEnd, soundType);
    }

    public void PlayFootStepSound(Vector3 position, float volume)
    {
        footstepTimer -= Time.deltaTime;
        if (footstepTimer < 0)
        {
            footstepTimer = footstepTimerMax;

            PlaySound(AudioClipRefsSO.footstep, position, volume, soundType : SoundType.Footstep);
        }
    }
    public void PlayCountdownSound()
    {
        PlaySound(AudioClipRefsSO.warning, Vector3.zero, soundType: SoundType.CountDown);

    }
    public void PlayWarningSound(Vector3 position)
    {
        PlaySound(AudioClipRefsSO.warning, position, soundType : SoundType.Warning);

    }
    public void PlayCookingSound(Vector3 position)
    {
        PlaySound(AudioClipRefsSO.stoveSizzle, position, soundType: SoundType.Cooking);

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