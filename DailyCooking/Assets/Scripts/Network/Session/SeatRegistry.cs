using System.Collections.Generic;

// Server-side seats by player id: live connections plus seats held for 60 s after a drop, so a
// player who loses signal can come back and nobody new takes the place meanwhile.
public sealed class SeatRegistry
{
    public const float HoldSeconds = 60f;

    public enum ApprovalResult { Approved, ApprovedReplaceLive, ApprovedReclaimHeld, RoomFull }

    private readonly Dictionary<string, ulong> live = new Dictionary<string, ulong>();
    private readonly Dictionary<string, float> heldUntil = new Dictionary<string, float>();
    private readonly List<string> expired = new List<string>();

    public int TakenSeats => live.Count + heldUntil.Count;

    public ApprovalResult Approve(string playerId, int max)
    {
        if (!string.IsNullOrEmpty(playerId))
        {
            if (live.ContainsKey(playerId)) return ApprovalResult.ApprovedReplaceLive;
            if (heldUntil.ContainsKey(playerId)) return ApprovalResult.ApprovedReclaimHeld;
        }
        return TakenSeats >= max ? ApprovalResult.RoomFull : ApprovalResult.Approved;
    }

    public void Connected(string playerId, ulong clientId)
    {
        if (string.IsNullOrEmpty(playerId)) return;
        live[playerId] = clientId;
        heldUntil.Remove(playerId);
    }

    // Only the player's current connection holds the seat; an old, replaced connection closing does not.
    public void Disconnected(string playerId, ulong clientId, float now)
    {
        if (string.IsNullOrEmpty(playerId)) return;
        if (!live.TryGetValue(playerId, out ulong current) || current != clientId) return;
        live.Remove(playerId);
        heldUntil[playerId] = now + HoldSeconds;
    }

    public bool IsHeld(string playerId) => !string.IsNullOrEmpty(playerId) && heldUntil.ContainsKey(playerId);

    public ulong? LiveClient(string playerId) =>
        !string.IsNullOrEmpty(playerId) && live.TryGetValue(playerId, out ulong clientId) ? clientId : (ulong?)null;

    // Player ids whose hold ran out; their seats are free again.
    public IReadOnlyList<string> Expire(float now)
    {
        expired.Clear();
        foreach (var entry in heldUntil)
        {
            if (entry.Value <= now) expired.Add(entry.Key);
        }
        for (int i = 0; i < expired.Count; i++)
            heldUntil.Remove(expired[i]);
        return expired;
    }

    public void Clear()
    {
        live.Clear();
        heldUntil.Clear();
    }
}
