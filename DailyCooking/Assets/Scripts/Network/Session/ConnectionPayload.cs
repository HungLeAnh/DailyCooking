using System.Text;

// The joining player's id, sent with the connection request so the host can approve by player
// (held seats, replacing a stale connection) before the player exists in the session.
public static class ConnectionPayload
{
    public const int MaxIdLength = 128;

    public static byte[] Encode(string playerId) =>
        string.IsNullOrEmpty(playerId) ? new byte[0] : Encoding.UTF8.GetBytes(playerId);

    public static string Decode(byte[] payload)
    {
        if (payload == null || payload.Length == 0) return null;
        string id = Encoding.UTF8.GetString(payload).Trim();
        if (id.Length == 0 || id.Length > MaxIdLength) return null;
        return id;
    }
}
