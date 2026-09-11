using System;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Game
{
    public sealed class YandexEnvironmentBridge : MonoBehaviour
    {
        public const string ReceiverName = "YandexLangBridge";

        public event Action<string> LangReceived;
        public event Action<string> PlayerIdReceived;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void GetYandexLangMadSlime_js();

        [DllImport("__Internal")]
        private static extern void GetYandexPlayerIdMadSlime_js();
#endif

        public static YandexEnvironmentBridge Create(Transform parent)
        {
            GameObject bridgeObject = new GameObject(ReceiverName);
            bridgeObject.transform.SetParent(parent, false);
            return bridgeObject.AddComponent<YandexEnvironmentBridge>();
        }

        public void RequestLanguage()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            GetYandexLangMadSlime_js();
#else
            OnLangReceived(string.Empty);
#endif
        }

        public void RequestPlayerId()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            GetYandexPlayerIdMadSlime_js();
#else
            OnPlayerIdReceived(string.Empty);
#endif
        }

        private void OnLangReceived(string language)
        {
            LangReceived?.Invoke(language);
        }

        private void OnPlayerIdReceived(string playerId)
        {
            PlayerIdReceived?.Invoke(playerId);
        }
    }
}
