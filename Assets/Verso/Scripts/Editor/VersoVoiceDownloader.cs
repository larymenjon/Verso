using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Networking;

namespace Verso.EditorTools
{
    /// <summary>
    /// Baixa as falas da intro geradas no ElevenLabs (links em Audio/Voice/vozes_intro.json)
    /// para Assets/Verso/Audio/Voice e refaz a cena Intro já com as vozes ligadas.
    /// Os links do ElevenLabs expiram em poucas horas; se der erro 403, peça links novos.
    /// </summary>
    public static class VersoVoiceDownloader
    {
        [System.Serializable] private class Entry { public string name; public string url; }
        [System.Serializable] private class Manifest { public Entry[] files; }

        [MenuItem("Verso/Baixar vozes da intro (ElevenLabs)", priority = 4)]
        public static void Download()
        {
            string folder = VersoIntroBuilder.VoiceFolder;
            string jsonPath = folder + "/vozes_intro.json";
            if (!File.Exists(jsonPath)) { Debug.LogWarning($"[Verso] {jsonPath} não existe."); return; }
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(jsonPath));
            if (manifest?.files == null || manifest.files.Length == 0) return;

            int ok = 0;
            try
            {
                for (int i = 0; i < manifest.files.Length; i++)
                {
                    var e = manifest.files[i];
                    EditorUtility.DisplayProgressBar("Verso", $"Baixando {e.name}", (float)i / manifest.files.Length);
                    using (var req = UnityWebRequest.Get(e.url))
                    {
                        var op = req.SendWebRequest();
                        while (!op.isDone) System.Threading.Thread.Sleep(20);
                        if (req.result != UnityWebRequest.Result.Success)
                        {
                            Debug.LogError($"[Verso] Falhou {e.name}: {req.error} (o link pode ter expirado)");
                            continue;
                        }
                        File.WriteAllBytes($"{folder}/{e.name}", req.downloadHandler.data);
                        ok++;
                    }
                }
            }
            finally { EditorUtility.ClearProgressBar(); }

            AssetDatabase.Refresh();
            Debug.Log($"[Verso] {ok}/{manifest.files.Length} vozes baixadas.");
            if (ok == 0) return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string path = VersoIntroBuilder.BuildIntroScene();
            VersoIntroBuilder.AddToBuild(path);
            EditorSceneManager.OpenScene(path);
            Debug.Log("[Verso] Cena Intro refeita com as vozes.");
        }
    }
}
