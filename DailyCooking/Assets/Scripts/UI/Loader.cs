using UnityEngine;
using UnityEngine.SceneManagement;

public static class Loader
{
    public enum Scene
    {
        MainMenuScene,
        GameScene,
        LoadingScene,
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
}
