// Who may watch the host's ad: a client with an ad loaded whose player is not away. Used for the
// "Who watches?" rows and for which clients a force may pick.
public static class AdCandidates
{
    public static bool CanWatch(bool hasAd, bool isAway) => hasAd && !isAway;
}
