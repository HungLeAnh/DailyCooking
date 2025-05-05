using UnityEngine;

public class MusicManager : PersistentSingleton<MusicManager>
{
    private const string PLAYER_MUSIC_VOLUME = "MusicVolume";


    private AudioSource audioSource;
    private float volume = .3f;

    protected override void Awake()
    {
        base.Awake();
        audioSource = GetComponent<AudioSource>();
        volume = PlayerPrefs.GetFloat(PLAYER_MUSIC_VOLUME, .3f);
        audioSource.volume = volume;

    }
    public void ChangeVolume()
    {
        volume += .1f;
        if (volume > 1f)
        {
            volume = 0f;
        }
        audioSource.volume = volume;
        PlayerPrefs.SetFloat(PLAYER_MUSIC_VOLUME, volume);
        PlayerPrefs.Save();
    }
    public float GetVolume()
    {
        return volume;
    }
}
