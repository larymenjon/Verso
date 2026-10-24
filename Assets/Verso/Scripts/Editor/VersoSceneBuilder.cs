using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Verso.EditorTools
{
    /// <summary>
    /// Menu "Verso > Gerar Projeto": cria layers, materiais, prefabs e as 5 cenas
    /// (MainMenu, Island1, Island2, Island3, End) em blockout, e configura o Build.
    /// Pode rodar de novo a qualquer momento; as cenas geradas são sobrescritas.
    /// </summary>
    public static class VersoSceneBuilder
    {
        private const bool GhostPlatformsIsland1 = true;
        private const bool GhostPlatformsIsland2 = true;

        private static Transform levelRoot;

        [MenuItem("Verso/Gerar Projeto (cenas e prefabs)", priority = 0)]
        public static void GenerateAll()
        {
            if (!EditorUtility.DisplayDialog("Verso",
                "Isso vai criar/sobrescrever as cenas em Assets/Verso/Scenes e os prefabs em Assets/Verso/Prefabs.\n\nContinuar?",
                "Gerar", "Cancelar"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            try
            {
                EditorUtility.DisplayProgressBar("Verso", "Layers", 0.05f);
                VersoAssets.EnsureLayers();
                if (AssetDatabase.IsValidFolder(VersoUIBuilder.UIFolder))
                    AssetDatabase.ImportAsset(VersoUIBuilder.UIFolder, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
                VersoAssets.EnsureFolder(VersoAssets.SceneFolder);

                EditorUtility.DisplayProgressBar("Verso", "Materiais", 0.15f);
                VersoAssets.CreateMaterials();

                EditorUtility.DisplayProgressBar("Verso", "Prefabs", 0.3f);
                VersoAssets.CreatePrefabs();

                var paths = new List<string>();
                EditorUtility.DisplayProgressBar("Verso", "Menu", 0.4f);
                paths.Add(BuildMainMenu());
                EditorUtility.DisplayProgressBar("Verso", "Intro", 0.45f);
                paths.Add(VersoIntroBuilder.BuildIntroScene());
                EditorUtility.DisplayProgressBar("Verso", "Ilha 1", 0.5f);
                paths.Add(BuildIsland1());
                EditorUtility.DisplayProgressBar("Verso", "Ilha 2", 0.65f);
                paths.Add(BuildIsland2());
                EditorUtility.DisplayProgressBar("Verso", "Ilha 3", 0.72f);
                paths.Add(BuildIsland3());
                EditorUtility.DisplayProgressBar("Verso", "Ilha 4", 0.8f);
                paths.Add(BuildIsland4());
                EditorUtility.DisplayProgressBar("Verso", "Ilha 5", 0.86f);
                paths.Add(BuildIsland5());
                EditorUtility.DisplayProgressBar("Verso", "Final", 0.9f);
                paths.Add(BuildEnd());

                var scenes = new EditorBuildSettingsScene[paths.Count];
                for (int i = 0; i < paths.Count; i++) scenes[i] = new EditorBuildSettingsScene(paths[i], true);
                EditorBuildSettings.scenes = scenes;

                AssetDatabase.SaveAssets();
                VersoSkyAudioBuilder.ApplyToScenes(); // música da Intro e do Final
                EditorSceneManager.OpenScene(paths[0]);
                Debug.Log("[Verso] Projeto gerado. Abra MainMenu e aperte Play.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Verso/Abrir cena/Menu", priority = 20)] private static void OpenMenu() => Open(SceneFlow.MainMenu);
        [MenuItem("Verso/Abrir cena/Ilha 1", priority = 21)] private static void Open1() => Open(SceneFlow.Island1);
        [MenuItem("Verso/Abrir cena/Ilha 2", priority = 22)] private static void Open2() => Open(SceneFlow.Island2);
        [MenuItem("Verso/Abrir cena/Ilha 3", priority = 23)] private static void Open3() => Open(SceneFlow.Island3);
        [MenuItem("Verso/Abrir cena/Ilha 4", priority = 24)] private static void Open4() => Open(SceneFlow.Island4);
        [MenuItem("Verso/Abrir cena/Ilha 5", priority = 25)] private static void Open5() => Open(SceneFlow.Island5);
        [MenuItem("Verso/Abrir cena/Final", priority = 26)] private static void OpenEnd() => Open(SceneFlow.End);

        private static void Open(string name)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene($"{VersoAssets.SceneFolder}/{name}.unity");
        }

        // ================================================================ Helpers de cena

        private static Scene NewScene()
        {
            return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static string Save(Scene scene, string name)
        {
            string path = $"{VersoAssets.SceneFolder}/{name}.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static Light CreateSun()
        {
            var go = new GameObject("Sun");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;
            light.intensity = 1.2f;
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.sun = light;
            return light;
        }

        private static T Spawn<T>(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent = null) where T : Component
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (parent != null) go.transform.SetParent(parent, true);
            go.transform.SetPositionAndRotation(pos, rot);
            return go.GetComponent<T>();
        }

        private static GameObject Spawn(GameObject prefab, Vector3 pos, Transform parent = null)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (parent != null) go.transform.SetParent(parent, true);
            go.transform.position = pos;
            return go;
        }

        /// <summary>Base comum das ilhas: sistemas, jogador, câmera, UI, luz.</summary>
        private static void SetupGameplayScene(Vector3 spawn, float spawnYaw, WorldType startWorld,
            string title, string intro, string[] tipsPt = null, string[] tipsEn = null)
        {
            CreateSun();

            var systems = Spawn(VersoAssets.SystemsPrefab, Vector3.zero);
            VersoAssets.SetEnum(systems.GetComponent<WorldManager>(), "startWorld", (int)startWorld);

            Spawn<PlayerController>(VersoAssets.PlayerPrefab, spawn, Quaternion.Euler(0f, spawnYaw, 0f));
            Spawn(VersoAssets.CameraPrefab, spawn + new Vector3(0f, 3f, -7f));
            Spawn(VersoAssets.GameUIPrefab, Vector3.zero);
            VersoAssets.CreateEventSystem();

            var islandGo = new GameObject("Island");
            var island = islandGo.AddComponent<IslandManager>();
            VersoAssets.SetString(island, "islandTitle", title);
            VersoAssets.SetString(island, "introMessage", intro);
            SetStrings(island, "tipsPt", tipsPt);
            SetStrings(island, "tipsEn", tipsEn);

            levelRoot = new GameObject("Level").transform;
        }

        private static void SetStrings(Object target, string field, string[] values)
        {
            if (values == null) return;
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) return;
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).stringValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Bloco de cenário. Dia/Noite recebem WorldObject; Ambos fica neutro.</summary>
        private static GameObject Block(string name, Vector3 center, Vector3 size, WorldType world, bool ghost = false, Material mat = null)
        {
            var go = VersoAssets.Primitive(PrimitiveType.Cube, name, levelRoot, center, size,
                mat != null ? mat : VersoAssets.MaterialFor(world), true);
            go.isStatic = world == WorldType.Both;
            if (world != WorldType.Both)
            {
                var wo = go.AddComponent<WorldObject>();
                wo.Configure(world, ghost);
                int layer = LayerMask.NameToLayer(world == WorldType.Day ? WorldLayers.DayLayerName : WorldLayers.NightLayerName);
                if (layer >= 0) go.layer = layer;
            }
            return go;
        }

        private static GameObject Crystal(Vector3 pos, WorldType world)
        {
            var go = Spawn(VersoAssets.CrystalPrefab, pos, levelRoot);
            go.name = $"Crystal_{world}";
            VersoAssets.SetEnum(go.GetComponent<WorldObject>(), "world", (int)world);
            Material m = world == WorldType.Day ? VersoAssets.CrystalDay :
                         world == WorldType.Night ? VersoAssets.CrystalNight : VersoAssets.Crystal;
            var r = go.GetComponent<Renderer>(); // só existe no blockout; a alma 3D não usa
            if (r != null)
            {
                r.sharedMaterial = m;
                PrefabUtility.RecordPrefabInstancePropertyModifications(r);
            }
            return go;
        }

        private static GameObject Spikes(Vector3 center, Vector3 size, WorldType world)
        {
            var go = Spawn(VersoAssets.SpikesPrefab, center, levelRoot);
            go.transform.localScale = size;
            VersoAssets.SetEnum(go.GetComponent<WorldObject>(), "world", (int)world);
            return go;
        }

        private static PortalExit Portal(Vector3 pos, bool final)
        {
            var go = Spawn(VersoAssets.PortalPrefab, pos, levelRoot);
            var p = go.GetComponent<PortalExit>();
            if (final) VersoAssets.SetBool(p, "isFinalPortal", true);
            return p;
        }

        private static PressurePlate Plate(Vector3 pos, WorldType world)
        {
            var go = Spawn(VersoAssets.PlatePrefab, pos, levelRoot);
            go.name = $"Plate_{world}";
            VersoAssets.SetEnum(go.GetComponent<WorldObject>(), "world", (int)world);
            return go.GetComponent<PressurePlate>();
        }

        private static Door MakeDoor(Vector3 center, Vector3 size, PressurePlate[] plates, bool latch, float closeDelay)
        {
            var go = Spawn(VersoAssets.DoorPrefab, center, levelRoot);
            go.transform.localScale = size;
            var d = go.GetComponent<Door>();
            VersoAssets.SetObjectArray(d, "plates", plates);
            VersoAssets.SetBool(d, "latch", latch);
            VersoAssets.SetFloat(d, "closeDelay", closeDelay);
            return d;
        }

        // ================================================================ Menu

        private static string BuildMainMenu()
        {
            var scene = NewScene();
            CreateSun();
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.5f, 0.45f, 0.55f);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.1f, 0.22f);
            camGo.AddComponent<AudioListener>();
            camGo.transform.SetPositionAndRotation(new Vector3(0f, 1.5f, -7f), Quaternion.Euler(5f, 0f, 0f));

            VersoUIBuilder.BuildMenuUI();
            return Save(scene, SceneFlow.MainMenu);
        }

        // ================================================================ Ilha 1 · Despertar
        // Ensina o flip: caminho só da Noite, ponte só do Dia, parede só do Dia.

        private static string BuildIsland1()
        {
            var scene = NewScene();
            SetupGameplayScene(new Vector3(0f, 0.05f, -2f), 0f, WorldType.Day,
                "Ilha 1 · Despertar", "Existe um lado que você nunca viu.",
                new[] {
                    "Oi de novo! Essa é a Ilha 1. Aperte F pra virar o mundo entre os Vivos e o Além.",
                    "Os blocos transparentes existem só do outro lado. Vire o mundo pra eles ficarem sólidos.",
                    "Junte as 3 almas e pegue a chave no portal pra seguir em frente!" },
                new[] {
                    "Hi again! This is Island 1. Press F to flip the world between the Living and the Beyond.",
                    "See-through blocks only exist on the other side. Flip the world to make them solid.",
                    "Gather the 3 souls and grab the key at the portal to move on!" });
            bool g = GhostPlatformsIsland1;

            Block("Start", new Vector3(0f, -0.5f, 0f), new Vector3(10f, 1f, 10f), WorldType.Both);
            Block("NightPath", new Vector3(0f, -0.25f, 12f), new Vector3(3f, 0.5f, 14f), WorldType.Night, g);

            Block("IslandA", new Vector3(0f, -0.5f, 23f), new Vector3(8f, 1f, 8f), WorldType.Both);
            Crystal(new Vector3(0f, 1f, 23f), WorldType.Both);

            Block("DayBridge", new Vector3(9f, -0.25f, 23f), new Vector3(10f, 0.5f, 3f), WorldType.Day, g);

            Block("IslandB", new Vector3(18f, -0.5f, 23f), new Vector3(8f, 1f, 8f), WorldType.Both);
            Crystal(new Vector3(16.5f, 1f, 21.5f), WorldType.Day);

            Block("DayWall", new Vector3(18f, 1.5f, 27.25f), new Vector3(10f, 3f, 0.5f), WorldType.Day, false, VersoAssets.Day);
            Block("IslandC", new Vector3(18f, -0.5f, 31.25f), new Vector3(8f, 1f, 8.5f), WorldType.Both);
            Crystal(new Vector3(18f, 1f, 31f), WorldType.Night);

            Portal(new Vector3(18f, 1.6f, 34.2f), false);
            return Save(scene, SceneFlow.Island1);
        }

        // ================================================================ Ilha 2 · Travessia
        // Flip + pulo + timing: plataformas alternadas, espinhos da Noite.

        private static string BuildIsland2()
        {
            var scene = NewScene();
            SetupGameplayScene(new Vector3(0f, 0.05f, -2f), 0f, WorldType.Day, "Ilha 2 · Travessia", "",
                new[] {
                    "Aqui os degraus trocam de lado: um é dos Vivos, o próximo é do Além.",
                    "Pule primeiro e aperte F ainda no ar. Assim o próximo degrau aparece embaixo de você!",
                    "No corredor, os espinhos só existem no Além. Atravesse nos Vivos." },
                new[] {
                    "Here the steps switch sides: one belongs to the Living, the next to the Beyond.",
                    "Jump first, then press F while you're in the air. The next step will appear under you!",
                    "In the hallway, the spikes only exist in the Beyond. Cross it in the Living." });
            bool g = GhostPlatformsIsland2;

            Block("Start", new Vector3(0f, -0.5f, 0f), new Vector3(10f, 1f, 10f), WorldType.Both);
            Block("Step1_Day", new Vector3(0f, -0.25f, 8f), new Vector3(3f, 0.5f, 3f), WorldType.Day, g);
            Block("Step2_Night", new Vector3(0f, 0.75f, 13f), new Vector3(3f, 0.5f, 3f), WorldType.Night, g);
            Block("Step3_Day", new Vector3(0f, 1.75f, 18f), new Vector3(3f, 0.5f, 3f), WorldType.Day, g);
            Block("Step4_Night", new Vector3(0f, 2.75f, 23f), new Vector3(3f, 0.5f, 3f), WorldType.Night, g);

            Block("IslandA", new Vector3(0f, 2.5f, 30f), new Vector3(8f, 1f, 8f), WorldType.Both);
            Crystal(new Vector3(0f, 4f, 30f), WorldType.Both);

            Block("Corridor", new Vector3(13f, 2.5f, 30f), new Vector3(18f, 1f, 4f), WorldType.Both);
            Spikes(new Vector3(8f, 3.25f, 30f), new Vector3(1.2f, 0.5f, 4f), WorldType.Night);
            Spikes(new Vector3(13f, 3.25f, 30f), new Vector3(1.2f, 0.5f, 4f), WorldType.Night);
            Spikes(new Vector3(18f, 3.25f, 30f), new Vector3(1.2f, 0.5f, 4f), WorldType.Night);
            Crystal(new Vector3(13f, 6f, 30f), WorldType.Night);

            Block("IslandB", new Vector3(26f, 2.5f, 30f), new Vector3(8f, 1f, 8f), WorldType.Both);
            Block("Up1_Night", new Vector3(26f, 3.75f, 37f), new Vector3(3f, 0.5f, 3f), WorldType.Night, g);
            Block("Up2_Day", new Vector3(26f, 4.75f, 42f), new Vector3(3f, 0.5f, 3f), WorldType.Day, g);
            Block("Up3_Night", new Vector3(26f, 5.75f, 47f), new Vector3(3f, 0.5f, 3f), WorldType.Night, g);

            Block("IslandC", new Vector3(26f, 5.5f, 53f), new Vector3(6f, 1f, 6f), WorldType.Both);
            Crystal(new Vector3(26f, 7f, 52f), WorldType.Both);

            Portal(new Vector3(26f, 7.6f, 55f), false);
            return Save(scene, SceneFlow.Island2);
        }

        // ================================================================ Ilha 3 · Salto
        // Pular e virar no ar: o bloco do outro lado ocupa o lugar do jogador, então virar parado é recusado.

        private static string BuildIsland3()
        {
            var scene = NewScene();
            SetupGameplayScene(new Vector3(0f, 0.05f, -2f), 0f, WorldType.Day, "Ilha 3 · Salto", "",
                new[] {
                    "Às vezes tem algo do outro lado bem onde você está. Se apertar F parado no chão, o mundo se recusa a virar.",
                    "Pra ativar essa plataforma, você não pode estar no chão: pule e aperte F logo em seguida.",
                    "Ela aparece embaixo dos seus pés e você cai em cima. Vamos tentar!" },
                new[] {
                    "Sometimes there's something on the other side right where you're standing. If you press F on the ground, the world refuses to flip.",
                    "To bring up that platform you can't be on the ground: jump, then press F right away.",
                    "It appears under your feet and you land on top. Let's try it!" });

            Block("Start", new Vector3(0f, -0.5f, 0f), new Vector3(10f, 1f, 10f), WorldType.Both);
            Block("DayFloor", new Vector3(0f, -0.5f, 10f), new Vector3(4f, 1f, 10f), WorldType.Day, true);
            // ocupa a altura do corpo de quem está no DayFloor: só dá pra virar pulando
            Block("NightBlock", new Vector3(0f, 0.5f, 13f), new Vector3(4f, 1f, 4f), WorldType.Night, true);
            Crystal(new Vector3(0f, 2f, 13f), WorldType.Night);
            Block("NightBridge", new Vector3(0f, 0.5f, 19.5f), new Vector3(3f, 1f, 9f), WorldType.Night, true);

            Block("IslandA", new Vector3(0f, 0.5f, 28f), new Vector3(8f, 1f, 8f), WorldType.Both);
            Block("DayPath", new Vector3(9f, 0.5f, 28f), new Vector3(10f, 1f, 3f), WorldType.Day, true);
            Block("NightBlock2", new Vector3(12f, 1.5f, 28f), new Vector3(3f, 1f, 3f), WorldType.Night, true);
            Crystal(new Vector3(12f, 3f, 28f), WorldType.Night);
            Block("NightPath", new Vector3(17.75f, 1.5f, 28f), new Vector3(8.5f, 1f, 3f), WorldType.Night, true);

            Block("IslandB", new Vector3(25.5f, 1.5f, 28f), new Vector3(7f, 1f, 7f), WorldType.Both);
            Crystal(new Vector3(26f, 3f, 26.5f), WorldType.Both);
            Portal(new Vector3(26f, 3.6f, 30.5f), false);
            return Save(scene, SceneFlow.Island3);
        }

        // ================================================================ Ilha 4 · Ritmo
        // Degraus menores e mais longe trocando de lado + corredor com espinhos dos dois mundos.

        private static string BuildIsland4()
        {
            var scene = NewScene();
            SetupGameplayScene(new Vector3(0f, 0.05f, -2f), 0f, WorldType.Day, "Ilha 4 · Ritmo", "",
                new[] {
                    "Essa ilha é mais rápida! Os degraus estão mais longe e mudam de lado o tempo todo.",
                    "No corredor tem espinhos dos dois mundos. Vire o mundo no ritmo certo pra sumir com os da frente.",
                    "Se cair, você perde uma vida. Respira e vai no seu tempo!" },
                new[] {
                    "This island is faster! The steps are farther apart and keep switching sides.",
                    "The hallway has spikes in both worlds. Flip at the right moment to make the ones ahead disappear.",
                    "If you fall, you lose a life. Take a breath and go at your own pace!" });

            Block("Start", new Vector3(0f, -0.5f, 0f), new Vector3(10f, 1f, 10f), WorldType.Both);
            var step = new Vector3(2.2f, 0.5f, 2.2f);
            Block("P1_Day", new Vector3(1.2f, -0.25f, 6.5f), step, WorldType.Day, true);
            Block("P2_Night", new Vector3(-1.2f, 0.25f, 9.7f), step, WorldType.Night, true);
            Block("P3_Day", new Vector3(1.2f, 0.75f, 12.9f), step, WorldType.Day, true);
            Block("P4_Night", new Vector3(-1.2f, 1.25f, 16.1f), step, WorldType.Night, true);
            Crystal(new Vector3(-1.2f, 2.5f, 16.1f), WorldType.Night);
            Block("P5_Day", new Vector3(1.2f, 1.75f, 19.3f), step, WorldType.Day, true);

            Block("IslandA", new Vector3(0f, 1.5f, 25f), new Vector3(8f, 1f, 7f), WorldType.Both);
            Block("Corridor", new Vector3(0f, 1.5f, 37f), new Vector3(4f, 1f, 17f), WorldType.Both);
            var spike = new Vector3(4f, 0.5f, 1.2f);
            Spikes(new Vector3(0f, 2.25f, 31f), spike, WorldType.Day);
            Spikes(new Vector3(0f, 2.25f, 34.5f), spike, WorldType.Night);
            Spikes(new Vector3(0f, 2.25f, 38f), spike, WorldType.Day);
            Spikes(new Vector3(0f, 2.25f, 41.5f), spike, WorldType.Night);
            Crystal(new Vector3(0f, 3f, 43.7f), WorldType.Day);

            Block("IslandB", new Vector3(0f, 1.5f, 50f), new Vector3(8f, 1f, 8f), WorldType.Both);
            Crystal(new Vector3(2.5f, 3f, 49f), WorldType.Both);
            Portal(new Vector3(0f, 3.6f, 53f), false);
            return Save(scene, SceneFlow.Island4);
        }

        // ================================================================ Ilha 5 · Eco (final)
        // O Eco repete o jogador 1,5 s depois, no mundo oposto, e pisa nas placas do outro lado.

        private static string BuildIsland5()
        {
            var scene = NewScene();
            Vector3 spawn = new Vector3(0f, 0.05f, -3f);
            SetupGameplayScene(spawn, 0f, WorldType.Day, "Ilha 5 · Eco", "",
                new[] {
                    "Sentiu? Alguém está repetindo os seus passos do outro lado, um pouquinho atrasado.",
                    "As placas do outro lado, só ele consegue apertar. Pare em cima da plataforma e espere ele chegar.",
                    "Quando a porta abrir, corra! Algumas fecham de novo quando ele sai da placa." },
                new[] {
                    "Feel that? Someone is repeating your steps on the other side, a little behind you.",
                    "Only they can press the plates on the other side. Stand on the platform and wait for them to arrive.",
                    "When the door opens, run! Some close again when they step off the plate." });

            Block("Floor", new Vector3(0f, -0.5f, 10f), new Vector3(12f, 1f, 32f), WorldType.Both);

            // Sala 1: placa da Noite em cima de uma plataforma só do Dia.
            // O jogador (Dia) fica ali; o Eco (Noite) chega depois e pressiona a placa.
            Block("DayLedge", new Vector3(-7.5f, -0.25f, 2f), new Vector3(3f, 0.5f, 3f), WorldType.Day);
            var plate1 = Plate(new Vector3(-7.5f, 0.05f, 2f), WorldType.Night);
            Crystal(new Vector3(4f, 1f, 1f), WorldType.Night);

            Block("WallA_Left", new Vector3(-3.5f, 1.5f, 8f), new Vector3(5f, 3f, 0.5f), WorldType.Both, false, VersoAssets.Wall);
            Block("WallA_Right", new Vector3(3.5f, 1.5f, 8f), new Vector3(5f, 3f, 0.5f), WorldType.Both, false, VersoAssets.Wall);
            MakeDoor(new Vector3(0f, 1.5f, 8f), new Vector3(2f, 3f, 0.5f), new[] { plate1 }, true, 0.6f);

            // Sala 2: placa do Dia em cima de uma plataforma só da Noite.
            // O jogador precisa estar na Noite; a porta fecha quando o Eco sai da placa.
            Block("NightLedge", new Vector3(7.5f, -0.25f, 13f), new Vector3(3f, 0.5f, 3f), WorldType.Night);
            var plate2 = Plate(new Vector3(7.5f, 0.05f, 13f), WorldType.Day);
            Crystal(new Vector3(-4f, 1f, 13f), WorldType.Day);

            Block("WallB_Left", new Vector3(-1.75f, 1.5f, 18f), new Vector3(8.5f, 3f, 0.5f), WorldType.Both, false, VersoAssets.Wall);
            Block("WallB_Right", new Vector3(5.25f, 1.5f, 18f), new Vector3(1.5f, 3f, 0.5f), WorldType.Both, false, VersoAssets.Wall);
            MakeDoor(new Vector3(3.5f, 1.5f, 18f), new Vector3(2f, 3f, 0.5f), new[] { plate2 }, false, 0.8f);

            // Sala 3: último cristal e portal final.
            Crystal(new Vector3(0f, 1f, 21.5f), WorldType.Both);
            Portal(new Vector3(0f, 1.6f, 24.5f), true);

            Spawn(VersoAssets.EchoPrefab, spawn);
            new GameObject("EndingSequence").AddComponent<EndingSequence>();

            return Save(scene, SceneFlow.Island5);
        }

        // ================================================================ Final

        private static string BuildEnd()
        {
            var scene = NewScene();
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            camGo.AddComponent<AudioListener>();

            VersoUIBuilder.BuildEndUI();
            return Save(scene, SceneFlow.End);
        }
    }
}
