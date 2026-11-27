using NUnit.Framework;

public class SessionEndTextTests
{
    [Test] public void Messages()
    {
        Assert.AreEqual("The restaurant has closed", SessionEndText.Message(SessionEndReason.HostClosed));
        Assert.AreEqual("Connection lost", SessionEndText.Message(SessionEndReason.ConnectionLost));
        Assert.AreEqual("Connection lost — your restaurant is saved", SessionEndText.Message(SessionEndReason.HostConnectionLost));
        Assert.AreEqual("Room full", SessionEndText.Message(SessionEndReason.RoomFull));
        Assert.AreEqual("Could not join the restaurant", SessionEndText.Message(SessionEndReason.JoinFailed));
        Assert.AreEqual("You joined from another device", SessionEndText.Message(SessionEndReason.Replaced));
        Assert.IsNull(SessionEndText.Message(SessionEndReason.Quit));
        Assert.IsNull(SessionEndText.Message(SessionEndReason.None));
    }
}
