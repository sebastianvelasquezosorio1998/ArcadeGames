using System.Collections.Generic;
using UnityEngine;

namespace HyperArcade
{
    /// <summary>
    /// Pool genérico por prefab (proyectiles, enemigos, monedas). Evita picos de GC en móvil.
    /// Se vacía al cambiar de escena (los objetos de la escena anterior se destruyen).
    /// </summary>
    public static class PoolManager
    {
        private static readonly Dictionary<GameObject, Stack<GameObject>> Pools = new Dictionary<GameObject, Stack<GameObject>>();

        public static T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component
        {
            return Spawn(prefab.gameObject, position, rotation).GetComponent<T>();
        }

        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (!Pools.TryGetValue(prefab, out var stack))
            {
                stack = new Stack<GameObject>();
                Pools[prefab] = stack;
            }

            GameObject go = null;
            while (stack.Count > 0 && go == null) go = stack.Pop(); // descarta referencias destruidas

            if (go == null)
            {
                go = Object.Instantiate(prefab, position, rotation);
                var pooled = go.GetComponent<PooledObject>();
                if (pooled == null) pooled = go.AddComponent<PooledObject>();
                pooled.SourcePrefab = prefab;
            }
            else
            {
                go.transform.SetPositionAndRotation(position, rotation);
                go.SetActive(true);
            }
            return go;
        }

        public static void Despawn(GameObject go)
        {
            if (go == null || !go.activeSelf) return;
            var pooled = go.GetComponent<PooledObject>();
            if (pooled == null || pooled.SourcePrefab == null)
            {
                Object.Destroy(go);
                return;
            }
            go.SetActive(false);
            if (!Pools.TryGetValue(pooled.SourcePrefab, out var stack))
            {
                stack = new Stack<GameObject>();
                Pools[pooled.SourcePrefab] = stack;
            }
            stack.Push(go);
        }

        public static void Clear() => Pools.Clear();
    }
}
