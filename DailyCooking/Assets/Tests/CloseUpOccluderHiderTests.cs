using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class CloseUpOccluderHiderTests
{
    [Test] public void TurnOff_SkipsDestroyedRenderers()
    {
        // A cached held-food renderer array can outlive the food it came from.
        var alive = new GameObject("alive").AddComponent<MeshRenderer>();
        var gone = new GameObject("gone").AddComponent<MeshRenderer>();
        Object.DestroyImmediate(gone.gameObject);
        var turnedOff = new List<Renderer>();
        try
        {
            Assert.DoesNotThrow(() => CloseUpOccluderHider.TurnOff(new Renderer[] { gone, alive }, turnedOff));
            Assert.IsFalse(alive.enabled);
            Assert.AreEqual(1, turnedOff.Count);
        }
        finally
        {
            Object.DestroyImmediate(alive.gameObject);
        }
    }
}
