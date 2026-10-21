using System.Collections;
using UnityEngine;

namespace HyperArcade
{
    public static class ScreenIds
    {
        public const string MainMenu = "MainMenu";
        public const string LevelMap = "LevelMap";
        public const string Shop = "Shop";
        public const string PreMatch = "PreMatch";
    }

    /// <summary>
    /// Base de pantallas y popups. Funciona con Time.timeScale = 0 (usa tiempo no escalado).
    /// Los popups aparecen con un pequeño "pop" de escala.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIScreen : MonoBehaviour
    {
        [SerializeField] private string screenId;
        [SerializeField] private bool isPopup;
        [SerializeField] private float fadeDuration = 0.15f;
        [Tooltip("Panel a escalar en la animación de popup (opcional).")]
        [SerializeField] private RectTransform popupPanel;

        public string Id => string.IsNullOrEmpty(screenId) ? GetType().Name : screenId;
        public bool IsPopup => isPopup;
        public bool IsVisible { get; private set; }

        private CanvasGroup _group;
        private Coroutine _anim;

        private CanvasGroup Group
        {
            get
            {
                if (_group == null) _group = GetComponent<CanvasGroup>();
                return _group;
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
            IsVisible = true;
            Group.interactable = true;
            Group.blocksRaycasts = true;
            transform.SetAsLastSibling();
            OnShow();
            Animate(true);
        }

        public void Hide()
        {
            if (!gameObject.activeInHierarchy) { HideImmediate(); return; }
            IsVisible = false;
            Group.interactable = false;
            Group.blocksRaycasts = false;
            OnHide();
            Animate(false);
        }

        public void HideImmediate()
        {
            bool wasVisible = IsVisible;
            IsVisible = false;
            Group.alpha = 0f;
            Group.interactable = false;
            Group.blocksRaycasts = false;
            if (wasVisible) OnHide();
            gameObject.SetActive(false);
        }

        protected virtual void OnShow() { }
        protected virtual void OnHide() { }

        private void Animate(bool show)
        {
            if (_anim != null) StopCoroutine(_anim);
            _anim = StartCoroutine(AnimRoutine(show));
        }

        private IEnumerator AnimRoutine(bool show)
        {
            float from = Group.alpha, to = show ? 1f : 0f;
            for (float t = 0f; t < fadeDuration; t += Time.unscaledDeltaTime)
            {
                float k = t / fadeDuration;
                Group.alpha = Mathf.Lerp(from, to, k);
                if (isPopup && popupPanel != null && show)
                    popupPanel.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, EaseOutBack(k));
                yield return null;
            }
            Group.alpha = to;
            if (popupPanel != null) popupPanel.localScale = Vector3.one;
            _anim = null;
            if (!show) gameObject.SetActive(false);
        }

        private static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }
    }
}
