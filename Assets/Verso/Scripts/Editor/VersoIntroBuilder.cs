using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Verso.EditorTools
{
    /// <summary>
    /// Cena "Intro": diálogo entre Lume e Alminha explicando o conceito antes da Ilha 1.
    /// Menu Verso > Criar Intro (diálogo). Também roda dentro de Gerar Projeto.
    /// </summary>
    public static class VersoIntroBuilder
    {
        public const string VoiceFolder = "Assets/Verso/Audio/Voice";

        [MenuItem("Verso/Criar Intro (diálogo)", priority = 3)]
        public static void BuildMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            AssetDatabase.ImportAsset(VersoUIBuilder.UIFolder, ImportAssetOptions.ImportRecursive);
            string path = BuildIntroScene();
            AddToBuild(path);
            EditorSceneManager.OpenScene(path);
            Debug.Log("[Verso] Cena Intro criada e colocada no Build depois do MainMenu.");
        }

        public static string BuildIntroScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = VersoUIBuilder.Hex("0E0820");
            camGo.AddComponent<AudioListener>();

            BuildUI();
            VersoAssets.CreateEventSystem();

            VersoAssets.EnsureFolder(VersoAssets.SceneFolder);
            string path = $"{VersoAssets.SceneFolder}/{SceneFlow.Intro}.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        public static void AddToBuild(string path)
        {
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (list.Exists(s => s.path == path)) return;
            int menu = list.FindIndex(s => s.path.EndsWith("/" + SceneFlow.MainMenu + ".unity"));
            list.Insert(menu >= 0 ? menu + 1 : 0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        private static void BuildUI()
        {
            var S = (System.Func<string, Sprite>)VersoUIBuilder.S;
            var canvas = VersoAssets.CreateCanvas("IntroUI");
            Transform root = canvas.transform;
            var group = canvas.gameObject.AddComponent<CanvasGroup>();
            var center = new Vector2(0.5f, 0.5f);

            // Fundo do menu escurecido
            var bg = VersoUIBuilder.Img(root, "Background", S("menu_background"), center, center, Vector2.zero);
            var fit = bg.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = 1920f / 1080f;
            var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image));
            dim.transform.SetParent(root, false);
            VersoUIBuilder.Stretch((RectTransform)dim.transform, Vector2.zero, Vector2.zero);
            var dimImg = dim.GetComponent<Image>();
            dimImg.color = new Color(0.055f, 0.03f, 0.125f, 0.55f);
            dimImg.raycastTarget = false;

            // Retratos (renders dos modelos 3D)
            var left = VersoUIBuilder.Img(root, "Portrait_Lume", S("intro_lume"), Vector2.zero, Vector2.zero, new Vector2(150f, 250f), new Vector2(560f, 560f));
            left.preserveAspect = true;
            left.gameObject.AddComponent<UIFloat>().Setup(6f, 1.1f, 0f);
            var right = VersoUIBuilder.Img(root, "Portrait_Direita", S("intro_alminha"), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-170f, 270f), new Vector2(520f, 520f));
            right.preserveAspect = true;
            right.gameObject.AddComponent<UIFloat>().Setup(10f, 1.4f, 1.2f);

            // Caixa de diálogo
            var box = VersoUIBuilder.Img(root, "DialogueBox", S("message_strip"), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f));
            var boxRt = box.rectTransform;
            boxRt.anchorMin = new Vector2(0f, 0f); boxRt.anchorMax = new Vector2(1f, 0f);
            boxRt.sizeDelta = new Vector2(-120f, 290f);
            box.raycastTarget = false;

            var tag = VersoUIBuilder.Img(box.transform, "NameTag", S("counter"), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(120f, 0f), new Vector2(330f, 100f));
            var name = VersoUIBuilder.Label(tag.transform, "Name", "Wisp", VersoUIBuilder.Display, 44, VersoUIBuilder.Cream, TextAnchor.MiddleCenter, 3f, 3f);
            VersoUIBuilder.Stretch(name.rectTransform, new Vector2(16f, 8f), new Vector2(-16f, -4f));

            var body = VersoUIBuilder.Label(box.transform, "Text", "", VersoUIBuilder.Body, 40, VersoUIBuilder.Cream, TextAnchor.UpperLeft, 2f);
            VersoUIBuilder.Stretch(body.rectTransform, new Vector2(150f, 50f), new Vector2(-150f, -80f));
            body.lineSpacing = 1.15f;

            var hint = VersoUIBuilder.Label(box.transform, "ContinueHint", "Espaço ou clique para continuar", VersoUIBuilder.Body, 24,
                new Color(VersoUIBuilder.Ghost.r, VersoUIBuilder.Ghost.g, VersoUIBuilder.Ghost.b, 0.9f), TextAnchor.LowerRight, 2f);
            VersoUIBuilder.Place(hint.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-70f, 28f), new Vector2(600f, 36f));
            hint.gameObject.AddComponent<UIFloat>().Setup(4f, 3f, 0f);

            // Pular
            var skip = VersoUIBuilder.Btn(root, "SkipButton", "btn_ghost", "Pular", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-48f, -40f));
            skip.transform.localScale = Vector3.one * 0.6f;

            var dlg = canvas.gameObject.AddComponent<IntroDialogue>();
            dlg.Bind(group, name, body, left, right, S("intro_alminha"), S("intro_misterio"), tag.rectTransform, hint.gameObject, skip);

            // Áudio: vozes (Assets/Verso/Audio/Voice/intro_01..14) e espaço pra música de fundo baixinha
            var voiceGo = new GameObject("Voice", typeof(AudioSource));
            voiceGo.transform.SetParent(root, false);
            var voice = voiceGo.GetComponent<AudioSource>();
            voice.playOnAwake = false; voice.spatialBlend = 0f; voice.volume = 1f;
            var musicGo = new GameObject("Music", typeof(AudioSource));
            musicGo.transform.SetParent(root, false);
            var music = musicGo.GetComponent<AudioSource>();
            music.playOnAwake = false; music.loop = true; music.spatialBlend = 0f; music.volume = 0.12f;
            VersoUIBuilder.NoLoc(name); // o nome é trocado pelo IntroDialogue a cada fala
            var clips = new List<AudioClip>();
            var clipsEn = new List<AudioClip>();
            for (int i = 1; i <= 14; i++)
            {
                clips.Add(AssetDatabase.LoadAssetAtPath<AudioClip>($"{VoiceFolder}/intro_{i:00}.mp3"));
                clipsEn.Add(AssetDatabase.LoadAssetAtPath<AudioClip>($"{VoiceFolder}/intro_en_{i:00}.mp3"));
            }
            dlg.BindAudio(voice, music, clips.ToArray(), clipsEn.ToArray());
        }
    }
}
