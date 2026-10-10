using System;
using UnityEngine;

namespace Player
{
    [Serializable]
    public class RadiusRecipientEntry
    {
        [Tooltip("Получатель радиуса: детектор, клампер и т.п. — реализует IRadiusRecipient.")]
        [SerializeField] private MonoBehaviour _recipient;

        [Tooltip("Базовый радиус на первом тире; скейлер умножает его на масштаб тира.")]
        [SerializeField, Min(0f)] private float _baseRadius;

        private IRadiusRecipient _radiusRecipient;

        public IRadiusRecipient Recipient => _radiusRecipient;
        public float BaseRadius => _baseRadius;

        public void Initialize(string ownerName)
        {
            if (_recipient == null)
            {
                throw new InvalidOperationException(
                    $"{ownerName}: a radius recipient entry has no recipient assigned.");
            }

            if (_recipient is IRadiusRecipient radiusRecipient == false)
            {
                throw new InvalidOperationException(
                    $"{ownerName}: recipient '{_recipient.name}' does not implement IRadiusRecipient.");
            }

            _radiusRecipient = radiusRecipient;
        }
    }
}
