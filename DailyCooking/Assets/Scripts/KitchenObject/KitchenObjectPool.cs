using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Pool;

public class KitchenObjectPool : NetworkPersistentSingleton<KitchenObjectPool>
{
    private Dictionary<string, IObjectPool<GameObject>> _pools;
    private Dictionary<string, KitchenObjectSO> _kitchenObjectSODic;

    private const int DEFAULT_POOL_SIZE = 10;
    private const int MAX_POOL_SIZE = 50;

    protected override void Awake()
    {
        base.Awake();
        _pools = new Dictionary<string, IObjectPool<GameObject>>();
        _kitchenObjectSODic = new Dictionary<string, KitchenObjectSO>();
    }

    public void InitializePool(KitchenObjectSO kitchenObjectSO, int defaultCapacity = DEFAULT_POOL_SIZE, int maxSize = MAX_POOL_SIZE)
    {
        if (kitchenObjectSO == null || kitchenObjectSO.prefab == null) return;
        if (_pools.ContainsKey(kitchenObjectSO.Guid)) return;

        var pool = new ObjectPool<GameObject>(
            createFunc: () => CreateKitchenObject(kitchenObjectSO),
            actionOnGet: OnGetKitchenObject,
            actionOnRelease: OnReleaseKitchenObject,
            actionOnDestroy: OnDestroyKitchenObject,
            defaultCapacity: defaultCapacity,
            maxSize: maxSize
        );
        _pools[kitchenObjectSO.Guid] = pool;
    }

    private GameObject CreateKitchenObject(KitchenObjectSO kitchenObjectSO)
    {
        GameObject kitchenObject = Instantiate(kitchenObjectSO.prefab).gameObject;
        if (kitchenObject.TryGetComponent<NetworkObject>(out var netObj))
        {
            netObj.Spawn(true);
        }
        return kitchenObject;
    }

    private void OnGetKitchenObject(GameObject kitchenObject)
    {
        kitchenObject.SetActive(true);

        if (kitchenObject.TryGetComponent<KitchenObject>(out var kitchenObj))
        {
            kitchenObj.ResetState();
        }
    }

    private void OnReleaseKitchenObject(GameObject kitchenObject)
    {
        kitchenObject.SetActive(false);
    }

    private void OnDestroyKitchenObject(GameObject kitchenObject)
    {
        if (kitchenObject.TryGetComponent<NetworkObject>(out var netObj) && netObj.IsSpawned)
        {
            netObj.Despawn();
        }
        Destroy(kitchenObject);
    }

    public GameObject GetKitchenObject(string guid)
    {
        if (_pools.TryGetValue(guid, out var pool))
        {
            return pool.Get();
        }
        return null;
    }

    public void ReturnKitchenObject(GameObject kitchenObject, string guid)
    {
        if (_pools.TryGetValue(guid, out var pool))
        {
            pool.Release(kitchenObject);
        }
        else
        {
            OnDestroyKitchenObject(kitchenObject);
        }
    }

    public bool HasPool(string guid)
    {
        return _pools.ContainsKey(guid);
    }

    public void PrewarmPools(List<KitchenObjectSO> kitchenObjectSOList)
    {
        foreach (var kitchenObjectSO in kitchenObjectSOList)
        {
            if (kitchenObjectSO == null || kitchenObjectSO.prefab == null) continue;
            InitializePool(kitchenObjectSO);
            var pool = _pools[kitchenObjectSO.Guid];
            int prewarmCount = Mathf.Min(DEFAULT_POOL_SIZE / 2, MAX_POOL_SIZE / 2);
            for (int i = 0; i < prewarmCount; i++)
            {
                var obj = pool.Get();
                pool.Release(obj);
            }
        }
    }
}
