using Unity.Netcode;
using UnityEngine.SceneManagement;

public static class Loader
{
    public enum Scene
    {
        MainMenuScene,
        GameScene,
    }

    private static Scene targetScene;

    public static void Load(Scene targetScene)
    {
        Loader.targetScene = targetScene;
        UIPopupManager.Instance.ShowPopup(UIPopupType.UILoadingPopup);
    }
    public static void LoaderCallback()
    {
        SceneManager.LoadScene(targetScene.ToString());
        if(targetScene == Scene.MainMenuScene)
            UIPopupManager.Instance.ShowPopup(UIPopupType.UIMainMenuPopup);

    }
    public static void LoadNetwork(Scene targetScene)
    {
        NetworkManager.Singleton.SceneManager.LoadScene(targetScene.ToString(), LoadSceneMode.Single);
    }
}
