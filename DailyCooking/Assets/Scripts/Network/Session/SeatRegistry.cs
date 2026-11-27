using System.Collections.Generic;

// Server-side seats by player id: live connections plus seats held for 60 s after a drop, so a
// player who loses signal can come back and nobody new takes the place meanwhile.
public sealed class SeatRegistry
{
    public const float HoldSeconds = 60f;
    // An approved new player who never finishes connecting gives its seat back after this long.
    public const float PendingSeconds = 30f;

    public enum ApprovalResult { Approved, ApprovedReplaceLive, ApprovedReclaimHeld, RoomFull }

    private readonly Dictionary<string, ulong> live = new Dictionary<string, ulong>();
    private readonly Dictionary<string, float> heldUntil = new Dictionary<string, float>();
    // Approved new players still connecting (by client id), so two joins at once can't both take the last seat.
    private readonly Dictionary<ulong, float> pendingSince = new Dictionary<ulong, float>();
    private readonly List<string> expired = new List<string>();
    private readonly List<ulong> stalePending = new List<ulong>();

    public int TakenSeats => live.Count + heldUntil.Count + pendingSince.Count;

    // A player who already owns a seat (live or held) is always let in; a new one takes a pending seat.
    public ApprovalResult Approve(string playerId, ulong clientId, int max, float now)
    {
        if (!string.IsNullOrEmpty(playerId))
        {
            if (live.ContainsKey(playerId)) return ApprovalResult.ApprovedReplaceLive;
            if (heldUntil.ContainsKey(playerId)) return ApprovalResult.ApprovedReclaimHeld;
        }
        if (TakenSeats >= max) return ApprovalResult.RoomFull;
        pendingSince[clientId] = now;
        return ApprovalResult.Approved;
    }

    // An approved connection that dropped before it finished connecting.
    public void DroppedBeforeConnect(ulong clientId) => pendingSince.Remove(clientId);

    public void Connected(string playerId, ulong clientId)
    {
        pendingSince.Remove(clientId);
        if (string.IsNullOrEmpty(playerId)) return;
        live[playerId] = clientId;
        heldUntil.Remove(playerId);
    }

    // Only the player's current connection holds the seat; an old, replaced connection closing does not.
    public void Disconnected(string playerId, ulong clientId, float now)
    {
        pendingSince.Remove(clientId);
        if (string.IsNullOrEmpty(playerId)) return;
        if (!live.TryGetValue(playerId, out ulong current) || current != clientId) return;
        live.Remove(playerId);
        heldUntil[playerId] = now + HoldSeconds;
    }

    // A player who left on purpose: free the held seat now instead of after 60 s.
    public bool Release(string playerId) => !string.IsNullOrEmpty(playerId) && heldUntil.Remove(playerId);

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
        stalePending.Clear();
        foreach (var entry in pendingSince)
        {
            if (now - entry.Value > PendingSeconds) stalePending.Add(entry.Key);
        }
        for (int i = 0; i < stalePending.Count; i++)
            pendingSince.Remove(stalePending[i]);
        return expired;
    }

    public void Clear()
    {
        live.Clear();
        heldUntil.Clear();
        pendingSince.Clear();
    }
}
