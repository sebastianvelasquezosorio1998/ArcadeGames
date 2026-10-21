using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace HyperArcade
{
    /// <summary>Muestra 0–3 estrellas (mapa de niveles y ventana de calificación).</summary>
    public class StarsDisplay : MonoBehaviour
    {
        [SerializeField] private Image[] stars = new Image[3];
        [SerializeField] private Color emptyTint = new Color(0.25f, 0.25f, 0.25f, 0.9f);

        private Sprite _full, _empty;
        private bool _loaded;

        private void LoadSprites()
        {
            if (_loaded) return;
            _loaded = true;
            _full = SkinManager.GetUiSprite(UiSlot.StarFull);
            _empty = SkinManager.GetUiSprite(UiSlot.StarEmpty);
        }

        public void SetStars(int count)
        {
            for (int i = 0; i < stars.Length; i++) SetStar(i, i < count);
        }

        /// <summary>Animación de calificación: estrella a estrella con sonido (tiempo no escalado).</summary>
        public Coroutine AnimateStars(int count, float delayBetween = 0.35f)
        {
            SetStars(0);
            return StartCoroutine(AnimateRoutine(count, delayBetween));
        }

        private IEnumerator AnimateRoutine(int count, float delayBetween)
        {
            for (int i = 0; i < Mathf.Min(count, stars.Length); i++)
            {
                yield return new WaitForSecondsRealtime(delayBetween);
                SetStar(i, true);
                AudioManager.Play(SfxId.Star);
                var t = stars[i].transform;
                for (float k = 0; k < 0.25f; k += Time.unscaledDeltaTime)
                {
                    t.localScale = Vector3.one * (1f + 0.6f * Mathf.Sin(k / 0.25f * Mathf.PI));
                    yield return null;
                }
                t.localScale = Vector3.one;
            }
        }

        private void SetStar(int index, bool on)
        {
            var img = stars[index];
            if (img == null) return;
            LoadSprites();
            if (on && _full != null) img.sprite = _full;
            else if (!on && _empty != null) img.sprite = _empty;
            img.color = on ? Color.white : (_empty != null ? Color.white : emptyTint);
        }
    }
}
