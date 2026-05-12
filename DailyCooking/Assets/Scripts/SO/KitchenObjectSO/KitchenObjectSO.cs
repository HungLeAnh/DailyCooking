using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class KitchenObjectSO : SerializableScriptableObject
{
    public Transform prefab;
    public Sprite Sprite;
    public string objectName;
    public KitchenObjectOptionalProcessSO processSO;
}