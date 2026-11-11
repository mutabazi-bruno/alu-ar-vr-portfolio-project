using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastShift.Menus
{
    /// <summary>
    /// Quitting from inside the game (the pause menu's QUIT) goes back to the title screen instead
    /// of closing the game; only the title screen's Quit really closes it. In the browser a page
    /// can't close itself, so going back to the title is the only sensible thing there anyway.
    /// </summary>
    public static class GameFlow
    {
        public const string TitleScene = "Title";
        static bool quitting;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Hook()
        {
            quitting = false;
            Application.wantsToQuit -= WantsToQuit;
            Application.wantsToQuit += WantsToQuit;
        }

        static bool TitleInBuild => SceneUtility.GetBuildIndexByScenePath(TitleScene) >= 0 ||
                                    SceneUtility.GetBuildIndexByScenePath("Assets/_LastShift/Scenes/" + TitleScene + ".unity") >= 0;

        static bool WantsToQuit()
        {
            if (quitting || SceneManager.GetActiveScene().name == TitleScene || !TitleInBuild)
                return true;
            ToTitle();
            return false;
        }

        public static void ToTitle() => SceneFader.Load(TitleScene);

        /// <summary>The title screen's Quit: closes the game for real.</summary>
        public static void QuitForReal()
        {
            quitting = true;
            Application.Quit();
        }
    }
}
