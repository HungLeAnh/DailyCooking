using UnityEngine;
using Unity.Netcode;

public class CookingToolItem : KitchenObject
{
    [SerializeField] private CookingTool cookingTool;
    [SerializeField] private string placedObjectTypeSOGuid;

    public CookingTool CookingTool => cookingTool != null ? cookingTool : GetComponent<CookingTool>();
    public string PlacedObjectTypeSOGuid => placedObjectTypeSOGuid;

    private FollowTransform _followTransform;

    protected override void Awake()
    {
        base.Awake();
        _followTransform = GetComponent<FollowTransform>();
    }

    public void SetCookingToolConfig(CookingToolConfigSO config)
    {
        cookingTool = GetComponent<CookingTool>();
        if (cookingTool != null)
            cookingTool.SetCookingToolConfig(config);
    }

    public void SetPlacedObjectTypeSOGuid(string guid)
    {
        placedObjectTypeSOGuid = guid;
    }

    public void EnableFollow(Transform target)
    {
        if (_followTransform != null)
            _followTransform.setTargetTransform(target);
    }

    public void DisableFollow()
    {
        if (_followTransform != null)
            _followTransform.setTargetTransform(null);
    }
}
