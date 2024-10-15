using UnityEngine;

public abstract class MonoSingleton<T> : MonoBehaviour where T : Component
{
    private static T _instance;

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<T>();

                if (_instance == null)
                {
                    GameObject gameObject = new GameObject(nameof(T));
                    _instance = gameObject.AddComponent<T>();
                }
            }
            return _instance;

        }
    }

    private void Awake()
    {
        if (_instance == null)
            _instance = this as T;
        else
        {
            if(_instance!= this)
            {
                Destroy(gameObject);
            }
        }

    }
}
