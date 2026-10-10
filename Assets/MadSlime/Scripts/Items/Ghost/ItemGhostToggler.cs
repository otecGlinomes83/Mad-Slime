using Items;
using Player;
using UnityEngine;
using VContainer;

namespace Collectables
{
    public class ItemGhostToggler : MonoBehaviour
    {
        private CloseItemDetector _detector;
        private CollectAvailability _availability;

        [Inject]
        public void Construct(CloseItemDetector detector, CollectAvailability availability)
        {
            _detector = detector;
            _availability = availability;
        }

        private void OnEnable()
        {
            _detector.Detected += OnDetected;
            _detector.Exited += OnExited;
        }

        private void OnDisable()
        {
            _detector.Detected -= OnDetected;
            _detector.Exited -= OnExited;
        }

        private void OnDetected(Item item)
        {
            if (item != null && item.TryGetComponent(out ItemVisual visual))
            {
                visual.SetGhost(_availability.CanCollect(item.Definition.Tier) == false);
            }
        }

        private void OnExited(Item item)
        {
            if (item != null && item.TryGetComponent(out ItemVisual visual))
            {
                visual.SetGhost(false);
            }
        }
    }
}
