// The message shown when a session ends; null when nothing needs explaining (the player quit).
public static class SessionEndText
{
    public static string Message(SessionEndReason reason)
    {
        switch (reason)
        {
            case SessionEndReason.HostClosed: return "The restaurant has closed";
            case SessionEndReason.ConnectionLost: return "Connection lost";
            case SessionEndReason.HostConnectionLost: return "Connection lost — your restaurant is saved";
            case SessionEndReason.RoomFull: return "Room full";
            case SessionEndReason.JoinFailed: return "Could not join the restaurant";
            case SessionEndReason.Replaced: return "You joined from another device";
            default: return null;
        }
    }
}
