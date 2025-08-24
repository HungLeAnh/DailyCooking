using UnityEngine;
using UnityEngine.Events;

public class GenericUpgrader : MonoBehaviour
{
    public UnityEvent OnUpgrade;

    public void Upgrade()
    {
        OnUpgrade.Invoke();
    }
}
