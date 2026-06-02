using System;
using System.Collections.Generic;

[System.Serializable]
public class PlayerStats
{
    [System.NonSerialized]
    public Action OnResourceChange;

    private string playerId;
    private float moveSpeed = 5f;
    private float cookingSpeed = 1f;
    private int carryingCapacity = 1;
    private float tipIncrease = 0f;
    private Dictionary<string, int> characterCustomizationIds = new Dictionary<string, int>();

    public string PlayerId { get => playerId; set => playerId = value; }
    public float MoveSpeed { get => moveSpeed; set => moveSpeed = value; }
    public float CookingSpeed { get => cookingSpeed; set => cookingSpeed = value; }
    public int CarryingCapacity { get => carryingCapacity; set => carryingCapacity = value; }
    public float TipIncrease { get => tipIncrease; set => tipIncrease = value; }
    public Dictionary<string, int> CharacterCustomizationIds { get => characterCustomizationIds; set => characterCustomizationIds = value; }

    public PlayerStats(string playerId)
    {
        PlayerId = playerId;
    }

    public void UpdatePlayerMoveSpeed(float amount)
    {
        MoveSpeed += MoveSpeed*amount;
        OnResourceChange?.Invoke();

    }
    public void UpdatePlayerCookingSpeed(float amount)
    {
        CookingSpeed += CookingSpeed * amount;
        OnResourceChange?.Invoke();

    }
    public void UpdatePlayerCarryingCapacity(float amount)
    {
        CarryingCapacity += (int)amount;
        OnResourceChange?.Invoke();

    }
    public void UpdatePlayerTipIncrease(float amount)
    {
        TipIncrease += amount;
        OnResourceChange?.Invoke();

    }
    public void UpdatePlayerCustomization(string type, int index)
    {
        if (CharacterCustomizationIds.ContainsKey(type))
        {
            CharacterCustomizationIds[type] = index;
        }
        else
        {
            CharacterCustomizationIds.Add(type, index);
        }
        OnResourceChange?.Invoke();
    }
}