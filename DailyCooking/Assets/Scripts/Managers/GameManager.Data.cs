using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

// Server-authoritative restaurant data the clients can change: name, menu, inventory, PostBox
// and the owning player's customization.
public partial class GameManager
{
    #region Restaurant data

    [Rpc(SendTo.Server)]
    public void UpdateRestaurantNameServerRpc(string name)
    {
        if (GameData == null || string.IsNullOrWhiteSpace(name)) return;
        string clean = name.Trim();
        if (clean.Length > MAX_RESTAURANT_NAME_LENGTH) clean = clean.Substring(0, MAX_RESTAURANT_NAME_LENGTH);
        GameData.RestaurantData.UpdateRestaurantName(clean);
        UpdateRestaurantNameClientRpc(clean);
    }

    [Rpc(SendTo.NotServer)]
    private void UpdateRestaurantNameClientRpc(string name)
    {
        GameData?.RestaurantData.UpdateRestaurantName(name);
    }

    [Rpc(SendTo.Server)]
    public void AddDishToMenuServerRpc(string foodGuid)
    {
        FoodSO dish = FindDish(foodGuid);
        if (GameData == null || dish == null || !GameData.MenuData.unlockedDishes.Contains(dish)) return;
        if (GameData.AddDishToMenu(dish))
            AddDishToMenuClientRpc(foodGuid);
    }

    [Rpc(SendTo.NotServer)]
    private void AddDishToMenuClientRpc(string foodGuid)
    {
        FoodSO dish = FindDish(foodGuid);
        if (dish != null)
            GameData?.AddDishToMenu(dish);
    }

    [Rpc(SendTo.Server)]
    public void RemoveDishFromMenuServerRpc(string foodGuid)
    {
        FoodSO dish = FindDish(foodGuid);
        if (GameData == null || dish == null) return;
        if (GameData.RemoveDishFromMenu(dish))
            RemoveDishFromMenuClientRpc(foodGuid);
    }

    [Rpc(SendTo.NotServer)]
    private void RemoveDishFromMenuClientRpc(string foodGuid)
    {
        FoodSO dish = FindDish(foodGuid);
        if (dish != null)
            GameData?.RemoveDishFromMenu(dish);
    }

    // Server only: inventory changes decided by the server (placing, picking up, buying).
    public void ServerAddInventory(string guid)
    {
        if (!IsServer || GameData == null || string.IsNullOrEmpty(guid)) return;
        if (GridBuildingSystem.Instance == null ||
            GridBuildingSystem.Instance.GetPlacedObjectTypeSOByGuid(guid) == null)
        {
            Debug.LogError($"ServerAddInventory: no PlacedObjectTypeSO for guid '{guid}'. Reward was not added.");
            return;
        }
        GameData.AddInventoryData(guid);
        AddInventoryDataClientRpc(guid);
    }

    public void ServerRemoveInventory(string guid)
    {
        if (!IsServer || GameData?.InventoryData == null || string.IsNullOrEmpty(guid)) return;
        GameData.RemoveInventoryData(guid);
        RemoveInventoryDataClientRpc(guid);
    }

    [Rpc(SendTo.NotServer)]
    private void AddInventoryDataClientRpc(string guid)
    {
        GameData?.AddInventoryData(guid);
    }

    [Rpc(SendTo.NotServer)]
    private void RemoveInventoryDataClientRpc(string guid)
    {
        GameData?.RemoveInventoryData(guid);
    }

    // Server only: a package was taken out of the PostBox.
    public void ServerRemovePostBoxPackage(string kitchenObjectSOGuid)
    {
        if (!IsServer || GameData?.PostBoxData == null || string.IsNullOrEmpty(kitchenObjectSOGuid)) return;
        GameData.PostBoxData.RemovePackage(kitchenObjectSOGuid);
        RemovePostBoxDataClientRpc(kitchenObjectSOGuid);
    }

    [Rpc(SendTo.NotServer)]
    private void RemovePostBoxDataClientRpc(string kitchenObjectSOGuid)
    {
        GameData?.PostBoxData.RemovePackage(kitchenObjectSOGuid);
    }

    #endregion

    #region Player customization

    // Called on the owning client after the player saves their look.
    public void RequestSaveCustomization(Dictionary<string, int> customizations)
    {
        var encoded = new StringBuilder();
        foreach (var entry in customizations)
        {
            if (encoded.Length > 0) encoded.Append(CUSTOMIZATION_ENTRY_SEPARATOR);
            encoded.Append(entry.Key).Append(CUSTOMIZATION_VALUE_SEPARATOR).Append(entry.Value);
        }
        SaveCustomizationServerRpc(encoded.ToString());
    }

    [Rpc(SendTo.Server)]
    private void SaveCustomizationServerRpc(string encodedCustomizations, RpcParams rpcParams = default)
    {
        string playerId = GetPlayerIdForClient(rpcParams.Receive.SenderClientId);
        PlayerStats stats = string.IsNullOrEmpty(playerId) ? null : GameData?.GetPlayerStatsById(playerId);
        Dictionary<string, int> customizations = DecodeCustomizations(encodedCustomizations);
        if (stats == null || customizations == null) return;
        stats.UpdatePlayerCustomization(customizations);
        SaveCustomizationClientRpc(playerId, encodedCustomizations);
    }

    [Rpc(SendTo.NotServer)]
    private void SaveCustomizationClientRpc(string playerId, string encodedCustomizations)
    {
        PlayerStats stats = GameData?.GetPlayerStatsById(playerId);
        Dictionary<string, int> customizations = DecodeCustomizations(encodedCustomizations);
        if (stats != null && customizations != null)
            stats.UpdatePlayerCustomization(customizations);
    }

    // Returns null when any entry names an unknown part or an out-of-range index (-1 = cleared).
    private static Dictionary<string, int> DecodeCustomizations(string encoded)
    {
        var result = new Dictionary<string, int>();
        if (string.IsNullOrEmpty(encoded)) return result;
        List<CosmeticsData> cosmeticDatas = ConfigManager.Instance.CustomizationData.CosmeticDatas;
        foreach (string entry in encoded.Split(CUSTOMIZATION_ENTRY_SEPARATOR))
        {
            string[] parts = entry.Split(CUSTOMIZATION_VALUE_SEPARATOR);
            if (parts.Length != 2 || !int.TryParse(parts[1], out int index)) return null;
            CosmeticsData cosmeticsData = cosmeticDatas.Find(x => x.Type == parts[0]);
            if (cosmeticsData == null || index < -1 || index >= cosmeticsData.Cosmetics.Count) return null;
            result[parts[0]] = index;
        }
        return result;
    }

    #endregion
}
