using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class SaveSystemTests
{
    private const string FileName = "GameData_test";
    private string tempDir;

    [SetUp] public void SetUp()
    {
        tempDir = Path.Combine(Application.temporaryCachePath, "SaveSystemTests_" + System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
    }

    [TearDown] public void TearDown()
    {
        if (Directory.Exists(tempDir))
            Directory.Delete(tempDir, true);
    }

    [Test] public void SaveThenLoad_RoundTripsData()
    {
        var handler = new FileDataHandler(tempDir, FileName);
        var data = GameData.CreateNew();
        data.RestaurantData.UpdateRestaurantCoins(123);
        int expectedCoins = data.RestaurantData.Coins;

        handler.Save(data);
        var loaded = handler.Load();

        Assert.IsNotNull(loaded);
        Assert.AreEqual(expectedCoins, loaded.RestaurantData.Coins);
        Assert.AreEqual(GameData.CURRENT_SAVE_VERSION, loaded.SaveVersion);
        Assert.IsFalse(File.Exists(Path.Combine(tempDir, FileName + ".tmp")));
    }

    [Test] public void Save_KeepsPreviousSaveAsBackup()
    {
        var handler = new FileDataHandler(tempDir, FileName);
        var data = GameData.CreateNew();
        handler.Save(data);
        data.RestaurantData.UpdateRestaurantCoins(50);
        handler.Save(data);

        Assert.IsTrue(File.Exists(Path.Combine(tempDir, FileName + ".bak")));
    }

    [Test] public void Load_FallsBackToBackup_WhenMainFileIsCorrupt()
    {
        var handler = new FileDataHandler(tempDir, FileName);
        var data = GameData.CreateNew();
        data.RestaurantData.UpdateRestaurantCoins(10);
        int backupCoins = data.RestaurantData.Coins;
        handler.Save(data);
        data.RestaurantData.UpdateRestaurantCoins(20);
        handler.Save(data);

        File.WriteAllText(Path.Combine(tempDir, FileName), "{\"RestaurantData\": {\"Coi");
        LogAssert.Expect(LogType.Error, new Regex(@"\[SaveJson\] Error loading"));
        var loaded = handler.Load();

        Assert.IsNotNull(loaded);
        Assert.AreEqual(backupCoins, loaded.RestaurantData.Coins);
    }

    [Test] public void Load_ReturnsNull_WhenNoSaveExists()
    {
        var handler = new FileDataHandler(tempDir, FileName);
        Assert.IsNull(handler.Load());
    }

    [Test] public void TypeBinder_RejectsTypesOutsideTheGame()
    {
        string json = "{\"$type\":\"System.IO.DirectoryInfo, mscorlib\",\"OriginalPath\":\"C:/\"}";
        object result = JsonConvert.DeserializeObject<object>(json, SaveJson.CreateSettings());
        Assert.IsNotInstanceOf<DirectoryInfo>(result);
    }

    [Test] public void TypeBinder_AllowsGameTypes()
    {
        string json = "{\"$type\":\"ShopData, Scripts\"}";
        object result = JsonConvert.DeserializeObject<object>(json, SaveJson.CreateSettings());
        Assert.IsInstanceOf<ShopData>(result);
    }

    [Test] public void SaveWithoutVersionField_LoadsAsVersion1()
    {
        var loaded = JsonConvert.DeserializeObject<GameData>("{}", SaveJson.CreateSettings());
        Assert.AreEqual(1, loaded.SaveVersion);
    }

    [Test] public void Migrate_V1_GrowsGridArrayAndKeepsCells()
    {
        var data = new GameData();
        data.GridData.WidthMax = 10;
        data.GridData.HeightMax = 10;
        data.GridData.GridArrayData = new List<GridObjectData>[5, 5];
        var cell = new List<GridObjectData> { new GridObjectData("guid", new Vector2Int(1, 1), Dir.Down, InventoryTabType.Counter) };
        data.GridData.GridArrayData[1, 1] = cell;

        data.Migrate();

        Assert.AreEqual(10, data.GridData.GridArrayData.GetLength(0));
        Assert.AreEqual(10, data.GridData.GridArrayData.GetLength(1));
        Assert.AreSame(cell, data.GridData.GridArrayData[1, 1]);
        Assert.IsNotNull(data.GridData.GridArrayData[9, 9]);
        Assert.AreEqual(GameData.CURRENT_SAVE_VERSION, data.SaveVersion);
    }

    [Test] public void EnsureArraySize_KeepsArrayThatIsLargeEnough()
    {
        var gridData = new GridData { WidthMax = 4, HeightMax = 4 };
        var array = new List<GridObjectData>[4, 4];
        gridData.GridArrayData = array;

        gridData.EnsureArraySize();

        Assert.AreSame(array, gridData.GridArrayData);
    }

    [Test] public void PasswordHasher_HashIsVersionedAndNotThePassword()
    {
        string hashed = PasswordHasher.Hash("secret123");

        Assert.IsTrue(PasswordHasher.IsHashed(hashed));
        Assert.AreNotEqual("secret123", hashed);
        StringAssert.StartsWith("v1$", hashed);
    }

    [Test] public void PasswordHasher_VerifyAcceptsCorrectPassword()
    {
        string hashed = PasswordHasher.Hash("secret123");

        Assert.IsTrue(PasswordHasher.Verify("secret123", hashed));
        Assert.IsFalse(PasswordHasher.Verify("wrong", hashed));
        Assert.IsFalse(PasswordHasher.Verify("", hashed));
    }

    [Test] public void PasswordHasher_HashUsesFreshSaltEachTime()
    {
        Assert.AreNotEqual(PasswordHasher.Hash("secret123"), PasswordHasher.Hash("secret123"));
    }

    [Test] public void PasswordHasher_VerifyStillAcceptsLegacyPlaintext()
    {
        Assert.IsFalse(PasswordHasher.IsHashed("secret123"));
        Assert.IsTrue(PasswordHasher.Verify("secret123", "secret123"));
        Assert.IsFalse(PasswordHasher.Verify("other", "secret123"));
    }

    [Test] public void PasswordHasher_VerifyRejectsMissingOrMalformedStoredValue()
    {
        Assert.IsFalse(PasswordHasher.Verify("secret123", null));
        Assert.IsFalse(PasswordHasher.Verify("secret123", "v1$zz"));
        Assert.IsFalse(PasswordHasher.Verify("secret123", "v1$nothex$nothex"));
    }
}
