using System;
using TMPro;
using UnityEngine;

namespace UI
{
    [ExecuteAlways]
    public class UniformTextSize : MonoBehaviour
    {
        [SerializeField] private TMP_Text[] _texts;

        private string[] _contents;
        private Vector2[] _sizes;
        private bool[] _visibility;
        private float[] _maximumSizes;
        private bool _isDirty;

        private void Awake()
        {
            InitializeCache();
        }

        private void InitializeCache()
        {
            if (_texts == null || _texts.Length == 0)
            {
                throw new InvalidOperationException($"{name}: assign the related text fields.");
            }

            foreach (TMP_Text text in _texts)
            {
                if (text == null)
                {
                    throw new InvalidOperationException($"{name}: a related text field is missing.");
                }
            }

            _contents = new string[_texts.Length];
            _sizes = new Vector2[_texts.Length];
            _visibility = new bool[_texts.Length];
            _maximumSizes = new float[_texts.Length];
        }

        private void OnEnable()
        {
            _isDirty = true;
        }

        private void OnValidate()
        {
            _isDirty = true;
        }

        private void LateUpdate()
        {
            if (_texts == null || _contents == null || _contents.Length != _texts.Length)
            {
                InitializeCache();
                _isDirty = true;
            }

            if (HasChanges() == false)
            {
                return;
            }

            float sharedSize = float.MaxValue;

            foreach (TMP_Text text in _texts)
            {
                if (text.isActiveAndEnabled == false)
                {
                    continue;
                }

                text.enableAutoSizing = true;
                text.ForceMeshUpdate();
                sharedSize = Mathf.Min(sharedSize, text.fontSize);
            }

            if (sharedSize < float.MaxValue)
            {
                foreach (TMP_Text text in _texts)
                {
                    text.enableAutoSizing = false;
                    text.fontSize = sharedSize;
                }
            }

            RememberState();
        }

        private bool HasChanges()
        {
            if (_isDirty)
            {
                return true;
            }

            for (int i = 0; i < _texts.Length; i++)
            {
                TMP_Text text = _texts[i];

                if (_contents[i] != text.text || _sizes[i] != text.rectTransform.rect.size
                    || _visibility[i] != text.isActiveAndEnabled || _maximumSizes[i] != text.fontSizeMax)
                {
                    return true;
                }
            }

            return false;
        }

        private void RememberState()
        {
            for (int i = 0; i < _texts.Length; i++)
            {
                TMP_Text text = _texts[i];
                _contents[i] = text.text;
                _sizes[i] = text.rectTransform.rect.size;
                _visibility[i] = text.isActiveAndEnabled;
                _maximumSizes[i] = text.fontSizeMax;
            }

            _isDirty = false;
        }
    }
}
