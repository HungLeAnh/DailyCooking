using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class CombineRecipeSO : ScriptableObject
{
    public List<KitchenObjectSO> input;
    public KitchenObjectSO output;
    public float combineTimerMax;
}
