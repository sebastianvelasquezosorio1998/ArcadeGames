using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HyperArcade
{
    /// <summary>
    /// Mapa de Niveles: genera un nodo por cada LevelData de GameConfig.
    /// Un nivel se desbloquea con 1★ en el anterior. Al tocar un nivel se abre la pre-partida.
    /// El "camino" visual es el fondo (UiSlot.MenuBackground) y el layout del contenedor.
    /// </summary>
    public class LevelMapScreen : UIScreen
    {
        [SerializeField] private RectTransform nodesContainer;
        [SerializeField] private LevelNodeUI nodePrefab;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private TMP_Text totalStarsText;
        [SerializeField] private Button backButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private PreMatchPopup preMatchPopup;
        [Tooltip("Desmarcar si el mapa sube de abajo (nivel 1) hacia arriba.")]
        [SerializeField] private bool firstLevelAtTop = true;

        private readonly List<LevelNodeUI> _nodes = new List<LevelNodeUI>();

        private void Awake()
        {
            if (backButton != null) backButton.onClick.AddListener(() => UIScreenManager.Instance.Back());
            if (shopButton != null) shopButton.onClick.AddListener(() => UIScreenManager.Instance.Show(ScreenIds.Shop));
        }

        protected override void OnShow() => Build();

        private void Build()
        {
            var levels = GameConfig.Instance.levels;
            while (_nodes.Count < levels.Count) _nodes.Add(Instantiate(nodePrefab, nodesContainer));
            for (int i = 0; i < _nodes.Count; i++) _nodes[i].gameObject.SetActive(i < levels.Count);

            int current = 0;
            for (int i = 0; i < levels.Count; i++)
            {
                bool unlocked = PlayerProgress.IsLevelUnlocked(i);
                int stars = PlayerProgress.GetStars(levels[i].id);
                if (unlocked) current = i;
                _nodes[i].Setup(levels[i], i, unlocked, stars, false, OnLevelSelected);
            }
            if (levels.Count > 0)
                _nodes[current].Setup(levels[current], current, true, PlayerProgress.GetStars(levels[current].id), true, OnLevelSelected);

            if (totalStarsText != null) totalStarsText.text = PlayerProgress.TotalStars + " / " + levels.Count * 3;
            ScrollTo(current, levels.Count);
        }

        private void ScrollTo(int index, int count)
        {
            if (scroll == null || count <= 1) return;
            Canvas.ForceUpdateCanvases();
            float t = (float)index / (count - 1);
            if (scroll.vertical) scroll.verticalNormalizedPosition = firstLevelAtTop ? 1f - t : t;
            else scroll.horizontalNormalizedPosition = t;
        }

        private void OnLevelSelected(LevelData level)
        {
            if (preMatchPopup != null)
            {
                preMatchPopup.SetupLevel(level);
                UIScreenManager.Instance.Show(preMatchPopup.Id);
            }
            else GameSession.StartLevel(level);
        }
    }
}
