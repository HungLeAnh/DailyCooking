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
        if (string.IsNullOrEmpty(_guid))
        {
            Debug.LogWarning($"[SerializableScriptableObject] GUID is empty for {this.name} at {path}. Save the asset to generate a GUID.", this);
        }
    }

    [ContextMenu("Validate SO")]
    private void ValidateSO()
    {
        var path = AssetDatabase.GetAssetPath(this);
        var guid = AssetDatabase.AssetPathToGUID(path);
        if (string.IsNullOrEmpty(guid))
        {
            Debug.LogError($"[SerializableScriptableObject] GUID is missing for {this.name}. Save the asset first.", this);
        }
        else
        {
            Debug.Log($"[SerializableScriptableObject] {this.name} is valid. GUID: {guid}", this);
        }
        OnValidate();
    }
#endif
}
