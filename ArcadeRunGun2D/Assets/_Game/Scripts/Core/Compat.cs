using UnityEngine;

namespace HyperArcade
{
    /// <summary>
    /// Pequeñas diferencias de API entre Unity 2021/2022 LTS y Unity 6.
    /// </summary>
    public static class Compat
    {
        public static Vector2 GetVelocity(this Rigidbody2D rb)
        {
#if UNITY_6000_0_OR_NEWER
            return rb.linearVelocity;
#else
            return rb.velocity;
#endif
        }

        public static void SetVelocity(this Rigidbody2D rb, Vector2 velocity)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = velocity;
#else
            rb.velocity = velocity;
#endif
        }
    }
}
