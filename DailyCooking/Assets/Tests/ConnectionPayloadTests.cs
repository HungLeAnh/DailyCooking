using System.Text;
using NUnit.Framework;

public class ConnectionPayloadTests
{
    [Test] public void RoundTrip()
    {
        Assert.AreEqual("abc123", ConnectionPayload.Decode(ConnectionPayload.Encode("abc123")));
    }

    [Test] public void EmptyOrNull_IsNull()
    {
        Assert.IsNull(ConnectionPayload.Decode(null));
        Assert.IsNull(ConnectionPayload.Decode(new byte[0]));
        Assert.AreEqual(0, ConnectionPayload.Encode(null).Length);
    }

    [Test] public void TooLong_IsNull()
    {
        Assert.IsNull(ConnectionPayload.Decode(Encoding.UTF8.GetBytes(new string('x', 129))));
        Assert.AreEqual(128, ConnectionPayload.Decode(Encoding.UTF8.GetBytes(new string('x', 128))).Length);
    }

    [Test] public void Whitespace_Trimmed()
    {
        Assert.AreEqual("id", ConnectionPayload.Decode(Encoding.UTF8.GetBytes("  id \n")));
        Assert.IsNull(ConnectionPayload.Decode(Encoding.UTF8.GetBytes("   ")));
    }
}
