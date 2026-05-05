using System;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
[Serializable]
public class SerializableScriptableObject : ScriptableObject
{
    [SerializeField, ReadOnly] private string _guid;
    public string Guid => _guid;

#if UNITY_EDITOR
    void OnValidate()
    {
        var path = AssetDatabase.GetAssetPath(this);
        _guid = AssetDatabase.AssetPathToGUID(path);
    }
#endif
}
