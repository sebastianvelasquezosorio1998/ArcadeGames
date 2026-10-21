using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HyperArcade
{
    /// <summary>
    /// Mensaje flotante breve ("+10 Diamantes", "Anuncio no disponible").
    /// Se construye por código para no depender de prefabs: Toast.Show("texto").
    /// </summary>
    public class Toast : MonoBehaviour
    {
        private static Toast _instance;

        private CanvasGroup _group;
        private TextMeshProUGUI _label;
        private Coroutine _routine;

        public static void Show(string message, float seconds = 1.8f)
        {
            if (_instance == null) { Debug.Log("[Toast] " + message); return; }
            _instance.ShowInternal(message, seconds);
        }

        private void Awake()
        {
            _instance = this;

            var canvasGo = new GameObject("ToastCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var panel = new GameObject("Toast", typeof(RectTransform));
            panel.transform.SetParent(canvasGo.transform, false);
            var rt = (RectTransform)panel.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.82f);
            rt.sizeDelta = new Vector2(900, 110);
            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.75f);
            bg.raycastTarget = false;
            _group = panel.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(panel.transform, false);
            var trt = (RectTransform)textGo.transform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            _label = textGo.AddComponent<TextMeshProUGUI>();
            _label.alignment = TextAlignmentOptions.Center;
            _label.fontSize = 48;
            _label.color = Color.white;
            _label.raycastTarget = false;
        }

        private void ShowInternal(string message, float seconds)
        {
            if (SkinManager.Font != null) _label.font = SkinManager.Font;
            _label.text = message;
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(Animate(seconds));
        }

        private IEnumerator Animate(float seconds)
        {
            for (float t = 0; t < 0.15f; t += Time.unscaledDeltaTime) { _group.alpha = t / 0.15f; yield return null; }
            _group.alpha = 1f;
            yield return new WaitForSecondsRealtime(seconds);
            for (float t = 0; t < 0.3f; t += Time.unscaledDeltaTime) { _group.alpha = 1f - t / 0.3f; yield return null; }
            _group.alpha = 0f;
        }
    }
}
