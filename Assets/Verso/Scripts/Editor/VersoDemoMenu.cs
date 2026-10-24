using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Verso.EditorTools
{
    /// <summary>
    /// Menu Verso > Gravar demo: abre o MainMenu e entra no Play com o piloto automático (DemoDirector).
    /// Grava Recordings/verso_gameplay.mp4 (DemoCapture, sem pacote extra), os screenshots
    /// e a linha do tempo (demo_timeline.json) em Recordings/.
    /// </summary>
    public static class VersoDemoMenu
    {
        [MenuItem("Verso/Gravar demo (vídeo + screenshots)", priority = 6)]
        public static void Record()
        {
            if (EditorApplication.isPlaying) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene($"{VersoAssets.SceneFolder}/{SceneFlow.MainMenu}.unity");
            PlayerPrefs.SetInt(DemoDirector.PrefKey, 1);
            PlayerPrefs.Save();
            Debug.Log("[Verso Demo] Começando. Deixe a janela Game em 16:9 e não mexa no teclado até o Play parar sozinho.");
            EditorApplication.isPlaying = true;
        }
    }
}
