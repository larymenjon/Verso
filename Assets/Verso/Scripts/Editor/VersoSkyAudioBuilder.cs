using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Verso.EditorTools
{
    /// <summary>
    /// Céu PSX (skybox Vivos/Além) e músicas de cada momento do jogo.
    /// Menu Verso > Aplicar Céu e Músicas. Também roda no fim de Gerar Projeto.
    /// </summary>
    public static class VersoSkyAudioBuilder
    {
        public const string SkyFolder = VersoAssets.Root + "/Sky";
        public const string MusicFolder = VersoAssets.Root + "/Audio/Music";
        private const string DaySkyTex = SkyFolder + "/Sky_Vivos_SunsetOrange.png";
        private const string NightSkyTex = SkyFolder + "/Sky_Alem_FullMoonNight.png";

        // Músicas por momento (Audio/Music)
        public const string MusicDay = MusicFolder + "/ilha_vivos.mp3";
        public const string MusicNight = MusicFolder + "/ilha_alem.mp3";
        public const string MusicIntro = MusicFolder + "/intro_dialogo.mp3";
        public const string MusicEnd = MusicFolder + "/final.mp3";
        public const string SfxFolder = VersoAssets.Root + "/Audio/SFX";
        public const string MusicGameOver = SfxFolder + "/sfx_fim_de_jogo.mp3";
        public const string SfxJump = SfxFolder + "/sfx_pulo.mp3";
        public const string SfxCollect = SfxFolder + "/sfx_coleta.mp3";   // chave
        public const string SfxSoul = SfxFolder + "/sfx_alma.mp3";        // alma
        public const string SfxLevelClear = SfxFolder + "/sfx_passou_de_fase.mp3";

        [MenuItem("Verso/Aplicar Céu e Músicas", priority = 5)]
        public static void ApplyMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ApplyToSystems();
            ApplyToScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[Verso] Céu e músicas aplicados.");
        }

        // ---------------------------------------------------------------- Céu

        private static void SetupSkyTexture(string path)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) return;
            ti.textureType = TextureImporterType.Default;
            ti.textureShape = TextureImporterShape.Texture2D;
            ti.mipmapEnabled = false;
            ti.filterMode = FilterMode.Point;           // pixel PSX "crocante"
            ti.wrapModeU = TextureWrapMode.Repeat;
            ti.wrapModeV = TextureWrapMode.Clamp;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.SaveAndReimport();
        }

        private static Material SkyMaterial(string name, string texPath, float rotation)
        {
            SetupSkyTexture(texPath);
            string path = $"{SkyFolder}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool isNew = m == null;
            if (isNew) m = new Material(Shader.Find("Skybox/Panoramic"));
            m.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
            m.SetFloat("_Mapping", 1f);     // Latitude/Longitude
            m.SetFloat("_ImageType", 0f);   // 360°
            m.SetFloat("_Exposure", 1f);
            m.SetFloat("_Rotation", rotation);
            m.SetColor("_Tint", new Color(0.5f, 0.5f, 0.5f));
            if (isNew) AssetDatabase.CreateAsset(m, path);
            else EditorUtility.SetDirty(m);
            return m;
        }

        // ---------------------------------------------------------------- Systems (WorldManager + AudioManager)

        public static void ApplyToSystems()
        {
            string prefabPath = VersoAssets.PrefabFolder + "/Systems.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null) return;

            Material day = AssetDatabase.LoadAssetAtPath<Texture2D>(DaySkyTex) != null ? SkyMaterial("M_Sky_Vivos", DaySkyTex, 0f) : null;
            Material night = AssetDatabase.LoadAssetAtPath<Texture2D>(NightSkyTex) != null ? SkyMaterial("M_Sky_Alem", NightSkyTex, 0f) : null;

            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var wm = root.GetComponent<WorldManager>();
                if (wm != null && day != null && night != null)
                {
                    var so = new SerializedObject(wm);
                    so.FindProperty("daySkybox").objectReferenceValue = day;
                    so.FindProperty("nightSkybox").objectReferenceValue = night;
                    // névoa na cor do horizonte de cada céu, pra o cenário longe "sumir" no céu
                    so.FindProperty("dayFog").colorValue = VersoUIBuilder.Hex("8E4F5E");
                    so.FindProperty("nightFog").colorValue = VersoUIBuilder.Hex("161C36");
                    so.FindProperty("dayAmbient").colorValue = VersoUIBuilder.Hex("C9A49A");
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                var am = root.GetComponent<AudioManager>();
                if (am != null)
                {
                    var so = new SerializedObject(am);
                    SetClip(so, "musicDay", MusicDay);
                    SetClip(so, "musicNight", MusicNight);
                    SetClip(so, "musicGameOver", MusicGameOver);
                    // efeitos: pulo, coleta de alma e chave, passou de fase
                    SetSfx(so, SfxType.Jump, SfxJump, 0.7f);
                    SetSfx(so, SfxType.Crystal, SfxSoul, 0.85f);
                    SetSfx(so, SfxType.KeyPickup, SfxCollect, 0.9f);
                    SetSfx(so, SfxType.PortalEnter, SfxLevelClear, 1f);
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void SetSfx(SerializedObject so, SfxType type, string path, float volume)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) { Debug.LogWarning($"[Verso] Efeito não encontrado: {path}"); return; }
            var arr = so.FindProperty("sfx");
            if (arr == null) return;
            int idx = -1;
            for (int i = 0; i < arr.arraySize; i++)
                if (arr.GetArrayElementAtIndex(i).FindPropertyRelative("type").enumValueIndex == (int)type) { idx = i; break; }
            if (idx < 0) { idx = arr.arraySize; arr.arraySize++; }
            var e = arr.GetArrayElementAtIndex(idx);
            e.FindPropertyRelative("type").enumValueIndex = (int)type;
            e.FindPropertyRelative("clip").objectReferenceValue = clip;
            e.FindPropertyRelative("volume").floatValue = volume;
        }

        private static void SetClip(SerializedObject so, string field, string path)
        {
            var p = so.FindProperty(field);
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (p == null) return;
            if (clip == null) { Debug.LogWarning($"[Verso] Música não encontrada: {path}"); return; }
            p.objectReferenceValue = clip;
        }

        // ---------------------------------------------------------------- Intro e Final

        public static void ApplyToScenes()
        {
            string current = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;

            // Intro: música baixinha no AudioSource "Music"
            SetSceneMusic($"{VersoAssets.SceneFolder}/{SceneFlow.Intro}.unity", "IntroUI/Music", MusicIntro, 0.12f, false);
            // Final: música na cena End
            SetSceneMusic($"{VersoAssets.SceneFolder}/{SceneFlow.End}.unity", "EndUI/Music", MusicEnd, 0.5f, true);

            if (!string.IsNullOrEmpty(current)) EditorSceneManager.OpenScene(current);
        }

        private static void SetSceneMusic(string scenePath, string objectPath, string clipPath, float volume, bool createIfMissing)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null) return;
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            if (clip == null) { Debug.LogWarning($"[Verso] Música não encontrada: {clipPath}"); return; }
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var go = GameObject.Find(objectPath);
            if (go == null && createIfMissing)
            {
                string parentName = objectPath.Split('/')[0];
                var parent = GameObject.Find(parentName);
                go = new GameObject("Music");
                if (parent != null) go.transform.SetParent(parent.transform, false);
            }
            if (go == null) return;
            var src = go.GetComponent<AudioSource>();
            if (src == null) src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.loop = true;
            src.spatialBlend = 0f;
            src.volume = volume;
            src.playOnAwake = true;
            // a música de fundo da intro já respeita o volume pelo IntroDialogue; nas outras, o VolumeBinder cuida
            if (go.GetComponentInParent<IntroDialogue>() == null)
            {
                var vb = go.GetComponent<VolumeBinder>();
                if (vb == null) vb = go.AddComponent<VolumeBinder>();
                vb.Setup(VolumeBinder.Kind.Music, volume);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
