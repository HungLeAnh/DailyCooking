using NUnit.Framework;
using UnityEngine;

public class GameDefineTests
{
    [Test] public void DefaultGridConfigSO_Exists_InResources()
    {
        var so = Resources.Load<DefaultGridConfigSO>("DefaultGridConfig");
        Assert.IsNotNull(so, "DefaultGridConfigSO asset should exist at Resources/DefaultGridConfig");
        Assert.AreEqual(5, so.gridSize);
        Assert.IsFalse(string.IsNullOrEmpty(so.gridArrayJson));
    }
    [Test] public void GridSize_IsFive()
    {
        Assert.AreEqual(5, GameDefine.GridSize);
    }
    [Test] public void EmotionAndBotConstants_AreCorrect()
    {
        Assert.AreEqual(15f, GameDefine.EMOTION_DURATION);
        Assert.AreEqual(0.7f, GameDefine.TIP_PERCENTAGE);
    }
}
