using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Collectables
{
    public sealed class WeightPopup : MonoBehaviour
    {
        private const float FlyDurationSeconds = 0.7f;
        private const float FlyDistance = 1.2f;

        [SerializeField] private Color _color = new Color(0.55f, 1f, 0.45f);
        [SerializeField] private float _fontSize = 3f;

        public void Show(Vector3 position, int mass)
        {
            if (mass <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(mass), "WeightPopup.Show requires a positive mass.");
            }

            TextMeshPro text = CreateText(position, mass);
            FlyAsync(text.transform, text).Forget();
        }

        private TextMeshPro CreateText(Vector3 position, int mass)
        {
            GameObject textObject = new GameObject("WeightPopup");
            textObject.transform.SetParent(transform);
            textObject.transform.position = position;

            TextMeshPro text = textObject.AddComponent<TextMeshPro>();
            text.text = $"+{mass}";
            text.color = _color;
            text.fontSize = _fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.font = TMP_Settings.defaultFontAsset;
            text.raycastTarget = false;

            return text;
        }

        private async UniTaskVoid FlyAsync(Transform textTransform, TextMeshPro text)
        {
            float elapsedTime = 0f;
            Vector3 startPosition = textTransform.position;

            try
            {
                while (elapsedTime < FlyDurationSeconds)
                {
                    elapsedTime += Time.deltaTime;
                    float progress = Mathf.Clamp01(elapsedTime / FlyDurationSeconds);

                    textTransform.position = startPosition + Vector3.up * (FlyDistance * progress);
                    textTransform.localScale = Vector3.one * (1f + 0.5f * progress);
                    text.color = new Color(_color.r, _color.g, _color.b, 1f - progress);

                    await UniTask.Yield(PlayerLoopTiming.Update, this.GetCancellationTokenOnDestroy());
                }
            }
            catch (OperationCanceledException)
            {
                Destroy(textTransform.gameObject);
                return;
            }

            Destroy(textTransform.gameObject);
        }
    }
}
