using NUnit.Framework;

public class DeviceTierPickerTests
{
    [Test] public void Strong_IsHigh() => Assert.AreEqual(MobileTier.High, DeviceTierPicker.Pick(8192, 4096, 8));
    [Test] public void HighNeedsAllThree() => Assert.AreEqual(MobileTier.Medium, DeviceTierPicker.Pick(8192, 1024, 8));
    [Test] public void ZeroGraphicsMemory_NotHigh() => Assert.AreEqual(MobileTier.Medium, DeviceTierPicker.Pick(8192, 0, 8));
    [Test] public void LowRam_IsLow() => Assert.AreEqual(MobileTier.Low, DeviceTierPicker.Pick(2048, 4096, 8));
    [Test] public void FewCores_IsLow() => Assert.AreEqual(MobileTier.Low, DeviceTierPicker.Pick(8192, 4096, 4));
    [Test] public void Typical_IsMedium() => Assert.AreEqual(MobileTier.Medium, DeviceTierPicker.Pick(4096, 1024, 8));

    [Test] public void Names()
    {
        Assert.AreEqual("Mobile Low", MobileTiers.QualityLevelName(MobileTier.Low));
        Assert.AreEqual("Mobile Medium", MobileTiers.QualityLevelName(MobileTier.Medium));
        Assert.AreEqual("Mobile High", MobileTiers.QualityLevelName(MobileTier.High));
    }

    [Test] public void PropShadows_HighOnly()
    {
        Assert.IsFalse(MobileTiers.PropShadowsOn(MobileTier.Low));
        Assert.IsFalse(MobileTiers.PropShadowsOn(MobileTier.Medium));
        Assert.IsTrue(MobileTiers.PropShadowsOn(MobileTier.High));
    }
}
