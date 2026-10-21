using System.Collections.Generic;
using UnityEngine;

namespace HyperArcade
{
    /// <summary>
    /// Construye el escenario (fondos con parallax + sprite del suelo) a partir del
    /// EnvironmentSet del skin. La geometría/colisiones del nivel no cambian con el reskin.
    /// </summary>
    public class EnvironmentBuilder : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [Tooltip("SpriteRenderers del suelo y plataformas (draw mode Tiled). Sus colliders no se tocan.")]
        [SerializeField] private SpriteRenderer[] groundRenderers;
        [SerializeField] private float layersBaseY = 2f;

        private readonly List<(Transform t, float parallax, float startX)> _layers = new List<(Transform, float, float)>();
        private float _camStartX;

        public void Build(string environmentKey)
        {
            if (targetCamera == null) targetCamera = Camera.main;
            var env = SkinManager.GetEnvironment(environmentKey);
            if (env == null) return;

            if (targetCamera != null)
            {
                targetCamera.backgroundColor = env.cameraBackground;
                _camStartX = targetCamera.transform.position.x;
            }

            if (env.groundSprite != null && groundRenderers != null)
            {
                foreach (var ground in groundRenderers)
                {
                    if (ground == null) continue;
                    Vector2 size = ground.drawMode == SpriteDrawMode.Simple ? (Vector2)ground.bounds.size : ground.size;
                    ground.sprite = env.groundSprite;
                    ground.drawMode = SpriteDrawMode.Tiled;
                    ground.size = size;
                }
            }

            foreach (var layer in env.layers)
            {
                if (layer == null || layer.sprite == null) continue;
                var go = new GameObject("BG_" + layer.sprite.name);
                go.transform.SetParent(transform, false);
                go.transform.position = new Vector3(_camStartX, layersBaseY + layer.yOffset, 10f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = layer.sprite;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = new Vector2(layer.tiledWidth, layer.sprite.bounds.size.y);
                sr.sortingOrder = layer.sortingOrder;
                _layers.Add((go.transform, layer.parallax, _camStartX));
            }
        }

        private void LateUpdate()
        {
            if (targetCamera == null) return;
            float camDelta = targetCamera.transform.position.x - _camStartX;
            foreach (var (t, parallax, startX) in _layers)
            {
                var p = t.position;
                p.x = startX + camDelta * parallax;
                t.position = p;
            }
        }
    }
}
