using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Verso.EditorTools
{
    /// <summary>
    /// Coloca os personagens 3D (Dia e Noite, feitos no Blender) nos prefabs Player e Echo.
    /// Menu Verso > Aplicar Personagens 3D. Também roda no fim de Verso > Gerar Projeto.
    /// </summary>
    public static class VersoCharacterBuilder
    {
        public const string Folder = VersoAssets.Root + "/Characters";
        private const string DayFbx = Folder + "/Verso_Dia.fbx";
        private const string NightFbx = Folder + "/Verso_Noite.fbx";
        private const string DayTex = Folder + "/Verso_Dia_Paleta.png";
        private const string NightTex = Folder + "/Verso_Noite_Paleta.png";
        private const string ControllerPath = Folder + "/AC_Verso.controller";
        private const string SoulFbx = Folder + "/Verso_Alma.fbx";
        private const string SoulTex = Folder + "/Verso_Alma_Paleta.png";
        private const string SoulControllerPath = Folder + "/AC_Alma.controller";
        private const string KeyFbx = Folder + "/Verso_Chave.fbx";
        private const string KeyTex = Folder + "/Verso_Chave_Paleta.png";
        /// <summary>Escala da chave no portal (0,6 m no Blender).</summary>
        private const float KeyScale = 2.2f;
        /// <summary>Escala da alma coletável (0,6 m no Blender).</summary>
        private const float SoulScale = 1.3f;

        /// <summary>Escala do modelo (1,65 m no Blender) para caber na cápsula de 2 m.</summary>
        private const float ModelScale = 1.15f;

        private static readonly string[] LoopClips = { "Idle", "Run", "Fall", "Celebrate" };

        [MenuItem("Verso/Aplicar Personagens 3D", priority = 2)]
        public static void ApplyMenu()
        {
            if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (ApplyToPrefabs())
            {
                FixCollectiblesInScenes();
                Debug.Log("[Verso] Personagens 3D aplicados: Player, Echo e almas coletáveis.");
            }
        }

        public static bool ApplyToPrefabs()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(DayFbx) == null || AssetDatabase.LoadAssetAtPath<GameObject>(NightFbx) == null)
            {
                Debug.LogWarning($"[Verso] FBX dos personagens não encontrados em {Folder}.");
                return false;
            }

            SetupTexture(DayTex);
            SetupTexture(NightTex);
            SetupModel(DayFbx);
            SetupModel(NightFbx);

            Material dayMat = MakeMaterial("M_Char_Dia", DayTex);
            Material nightMat = MakeMaterial("M_Char_Noite", NightTex);
            var controller = BuildController();

            var dayModel = AssetDatabase.LoadAssetAtPath<GameObject>(DayFbx);
            var nightModel = AssetDatabase.LoadAssetAtPath<GameObject>(NightFbx);

            ApplyTo(VersoAssets.PrefabFolder + "/Player.prefab", CharacterVisual.Mode.Player, dayModel, nightModel, dayMat, nightMat, controller);
            ApplyTo(VersoAssets.PrefabFolder + "/Echo.prefab", CharacterVisual.Mode.Echo, dayModel, nightModel, dayMat, nightMat, controller);
            ApplySoul();
            ApplyKey();
            AssetDatabase.SaveAssets();
            return true;
        }

        // ---------------------------------------------------------------- Import

        private static void SetupTexture(string path)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) return;
            bool dirty = ti.textureType != TextureImporterType.Default || ti.mipmapEnabled || ti.textureCompression != TextureImporterCompression.Uncompressed;
            if (!dirty) return;
            ti.textureType = TextureImporterType.Default;
            ti.sRGBTexture = true;
            ti.mipmapEnabled = false;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Bilinear;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
        }

        private static string ShortName(string clipName)
        {
            // "Rig|Dia_Idle" -> "Idle"
            int bar = clipName.LastIndexOf('|');
            string n = bar >= 0 ? clipName.Substring(bar + 1) : clipName;
            int us = n.IndexOf('_');
            return us >= 0 ? n.Substring(us + 1) : n;
        }

        private static void SetupModel(string path)
        {
            var mi = AssetImporter.GetAtPath(path) as ModelImporter;
            if (mi == null) return;
            mi.animationType = ModelImporterAnimationType.Generic;
            mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            mi.materialImportMode = ModelImporterMaterialImportMode.None;
            mi.importAnimation = true;
            mi.importBlendShapes = false;
            mi.importCameras = false;
            mi.importLights = false;

            var clips = mi.defaultClipAnimations;
            var list = new List<ModelImporterClipAnimation>();
            foreach (var c in clips)
            {
                string shortName = ShortName(c.takeName);
                c.name = shortName;
                c.loopTime = System.Array.IndexOf(LoopClips, shortName) >= 0;
                c.loopPose = false;
                c.lockRootRotation = false;
                c.keepOriginalPositionY = true;
                list.Add(c);
            }
            mi.clipAnimations = list.ToArray();
            mi.SaveAndReimport();
        }

        private static Material MakeMaterial(string name, string texPath)
        {
            string path = $"{Folder}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool isNew = m == null;
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Standard");
            if (isNew) m = new Material(s);

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            m.SetTexture("_BaseMap", tex);
            m.SetTexture("_MainTex", tex);
            m.SetColor("_BaseColor", Color.white);
            m.SetColor("_Color", Color.white);
            m.SetFloat("_Smoothness", 0.6f);
            m.SetFloat("_Metallic", 0f);
            // Brilho leve de "chama" usando a própria paleta
            m.EnableKeyword("_EMISSION");
            m.SetTexture("_EmissionMap", tex);
            m.SetColor("_EmissionColor", new Color(0.3f, 0.3f, 0.3f));
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

            if (isNew) AssetDatabase.CreateAsset(m, path);
            else EditorUtility.SetDirty(m);
            return m;
        }

        // ---------------------------------------------------------------- Alma coletável

        /// <summary>Troca o cubo do prefab Crystal pela alma 3D animada.</summary>
        public static void ApplySoul()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(SoulFbx);
            string prefabPath = VersoAssets.PrefabFolder + "/Crystal.prefab";
            if (model == null || AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                Debug.LogWarning($"[Verso] Alma não aplicada: falta {SoulFbx} ou {prefabPath}.");
                return;
            }

            SetupTexture(SoulTex);
            SetupModel(SoulFbx);
            Material mat = MakeMaterial("M_Alma", SoulTex);
            mat.SetColor("_EmissionColor", new Color(0.7f, 0.7f, 0.7f));
            EditorUtility.SetDirty(mat);

            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(SoulControllerPath) != null)
                AssetDatabase.DeleteAsset(SoulControllerPath);
            var ac = AnimatorController.CreateAnimatorControllerAtPath(SoulControllerPath);
            var idle = ac.layers[0].stateMachine.AddState("Idle");
            idle.motion = Clip(SoulFbx, "Idle");
            ac.layers[0].stateMachine.defaultState = idle;
            EditorUtility.SetDirty(ac);

            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                // tira o cubo do blockout
                var mr = root.GetComponent<MeshRenderer>(); if (mr != null) Object.DestroyImmediate(mr);
                var mf = root.GetComponent<MeshFilter>(); if (mf != null) Object.DestroyImmediate(mf);
                root.transform.localRotation = Quaternion.identity;
                root.transform.localScale = Vector3.one;
                var old = root.transform.Find("Model");
                if (old != null) Object.DestroyImmediate(old.gameObject);

                var go = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
                go.name = "Model";
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one * SoulScale;
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                {
                    r.sharedMaterial = mat;
                    if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = true;
                }
                var anim = go.GetComponent<Animator>();
                if (anim == null) anim = go.AddComponent<Animator>();
                anim.runtimeAnimatorController = ac;
                anim.applyRootMotion = false;
                // o layer vem do WorldObject no Awake (Dia/Noite)
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>Coloca a chave dourada dentro do prefab Portal (aparece quando a ilha libera).</summary>
        public static void ApplyKey()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(KeyFbx);
            string prefabPath = VersoAssets.PrefabFolder + "/Portal.prefab";
            if (model == null || AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                Debug.LogWarning($"[Verso] Chave não aplicada: falta {KeyFbx} ou {prefabPath}.");
                return;
            }

            SetupTexture(KeyTex);
            var mi = AssetImporter.GetAtPath(KeyFbx) as ModelImporter;
            if (mi != null && (mi.materialImportMode != ModelImporterMaterialImportMode.None || mi.importAnimation))
            {
                mi.materialImportMode = ModelImporterMaterialImportMode.None;
                mi.importAnimation = false;
                mi.animationType = ModelImporterAnimationType.None;
                mi.SaveAndReimport();
            }
            Material mat = MakeMaterial("M_Chave", KeyTex);
            mat.SetFloat("_Metallic", 0.35f);
            mat.SetFloat("_Smoothness", 0.75f);
            mat.SetColor("_EmissionColor", new Color(0.45f, 0.45f, 0.45f));
            EditorUtility.SetDirty(mat);

            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var old = root.transform.Find("Key");
                if (old != null) Object.DestroyImmediate(old.gameObject);
                var go = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
                go.name = "Key";
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one * KeyScale;
                foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial = mat;

                var portal = root.GetComponent<PortalExit>();
                if (portal != null)
                {
                    VersoAssets.SetObject(portal, "keyVisual", go.transform);
                    // o anel e o núcleo continuam sendo os únicos tingidos pelo portal
                    var tint = new System.Collections.Generic.List<Renderer>();
                    foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                        if (!r.transform.IsChildOf(go.transform)) tint.Add(r);
                    VersoAssets.SetObjectArray(portal, "visuals", tint.ToArray());
                }
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>As ilhas já geradas guardam a rotação 45° do cubo antigo; deixa as almas em pé.</summary>
        private static void FixCollectiblesInScenes()
        {
            string current = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            foreach (string name in new[] { SceneFlow.Island1, SceneFlow.Island2, SceneFlow.Island3, SceneFlow.Island4, SceneFlow.Island5 })
            {
                string path = $"{VersoAssets.SceneFolder}/{name}.unity";
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) continue;
                var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Single);
                foreach (var c in Object.FindObjectsByType<Collectible>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    c.transform.rotation = Quaternion.identity;
                    c.transform.localScale = Vector3.one;
                }
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            }
            if (!string.IsNullOrEmpty(current)) UnityEditor.SceneManagement.EditorSceneManager.OpenScene(current);
        }

        private static AnimationClip Clip(string fbx, string name)
        {
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(fbx))
                if (o is AnimationClip c && !c.name.StartsWith("__preview__") && c.name == name) return c;
            Debug.LogWarning($"[Verso] Clipe {name} não encontrado em {fbx}");
            return null;
        }

        // ---------------------------------------------------------------- Animator

        private static AnimatorController BuildController()
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
                AssetDatabase.DeleteAsset(ControllerPath);

            var ac = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ac.AddParameter(new AnimatorControllerParameter { name = "Grounded", type = AnimatorControllerParameterType.Bool, defaultBool = true });
            ac.AddParameter("VSpeed", AnimatorControllerParameterType.Float);
            ac.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
            ac.AddParameter("Flip", AnimatorControllerParameterType.Trigger);
            ac.AddParameter("Celebrate", AnimatorControllerParameterType.Bool);

            // As duas malhas têm o mesmo rig e os mesmos caminhos, então os clipes do Dia servem pros dois.
            var idle = Clip(DayFbx, "Idle");
            var run = Clip(DayFbx, "Run");
            var jump = Clip(DayFbx, "Jump");
            var fall = Clip(DayFbx, "Fall");
            var flip = Clip(DayFbx, "Flip");
            var celebrate = Clip(DayFbx, "Celebrate");

            var sm = ac.layers[0].stateMachine;

            var loco = ac.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            tree.AddChild(idle, 0f);
            tree.AddChild(run, 1f);
            sm.defaultState = loco;

            var sJump = sm.AddState("Jump", new Vector3(560, -120, 0)); sJump.motion = jump;
            var sFall = sm.AddState("Fall", new Vector3(560, 0, 0)); sFall.motion = fall;
            var sFlip = sm.AddState("Flip", new Vector3(300, 160, 0)); sFlip.motion = flip;
            var sCel = sm.AddState("Celebrate", new Vector3(560, 160, 0)); sCel.motion = celebrate;

            // Pulo
            var t = sm.AddAnyStateTransition(sJump);
            t.AddCondition(AnimatorConditionMode.If, 0, "Jump");
            t.canTransitionToSelf = false; t.duration = 0.05f;

            t = sJump.AddTransition(sFall);
            t.hasExitTime = true; t.exitTime = 0.95f; t.duration = 0.12f;

            t = sJump.AddTransition(loco);
            t.AddCondition(AnimatorConditionMode.If, 0, "Grounded");
            t.hasExitTime = true; t.exitTime = 0.4f; t.duration = 0.1f;

            // Queda
            t = loco.AddTransition(sFall);
            t.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");
            t.AddCondition(AnimatorConditionMode.Less, -2f, "VSpeed");
            t.hasExitTime = false; t.duration = 0.15f;

            t = sFall.AddTransition(loco);
            t.AddCondition(AnimatorConditionMode.If, 0, "Grounded");
            t.hasExitTime = false; t.duration = 0.08f;

            // Virar o mundo
            t = sm.AddAnyStateTransition(sFlip);
            t.AddCondition(AnimatorConditionMode.If, 0, "Flip");
            t.canTransitionToSelf = false; t.duration = 0.05f;

            t = sFlip.AddTransition(loco);
            t.hasExitTime = true; t.exitTime = 0.95f; t.duration = 0.1f;

            // Comemoração
            t = sm.AddAnyStateTransition(sCel);
            t.AddCondition(AnimatorConditionMode.If, 0, "Celebrate");
            t.canTransitionToSelf = false; t.duration = 0.15f;

            t = sCel.AddTransition(loco);
            t.AddCondition(AnimatorConditionMode.IfNot, 0, "Celebrate");
            t.hasExitTime = false; t.duration = 0.15f;

            EditorUtility.SetDirty(ac);
            return ac;
        }

        // ---------------------------------------------------------------- Prefabs

        private static GameObject AddModel(Transform parent, GameObject model, string name, Material mat, RuntimeAnimatorController controller)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
            go.name = name;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one * ModelScale;

            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                r.sharedMaterial = mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = true;
            }

            var anim = go.GetComponent<Animator>();
            if (anim == null) anim = go.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            return go;
        }

        private static void ApplyTo(string prefabPath, CharacterVisual.Mode mode, GameObject dayModel, GameObject nightModel,
            Material dayMat, Material nightMat, RuntimeAnimatorController controller)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                Debug.LogWarning($"[Verso] {prefabPath} não existe. Rode Verso > Gerar Projeto primeiro.");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                // tira o blockout (cápsula + nariz) e um modelo antigo, se houver
                foreach (string n in new[] { "Body", "Nose", "Model" })
                {
                    var old = root.transform.Find(n);
                    if (old != null) Object.DestroyImmediate(old.gameObject);
                }

                var holder = new GameObject("Model").transform;
                holder.SetParent(root.transform, false);
                var day = AddModel(holder, dayModel, "Dia", dayMat, controller);
                var night = AddModel(holder, nightModel, "Noite", nightMat, controller);
                night.SetActive(false);

                var cv = root.GetComponent<CharacterVisual>();
                if (cv == null) cv = root.AddComponent<CharacterVisual>();
                VersoAssets.SetEnum(cv, "mode", (int)mode);
                VersoAssets.SetObject(cv, "dayModel", day);
                VersoAssets.SetObject(cv, "nightModel", night);

                var echo = root.GetComponent<EchoGhost>();
                if (echo != null)
                {
                    VersoAssets.SetObjectArray(echo, "visuals", root.GetComponentsInChildren<Renderer>(true));
                    // O Eco já usa o modelo do outro lado; a cor dele só reforça o brilho.
                    var so = new SerializedObject(echo);
                    so.FindProperty("dayColor").colorValue = new Color(1f, 0.9f, 0.75f);
                    so.FindProperty("nightColor").colorValue = new Color(0.75f, 0.9f, 1f);
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
