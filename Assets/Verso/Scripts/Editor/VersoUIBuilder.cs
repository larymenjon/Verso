using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Verso.EditorTools
{
    /// <summary>
    /// Sprites da pasta Assets/Verso/UI/Sprites entram como Sprite (sem mipmap, sem compressão).
    /// </summary>
    public class VersoUISpriteImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(VersoUIBuilder.SpriteFolder)) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.filterMode = FilterMode.Bilinear;
            ti.maxTextureSize = 2048;
            string file = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            if (file.StartsWith("slider_")) ti.spriteBorder = new Vector4(15f, 15f, 15f, 15f);
            if (file.StartsWith("chip_")) ti.spriteBorder = new Vector4(22f, 22f, 22f, 22f);
        }
    }

    /// <summary>
    /// Monta a UI desenhada no Figma ("Verso · UI") com uGUI.
    /// Menu Verso > Aplicar UI refaz só a interface, sem mexer nas ilhas.
    /// </summary>
    public static class VersoUIBuilder
    {
        public const string UIFolder = "Assets/Verso/UI";
        public const string SpriteFolder = UIFolder + "/Sprites/";
        public const string FontFolder = UIFolder + "/Fonts/";
        /// <summary>Texto da caixa de créditos (versões em Loc.CreditsPt / Loc.CreditsEn).</summary>
        public const string CreditsText = Loc.CreditsPt;

        public const string MenuMusicPath = "Assets/Verso/Audio/Music/menu_frankensteins_lab.ogg";

        // Paleta do Figma
        public static readonly Color Ink = Hex("1B1029");
        public static readonly Color Bg0 = Hex("160F26");
        public static readonly Color Bg1 = Hex("231A3D");
        public static readonly Color Cream = Hex("FFF2D9");
        public static readonly Color Ghost = Hex("7DF9E9");
        public static readonly Color PumpkinLight = Hex("FFB347");

        // Caixa de pausa nova (pause_panel.png, 1187x852) desenhada a 95%
        public static readonly Vector2 PausePanelSize = new Vector2(1128f, 809f);
        public static readonly Vector2 PanelTitlePos = new Vector2(0f, 300f);

        private static Font display;
        private static Font body;

        public static Color Hex(string h)
        {
            ColorUtility.TryParseHtmlString("#" + h, out Color c);
            return c;
        }

        public static Sprite S(string name)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + name + ".png");
            if (s == null) Debug.LogWarning($"[Verso] Sprite {name}.png não encontrado em {SpriteFolder}");
            return s;
        }

        public static Font Display
        {
            get
            {
                if (display == null) display = AssetDatabase.LoadAssetAtPath<Font>(FontFolder + "LilitaOne-Regular.ttf");
                return display != null ? display : VersoAssets.UIFont;
            }
        }

        public static Font Body
        {
            get
            {
                if (body == null) body = AssetDatabase.LoadAssetAtPath<Font>(FontFolder + "Fredoka-SemiBold.ttf");
                return body != null ? body : VersoAssets.UIFont;
            }
        }

        // ================================================================ Menu

        [MenuItem("Verso/Aplicar UI (Figma)", priority = 1)]
        public static void ApplyUI()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            AssetDatabase.ImportAsset(UIFolder, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            display = null; body = null;

            BuildGameUIPrefab();

            ReplaceInScene(VersoAssets.SceneFolder + "/" + SceneFlow.MainMenu + ".unity",
                new[] { "MenuUI", "MenuPivot", "Floor" }, BuildMenuUI);
            ReplaceInScene(VersoAssets.SceneFolder + "/" + SceneFlow.End + ".unity",
                new[] { "EndUI" }, BuildEndUI);

            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(VersoAssets.SceneFolder + "/" + SceneFlow.MainMenu + ".unity");
            Debug.Log("[Verso] UI aplicada: prefab GameUI, Menu e Final atualizados.");
        }

        private static void ReplaceInScene(string path, string[] removeNames, System.Action build)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
            {
                Debug.LogWarning($"[Verso] Cena {path} não existe. Rode Verso > Gerar Projeto primeiro.");
                return;
            }
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects())
                if (System.Array.IndexOf(removeNames, root.name) >= 0) Object.DestroyImmediate(root);
            build();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // ================================================================ Peças

        public static Image Img(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2? size = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            if (size.HasValue) rt.sizeDelta = size.Value;
            else if (sprite != null) img.SetNativeSize();
            return img;
        }

        public static Text Label(Transform parent, string name, string text, Font font, int size, Color color, TextAnchor align,
            float outline = 0f, float drop = 0f, Color? glow = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            // "glow" foi desligado: o Outline largo deixava um borrão azul em volta das letras.
            if (outline > 0f)
            {
                var o = go.AddComponent<Outline>();
                o.effectColor = Ink;
                o.effectDistance = new Vector2(outline, outline);
                var o2 = go.AddComponent<Outline>();
                o2.effectColor = Ink;
                o2.effectDistance = new Vector2(outline, -outline * 0.5f);
            }
            if (drop > 0f)
            {
                var s = go.AddComponent<Shadow>();
                s.effectColor = Ink;
                s.effectDistance = new Vector2(0f, -drop);
            }
            // texto fixo vira traduzível (inglês/português); os dinâmicos tiram isso com NoLoc
            if (!string.IsNullOrEmpty(text) && System.Text.RegularExpressions.Regex.IsMatch(text, "[A-Za-zÀ-ú]{2,}"))
                go.AddComponent<LocalizedText>().SetSource(text);
            return t;
        }

        /// <summary>Tira a tradução automática de um texto que o código atualiza sozinho.</summary>
        public static Text NoLoc(Text t)
        {
            var lt = t.GetComponent<LocalizedText>();
            if (lt != null) Object.DestroyImmediate(lt);
            return t;
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        public static void Stretch(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = min;
            rt.offsetMax = max;
        }

        /// <summary>Botão 440x124 com sprite, texto Lilita e feedback.</summary>
        public static Button Btn(Transform parent, string name, string spriteName, string text, Vector2 anchor, Vector2 pivot, Vector2 pos)
        {
            var img = Img(parent, name, S(spriteName), anchor, pivot, pos);
            img.raycastTarget = true;
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1f, 0.96f, 0.88f);
            cb.selectedColor = new Color(1f, 0.96f, 0.88f);
            cb.pressedColor = new Color(0.85f, 0.82f, 0.9f);
            cb.fadeDuration = 0.08f;
            btn.colors = cb;
            img.gameObject.AddComponent<UIButtonFx>();

            var t = Label(img.transform, "Label", text, Display, 42, Cream, TextAnchor.MiddleCenter, 3f, 4f);
            Stretch(t.rectTransform, new Vector2(20f, 18f), new Vector2(-20f, -6f));
            return btn;
        }

        public static Button IconBtn(Transform parent, string name, string bgSprite, string iconSprite, Vector2 anchor, Vector2 pivot, Vector2 pos)
        {
            var img = Img(parent, name, S(bgSprite), anchor, pivot, pos);
            img.raycastTarget = true;
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            img.gameObject.AddComponent<UIButtonFx>();
            Img(img.transform, "Icon", S(iconSprite), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(64f, 64f));
            return btn;
        }

        private static GameObject KeyHint(Transform parent, string key, string text, string extraIcon = null)
        {
            var go = new GameObject("Key_" + key, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            go.transform.SetParent(parent, false);
            var h = go.GetComponent<HorizontalLayoutGroup>();
            h.spacing = 14f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = false; h.childForceExpandHeight = false;

            var cap = Img(go.transform, "Cap", S("keycap"), Vector2.zero, Vector2.zero, Vector2.zero);
            var le = cap.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 76f; le.preferredHeight = 80f;
            var k = Label(cap.transform, "Letter", key, Display, key.Length > 1 ? 28 : 40, Ink, TextAnchor.MiddleCenter);
            Stretch(k.rectTransform, new Vector2(0f, 14f), new Vector2(0f, -4f));

            // ícone extra ao lado da tecla (ex.: mouse com o botão aceso = "ou clique")
            if (!string.IsNullOrEmpty(extraIcon))
            {
                var icon = Img(go.transform, "Icon_" + extraIcon, S(extraIcon), Vector2.zero, Vector2.zero, Vector2.zero);
                icon.preserveAspect = true;
                var ile = icon.gameObject.AddComponent<LayoutElement>();
                ile.preferredWidth = 76f; ile.preferredHeight = 80f;
            }

            Label(go.transform, "Label", text, Body, 28, Cream, TextAnchor.MiddleLeft, 2f);
            return go;
        }

        private static Canvas NewCanvas(string name, int order)
        {
            var c = VersoAssets.CreateCanvas(name);
            c.sortingOrder = order;
            return c;
        }

        // ================================================================ Configurações (engrenagem)

        private static Button Chip(Transform parent, string name, string text, Vector2 pos)
        {
            var center = new Vector2(0.5f, 0.5f);
            var img = Img(parent, name, S("chip_off"), center, center, pos, new Vector2(230f, 72f));
            img.type = Image.Type.Sliced;
            img.raycastTarget = true;
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            img.gameObject.AddComponent<UIButtonFx>();
            var t = Label(img.transform, "Label", text, Display, 34, Cream, TextAnchor.MiddleCenter, 3f, 3f);
            Stretch(t.rectTransform, new Vector2(8f, 6f), new Vector2(-8f, -2f));
            return b;
        }

        private static Slider VolumeSlider(Transform parent, string name, Vector2 pos)
        {
            var center = new Vector2(0.5f, 0.5f);
            var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, center, center, pos, new Vector2(440f, 36f));
            var bg = Img(go.transform, "Background", S("slider_track"), center, center, Vector2.zero);
            bg.type = Image.Type.Sliced;
            Stretch(bg.rectTransform, Vector2.zero, Vector2.zero);

            var area = new GameObject("Fill Area", typeof(RectTransform));
            area.transform.SetParent(go.transform, false);
            Stretch((RectTransform)area.transform, new Vector2(4f, 4f), new Vector2(-4f, -4f));
            var fill = Img(area.transform, "Fill", S("slider_fill"), center, center, Vector2.zero);
            fill.type = Image.Type.Sliced;
            Stretch(fill.rectTransform, Vector2.zero, Vector2.zero);

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(go.transform, false);
            Stretch((RectTransform)handleArea.transform, new Vector2(18f, 0f), new Vector2(-18f, 0f));
            var handle = Img(handleArea.transform, "Handle", S("icon_life"), center, center, Vector2.zero, new Vector2(64f, 64f));
            handle.raycastTarget = true;
            handle.preserveAspect = true;

            var sl = go.GetComponent<Slider>();
            sl.fillRect = fill.rectTransform;
            sl.handleRect = handle.rectTransform;
            sl.targetGraphic = handle;
            sl.direction = Slider.Direction.LeftToRight;
            sl.minValue = 0f; sl.maxValue = 1f; sl.value = 0.8f;
            return sl;
        }

        /// <summary>Engrenagem + caixa de configurações (idioma e volumes). Usada no menu e na pausa.</summary>
        public static SettingsUI BuildSettings(Transform root, Transform gearParent, Vector2 gearPos)
        {
            var center = new Vector2(0.5f, 0.5f);
            var topRight = new Vector2(1f, 1f);
            var gear = IconBtn(gearParent, "SettingsButton", "iconbtn_secondary", "icon_gear", topRight, topRight, gearPos);

            var holder = new GameObject("Settings", typeof(RectTransform));
            holder.transform.SetParent(root, false);
            Stretch((RectTransform)holder.transform, Vector2.zero, Vector2.zero);

            var panel = new GameObject("SettingsPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(holder.transform, false);
            Stretch((RectTransform)panel.transform, Vector2.zero, Vector2.zero);
            panel.GetComponent<Image>().color = new Color(0.055f, 0.03f, 0.125f, 0.8f);

            var box = Img(panel.transform, "Panel", S("pause_panel"), center, center, new Vector2(0f, -20f), PausePanelSize);
            var title = Label(box.transform, "Title", "CONFIGURAÇÕES", Display, 50, Cream, TextAnchor.MiddleCenter, 3f, 4f);
            Place(title.rectTransform, center, center, PanelTitlePos, new Vector2(640f, 90f));

            var langLabel = Label(panel.transform, "LanguageLabel", "Idioma", Display, 40, Cream, TextAnchor.MiddleRight, 3f, 3f);
            Place(langLabel.rectTransform, center, center, new Vector2(-250f, 140f), new Vector2(280f, 60f));
            var en = Chip(panel.transform, "English", "English", new Vector2(20f, 140f));
            var pt = Chip(panel.transform, "Portugues", "Português", new Vector2(265f, 140f));

            var musicLabel = Label(panel.transform, "MusicLabel", "Música", Display, 40, Cream, TextAnchor.MiddleRight, 3f, 3f);
            Place(musicLabel.rectTransform, center, center, new Vector2(-250f, 20f), new Vector2(280f, 60f));
            var music = VolumeSlider(panel.transform, "MusicSlider", new Vector2(140f, 20f));

            var sfxLabel = Label(panel.transform, "EffectsLabel", "Efeitos", Display, 40, Cream, TextAnchor.MiddleRight, 3f, 3f);
            Place(sfxLabel.rectTransform, center, center, new Vector2(-250f, -100f), new Vector2(280f, 60f));
            var sfx = VolumeSlider(panel.transform, "EffectsSlider", new Vector2(140f, -100f));

            var back = Btn(panel.transform, "BackButton", "btn_secondary", "Voltar", center, center, new Vector2(0f, -250f));

            var ui = holder.AddComponent<SettingsUI>();
            ui.Bind(panel, gear, back, en, pt, music, sfx, S("chip_on"), S("chip_off"));
            panel.SetActive(false);
            return ui;
        }

        // ================================================================ GameUI (HUD + pausa)

        public static GameObject BuildGameUIPrefab()
        {
            Canvas canvas = NewCanvas("GameUI", 10);
            Transform root = canvas.transform;
            var ui = canvas.gameObject.AddComponent<GameUI>();
            var r = new GameUI.Refs();

            var topLeft = new Vector2(0f, 1f);
            var topRight = new Vector2(1f, 1f);
            var center = new Vector2(0.5f, 0.5f);

            // HUD
            var hud = new GameObject("HUD", typeof(RectTransform));
            hud.transform.SetParent(root, false);
            Stretch((RectTransform)hud.transform, Vector2.zero, Vector2.zero);
            r.hudRoot = hud;

            var counter = Img(hud.transform, "SoulCounter", S("counter"), topLeft, topLeft, new Vector2(48f, -36f));
            Img(counter.transform, "SoulIcon", S("icon_soul"), topLeft, topLeft, new Vector2(20f, -18f), new Vector2(72f, 72f));
            r.crystalText = Label(counter.transform, "Value", "0 / 3", Display, 46, Cream, TextAnchor.MiddleLeft, 3f, 3f);
            Place(r.crystalText.rectTransform, topLeft, topLeft, new Vector2(128f, -20f), new Vector2(210f, 74f));

            // Vidas (3 Lumes embaixo do contador)
            r.lifeFull = S("icon_life");
            r.lifeEmpty = S("icon_life_empty");
            r.lifeIcons = new Image[Lives.Max];
            for (int i = 0; i < Lives.Max; i++)
                r.lifeIcons[i] = Img(hud.transform, "Life" + i, r.lifeFull, topLeft, topLeft, new Vector2(58f + i * 70f, -156f), new Vector2(64f, 64f));

            r.pauseHudButton = IconBtn(hud.transform, "PauseButton", "iconbtn_primary", "icon_pause", topRight, topRight, new Vector2(-48f, -40f));

            r.worldBadge = Img(hud.transform, "WorldBadge", S("badge_vivos"), topRight, topRight, new Vector2(-196f, -26f));
            r.badgeDay = S("badge_vivos");
            r.badgeNight = S("badge_alem");
            r.iconDay = S("icon_pumpkin");
            r.iconNight = S("icon_moon");
            r.worldBadgeIcon = Img(r.worldBadge.transform, "Icon", r.iconDay, center, center, Vector2.zero, new Vector2(84f, 84f));
            r.blockedRing = Img(r.worldBadge.transform, "BlockedRing", S("badge_ring_blocked"), center, center, Vector2.zero);
            r.worldText = NoLoc(Label(r.worldBadge.transform, "Label", "LIVING", Display, 34, PumpkinLight, TextAnchor.UpperCenter, 3f, 3f));
            Place(r.worldText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(220f, 46f));

            var keys = new GameObject("KeyHints", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            keys.transform.SetParent(hud.transform, false);
            Place((RectTransform)keys.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-48f, 40f), new Vector2(600f, 80f));
            var kh = keys.GetComponent<HorizontalLayoutGroup>();
            kh.spacing = 36f; kh.childAlignment = TextAnchor.MiddleRight;
            kh.childControlWidth = true; kh.childControlHeight = true;
            kh.childForceExpandWidth = false; kh.childForceExpandHeight = false;
            var fit = keys.GetComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            KeyHint(keys.transform, "F", "virar", "mouse_click");
            KeyHint(keys.transform, "R", "reiniciar");
            KeyHint(keys.transform, "Esc", "pausa");

            // Mensagem de ilha
            var msg = Img(root, "Message", S("message_strip"), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 230f));
            var msgRt = msg.rectTransform;
            msgRt.anchorMin = new Vector2(0f, 0.5f); msgRt.anchorMax = new Vector2(1f, 0.5f);
            msgRt.sizeDelta = new Vector2(0f, 220f);
            r.messageGroup = msg.gameObject.AddComponent<CanvasGroup>();
            r.messageGroup.blocksRaycasts = false;
            r.messageTitle = NoLoc(Label(msg.transform, "Title", "ISLAND 1 · AWAKENING", Body, 30, Ghost, TextAnchor.MiddleCenter));
            Place(r.messageTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(1600f, 44f));
            r.messageText = NoLoc(Label(msg.transform, "Text", "There's a side you've never seen.", Display, 72, Cream, TextAnchor.MiddleCenter,
                4f, 4f, new Color(Ghost.r, Ghost.g, Ghost.b, 0.35f)));
            Place(r.messageText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -18f), new Vector2(1800f, 110f));

            // Dicas da Alminha no começo da ilha (caixa embaixo com o retrato dela)
            var tips = new GameObject("Tips", typeof(RectTransform), typeof(CanvasGroup));
            tips.transform.SetParent(root, false);
            Stretch((RectTransform)tips.transform, Vector2.zero, Vector2.zero);
            r.tipGroup = tips.GetComponent<CanvasGroup>();
            r.tipGroup.blocksRaycasts = false;
            var tipBox = Img(tips.transform, "Box", S("message_strip"), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f));
            tipBox.rectTransform.anchorMin = new Vector2(0f, 0f);
            tipBox.rectTransform.anchorMax = new Vector2(1f, 0f);
            tipBox.rectTransform.sizeDelta = new Vector2(-120f, 250f);
            var tipPortrait = Img(tips.transform, "Portrait", S("intro_alminha"), Vector2.zero, Vector2.zero, new Vector2(60f, 150f), new Vector2(320f, 320f));
            tipPortrait.preserveAspect = true;
            tipPortrait.gameObject.AddComponent<UIFloat>().Setup(8f, 1.4f, 0f);
            var tipTag = Img(tipBox.transform, "NameTag", S("counter"), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(330f, 0f), new Vector2(300f, 92f));
            var tipName = Label(tipTag.transform, "Name", "Alminha", Display, 40, Cream, TextAnchor.MiddleCenter, 3f, 3f);
            Stretch(tipName.rectTransform, new Vector2(16f, 8f), new Vector2(-16f, -4f));
            r.tipText = NoLoc(Label(tipBox.transform, "Text", "", Body, 36, Cream, TextAnchor.UpperLeft, 2f));
            Stretch(r.tipText.rectTransform, new Vector2(370f, 40f), new Vector2(-120f, -70f));
            var tipHint = Label(tipBox.transform, "Hint", "Espaço ou clique para continuar", Body, 22,
                new Color(Ghost.r, Ghost.g, Ghost.b, 0.9f), TextAnchor.LowerRight, 2f);
            Place(tipHint.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-60f, 22f), new Vector2(600f, 34f));
            r.tipHint = tipHint.gameObject;

            // Pausa
            var pause = new GameObject("PausePanel", typeof(RectTransform), typeof(Image));
            pause.transform.SetParent(root, false);
            Stretch((RectTransform)pause.transform, Vector2.zero, Vector2.zero);
            var dim = pause.GetComponent<Image>();
            dim.color = new Color(0.055f, 0.03f, 0.125f, 0.72f);
            r.pausePanel = pause;

            // Caixa nova (pause_panel.png): o título vai na tábua de cima, os 3 botões no miolo
            var panel = Img(pause.transform, "Panel", S("pause_panel"), center, center, new Vector2(0f, -20f), PausePanelSize);
            var bt = Label(panel.transform, "Title", "PAUSA", Display, 56, Cream, TextAnchor.MiddleCenter, 3f, 4f);
            Place(bt.rectTransform, center, center, PanelTitlePos, new Vector2(600f, 90f));

            r.resumeButton = Btn(pause.transform, "ResumeButton", "btn_primary", "Continuar", center, center, new Vector2(0f, 46f));
            r.restartButton = Btn(pause.transform, "RestartButton", "btn_secondary", "Reiniciar ilha", center, center, new Vector2(0f, -101f));
            r.menuButton = Btn(pause.transform, "MenuButton", "btn_secondary", "Menu principal", center, center, new Vector2(0f, -248f));

            // Fim de jogo (mesma caixa)
            var over = new GameObject("GameOverPanel", typeof(RectTransform), typeof(Image));
            over.transform.SetParent(root, false);
            Stretch((RectTransform)over.transform, Vector2.zero, Vector2.zero);
            over.GetComponent<Image>().color = new Color(0.055f, 0.03f, 0.125f, 0.8f);
            r.gameOverPanel = over;
            var overPanel = Img(over.transform, "Panel", S("pause_panel"), center, center, new Vector2(0f, -20f), PausePanelSize);
            var ot = Label(overPanel.transform, "Title", "FIM DE JOGO", Display, 56, Cream, TextAnchor.MiddleCenter, 3f, 4f);
            Place(ot.rectTransform, center, center, PanelTitlePos, new Vector2(600f, 90f));
            var lives = new GameObject("LivesLost", typeof(RectTransform));
            lives.transform.SetParent(over.transform, false);
            Place((RectTransform)lives.transform, center, center, new Vector2(0f, 120f), new Vector2(300f, 90f));
            for (int i = 0; i < Lives.Max; i++)
                Img(lives.transform, "Life" + i, S("icon_life_empty"), center, center, new Vector2((i - 1) * 100f, 0f), new Vector2(86f, 86f));
            var oText = Label(over.transform, "Text", "Suas 3 vidas acabaram.", Body, 36, Cream, TextAnchor.MiddleCenter, 2f);
            Place(oText.rectTransform, center, center, new Vector2(0f, 30f), new Vector2(800f, 50f));
            r.retryButton = Btn(over.transform, "RetryButton", "btn_primary", "Tentar de novo", center, center, new Vector2(0f, -90f));
            r.gameOverMenuButton = Btn(over.transform, "MenuButton", "btn_secondary", "Menu principal", center, center, new Vector2(0f, -237f));

            // Configurações: engrenagem no canto da pausa
            BuildSettings(root, pause.transform, new Vector2(-48f, -40f));

            // Fade (por cima de tudo)
            var fade = new GameObject("Fade", typeof(RectTransform), typeof(Image));
            fade.transform.SetParent(root, false);
            Stretch((RectTransform)fade.transform, Vector2.zero, Vector2.zero);
            r.fadeImage = fade.GetComponent<Image>();
            r.fadeImage.color = new Color(0.06f, 0.04f, 0.1f, 0f);
            r.fadeImage.raycastTarget = false;

            ui.Bind(r);

            VersoAssets.EnsureFolder(VersoAssets.PrefabFolder);
            string path = VersoAssets.PrefabFolder + "/GameUI.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(canvas.gameObject, path);
            Object.DestroyImmediate(canvas.gameObject);
            return prefab;
        }

        // ================================================================ Menu principal

        public static void BuildMenuUI()
        {
            Canvas canvas = NewCanvas("MenuUI", 0);
            // menu sempre com 1080 de altura útil: em telas mais largas que 16:9 sobra espaço dos lados
            // em vez de os botões descerem para cima dos créditos
            canvas.GetComponent<CanvasScaler>().matchWidthOrHeight = 1f;
            var menu = canvas.gameObject.AddComponent<MainMenuUI>();
            Transform root = canvas.transform;
            var bg = Img(root, "Background", S("menu_background"), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
            var fitter = bg.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 1920f / 1080f;

            // Layout da tela "01 · Menu principal" (Figma, página Telas): tudo centralizado
            var top = new Vector2(0.5f, 1f);
            var logo = Img(root, "Logo", S("logo"), top, top, new Vector2(0f, -181f), new Vector2(1010f, 404f));
            logo.preserveAspect = true;
            logo.gameObject.AddComponent<UIFloat>().Setup(8f, 1.3f, 0f);

            var sub = Label(root, "Subtitle", "Vire o mundo. Cruze para o outro lado.", Body, 36, Cream, TextAnchor.MiddleCenter, 3f);
            Place(sub.rectTransform, top, top, new Vector2(0f, -583f), new Vector2(900f, 50f));

            var play = Btn(root, "PlayButton", "btn_primary", "Jogar", top, top, new Vector2(0f, -650f));

            // Música do menu (Assets/Verso/Audio/Music)
            var music = AssetDatabase.LoadAssetAtPath<AudioClip>(MenuMusicPath);
            var src = canvas.gameObject.AddComponent<AudioSource>();
            src.clip = music;
            src.loop = true;
            src.playOnAwake = true;
            src.volume = 0.55f;
            src.spatialBlend = 0f;
            canvas.gameObject.AddComponent<VolumeBinder>().Setup(VolumeBinder.Kind.Music, 0.55f);
            if (music == null) Debug.LogWarning($"[Verso] Música do menu não encontrada em {MenuMusicPath}");

            // Créditos: botãozinho embaixo que abre a caixa
            var credBtnGo = new GameObject("CreditsButton", typeof(RectTransform), typeof(Image), typeof(Button));
            credBtnGo.transform.SetParent(root, false);
            Place((RectTransform)credBtnGo.transform, Vector2.zero, Vector2.zero, new Vector2(40f, 32f), new Vector2(220f, 56f));
            var credImg = credBtnGo.GetComponent<Image>();
            credImg.color = new Color(1f, 1f, 1f, 0f);
            var credBtn = credBtnGo.GetComponent<Button>();
            credBtn.targetGraphic = credImg;
            credBtnGo.AddComponent<UIButtonFx>();
            var credLabel = Label(credBtnGo.transform, "Label", "Créditos", Body, 32, new Color(Cream.r, Cream.g, Cream.b, 0.9f), TextAnchor.MiddleLeft, 2f);
            Stretch(credLabel.rectTransform, Vector2.zero, Vector2.zero);

            // Sair: canto oposto aos créditos (escondido na versão Web, onde não há como fechar)
            var quitGo = new GameObject("QuitButton", typeof(RectTransform), typeof(Image), typeof(Button));
            quitGo.transform.SetParent(root, false);
            Place((RectTransform)quitGo.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 32f), new Vector2(220f, 56f));
            var quitImg = quitGo.GetComponent<Image>();
            quitImg.color = new Color(1f, 1f, 1f, 0f);
            var quit = quitGo.GetComponent<Button>();
            quit.targetGraphic = quitImg;
            quitGo.AddComponent<UIButtonFx>();
            var quitLabel = Label(quitGo.transform, "Label", "Sair", Body, 32, new Color(Cream.r, Cream.g, Cream.b, 0.9f), TextAnchor.MiddleRight, 2f);
            Stretch(quitLabel.rectTransform, Vector2.zero, Vector2.zero);

            // Créditos rolando (sem caixa): fundo escurecido, texto subindo e "Voltar" embaixo
            var credPanel = new GameObject("CreditsPanel", typeof(RectTransform), typeof(Image));
            credPanel.transform.SetParent(root, false);
            Stretch((RectTransform)credPanel.transform, Vector2.zero, Vector2.zero);
            credPanel.GetComponent<Image>().color = new Color(0.04f, 0.02f, 0.1f, 0.88f);
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(credPanel.transform, false);
            Stretch((RectTransform)viewport.transform, new Vector2(0f, 150f), new Vector2(0f, -40f));
            var cText = Label(viewport.transform, "Text", CreditsText, Body, 38, Cream, TextAnchor.UpperCenter, 2f);
            var cRt = cText.rectTransform;
            cRt.anchorMin = new Vector2(0.5f, 1f); cRt.anchorMax = new Vector2(0.5f, 1f);
            cRt.pivot = new Vector2(0.5f, 1f);
            cRt.sizeDelta = new Vector2(1400f, 100f);
            cText.lineSpacing = 1.15f;
            cText.supportRichText = true;
            var creditsFit = cText.gameObject.AddComponent<ContentSizeFitter>();
            creditsFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var roll = credPanel.AddComponent<CreditsRoll>();
            roll.Bind(cRt, (RectTransform)viewport.transform);
            var center = new Vector2(0.5f, 0.5f);
            var close = Btn(credPanel.transform, "CloseButton", "btn_secondary", "Voltar", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f));
            close.transform.localScale = Vector3.one * 0.8f;
            credPanel.SetActive(false);

            menu.Bind(play, quit);
            menu.BindCredits(credBtn, credPanel, close);
            BuildSettings(root, root, new Vector2(-48f, -40f));
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null) VersoAssets.CreateEventSystem();
        }

        // ================================================================ Final

        public static void BuildEndUI()
        {
            Canvas canvas = NewCanvas("EndUI", 0);
            var end = canvas.gameObject.AddComponent<EndScreenUI>();
            Transform root = canvas.transform;
            var center = new Vector2(0.5f, 0.5f);

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(root, false);
            Stretch((RectTransform)bg.transform, Vector2.zero, Vector2.zero);
            var bgImg = bg.GetComponent<Image>();
            bgImg.color = Hex("130C22");
            bgImg.raycastTarget = false;

            var souls = new[] {
                new Vector4(-610f, 240f, 1.4f, 0.5f), new Vector4(690f, 300f, 1f, 0.4f), new Vector4(540f, -260f, 1.8f, 0.3f),
                new Vector4(-500f, -280f, 0.9f, 0.4f), new Vector4(40f, 380f, 0.7f, 0.35f) };
            for (int i = 0; i < souls.Length; i++)
            {
                var s = Img(root, "Soul" + i, S("icon_soul"), center, center, new Vector2(souls[i].x, souls[i].y), Vector2.one * 96f * souls[i].z);
                s.color = new Color(1f, 1f, 1f, souls[i].w);
                s.gameObject.AddComponent<UIFloat>().Setup(14f, 0.8f + i * 0.15f, i * 1.3f);
            }

            var textHolder = new GameObject("Phrase", typeof(RectTransform), typeof(CanvasGroup));
            textHolder.transform.SetParent(root, false);
            Place((RectTransform)textHolder.transform, center, center, new Vector2(0f, 60f), new Vector2(1800f, 140f));
            var phrase = Label(textHolder.transform, "Text", "O outro lado sempre foi você.", Display, 84, Cream, TextAnchor.MiddleCenter,
                4f, 5f, new Color(Ghost.r, Ghost.g, Ghost.b, 0.4f));
            Stretch(phrase.rectTransform, Vector2.zero, Vector2.zero);

            var btnHolder = new GameObject("ButtonGroup", typeof(RectTransform), typeof(CanvasGroup));
            btnHolder.transform.SetParent(root, false);
            Place((RectTransform)btnHolder.transform, center, center, new Vector2(0f, -182f), new Vector2(440f, 124f));
            var btn = Btn(btnHolder.transform, "MenuButton", "btn_ghost", "Voltar ao menu", center, center, Vector2.zero);

            end.Bind(textHolder.GetComponent<CanvasGroup>(), btnHolder.GetComponent<CanvasGroup>(), btn);
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null) VersoAssets.CreateEventSystem();
        }
    }
}
