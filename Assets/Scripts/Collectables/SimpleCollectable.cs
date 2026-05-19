using UnityEngine;

namespace Collectables
{
    public class SimpleCollectable : MonoBehaviour, ICollectable
    {
        public void Collect()
        {
            gameObject.SetActive(false);
        }
    }
}