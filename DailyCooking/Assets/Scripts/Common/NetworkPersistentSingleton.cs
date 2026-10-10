using UnityEngine;

/// <summary>
/// Singleton for a <see cref="Unity.Netcode.NetworkBehaviour"/> that survives scene
/// loading/unloading.
/// </summary>
/// <typeparam name="T">The type of the NetworkBehaviour that should be a Singleton.</typeparam>
public class NetworkPersistentSingleton<T> : NetworkSingleton<T> where T : Component
{
    protected override bool PersistAcrossScenes => true;
}
