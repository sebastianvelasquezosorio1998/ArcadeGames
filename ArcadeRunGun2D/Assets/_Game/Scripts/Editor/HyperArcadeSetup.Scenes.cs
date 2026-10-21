using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UB = HyperArcade.EditorTools.EditorUIBuilder;

namespace HyperArcade.EditorTools
{
    public static partial class HyperArcadeSetup
    {
        private const string LevelNodePath = PrefabDir + "/UI/LevelNode.prefab";
        private const string ShopItemPath = PrefabDir + "/UI/ShopItem.prefab";
        private const string ConsumableSlotPath = PrefabDir + "/UI/ConsumableSlot.prefab";

        private static GameConfig LoadGameConfig() => AssetDatabase.LoadAssetAtPath<GameConfig>(ResourcesDir + "/GameConfig.asset");

        /// <summary>Carga un prefab desde disco (nunca reutilizar referencias de antes de un cambio de escena).</summary>
        private static T LoadPrefab<T>(string path) where T : Component
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var component = go != null ? go.GetComponent<T>() : null;
            if (component == null) Debug.LogError($"[HyperArcade] No se pudo cargar {typeof(T).Name} desde {path}");
            return component;
        }

        private static SkinProfile _skin;
        private static readonly Color Green = new Color(0.25f, 0.75f, 0.35f);
        private static readonly Color Blue = new Color(0.2f, 0.5f, 0.95f);
        private static readonly Color Purple = new Color(0.6f, 0.35f, 0.9f);
        private static readonly Color Gold = new Color(1f, 0.75f, 0.1f);
        private static readonly Color Red = new Color(0.9f, 0.25f, 0.25f);
        private static readonly Color Gray = new Color(0.35f, 0.37f, 0.45f);
        private static readonly Color Dark = new Color(0.05f, 0.05f, 0.08f, 0.85f);
        private static readonly Vector2 TopLeft = new Vector2(0, 1), TopCenter = new Vector2(0.5f, 1), TopRight = new Vector2(1, 1);
        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f), Left = new Vector2(0, 0.5f), Right = new Vector2(1, 0.5f);
        private static readonly Vector2 BottomLeft = new Vector2(0, 0), BottomCenter = new Vector2(0.5f, 0), BottomRight = new Vector2(1, 0);

        private static Sprite S(UiSlot slot) => _skin != null ? _skin.FindUiSprite(slot) : null;

        private static void PrepareBuilder(SkinProfile skin)
        {
            _skin = skin;
            UB.PanelSprite = S(UiSlot.PanelBackground);
            UB.ButtonSprite = S(UiSlot.ButtonBackground);
        }

        private static Image Skinned(Image img, UiSlot slot)
        {
            var s = img.gameObject.AddComponent<SkinnedUIImage>();
            UB.SetValue(s, "slot", (int)slot);
            return img;
        }

        private static Image Icon(Transform parent, string name, UiSlot slot, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var img = UB.Image(parent, name, S(slot), Color.white);
            img.preserveAspect = true;
            img.rectTransform.Place(anchor, pos, size);
            return Skinned(img, slot);
        }

        private static TextMeshProUGUI Label(Transform parent, string name, string text, float size, Vector2 anchor, Vector2 pos, Vector2 box,
            TextAlignmentOptions align = TextAlignmentOptions.Center, Color? color = null)
        {
            var t = UB.Text(parent, name, text, size, align, color);
            t.rectTransform.Place(anchor, pos, box);
            return t;
        }

        private static Button Btn(Transform parent, string name, string label, Color color, Vector2 anchor, Vector2 pos, Vector2 size,
            out TextMeshProUGUI text, float fontSize = 48f)
        {
            var b = UB.Button(parent, name, label, out text, color, fontSize);
            ((RectTransform)b.transform).Place(anchor, pos, size);
            return b;
        }

        private static Button Btn(Transform parent, string name, string label, Color color, Vector2 anchor, Vector2 pos, Vector2 size, float fontSize = 48f) =>
            Btn(parent, name, label, color, anchor, pos, size, out _, fontSize);

        private static StarsDisplay Stars(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, float starSize, float spacing, bool raiseMiddle = false)
        {
            var root = UB.Rect("Stars", parent).Place(anchor, pos, size);
            var display = root.gameObject.AddComponent<StarsDisplay>();
            var imgs = new Object[3];
            for (int i = 0; i < 3; i++)
            {
                float s = raiseMiddle && i == 1 ? starSize * 1.2f : starSize;
                var img = UB.Image(root, "Star" + i, S(UiSlot.StarEmpty), Color.white);
                img.preserveAspect = true;
                img.rectTransform.Place(Center, new Vector2((i - 1) * spacing, raiseMiddle && i == 1 ? starSize * 0.2f : 0f), new Vector2(s, s));
                imgs[i] = img;
            }
            UB.SetArray(display, "stars", imgs);
            return display;
        }

        /// <summary>Controles táctiles al 40 % de opacidad (PlayerInputHub.ControlsOpacity).</summary>
        private static void Translucent(Component control)
        {
            var group = control.gameObject.AddComponent<CanvasGroup>();
            group.alpha = PlayerInputHub.ControlsOpacity;
        }

        private static RectTransform ScreenRoot(Transform parent, string name) => UB.Rect(name, parent).Full();

        // ================================================================ Prefabs de UI

        private static void CreateUIPrefabs(SkinProfile skin)
        {
            PrepareBuilder(skin);

            // --- Nodo de nivel
            {
                var root = UB.Rect("LevelNode", null);
                root.sizeDelta = new Vector2(220, 220);
                var marker = UB.Image(root, "CurrentMarker", UB.ButtonSprite, new Color(1f, 0.9f, 0.2f, 0.6f));
                marker.rectTransform.Full(-14f);
                var bg = root.gameObject.AddComponent<Image>();
                bg.sprite = UB.ButtonSprite; bg.type = Image.Type.Sliced; bg.color = UB.ButtonColor;
                var button = root.gameObject.AddComponent<Button>();
                button.targetGraphic = bg;
                var number = UB.Text(root, "Number", "1", 96);
                number.rectTransform.Stretch(new Vector2(0, 0.3f), Vector2.one, 6f);
                var stars = Stars(root, BottomCenter, new Vector2(0, 14), new Vector2(200, 60), 56, 62);
                var locked = UB.Image(root, "Locked", UB.ButtonSprite, new Color(0.1f, 0.1f, 0.15f, 0.85f));
                locked.rectTransform.Full();
                Icon(locked.transform, "LockIcon", UiSlot.LockIcon, Center, Vector2.zero, new Vector2(110, 110));

                var node = root.gameObject.AddComponent<LevelNodeUI>();
                UB.Set(node, "button", button);
                UB.Set(node, "numberText", number);
                UB.Set(node, "stars", stars);
                UB.Set(node, "lockedOverlay", locked.gameObject);
                UB.Set(node, "currentMarker", marker.gameObject);
                SavePrefab(root.gameObject, LevelNodePath);
            }

            // --- Tarjeta de tienda
            {
                var root = UB.Rect("ShopItem", null);
                root.sizeDelta = new Vector2(1500, 230);
                root.gameObject.AddComponent<LayoutElement>().preferredHeight = 230;
                var highlight = UB.Image(root, "Selected", UB.PanelSprite, new Color(0.3f, 1f, 0.4f, 0.8f));
                highlight.rectTransform.Full(-6f);
                var bg = UB.Panel(root, "Background", new Color(0.2f, 0.23f, 0.35f));
                bg.rectTransform.Full();
                var icon = UB.Image(root, "Icon", null, Color.white);
                icon.preserveAspect = true;
                icon.rectTransform.Place(Left, new Vector2(30, 0), new Vector2(180, 180));
                var title = Label(root, "Name", "Nombre", 52, TopLeft, new Vector2(240, -18), new Vector2(560, 64), TextAlignmentOptions.Left);
                var info = Label(root, "Info", "Descripción", 30, TopLeft, new Vector2(240, -88), new Vector2(640, 130), TextAlignmentOptions.TopLeft, new Color(0.85f, 0.88f, 1f));
                var badge = Label(root, "Badge", "", 30, TopLeft, new Vector2(24, -8), new Vector2(190, 40), TextAlignmentOptions.Left, Gold);

                var item = root.gameObject.AddComponent<ShopItemUI>();
                Color[] colors = { Green, Red, Blue };
                for (int i = 0; i < 3; i++)
                {
                    var b = Btn(root, "Action" + i, "", colors[i], Right, new Vector2(-30 - (2 - i) * 230, 0), new Vector2(210, 150), out var lbl, 34);
                    lbl.rectTransform.offsetMin = new Vector2(46, 10);
                    var cur = UB.Image(b.transform, "Currency", S(UiSlot.CoinIcon), Color.white);
                    cur.preserveAspect = true;
                    cur.rectTransform.Place(Left, new Vector2(8, 0), new Vector2(44, 44));
                    UB.SetArrayElementField(item, "actions", i, "button", b);
                    UB.SetArrayElementField(item, "actions", i, "label", lbl);
                    UB.SetArrayElementField(item, "actions", i, "currencyIcon", cur);
                }
                UB.Set(item, "icon", icon);
                UB.Set(item, "nameText", title);
                UB.Set(item, "infoText", info);
                UB.Set(item, "badgeText", badge);
                UB.Set(item, "selectedHighlight", highlight.gameObject);
                SavePrefab(root.gameObject, ShopItemPath);
            }

            // --- Fila de consumible (pre-partida)
            {
                var root = UB.Rect("ConsumableSlot", null);
                root.sizeDelta = new Vector2(1050, 115);
                root.gameObject.AddComponent<LayoutElement>().preferredHeight = 115;
                var bg = UB.Panel(root, "Background", new Color(0.2f, 0.23f, 0.35f));
                bg.rectTransform.Full();
                var icon = UB.Image(root, "Icon", null, Color.white);
                icon.preserveAspect = true;
                icon.rectTransform.Place(Left, new Vector2(16, 0), new Vector2(90, 90));
                var title = Label(root, "Name", "Consumible", 40, Left, new Vector2(125, 18), new Vector2(380, 56), TextAlignmentOptions.Left);
                var count = Label(root, "Count", "x0", 32, Left, new Vector2(125, -28), new Vector2(200, 44), TextAlignmentOptions.Left, Gold);
                Label(root, "TakeLabel", "Llevar", 34, Right, new Vector2(-400, 0), new Vector2(140, 50), TextAlignmentOptions.Right);
                var toggle = UB.Toggle(root, "Equip", out _);
                ((RectTransform)toggle.transform).Place(Right, new Vector2(-290, 0), new Vector2(86, 86));
                var buy = Btn(root, "Buy", "0", Green, Right, new Vector2(-16, 0), new Vector2(240, 95), out var price, 40);
                price.rectTransform.offsetMin = new Vector2(70, 8);
                var cur = UB.Image(buy.transform, "Currency", S(UiSlot.CoinIcon), Color.white);
                cur.preserveAspect = true;
                cur.rectTransform.Place(Left, new Vector2(14, 0), new Vector2(54, 54));

                var slot = root.gameObject.AddComponent<ConsumableSlotUI>();
                UB.Set(slot, "icon", icon);
                UB.Set(slot, "nameText", title);
                UB.Set(slot, "countText", count);
                UB.Set(slot, "equipToggle", toggle);
                UB.Set(slot, "buyButton", buy);
                UB.Set(slot, "priceText", price);
                UB.Set(slot, "priceCurrencyIcon", cur);
                SavePrefab(root.gameObject, ConsumableSlotPath);
            }
        }

        // ================================================================ Escena: Menú

        private static string BuildMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var config = LoadGameConfig();
            PrepareBuilder(config.activeSkin);
            var levelNodePrefab = LoadPrefab<LevelNodeUI>(LevelNodePath);
            var shopItemPrefab = LoadPrefab<ShopItemUI>(ShopItemPath);
            var consumableSlotPrefab = LoadPrefab<ConsumableSlotUI>(ConsumableSlotPath);

            var cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.14f, 0.22f);
            cam.transform.position = new Vector3(0, 0, -10);
            cam.gameObject.AddComponent<AudioListener>();
            UB.CreateEventSystem();

            var canvas = UB.CreateCanvas("MenuCanvas");
            var bg = Skinned(UB.Image(canvas.transform, "Background", S(UiSlot.MenuBackground), Color.white), UiSlot.MenuBackground);
            bg.rectTransform.Full();

            var screensRoot = UB.Rect("Screens", canvas.transform).Full();
            var manager = screensRoot.gameObject.AddComponent<UIScreenManager>();

            // ---------------- Menú principal
            var mainRt = ScreenRoot(screensRoot, "MainMenuScreen");
            var main = mainRt.gameObject.AddComponent<MainMenuScreen>();
            Icon(mainRt, "Logo", UiSlot.Logo, TopCenter, new Vector2(0, -40), new Vector2(700, 350));
            var title = Label(mainRt, "Title", "Arcade Blaster", 84, TopCenter, new Vector2(0, -165), new Vector2(560, 120));
            UB.SetValue(title.gameObject.AddComponent<SkinnedText>(), "slot", (int)BrandTextSlot.GameTitle);
            var slogan = Label(mainRt, "Slogan", "", 40, TopCenter, new Vector2(0, -395), new Vector2(1000, 60));
            UB.SetValue(slogan.gameObject.AddComponent<SkinnedText>(), "slot", (int)BrandTextSlot.Slogan);

            var play = Btn(mainRt, "PlayButton", "JUGAR", Green, Center, new Vector2(0, -90), new Vector2(580, 170), 84);
            var endless = Btn(mainRt, "EndlessButton", "INFINITO", Purple, Center, new Vector2(-220, -300), new Vector2(400, 120), 52);
            var endlessBest = Label(mainRt, "EndlessBest", "Récord: 0", 32, Center, new Vector2(-220, -390), new Vector2(400, 50));
            var shop = Btn(mainRt, "ShopButton", "TIENDA", Blue, Center, new Vector2(220, -300), new Vector2(400, 120), 52);
            var stars = Label(mainRt, "TotalStars", "0 / 27", 44, TopLeft, new Vector2(40, -150), new Vector2(320, 60), TextAlignmentOptions.Left, Gold);
            var sound = Btn(mainRt, "SoundButton", "Sonido: SÍ", Gray, BottomLeft, new Vector2(40, 40), new Vector2(320, 100), out var soundLabel, 38);

            var adDiamonds = AdButton(mainRt, "AdDiamondsButton", AdPlacement.MenuDiamonds, RewardedAdButton.RewardKind.Diamonds,
                UiSlot.DiamondIcon, new Color(0.15f, 0.6f, 0.75f), Right, new Vector2(-40, 90));
            AdButton(mainRt, "AdCoinsButton", AdPlacement.MenuCoins, RewardedAdButton.RewardKind.Coins,
                UiSlot.CoinIcon, new Color(0.85f, 0.6f, 0.1f), Right, new Vector2(-40, -90));

            UB.Set(main, "playButton", play);
            UB.Set(main, "endlessButton", endless);
            UB.Set(main, "shopButton", shop);
            UB.Set(main, "soundButton", sound);
            UB.Set(main, "soundLabel", soundLabel);
            UB.Set(main, "endlessBestText", endlessBest);
            UB.Set(main, "totalStarsText", stars);
            UB.SetValue(main, "screenId", ScreenIds.MainMenu);

            // ---------------- Mapa de niveles
            var mapRt = ScreenRoot(screensRoot, "LevelMapScreen");
            var map = mapRt.gameObject.AddComponent<LevelMapScreen>();
            Label(mapRt, "Title", "MAPA DE NIVELES", 72, TopCenter, new Vector2(0, -30), new Vector2(900, 100));
            var mapStars = Label(mapRt, "TotalStars", "0 / 27", 44, TopCenter, new Vector2(0, -125), new Vector2(400, 60), TextAlignmentOptions.Center, Gold);
            var mapBack = Btn(mapRt, "BackButton", "Atrás", Gray, TopLeft, new Vector2(30, -30), new Vector2(220, 100), 44);
            var mapShop = Btn(mapRt, "ShopButton", "Tienda", Blue, TopRight, new Vector2(-30, -150), new Vector2(240, 100), 44);
            var nodes = UB.VerticalScroll(mapRt, "LevelsScroll", 40, out var mapScroll, true, new Vector2(220, 220), 5);
            ((RectTransform)mapScroll.transform).Stretch(new Vector2(0.12f, 0.04f), new Vector2(0.88f, 0.8f));
            UB.Set(map, "nodesContainer", nodes);
            UB.Set(map, "nodePrefab", levelNodePrefab);
            UB.Set(map, "scroll", mapScroll);
            UB.Set(map, "totalStarsText", mapStars);
            UB.Set(map, "backButton", mapBack);
            UB.Set(map, "shopButton", mapShop);
            UB.SetValue(map, "screenId", ScreenIds.LevelMap);

            // ---------------- Tienda
            var shopRt = ScreenRoot(screensRoot, "ShopScreen");
            var shopCtrl = shopRt.gameObject.AddComponent<ShopController>();
            Label(shopRt, "Title", "TIENDA", 72, TopCenter, new Vector2(0, -30), new Vector2(600, 100));
            var shopBack = Btn(shopRt, "BackButton", "Atrás", Gray, TopLeft, new Vector2(30, -30), new Vector2(220, 100), 44);
            var tabChars = Btn(shopRt, "TabCharacters", "Personajes", Blue, TopCenter, new Vector2(-420, -150), new Vector2(380, 100), 42);
            var tabWeapons = Btn(shopRt, "TabWeapons", "Armas", Blue, TopCenter, new Vector2(0, -150), new Vector2(380, 100), 42);
            var tabCons = Btn(shopRt, "TabConsumables", "Potenciadores", Blue, TopCenter, new Vector2(420, -150), new Vector2(380, 100), 42);
            RectTransform Panel(string n, out RectTransform content)
            {
                var p = UB.Rect(n, shopRt).Stretch(new Vector2(0.05f, 0.17f), new Vector2(0.95f, 0.76f));
                content = UB.VerticalScroll(p, "Scroll", 20, out var sc);
                ((RectTransform)sc.transform).Full();
                return p;
            }
            var pChars = Panel("CharactersPanel", out var cChars);
            var pWeapons = Panel("WeaponsPanel", out var cWeapons);
            var pCons = Panel("ConsumablesPanel", out var cCons);
            AdButton(shopRt, "AdDiamondsButton", AdPlacement.ShopDiamonds, RewardedAdButton.RewardKind.Diamonds,
                UiSlot.DiamondIcon, new Color(0.15f, 0.6f, 0.75f), BottomLeft, new Vector2(60, 30));
            var shopAdCoins = AdButton(shopRt, "AdCoinsButton", AdPlacement.ShopCoins, RewardedAdButton.RewardKind.Coins,
                UiSlot.CoinIcon, new Color(0.85f, 0.6f, 0.1f), BottomRight, new Vector2(-60, 30));
            UB.Set(shopCtrl, "charactersTab", tabChars);
            UB.Set(shopCtrl, "weaponsTab", tabWeapons);
            UB.Set(shopCtrl, "consumablesTab", tabCons);
            UB.Set(shopCtrl, "charactersPanel", pChars.gameObject);
            UB.Set(shopCtrl, "weaponsPanel", pWeapons.gameObject);
            UB.Set(shopCtrl, "consumablesPanel", pCons.gameObject);
            UB.Set(shopCtrl, "charactersContainer", cChars);
            UB.Set(shopCtrl, "weaponsContainer", cWeapons);
            UB.Set(shopCtrl, "consumablesContainer", cCons);
            UB.Set(shopCtrl, "itemPrefab", shopItemPrefab);
            UB.Set(shopCtrl, "backButton", shopBack);
            UB.Set(shopCtrl, "adButtonHighlight", shopAdCoins.transform);
            UB.SetValue(shopCtrl, "screenId", ScreenIds.Shop);

            // ---------------- Pre-partida (popup)
            var preRoot = UB.PopupShell(screensRoot, "PreMatchPopup", new Vector2(1150, 1000), out var prePanel);
            var pre = preRoot.gameObject.AddComponent<PreMatchPopup>();
            var preTitle = Label(prePanel, "Title", "Nivel 1", 70, TopCenter, new Vector2(0, -25), new Vector2(900, 90));
            var preStars = Stars(prePanel, TopCenter, new Vector2(0, -115), new Vector2(300, 80), 70, 85);
            var preMission = Label(prePanel, "Mission", "Misión", 40, TopCenter, new Vector2(0, -200), new Vector2(1000, 110));
            var preIcon = UB.Image(prePanel, "CharacterIcon", null, Color.white);
            preIcon.preserveAspect = true;
            preIcon.rectTransform.Place(TopLeft, new Vector2(60, -320), new Vector2(120, 120));
            var preLoadout = Label(prePanel, "Loadout", "", 40, TopLeft, new Vector2(200, -345), new Vector2(850, 70), TextAlignmentOptions.Left);
            var consContainer = UB.Rect("Consumables", prePanel).Place(TopCenter, new Vector2(0, -455), new Vector2(1050, 365));
            var vl = consContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 10; vl.childControlWidth = true; vl.childControlHeight = false; vl.childForceExpandWidth = true; vl.childForceExpandHeight = false;
            var prePlay = Btn(prePanel, "PlayButton", "¡JUGAR!", Green, BottomCenter, new Vector2(170, 30), new Vector2(520, 130), 64);
            var preShop = Btn(prePanel, "ShopButton", "Tienda", Blue, BottomCenter, new Vector2(-290, 30), new Vector2(320, 110), 44);
            var preClose = Btn(prePanel, "CloseButton", "X", Red, TopRight, new Vector2(-20, -20), new Vector2(90, 90), 52);
            UB.Set(pre, "titleText", preTitle);
            UB.Set(pre, "missionText", preMission);
            UB.Set(pre, "bestStars", preStars);
            UB.Set(pre, "characterIcon", preIcon);
            UB.Set(pre, "loadoutText", preLoadout);
            UB.Set(pre, "consumablesContainer", consContainer);
            UB.Set(pre, "consumableSlotPrefab", consumableSlotPrefab);
            UB.Set(pre, "playButton", prePlay);
            UB.Set(pre, "shopButton", preShop);
            UB.Set(pre, "closeButton", preClose);
            UB.SetValue(pre, "screenId", ScreenIds.PreMatch);
            UB.SetValue(pre, "isPopup", true);
            UB.Set(pre, "popupPanel", prePanel);

            UB.Set(main, "preMatchPopup", pre);
            UB.Set(map, "preMatchPopup", pre);
            UB.SetArray(manager, "screens", new Object[] { main, map, shopCtrl, pre });
            UB.Set(manager, "defaultScreen", main);

            // ---------------- Barra superior de monedas (siempre visible)
            var topBar = UB.Rect("TopBar", canvas.transform).Full();
            CurrencyBox(topBar, "Diamonds", CurrencyType.Diamonds, new Vector2(-400, -30));
            CurrencyBox(topBar, "Coins", CurrencyType.Coins, new Vector2(-40, -30));

            string path = SceneDir + "/MainMenu.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static RewardedAdButton AdButton(Transform parent, string name, AdPlacement placement, RewardedAdButton.RewardKind kind,
            UiSlot icon, Color color, Vector2 anchor, Vector2 pos)
        {
            var b = Btn(parent, name, "Ver anuncio", color, anchor, pos, new Vector2(440, 140), out var label, 38);
            label.rectTransform.offsetMin = new Vector2(110, 10);
            Icon(b.transform, "Icon", icon, Left, new Vector2(18, 0), new Vector2(84, 84));
            Icon(b.transform, "PlayIcon", UiSlot.JumpButton, TopLeft, new Vector2(-14, 14), new Vector2(50, 50)).transform.localEulerAngles = new Vector3(0, 0, -90);
            var ad = b.gameObject.AddComponent<RewardedAdButton>();
            UB.SetValue(ad, "placement", (int)placement);
            UB.SetValue(ad, "reward", (int)kind);
            UB.Set(ad, "label", label);
            return ad;
        }

        private static void CurrencyBox(Transform parent, string name, CurrencyType type, Vector2 pos)
        {
            var box = UB.Panel(parent, name, Dark);
            box.rectTransform.Place(TopRight, pos, new Vector2(330, 90));
            var icon = Icon(box.transform, "Icon", type == CurrencyType.Coins ? UiSlot.CoinIcon : UiSlot.DiamondIcon, Left, new Vector2(8, 0), new Vector2(74, 74));
            var amount = Label(box.transform, "Amount", "0", 46, Left, new Vector2(90, 0), new Vector2(170, 80), TextAlignmentOptions.Left);
            var add = Btn(box.transform, "Add", "+", Green, Right, new Vector2(-8, 0), new Vector2(70, 70), 52);
            var display = box.gameObject.AddComponent<CurrencyDisplay>();
            UB.SetValue(display, "currency", (int)type);
            UB.Set(display, "amountText", amount);
            UB.Set(display, "icon", icon);
            UB.Set(display, "addButton", add);
        }

        // ================================================================ Escena: Juego

        private static string BuildGameScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var config = LoadGameConfig();
            PrepareBuilder(config.activeSkin);
            var env = config.activeSkin != null ? config.activeSkin.FindEnvironment("env_city") : null;

            // --- Cámara
            var cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = env != null ? env.cameraBackground : new Color(0.45f, 0.75f, 1f);
            cam.transform.position = new Vector3(0, 4, -10);
            cam.gameObject.AddComponent<AudioListener>();
            var follow = cam.gameObject.AddComponent<CameraFollow2D>();
            UB.SetValue(follow, "xLimits", new Vector2(-11.5f, 11.5f));
            UB.SetValue(follow, "yLimits", new Vector2(4f, 7f));

            // --- Escenario (geometría fija; el arte lo pone el skin)
            var envRoot = new GameObject("Environment");
            var builder = envRoot.AddComponent<EnvironmentBuilder>();
            var groundSprite = env != null ? env.groundSprite : null;
            int ground = Layer("Ground");

            SpriteRenderer Solid(string n, Vector2 pos, Vector2 size, bool oneWay)
            {
                var go = new GameObject(n) { layer = ground };
                go.transform.SetParent(envRoot.transform, false);
                go.transform.position = pos;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = groundSprite;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = size;
                var col = go.AddComponent<BoxCollider2D>();
                col.size = oneWay ? new Vector2(size.x, 0.3f) : size;
                col.offset = oneWay ? new Vector2(0, size.y * 0.5f - 0.15f) : Vector2.zero;
                if (oneWay)
                {
                    col.usedByEffector = true;
                    var eff = go.AddComponent<PlatformEffector2D>();
                    eff.useOneWay = true;
                    eff.surfaceArc = 170f;
                }
                return sr;
            }

            var floor = Solid("Ground", new Vector2(0, -1.5f), new Vector2(48, 3), false);
            var p1 = Solid("Platform_L", new Vector2(-10, 3), new Vector2(6, 1), true);
            var p2 = Solid("Platform_R", new Vector2(10, 3), new Vector2(6, 1), true);
            var p3 = Solid("Platform_Top", new Vector2(0, 5.8f), new Vector2(7, 1), true);
            UB.Set(builder, "targetCamera", cam);
            UB.SetArray(builder, "groundRenderers", new Object[] { floor, p1, p2, p3 });

            foreach (float x in new[] { -23.5f, 23.5f })
            {
                var wall = new GameObject(x < 0 ? "Wall_L" : "Wall_R") { layer = ground };
                wall.transform.SetParent(envRoot.transform, false);
                wall.transform.position = new Vector3(x, 10, 0);
                wall.AddComponent<BoxCollider2D>().size = new Vector2(1, 30);
            }

            var kill = new GameObject("KillZone");
            kill.transform.position = new Vector3(0, -9, 0);
            var killCol = kill.AddComponent<BoxCollider2D>();
            killCol.isTrigger = true;
            killCol.size = new Vector2(100, 2);
            kill.AddComponent<KillZone>();

            var spawn = new GameObject("PlayerSpawn").transform;
            spawn.position = new Vector3(0, 0.2f, 0);

            Transform Point(string n, Vector2 p, Transform parent)
            {
                var t = new GameObject(n).transform;
                t.SetParent(parent, false);
                t.position = p;
                return t;
            }
            var points = new GameObject("SpawnPoints").transform;
            var groundPoints = new Object[]
            {
                Point("G_L1", new Vector2(-21, 0.3f), points), Point("G_R1", new Vector2(21, 0.3f), points),
                Point("G_L2", new Vector2(-15, 0.3f), points), Point("G_R2", new Vector2(15, 0.3f), points)
            };
            var airPoints = new Object[]
            {
                Point("A_L", new Vector2(-19, 7.5f), points), Point("A_R", new Vector2(19, 7.5f), points), Point("A_Top", new Vector2(0, 10), points)
            };

            // --- Sistemas de partida
            var matchGo = new GameObject("Match");
            var match = matchGo.AddComponent<MatchController>();
            var spawner = matchGo.AddComponent<EnemySpawner>();
            var endless = matchGo.AddComponent<EndlessDirector>();
            UB.SetArray(spawner, "groundSpawnPoints", groundPoints);
            UB.SetArray(spawner, "airSpawnPoints", airPoints);
            UB.Set(endless, "spawner", spawner);

            // --- UI
            UB.CreateEventSystem();
            var canvas = UB.CreateCanvas("GameCanvas");
            var hudRt = UB.Rect("HUD", canvas.transform).Full();
            var hud = hudRt.gameObject.AddComponent<HUDController>();
            var input = hudRt.gameObject.AddComponent<PlayerInputHub>();

            // Joystick (mitad izquierda)
            var zone = UB.Image(hudRt, "JoystickZone", null, new Color(1, 1, 1, 0), true);
            zone.rectTransform.Stretch(Vector2.zero, new Vector2(0.45f, 0.7f));
            var joyBase = Icon(zone.transform, "Base", UiSlot.JoystickBase, Center, new Vector2(-180, -110), new Vector2(300, 300));
            var knob = Icon(joyBase.transform, "Knob", UiSlot.JoystickKnob, Center, Vector2.zero, new Vector2(140, 140));
            var joystick = zone.gameObject.AddComponent<VirtualJoystick>();
            Translucent(zone);
            UB.Set(joystick, "joystickBase", joyBase.rectTransform);
            UB.Set(joystick, "knob", knob.rectTransform);

            // Botones de acción (derecha)
            TouchActionButton Action(string n, UiSlot slot, Vector2 pos, float size)
            {
                var img = Icon(hudRt, n, slot, BottomRight, pos, new Vector2(size, size));
                img.raycastTarget = true;
                Translucent(img);
                return img.gameObject.AddComponent<TouchActionButton>();
            }
            var attack = Action("AttackButton", UiSlot.AttackButton, new Vector2(-60, 60), 250);
            var jump = Action("JumpButton", UiSlot.JumpButton, new Vector2(-350, 50), 200);
            var special = Action("SpecialButton", UiSlot.SpecialButton, new Vector2(-110, 345), 170);
            var cd = UB.Image(special.transform, "Cooldown", null, new Color(0, 0, 0, 0.6f));
            cd.rectTransform.Full(8f);
            cd.type = Image.Type.Filled; cd.fillMethod = Image.FillMethod.Radial360; cd.fillAmount = 0f;
            UB.Set(special, "cooldownFill", cd);
            UB.Set(input, "joystick", joystick);
            UB.Set(input, "jumpButton", jump);
            UB.Set(input, "attackButton", attack);
            UB.Set(input, "specialButton", special);

            // Potenciadores en partida
            void PowerUp(string n, ConsumableType type, UiSlot slot, Vector2 pos)
            {
                var img = Icon(hudRt, n, slot, BottomRight, pos, new Vector2(130, 130));
                img.raycastTarget = true;
                img.gameObject.AddComponent<Button>().targetGraphic = img;
                var fill = UB.Image(img.transform, "Duration", null, new Color(0, 0, 0, 0.5f));
                fill.rectTransform.Full(6f);
                fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Radial360; fill.fillAmount = 0f;
                Translucent(img);
                var pu = img.gameObject.AddComponent<PowerUpButtonUI>();
                UB.SetValue(pu, "type", (int)type);
                UB.Set(pu, "icon", img);
                UB.Set(pu, "durationFill", fill);
            }
            PowerUp("ShieldButton", ConsumableType.Shield, UiSlot.ShieldIcon, new Vector2(-600, 60));
            PowerUp("ConverterButton", ConsumableType.EnemyToCoins, UiSlot.ConverterIcon, new Vector2(-760, 60));

            // Vida
            var healthBar = UB.FillBar(hudRt, "HealthBar", Dark, Green, out var healthFill);
            healthBar.rectTransform.Place(TopLeft, new Vector2(30, -30), new Vector2(480, 60));
            var healthText = UB.Text(healthBar.transform, "Text", "100/100", 34);
            healthText.rectTransform.Full();

            // Misión, tiempo y puntuación
            var mission = Label(hudRt, "Mission", "Misión", 46, TopCenter, new Vector2(0, -20), new Vector2(1000, 70));
            var missionBar = UB.FillBar(hudRt, "MissionProgress", Dark, Gold, out var missionFill);
            missionBar.rectTransform.Place(TopCenter, new Vector2(0, -95), new Vector2(600, 30));
            var timer = Label(hudRt, "Timer", "2:30", 58, TopCenter, new Vector2(0, -130), new Vector2(260, 70));
            var score = Label(hudRt, "Score", "Puntos: 0", 44, TopCenter, new Vector2(0, -200), new Vector2(600, 60));

            // Monedas, x2 y pausa
            var coinsBox = UB.Rect("Coins", hudRt).Place(TopRight, new Vector2(-170, -40), new Vector2(280, 80));
            var coinIcon = Icon(coinsBox, "Icon", UiSlot.CoinIcon, Left, Vector2.zero, new Vector2(70, 70));
            var coinsText = Label(coinsBox, "Amount", "0", 52, Left, new Vector2(80, 0), new Vector2(200, 70), TextAlignmentOptions.Left);
            var badge = UB.Panel(hudRt, "MultiplierBadge", Gold);
            badge.rectTransform.Place(TopRight, new Vector2(-170, -125), new Vector2(140, 60));
            UB.Text(badge.transform, "Text", "x2", 44).rectTransform.Full();
            var pauseImg = Icon(hudRt, "PauseButton", UiSlot.PauseButton, TopRight, new Vector2(-30, -30), new Vector2(110, 110));
            pauseImg.raycastTarget = true;
            var pause = pauseImg.gameObject.AddComponent<Button>();
            pause.targetGraphic = pauseImg;

            // Jefe
            var bossBar = UB.FillBar(hudRt, "BossBar", Dark, Red, out var bossFill);
            bossBar.rectTransform.Place(TopCenter, new Vector2(0, -270), new Vector2(900, 44));
            UB.Text(bossBar.transform, "Label", "JEFE", 32).rectTransform.Full();

            // Banner
            var bannerImg = UB.Panel(hudRt, "Banner", new Color(0, 0, 0, 0.6f));
            bannerImg.raycastTarget = false;
            bannerImg.rectTransform.Place(Center, new Vector2(0, 180), new Vector2(1500, 180));
            var banner = bannerImg.gameObject.AddComponent<CanvasGroup>();
            banner.blocksRaycasts = false;
            var bannerText = UB.Text(bannerImg.transform, "Text", "", 64);
            bannerText.rectTransform.Full(20f);
            bannerImg.gameObject.SetActive(false);

            UB.Set(hud, "healthFill", healthFill);
            UB.Set(hud, "healthText", healthText);
            UB.Set(hud, "coinsText", coinsText);
            UB.Set(hud, "coinsIcon", coinIcon.rectTransform);
            UB.Set(hud, "missionText", mission);
            UB.Set(hud, "missionProgressFill", missionFill);
            UB.Set(hud, "missionProgressRoot", missionBar.gameObject);
            UB.Set(hud, "timerText", timer);
            UB.Set(hud, "scoreText", score);
            UB.Set(hud, "multiplierBadge", badge.gameObject);
            UB.Set(hud, "bossBarRoot", bossBar.gameObject);
            UB.Set(hud, "bossFill", bossFill);
            UB.Set(hud, "banner", banner);
            UB.Set(hud, "bannerText", bannerText);
            UB.Set(hud, "pauseButton", pause);

            // --- Popups
            var revive = BuildRevivePopup(canvas.transform);
            var pausePopup = BuildPausePopup(canvas.transform);
            var result = BuildResultPopup(canvas.transform);

            UB.Set(match, "playerSpawnPoint", spawn);
            UB.Set(match, "spawner", spawner);
            UB.Set(match, "endlessDirector", endless);
            UB.Set(match, "cameraFollow", follow);
            UB.Set(match, "environment", builder);
            UB.Set(match, "hud", hud);
            UB.Set(match, "revivePopup", revive);
            UB.Set(match, "resultPopup", result);
            UB.Set(match, "pausePopup", pausePopup);

            string path = SceneDir + "/Game.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static void HidePopup(Component popup)
        {
            popup.GetComponent<CanvasGroup>().alpha = 0f;
            popup.gameObject.SetActive(false);
        }

        private static RevivePopup BuildRevivePopup(Transform canvas)
        {
            var root = UB.PopupShell(canvas, "RevivePopup", new Vector2(900, 780), out var panel);
            var popup = root.gameObject.AddComponent<RevivePopup>();
            Label(panel, "Title", "¿CONTINUAR?", 80, TopCenter, new Vector2(0, -40), new Vector2(800, 110));
            var ring = UB.FillBar(panel, "Countdown", new Color(0, 0, 0, 0.4f), Gold, out var fill, true);
            ring.rectTransform.Place(Center, new Vector2(0, 90), new Vector2(220, 220));
            var count = UB.Text(ring.transform, "Number", "6", 110);
            count.rectTransform.Full();
            var reviveBtn = Btn(panel, "ReviveButton", Texts.AdRevive, Green, BottomCenter, new Vector2(0, 190), new Vector2(680, 150), out var reviveLabel, 46);
            var decline = Btn(panel, "DeclineButton", Texts.NoThanks, Gray, BottomCenter, new Vector2(0, 45), new Vector2(380, 100), 38);
            UB.Set(popup, "reviveButton", reviveBtn);
            UB.Set(popup, "reviveLabel", reviveLabel);
            UB.Set(popup, "declineButton", decline);
            UB.Set(popup, "countdownText", count);
            UB.Set(popup, "countdownFill", fill);
            UB.SetValue(popup, "isPopup", true);
            UB.Set(popup, "popupPanel", panel);
            HidePopup(popup);
            return popup;
        }

        private static PausePopup BuildPausePopup(Transform canvas)
        {
            var root = UB.PopupShell(canvas, "PausePopup", new Vector2(700, 840), out var panel);
            var popup = root.gameObject.AddComponent<PausePopup>();
            Label(panel, "Title", "PAUSA", 80, TopCenter, new Vector2(0, -40), new Vector2(600, 110));
            var resume = Btn(panel, "ResumeButton", "Continuar", Green, Center, new Vector2(0, 130), new Vector2(480, 115), 50);
            var restart = Btn(panel, "RestartButton", "Reiniciar", Blue, Center, new Vector2(0, -5), new Vector2(480, 115), 50);
            var menu = Btn(panel, "MenuButton", "Menú", Gray, Center, new Vector2(0, -140), new Vector2(480, 115), 50);
            var sound = Btn(panel, "SoundButton", "Sonido: SÍ", Gray, Center, new Vector2(0, -275), new Vector2(480, 100), out var soundLabel, 40);
            UB.Set(popup, "resumeButton", resume);
            UB.Set(popup, "restartButton", restart);
            UB.Set(popup, "menuButton", menu);
            UB.Set(popup, "soundButton", sound);
            UB.Set(popup, "soundLabel", soundLabel);
            UB.SetValue(popup, "isPopup", true);
            UB.Set(popup, "popupPanel", panel);
            HidePopup(popup);
            return popup;
        }

        private static ResultPopup BuildResultPopup(Transform canvas)
        {
            var root = UB.PopupShell(canvas, "ResultPopup", new Vector2(1100, 980), out var panel);
            var popup = root.gameObject.AddComponent<ResultPopup>();
            var title = Label(panel, "Title", Texts.Victory, 96, TopCenter, new Vector2(0, -25), new Vector2(1000, 120));
            var subtitle = Label(panel, "Subtitle", "Nivel 1", 44, TopCenter, new Vector2(0, -140), new Vector2(900, 60));
            var stars = Stars(panel, TopCenter, new Vector2(0, -215), new Vector2(620, 200), 160, 195, true);
            var score = Label(panel, "Score", "Puntos: 0", 56, TopCenter, new Vector2(0, -230), new Vector2(900, 150));
            var coinsRow = UB.Rect("Coins", panel).Place(TopCenter, new Vector2(0, -440), new Vector2(460, 100));
            Icon(coinsRow, "Icon", UiSlot.CoinIcon, Left, Vector2.zero, new Vector2(90, 90));
            var coins = Label(coinsRow, "Amount", "+0", 72, Left, new Vector2(110, 0), new Vector2(350, 100), TextAlignmentOptions.Left, Gold);
            var record = UB.Panel(panel, "NewRecord", Gold);
            record.rectTransform.Place(TopCenter, new Vector2(330, -150), new Vector2(330, 70));
            record.rectTransform.localEulerAngles = new Vector3(0, 0, 8);
            UB.Text(record.transform, "Text", Texts.NewRecord, 36).rectTransform.Full(6f);

            var doubleAd = AdButton(panel, "DoubleCoinsButton", AdPlacement.DoubleCoins, RewardedAdButton.RewardKind.Custom,
                UiSlot.CoinIcon, Green, BottomCenter, new Vector2(0, 195));
            ((RectTransform)doubleAd.transform).sizeDelta = new Vector2(780, 140);
            var menu = Btn(panel, "MenuButton", "Menú", Gray, BottomCenter, new Vector2(-340, 40), new Vector2(300, 120), 46);
            var retry = Btn(panel, "RetryButton", "Reintentar", Blue, BottomCenter, new Vector2(0, 40), new Vector2(320, 120), 46);
            var next = Btn(panel, "NextButton", "Siguiente", Green, BottomCenter, new Vector2(340, 40), new Vector2(300, 120), 46);

            UB.Set(popup, "titleText", title);
            UB.Set(popup, "subtitleText", subtitle);
            UB.Set(popup, "stars", stars);
            UB.Set(popup, "coinsText", coins);
            UB.Set(popup, "scoreText", score);
            UB.Set(popup, "newRecordBadge", record.gameObject);
            UB.Set(popup, "doubleCoinsButton", doubleAd);
            UB.Set(popup, "nextButton", next);
            UB.Set(popup, "retryButton", retry);
            UB.Set(popup, "menuButton", menu);
            UB.SetValue(popup, "isPopup", true);
            UB.Set(popup, "popupPanel", panel);
            HidePopup(popup);
            return popup;
        }
    }
}
