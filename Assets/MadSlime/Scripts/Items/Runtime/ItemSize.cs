using System;
using Items;
using UnityEngine;

namespace Game
{
    public static class ItemSize
    {
        public static float GetRadiusXZ(Item prefab)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab), "ItemSize: Item prefab is not assigned.");
            }

            if (prefab.Collider is BoxCollider boxCollider == false)
            {
                throw new InvalidOperationException(
                    $"{prefab.name}: Item prefab needs a BoxCollider in the Collider field on the Item component to measure the XZ radius.");
            }

            Vector3 scaledSize = Vector3.Scale(boxCollider.size, prefab.transform.lossyScale);

            return Mathf.Max(scaledSize.x, scaledSize.z) * 0.5f;
        }
    }
}
