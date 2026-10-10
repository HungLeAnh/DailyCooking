using NUnit.Framework;

public class GameDataTests
{
    [Test] public void GetPlayerStatsById_ReturnsTheMatchingStats()
    {
        var data = new GameData();
        data.TryAddPlayerStats("host");
        data.TryAddPlayerStats("guest");

        var found = data.GetPlayerStatsById("guest");

        Assert.IsNotNull(found);
        Assert.AreEqual("guest", found.PlayerId);
    }

    [Test] public void GetPlayerStatsById_ReturnsNullForUnknownId()
    {
        var data = new GameData();
        data.TryAddPlayerStats("host");

        Assert.IsNull(data.GetPlayerStatsById("nobody"));
    }

    [Test] public void GetPlayerStatsById_ReturnsNullForNullOrEmptyId()
    {
        var data = new GameData();
        data.TryAddPlayerStats("host");

        Assert.IsNull(data.GetPlayerStatsById(null));
        Assert.IsNull(data.GetPlayerStatsById(""));
    }

    [Test] public void GetPlayerStatsById_ReturnsNullOnEmptyDatabase()
    {
        Assert.IsNull(new GameData().GetPlayerStatsById("host"));
    }

    [Test] public void TryAddPlayerStats_DoesNotDuplicateAnExistingPlayer()
    {
        var data = new GameData();
        data.TryAddPlayerStats("host");
        var first = data.GetPlayerStatsById("host");

        data.TryAddPlayerStats("host");

        Assert.AreEqual(1, data.PlayersStats.Count);
        Assert.AreSame(first, data.GetPlayerStatsById("host"));
    }

    [Test] public void TryAddPlayerStats_FiresOnPlayerStatsAddedOnce()
    {
        var data = new GameData();
        int fired = 0;
        data.OnPlayerStatsAdded += _ => fired++;

        data.TryAddPlayerStats("host");
        data.TryAddPlayerStats("host");

        Assert.AreEqual(1, fired);
    }
}
