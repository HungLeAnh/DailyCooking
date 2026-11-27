// Rules for avatars whose player is away and for the food they leave behind.
public static class AvatarRules
{
    // Only an avatar that belongs to a known player (a seat to come back to) outlives its connection.
    public static bool KeepWhenOwnerLeaves(string playerId) => !string.IsNullOrEmpty(playerId);

    // Where an expired seat's food may be put down: a plain counter, never a cooking-tool counter
    // (it is a ClearCounterController too, but its food sits in the tool and can't be reached).
    public static bool AcceptsExpiredFood(ClearCounterController counter) =>
        counter != null && !(counter is CookingToolCounterController);
}
