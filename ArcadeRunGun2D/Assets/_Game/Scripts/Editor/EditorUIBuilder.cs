using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HyperArcade.EditorTools
{
    /// <summary>Atajos para construir jerarquías de uGUI desde código de editor.</summary>
    public static class EditorUIBuilder
    {
        public static Sprite PanelSprite;
        public static Sprite ButtonSprite;
        public static Color TextColor = Color.white;
        public static Color ButtonColor = new Color(1f, 0.55f, 0.1f);
        public static Color PanelColor = new Color(0.12f, 0.14f, 0.22f, 0.96f);

        // ------------------------------------------------------------ Raíz

        public static Canvas CreateCanvas(string name, int sortingOrder = 0)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        // ------------------------------------------------------------ Layout

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>Ancla a un punto (anchor = pivot) con posición y tamaño.</summary>
        public static RectTransform Place(this RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>Estira entre dos anclas con márgenes.</summary>
        public static RectTransform Stretch(this RectTransform rt, Vector2 min, Vector2 max, float margin = 0f)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(margin, margin);
            rt.offsetMax = new Vector2(-margin, -margin);
            return rt;
        }

        public static RectTransform Full(this RectTransform rt, float margin = 0f) => rt.Stretch(Vector2.zero, Vector2.one, margin);

        // ------------------------------------------------------------ Elementos

        public static Image Image(Transform parent, string name, Sprite sprite, Color color, bool raycast = false)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            if (sprite != null && sprite.border != Vector4.zero) img.type = UnityEngine.UI.Image.Type.Sliced;
            return img;
        }

        public static Image Panel(Transform parent, string name, Color? color = null) =>
            Image(parent, name, PanelSprite, color ?? PanelColor, true);

        public static Image FillBar(Transform parent, string name, Color background, Color fill, out Image fillImage, bool radial = false)
        {
            var bg = Image(parent, name, radial ? null : PanelSprite, background);
            fillImage = Image(bg.transform, "Fill", radial ? null : PanelSprite, fill);
            fillImage.rectTransform.Full(radial ? 0f : 6f);
            fillImage.type = UnityEngine.UI.Image.Type.Filled;
            fillImage.fillMethod = radial ? UnityEngine.UI.Image.FillMethod.Radial360 : UnityEngine.UI.Image.FillMethod.Horizontal;
            fillImage.fillAmount = 1f;
            return bg;
        }

        public static TextMeshProUGUI Text(Transform parent, string name, string text, float size,
            TextAlignmentOptions align = TextAlignmentOptions.Center, Color? color = null)
        {
            var rt = Rect(name, parent);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.color = color ?? TextColor;
            tmp.raycastTarget = false;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = size * 0.5f;
            tmp.fontSizeMax = size;
            rt.gameObject.AddComponent<SkinnedFont>();
            return tmp;
        }

        public static Button Button(Transform parent, string name, string label, out TextMeshProUGUI labelText,
            Color? color = null, float fontSize = 44f)
        {
            var img = Image(parent, name, ButtonSprite, color ?? ButtonColor, true);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            labelText = Text(img.transform, "Label", label, fontSize);
            labelText.rectTransform.Full(12f);
            return btn;
        }

        public static Button Button(Transform parent, string name, string label, Color? color = null, float fontSize = 44f) =>
            Button(parent, name, label, out _, color, fontSize);

        public static Button IconButton(Transform parent, string name, Sprite icon, Color bg)
        {
            var img = Image(parent, name, ButtonSprite, bg, true);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var i = Image(img.transform, "Icon", icon, Color.white);
            i.preserveAspect = true;
            i.rectTransform.Full(14f);
            return btn;
        }

        public static Toggle Toggle(Transform parent, string name, out Image checkmark)
        {
            var bg = Image(parent, name, ButtonSprite, new Color(1f, 1f, 1f, 0.9f), true);
            var toggle = bg.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = bg;
            checkmark = Image(bg.transform, "Check", ButtonSprite, new Color(0.2f, 0.8f, 0.3f));
            checkmark.rectTransform.Full(10f);
            toggle.graphic = checkmark;
            toggle.isOn = false;
            return toggle;
        }

        /// <summary>ScrollRect vertical con contenido auto-ajustable. Devuelve el contenedor de contenido.</summary>
        public static RectTransform VerticalScroll(Transform parent, string name, float spacing, out ScrollRect scroll, bool grid = false, Vector2 cellSize = default, int columns = 3)
        {
            var root = Rect(name, parent);
            var bg = root.gameObject.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.01f);
            scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;

            var viewport = Rect("Viewport", root).Full();
            viewport.gameObject.AddComponent<RectMask2D>();
            scroll.viewport = viewport;

            var content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            scroll.content = content;

            if (grid)
            {
                var g = content.gameObject.AddComponent<GridLayoutGroup>();
                g.cellSize = cellSize;
                g.spacing = new Vector2(spacing, spacing);
                g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                g.constraintCount = columns;
                g.childAlignment = TextAnchor.UpperCenter;
                g.padding = new RectOffset(20, 20, 20, 20);
            }
            else
            {
                var v = content.gameObject.AddComponent<VerticalLayoutGroup>();
                v.spacing = spacing;
                v.childControlWidth = true;
                v.childControlHeight = false;
                v.childForceExpandWidth = true;
                v.childForceExpandHeight = false;
                v.padding = new RectOffset(16, 16, 16, 16);
            }
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return content;
        }

        /// <summary>Fondo oscuro a pantalla completa + panel central (estructura de popup).</summary>
        public static RectTransform PopupShell(Transform parent, string name, Vector2 panelSize, out RectTransform panel)
        {
            var root = Rect(name, parent).Full();
            root.gameObject.AddComponent<CanvasGroup>();
            var dim = Image(root, "Dim", null, new Color(0f, 0f, 0f, 0.7f), true);
            dim.rectTransform.Full();
            panel = Panel(root, "Panel").rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, panelSize);
            return root;
        }

        // ------------------------------------------------------------ Campos privados [SerializeField]

        public static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[Setup] Campo '{field}' no encontrado en {target.GetType().Name}"); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            if (!ReferenceEquals(value, null) && p.objectReferenceValue == null)
                Debug.LogError($"[Setup] '{target.GetType().Name}.{field}' quedó vacío: el asset se descargó de memoria.");
        }

        public static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[Setup] Campo '{field}' no encontrado en {target.GetType().Name}"); return; }
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Asigna un campo de un elemento de un array de clases serializables (ej. actions[1].button).</summary>
        public static void SetArrayElementField(Object target, string arrayField, int index, string field, Object value)
        {
            var so = new SerializedObject(target);
            var arr = so.FindProperty(arrayField);
            if (arr.arraySize <= index) arr.arraySize = index + 1;
            arr.GetArrayElementAtIndex(index).FindPropertyRelative(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetValue(Object target, string field, int value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[Setup] Campo '{field}' no encontrado en {target.GetType().Name}"); return; }
            if (p.propertyType == SerializedPropertyType.Enum) p.enumValueIndex = value;
            else p.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetValue(Object target, string field, bool value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[Setup] Campo '{field}' no encontrado en {target.GetType().Name}"); return; }
            p.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetValue(Object target, string field, string value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[Setup] Campo '{field}' no encontrado en {target.GetType().Name}"); return; }
            p.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetValue(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[Setup] Campo '{field}' no encontrado en {target.GetType().Name}"); return; }
            p.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetValue(Object target, string field, Vector2 value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[Setup] Campo '{field}' no encontrado en {target.GetType().Name}"); return; }
            p.vector2Value = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
