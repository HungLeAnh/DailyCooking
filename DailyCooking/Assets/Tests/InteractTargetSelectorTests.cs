using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class InteractTargetSelectorTests
{
    private static readonly Vector3 Origin = Vector3.zero;
    private static readonly Vector3 Forward = new Vector3(0f, 0f, 1f);

    private static int Select(params Vector3[] candidates)
    {
        return InteractTargetSelector.SelectIndex(Origin, Forward, new List<Vector3>(candidates));
    }

    [Test] public void Empty_ReturnsMinusOne()
    {
        Assert.AreEqual(-1, Select());
    }

    [Test] public void PicksMostInFront()
    {
        Assert.AreEqual(1, Select(new Vector3(2f, 0f, 0f), new Vector3(0f, 0f, 2f)));
    }

    [Test] public void TieWithinAngle_PicksNearest()
    {
        Assert.AreEqual(1, Select(new Vector3(0.3f, 0f, 3f), new Vector3(0f, 0f, 1f)));
    }

    [Test] public void OutsideTie_AngleBeatsDistance()
    {
        Assert.AreEqual(1, Select(new Vector3(1f, 0f, 1f), new Vector3(0f, 0f, 3f)));
    }

    [Test] public void BehindPlayer_StillSelectableWhenOnlyOne()
    {
        Assert.AreEqual(0, Select(new Vector3(0f, 0f, -1f)));
    }

    [Test] public void IgnoresHeight()
    {
        Assert.AreEqual(0, Select(new Vector3(0f, 5f, 1f), new Vector3(1f, 0f, 1f)));
    }
}
