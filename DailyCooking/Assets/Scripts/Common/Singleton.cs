using UnityEngine;

/// <summary>
/// Base singleton for scene-local MonoBehaviours. Derived types that must survive scene
/// loading override <see cref="PersistAcrossScenes"/>.
/// </summary>
/// <typeparam name="T">The type of the MonoBehaviour that should be a Singleton.</typeparam>
public abstract class Singleton<T> : MonoBehaviour where T : Component
{
    private static T s_Instance;

    public static T Instance
    {
        get
        {
            if (s_Instance == null)
                s_Instance = FindFirstObjectByType<T>();
            return s_Instance;
        }
    }

    protected virtual bool PersistAcrossScenes => false;

    protected virtual void Awake()
    {
        if (s_Instance == null)
        {
            s_Instance = this as T;
            if (PersistAcrossScenes)
                DontDestroyOnLoad(gameObject);
        }
        else if (s_Instance != this)
        {
            Destroy(gameObject);
        }
    }
}
