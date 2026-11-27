using NUnit.Framework;
using UnityEngine;

public class AvatarRulesTests
{
    [Test] public void KeepWhenOwnerLeaves_WithId_True() => Assert.IsTrue(AvatarRules.KeepWhenOwnerLeaves("player-1"));

    [Test] public void KeepWhenOwnerLeaves_EmptyOrNull_False()
    {
        Assert.IsFalse(AvatarRules.KeepWhenOwnerLeaves(null));
        Assert.IsFalse(AvatarRules.KeepWhenOwnerLeaves(""));
    }

    [Test] public void AcceptsExpiredFood_ClearCounter_True()
    {
        var go = new GameObject("clear");
        try { Assert.IsTrue(AvatarRules.AcceptsExpiredFood(go.AddComponent<ClearCounterController>())); }
        finally { Object.DestroyImmediate(go); }
    }

    [Test] public void AcceptsExpiredFood_CookingToolCounter_False()
    {
        // A cooking-tool counter is a ClearCounterController, but its food sits in the tool.
        var go = new GameObject("tool counter");
        try { Assert.IsFalse(AvatarRules.AcceptsExpiredFood(go.AddComponent<CookingToolCounterController>())); }
        finally { Object.DestroyImmediate(go); }
    }

    [Test] public void AcceptsExpiredFood_Null_False() => Assert.IsFalse(AvatarRules.AcceptsExpiredFood(null));
}
