using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class KitchenObjectOptionalProcessSO : ScriptableObject
{
    public KitchenObjectSO input;
    public List<KitchenObjectSO> processListOutput;
}