using UnityEngine;

public abstract class SimpleSingleton<T> : MonoBehaviour where T : Component
{
    private static T _instance;

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<T>();

                //if (_instance == null)
                //{
                //    GameObject gameObject = new GameObject(nameof(T));
                //    _instance = gameObject.AddComponent<T>();

                //    gameObject.name = typeof(T).ToString();
                //}
            }
            return _instance;

        }
    }

    protected virtual void Awake()
    {
        if (_instance == null)
            _instance = this as T;
        else if(_instance!= this)
        {
           Destroy(gameObject);
        }

    }
}
