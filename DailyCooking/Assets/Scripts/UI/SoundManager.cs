using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundManager : PersistentSingleton<SoundManager>   
{
    private const string PLAYER_SOUND_EFFECTS_VOLUME = "SoundEffectVolume";

    [SerializeField] private AudioClipRefsSO AudioClipRefsSO;
    private float footstepTimer;
    [SerializeField] private float footstepTimerMax = 0.1f;
    private float volume = 1f;
    private List<AudioSource> audioSourcePool;
    protected override void Awake()
    {
        base.Awake();
        audioSourcePool = new List<AudioSource>();
        volume = PlayerPrefs.GetFloat(PLAYER_SOUND_EFFECTS_VOLUME, 1f);
    }
    private void Start()
    {
        SceneManager.sceneLoaded += SceneManager_sceneLoaded;
    }

    private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
    {

    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= SceneManager_sceneLoaded;

    }

    private void TrashCounter_OnAnyObjectTrashed(object sender, System.EventArgs e)
    {
        TrashCounterController trashCounter = sender as TrashCounterController;
        PlaySound(AudioClipRefsSO.objectDrop, trashCounter.BaseCounterView.transform.position);
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

    private AudioSource GetPooledAudioSource()
    {
        for (int i = 0; i < audioSourcePool.Count; i++)
        {
            if (!audioSourcePool[i].isPlaying)
            {
                return audioSourcePool[i];
            }
        }

        GameObject tempGO = new GameObject("TempAudio");
        tempGO.transform.SetParent(transform);
        AudioSource audioSource = tempGO.AddComponent<AudioSource>();
        audioSourcePool.Add(audioSource);
        return audioSource;
    }

    private AudioSource PlaySoundAtPosition(AudioClip clip, Vector3 position, float volume = 1f, bool loop = false)
    {
        AudioSource audioSource = GetPooledAudioSource();
        audioSource.transform.position = position;
        audioSource.clip = clip;
        audioSource.spatialBlend = 1f;
        audioSource.volume = volume;
        audioSource.loop = loop;
        audioSource.Play();
        return audioSource;
    }

    public AudioSource PlaySound(AudioClip audioClip, Vector3 position, bool loop = false)
    {
        return PlaySoundAtPosition(audioClip, position, volume, loop);
    }
    
    public AudioSource PlaySound(AudioClip[] audioClipArray, Vector3 position, bool loop = false)
    {
        return PlaySound(audioClipArray[Random.Range(0, audioClipArray.Length)], position, loop);
    }

    public void PlayFootStepSound(Vector3 position)
    {
        footstepTimer -= Time.deltaTime;
        if (footstepTimer < 0)
        {
            footstepTimer = footstepTimerMax;

            PlaySound(AudioClipRefsSO.footstep, position);
        }
    }
    public void PlayCountdownSound()
    {
        PlaySound(AudioClipRefsSO.warning, Vector3.zero);

    }
    public AudioSource PlayWarningSound(Vector3 position, bool loop = true)
    {
        return PlaySound(AudioClipRefsSO.warning, position, loop);

    }
    public AudioSource PlayCookingSound(Vector3 position, bool loop = true)
    {
        return PlaySound(AudioClipRefsSO.stoveSizzle, position, loop);

    }
    public void StopSound(AudioSource audioSource)
    {
        var audio = audioSourcePool.Find(x => x == audioSource);
        if (audio != null)
        {
            audio?.Stop();
        }
    }

    public void StopAllSounds()
    {
        foreach (AudioSource audioSource in audioSourcePool)
        {
            audioSource.Stop();
        }
    }

    public void ChangeVolume(float newVolume)
    {
        this.volume = newVolume;
        PlayerPrefs.SetFloat(PLAYER_SOUND_EFFECTS_VOLUME, this.volume);
        PlayerPrefs.Save();
    }

    public float GetVolume()
    {
        return volume;
    }
}