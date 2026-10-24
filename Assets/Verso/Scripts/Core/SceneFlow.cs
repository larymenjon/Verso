using UnityEngine;
using UnityEngine.SceneManagement;

namespace Verso
{
    /// <summary>Nomes das cenas e navegação entre elas.</summary>
    public static class SceneFlow
    {
        public const string MainMenu = "MainMenu";
        public const string Intro = "Intro";
        public const string Island1 = "Island1";
        public const string Island2 = "Island2";
        public const string Island3 = "Island3";
        public const string Island4 = "Island4";
        public const string Island5 = "Island5";
        public const string End = "End";

        public static readonly string[] Order = { MainMenu, Intro, Island1, Island2, Island3, Island4, Island5, End };

        public static void Load(string sceneName)
        {
            Time.timeScale = 1f;
            GameUI.IsPaused = false;
            AudioListener.pause = false;
            SceneManager.LoadScene(sceneName);
        }

        public static void ReloadCurrent()
        {
            Load(SceneManager.GetActiveScene().name);
        }

        public static void LoadNext()
        {
            string current = SceneManager.GetActiveScene().name;
            int idx = System.Array.IndexOf(Order, current);
            string next = (idx >= 0 && idx < Order.Length - 1) ? Order[idx + 1] : MainMenu;
            Load(next);
        }

        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
