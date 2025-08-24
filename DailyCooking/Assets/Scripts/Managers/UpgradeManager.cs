using System.Collections.Generic;
using UnityEngine;

public class UpgradeManager : SimpleSingleton<UpgradeManager>
{
    private Dictionary<GameObject, IUpgradeable> upgradeableObjects = new Dictionary<GameObject, IUpgradeable>();

    public void Register(IUpgradeable upgradeable)
    {
        if (upgradeable is MonoBehaviour monoBehaviour)
        {
            if (!upgradeableObjects.ContainsKey(monoBehaviour.gameObject))
            {
                upgradeableObjects.Add(monoBehaviour.gameObject, upgradeable);
            }
        }
    }

    public void Unregister(IUpgradeable upgradeable)
    {
        if (upgradeable is MonoBehaviour monoBehaviour)
        {
            if (upgradeableObjects.ContainsKey(monoBehaviour.gameObject))
            {
                upgradeableObjects.Remove(monoBehaviour.gameObject);
            }
        }
    }

    public void Upgrade(GameObject upgradeableObject)
    {
        if (upgradeableObjects.TryGetValue(upgradeableObject, out IUpgradeable upgradeable))
        {
            upgradeable.Upgrade();
        }
    }
}
