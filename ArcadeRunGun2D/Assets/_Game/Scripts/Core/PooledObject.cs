using UnityEngine;

namespace HyperArcade
{
    /// <summary>Marca un objeto con el prefab del que proviene para devolverlo a su pool.</summary>
    public class PooledObject : MonoBehaviour
    {
        public GameObject SourcePrefab { get; set; }
    }
}
