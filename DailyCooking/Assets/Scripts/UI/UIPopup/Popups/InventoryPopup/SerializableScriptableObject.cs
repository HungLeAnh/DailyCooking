using System;
using Unity.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
[Serializable]
public class SerializableScriptableObject : ScriptableObject
{
    [SerializeField, ReadOnly] private string _guid;

    // Lazy self-heal: OnValidate does not run for assets created via script
    // (AssetDatabase.CreateAsset), so a scripted asset would otherwise keep an empty
    // _guid forever and silently drop out of every GUID lookup. The first read in the
    // editor fills it from the asset path and marks the asset dirty so the next save
    // persists it. Runtime builds keep reading the serialized field unchanged.
    public string Guid
    {
        get
        {
            if (string.IsNullOrEmpty(_guid))
            {
#if UNITY_EDITOR
                _guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(this));
                if (!string.IsNullOrEmpty(_guid))
                    EditorUtility.SetDirty(this);
#endif
            }
            return _guid;
        }
    }

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
