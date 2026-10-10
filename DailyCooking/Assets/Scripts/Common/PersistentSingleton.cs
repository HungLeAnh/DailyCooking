using UnityEngine;

/// <summary>
/// Singleton that survives scene loading/unloading.
/// </summary>
/// <typeparam name="T">The type of the MonoBehaviour that should be a Singleton.</typeparam>
public class PersistentSingleton<T> : Singleton<T> where T : Component
{
    protected override bool PersistAcrossScenes => true;
}
