using System;
using System.Collections.Generic;

[Serializable]
public class PostBoxData
{
    [System.NonSerialized]
    public Action OnResourceChange;

    public List<string> KitchenObjectSOGuidList { get; private set; } = new List<string>();
    public void AddPackage(string kitchenObjectSOGuid)
    {
        KitchenObjectSOGuidList.Add(kitchenObjectSOGuid);
        OnResourceChange?.Invoke();
    }
    public void RemovePackage(string guid)
    {
        KitchenObjectSOGuidList.Remove(guid);
        OnResourceChange?.Invoke();
    }
}