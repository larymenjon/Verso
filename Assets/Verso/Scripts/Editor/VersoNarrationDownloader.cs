using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Verso.EditorTools
{
    /// <summary>
    /// Baixa a narração em inglês do vídeo de gameplay (links em Docs/itch/narration/narracao.json)
    /// para Docs/itch/narration. Fica fora de Assets, então não entra no jogo.
    /// Os links do ElevenLabs expiram em cerca de 2 horas; se der erro, peça links novos.
    /// </summary>
    public static class VersoNarrationDownloader
    {
        [System.Serializable] private class Entry { public string name; public string url; }
        [System.Serializable] private class Manifest { public Entry[] files; }

        [MenuItem("Verso/Baixar narração do vídeo (ElevenLabs)", priority = 41)]
        public static void Download()
        {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/itch/narration"));
            string jsonPath = Path.Combine(folder, "narracao.json");
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
                        File.WriteAllBytes(Path.Combine(folder, e.name), req.downloadHandler.data);
                        ok++;
                    }
                }
            }
            finally { EditorUtility.ClearProgressBar(); }

            Debug.Log($"[Verso] {ok}/{manifest.files.Length} trechos de narração baixados em {folder}");
            EditorUtility.RevealInFinder(folder);
        }
    }
}
