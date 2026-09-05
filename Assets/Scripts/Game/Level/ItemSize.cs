using Items;
using UnityEngine;

namespace Game
{
    public static class ItemSize
    {
        private const float FallbackRadius = 1f;

        public static float GetRadiusXZ(Item prefab)
        {
            BoxCollider collider = prefab.GetComponentInChildren<BoxCollider>();

            if (collider == null)
            {
                return FallbackRadius;
            }

            Vector3 scaledSize = Vector3.Scale(collider.size, prefab.transform.lossyScale);

            return Mathf.Max(scaledSize.x, scaledSize.z) * 0.5f;
        }
    }
}
