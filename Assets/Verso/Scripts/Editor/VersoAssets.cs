using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Verso.EditorTools
{
    /// <summary>
    /// Cria layers, materiais e prefabs de blockout (cubos, planos, esferas, cápsulas).
    /// Tudo fica em Assets/Verso para depois ser trocado pela arte final.
    /// </summary>
    public static class VersoAssets
    {
        public const string Root = "Assets/Verso";
        public const string MatFolder = Root + "/Materials";
        public const string PrefabFolder = Root + "/Prefabs";
        public const string SceneFolder = Root + "/Scenes";

        // Materiais
        public static Material Ground, Day, Night, Wall, Crystal, CrystalDay, CrystalNight, Hazard, PlayerMat, EchoMat, Portal, Plate, DoorMat, Ghost, UIDummy;

        // Prefabs
        public static GameObject PlayerPrefab, CameraPrefab, SystemsPrefab, GameUIPrefab, CrystalPrefab, PortalPrefab, PlatePrefab, DoorPrefab, EchoPrefab, SpikesPrefab;

        private static Font font;
        public static Font UIFont
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        // ---------------------------------------------------------------- Pastas e layers

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            string name = System.IO.Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        public static void EnsureLayers()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) return;
            var so = new SerializedObject(assets[0]);
            var layers = so.FindProperty("layers");
            string[] wanted = { WorldLayers.PlayerLayerName, WorldLayers.DayLayerName, WorldLayers.NightLayerName };
            foreach (string w in wanted)
            {
                bool exists = false;
                for (int i = 0; i < layers.arraySize; i++)
                    if (layers.GetArrayElementAtIndex(i).stringValue == w) { exists = true; break; }
                if (exists) continue;
                for (int i = 8; i < layers.arraySize; i++)
                {
                    var p = layers.GetArrayElementAtIndex(i);
                    if (string.IsNullOrEmpty(p.stringValue)) { p.stringValue = w; break; }
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        // ---------------------------------------------------------------- Materiais

        private static Shader LitShader()
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Standard");
            return s;
        }

        private static Material MakeMaterial(string name, Color color, float emission = 0f, bool transparent = false)
        {
            string path = $"{MatFolder}/{name}.mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool isNew = m == null;
            if (isNew) m = new Material(LitShader());

            m.SetColor("_BaseColor", color);
            m.SetColor("_Color", color);
            m.SetFloat("_Smoothness", 0.1f);

            if (emission > 0f)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", color * emission);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            if (transparent)
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)RenderQueue.Transparent;
            }

            if (isNew) AssetDatabase.CreateAsset(m, path);
            else EditorUtility.SetDirty(m);
            return m;
        }

        public static void CreateMaterials()
        {
            EnsureFolder(MatFolder);
            Ground = MakeMaterial("M_Ground", new Color(0.55f, 0.6f, 0.5f));
            Day = MakeMaterial("M_Day", new Color(1f, 0.6f, 0.25f));
            Night = MakeMaterial("M_Night", new Color(0.45f, 0.3f, 0.9f), 0.25f);
            Wall = MakeMaterial("M_Wall", new Color(0.7f, 0.68f, 0.65f));
            Crystal = MakeMaterial("M_Crystal", new Color(0.4f, 1f, 0.9f), 1.2f);
            CrystalDay = MakeMaterial("M_CrystalDay", new Color(1f, 0.85f, 0.2f), 1.2f);
            CrystalNight = MakeMaterial("M_CrystalNight", new Color(0.6f, 0.4f, 1f), 1.5f);
            Hazard = MakeMaterial("M_Hazard", new Color(0.9f, 0.15f, 0.2f), 0.4f);
            PlayerMat = MakeMaterial("M_Player", new Color(0.95f, 0.95f, 0.95f));
            EchoMat = MakeMaterial("M_Echo", new Color(0.45f, 0.8f, 1f), 1.2f);
            Portal = MakeMaterial("M_Portal", new Color(0.4f, 1f, 0.9f), 1f);
            Plate = MakeMaterial("M_Plate", new Color(0.9f, 0.4f, 0.3f), 0.3f);
            DoorMat = MakeMaterial("M_Door", new Color(0.35f, 0.3f, 0.3f));
            Ghost = MakeMaterial("M_Ghost", new Color(1f, 1f, 1f, 0.18f), 0f, true);
            AssetDatabase.SaveAssets();
        }

        public static Material MaterialFor(WorldType w)
        {
            switch (w)
            {
                case WorldType.Day: return Day;
                case WorldType.Night: return Night;
                default: return Ground;
            }
        }

        // ---------------------------------------------------------------- Helpers de GameObject

        public static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat, bool keepCollider)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!keepCollider)
            {
                var c = go.GetComponent<Collider>();
                if (c != null) Object.DestroyImmediate(c);
            }
            return go;
        }

        private static GameObject SavePrefab(GameObject temp, string name)
        {
            EnsureFolder(PrefabFolder);
            string path = $"{PrefabFolder}/{name}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
            Object.DestroyImmediate(temp);
            return prefab;
        }

        public static void SetEnum(Object target, string field, int value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p != null) { p.enumValueIndex = value; so.ApplyModifiedPropertiesWithoutUndo(); }
            else Debug.LogWarning($"[Verso] Campo {field} não encontrado em {target}");
        }

        public static void SetBool(Object target, string field, bool value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p != null) { p.boolValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
            else Debug.LogWarning($"[Verso] Campo {field} não encontrado em {target}");
        }

        public static void SetFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p != null) { p.floatValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
            else Debug.LogWarning($"[Verso] Campo {field} não encontrado em {target}");
        }

        public static void SetString(Object target, string field, string value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p != null) { p.stringValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
            else Debug.LogWarning($"[Verso] Campo {field} não encontrado em {target}");
        }

        public static void SetObject(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p != null) { p.objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
            else Debug.LogWarning($"[Verso] Campo {field} não encontrado em {target}");
        }

        public static void SetObjectArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogWarning($"[Verso] Campo {field} não encontrado em {target}"); return; }
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- Prefabs

        public static void CreatePrefabs()
        {
            PlayerPrefab = CreatePlayer();
            CameraPrefab = CreateCamera();
            SystemsPrefab = CreateSystems();
            GameUIPrefab = VersoUIBuilder.BuildGameUIPrefab();
            CrystalPrefab = CreateCrystal();
            PortalPrefab = CreatePortal();
            PlatePrefab = CreatePlate();
            DoorPrefab = CreateDoor();
            EchoPrefab = CreateEcho();
            VersoCharacterBuilder.ApplyToPrefabs(); // troca as cápsulas pelos personagens 3D, se os FBX existirem
            VersoSkyAudioBuilder.ApplyToSystems();   // céu PSX e músicas das ilhas
            SpikesPrefab = CreateSpikes();
            AssetDatabase.SaveAssets();
        }

        private static GameObject CreatePlayer()
        {
            var root = new GameObject("Player");
            var cc = root.AddComponent<CharacterController>();
            cc.center = new Vector3(0f, 1f, 0f);
            cc.height = 2f;
            cc.radius = 0.4f;
            cc.stepOffset = 0.3f;
            cc.slopeLimit = 50f;
            cc.skinWidth = 0.04f;
            root.AddComponent<PlayerController>();
            int layer = LayerMask.NameToLayer(WorldLayers.PlayerLayerName);
            if (layer >= 0) root.layer = layer;

            Primitive(PrimitiveType.Capsule, "Body", root.transform, new Vector3(0f, 1f, 0f), new Vector3(0.8f, 1f, 0.8f), PlayerMat, false);
            Primitive(PrimitiveType.Cube, "Nose", root.transform, new Vector3(0f, 1.45f, 0.4f), new Vector3(0.25f, 0.15f, 0.25f), Day, false);
            return SavePrefab(root, "Player");
        }

        private static GameObject CreateCamera()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 300f;
            go.AddComponent<AudioListener>();
            go.AddComponent<ThirdPersonCamera>();
            return SavePrefab(go, "CameraRig");
        }

        private static GameObject CreateSystems()
        {
            var go = new GameObject("Systems");
            var wm = go.AddComponent<WorldManager>();
            SetObject(wm, "ghostMaterial", Ghost);
            go.AddComponent<AudioManager>();
            return SavePrefab(go, "Systems");
        }

        private static GameObject CreateCrystal()
        {
            var go = Primitive(PrimitiveType.Cube, "Crystal", null, Vector3.zero, new Vector3(0.45f, 0.45f, 0.45f), Crystal, false);
            go.transform.rotation = Quaternion.Euler(45f, 0f, 45f);
            go.AddComponent<WorldObject>();
            go.AddComponent<Collectible>();
            return SavePrefab(go, "Crystal");
        }

        private static GameObject CreatePortal()
        {
            var root = new GameObject("Portal");
            var ring = Primitive(PrimitiveType.Cylinder, "Ring", root.transform, Vector3.zero, new Vector3(2f, 0.08f, 2f), Portal, false);
            ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Primitive(PrimitiveType.Sphere, "Core", root.transform, Vector3.zero, new Vector3(0.6f, 0.6f, 0.6f), Portal, false);
            var p = root.AddComponent<PortalExit>();
            SetObjectArray(p, "visuals", root.GetComponentsInChildren<Renderer>());
            return SavePrefab(root, "Portal");
        }

        private static GameObject CreatePlate()
        {
            var go = Primitive(PrimitiveType.Cube, "PressurePlate", null, Vector3.zero, new Vector3(2f, 0.1f, 2f), Plate, false);
            var wo = go.AddComponent<WorldObject>();
            SetBool(wo, "ghostWhenInactive", true);
            go.AddComponent<PressurePlate>();
            return SavePrefab(go, "PressurePlate");
        }

        private static GameObject CreateDoor()
        {
            var go = Primitive(PrimitiveType.Cube, "Door", null, Vector3.zero, new Vector3(2f, 3f, 0.5f), DoorMat, true);
            go.AddComponent<Door>();
            return SavePrefab(go, "Door");
        }

        private static GameObject CreateEcho()
        {
            var root = new GameObject("Echo");
            Primitive(PrimitiveType.Capsule, "Body", root.transform, new Vector3(0f, 1f, 0f), new Vector3(0.8f, 1f, 0.8f), EchoMat, false);
            Primitive(PrimitiveType.Cube, "Nose", root.transform, new Vector3(0f, 1.45f, 0.4f), new Vector3(0.25f, 0.15f, 0.25f), EchoMat, false);
            var e = root.AddComponent<EchoGhost>();
            SetObjectArray(e, "visuals", root.GetComponentsInChildren<Renderer>());
            return SavePrefab(root, "Echo");
        }

        private static GameObject CreateSpikes()
        {
            var go = Primitive(PrimitiveType.Cube, "Spikes", null, Vector3.zero, Vector3.one, Hazard, true);
            go.GetComponent<Collider>().isTrigger = true;
            go.AddComponent<WorldObject>();
            go.AddComponent<Hazard>();
            return SavePrefab(go, "Spikes");
        }

        // ---------------------------------------------------------------- UI

        public static Canvas CreateCanvas(string name, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (parent != null) go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Text AddText(RectTransform rt, string text, int size, TextAnchor align, Color color)
        {
            var t = rt.gameObject.AddComponent<Text>();
            t.font = UIFont;
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Image AddImage(RectTransform rt, Color color)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static Button CreateButton(Transform parent, string name, string label, Vector2 anchor, Vector2 pos)
        {
            var rt = Rect(name, parent, anchor, pos, new Vector2(360f, 70f));
            rt.pivot = new Vector2(0.5f, 0.5f);
            var img = AddImage(rt, new Color(1f, 1f, 1f, 0.9f));
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = new Color(1f, 0.85f, 0.6f);
            colors.selectedColor = new Color(1f, 0.85f, 0.6f);
            colors.pressedColor = new Color(0.8f, 0.7f, 1f);
            btn.colors = colors;
            var labelRt = Stretch("Label", rt);
            AddText(labelRt, label, 32, TextAnchor.MiddleCenter, new Color(0.1f, 0.1f, 0.15f));
            return btn;
        }

        public static void CreateEventSystem()
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
    }
}
