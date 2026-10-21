using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HyperArcade
{
    /// <summary>Nodo del mapa de progresión: número, 0–3 estrellas y candado.</summary>
    public class LevelNodeUI : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text numberText;
        [SerializeField] private StarsDisplay stars;
        [SerializeField] private GameObject lockedOverlay;
        [Tooltip("Marcador del siguiente nivel a jugar (pulso/flecha).")]
        [SerializeField] private GameObject currentMarker;

        private LevelData _level;
        private Action<LevelData> _onClick;

        public RectTransform Rect => (RectTransform)transform;

        private void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            button.onClick.AddListener(() =>
            {
                AudioManager.Play(SfxId.ButtonClick);
                _onClick?.Invoke(_level);
            });
        }

        public void Setup(LevelData level, int index, bool unlocked, int starCount, bool isCurrent, Action<LevelData> onClick)
        {
            _level = level;
            _onClick = onClick;
            numberText.text = (index + 1).ToString();
            button.interactable = unlocked;
            if (lockedOverlay != null) lockedOverlay.SetActive(!unlocked);
            if (stars != null)
            {
                stars.gameObject.SetActive(unlocked);
                stars.SetStars(starCount);
            }
            if (currentMarker != null) currentMarker.SetActive(isCurrent);
        }

        private void Update()
        {
            if (currentMarker != null && currentMarker.activeSelf)
                currentMarker.transform.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(Time.unscaledTime * 5f));
        }
    }
}
