using System;
using DG.Tweening;
using Interfaces;
using Skills;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Items
{
    public sealed class Item : MonoBehaviour, IAttractable
    {
        private const float SolidOpacity = 1f;

        private static readonly int OpacityId = Shader.PropertyToID("_Opacity");

        [SerializeField] private ItemDefinition _definition;
        [SerializeField] private Collider _collider;
        [SerializeField] private Material _ghostMaterial;
        [SerializeField] private GhostFadeConfig _ghostFadeConfig;

        private Renderer[] _renderers;

        private Vector3 _defaultScale;
        private Material[][] _originalMaterials;
        private Material[][] _ghostMaterials;
        private MaterialPropertyBlock _propertyBlock;
        private Tween _fadeTween;
        private float _ghostTargetOpacity;
        private float _currentOpacity;
        private bool _isGhost;

        public ItemDefinition Definition => _definition;
        public Transform Self => transform;
        public Collider Collider => _collider;

        public ItemTier Tier
        {
            get
            {
                if (_definition == null)
                {
                    throw new InvalidOperationException(
                        $"{name}: Tier requested but Definition is null. Assign a definition on the prefab or via SetDefinition.");
                }

                return _definition.Tier;
            }
        }

        private void Awake()
        {
            if (_collider == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Collider is not assigned. Drag a Collider into the _collider field.");
            }

            if (_ghostMaterial == null)
            {
                throw new InvalidOperationException(
                    $"{name}: GhostMaterial is not assigned. Drag a Material with the MadSlime/GhostDither shader into the _ghostMaterial field.");
            }

            if (_ghostMaterial.HasProperty(OpacityId) == false)
            {
                throw new InvalidOperationException(
                    $"{name}: GhostMaterial has no _Opacity property. Drag a Material with the MadSlime/GhostDither shader into the _ghostMaterial field.");
            }

            if (_ghostFadeConfig == null)
            {
                throw new InvalidOperationException(
                    $"{name}: GhostFadeConfig is not assigned. Drag a GhostFadeConfig asset into the _ghostFadeConfig field.");
            }

            if (_ghostFadeConfig.FadeDuration <= 0f)
            {
                throw new InvalidOperationException(
                    $"{name}: GhostFadeConfig '{_ghostFadeConfig.name}' has FadeDuration <= 0. Set a positive duration in the config asset.");
            }

            _defaultScale = new Vector3(
                Mathf.Abs(transform.localScale.x),
                Mathf.Abs(transform.localScale.y),
                Mathf.Abs(transform.localScale.z));

            _renderers = GetComponentsInChildren<Renderer>(true);

            if (_renderers.Length == 0)
            {
                throw new InvalidOperationException(
                    $"{name}: no Renderers found in children. The item prefab must contain its visual model.");
            }

            _originalMaterials = new Material[_renderers.Length][];
            _ghostMaterials = new Material[_renderers.Length][];

            for (int i = 0; i < _renderers.Length; i++)
            {
                Material[] originalSet = _renderers[i].sharedMaterials;
                Material[] ghostSet = new Material[originalSet.Length];

                for (int j = 0; j < ghostSet.Length; j++)
                {
                    ghostSet[j] = _ghostMaterial;
                }

                _originalMaterials[i] = originalSet;
                _ghostMaterials[i] = ghostSet;
            }

            _propertyBlock = new MaterialPropertyBlock();
            _ghostTargetOpacity = _ghostMaterial.GetFloat(OpacityId);
            _currentOpacity = SolidOpacity;
        }

        public void SetDefinition(ItemDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition),
                    $"{name}: SetDefinition requires a non-null definition.");
            }

            _definition = definition;
        }

        public void SetGhost(bool isGhost)
        {
            if (_isGhost == isGhost)
            {
                return;
            }

            _isGhost = isGhost;

            float targetOpacity;

            if (isGhost)
            {
                SwapGhostMaterials();
                targetOpacity = _ghostTargetOpacity;
            }
            else
            {
                targetOpacity = SolidOpacity;
            }

            DOTween.Kill(this);

            _fadeTween = DOTween.To(ReadOpacity, ApplyOpacity, targetOpacity, _ghostFadeConfig.FadeDuration)
                .SetEase(_ghostFadeConfig.Ease)
                .SetTarget(this)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(OnFadeCompleted);
        }

        public void Initialize(Vector3 position, float scale)
        {
            transform.position = position;
            transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            transform.localScale = _defaultScale * scale;
            _collider.enabled = true;
            ResetVisuals();
        }

        public void Collect()
        {
            _collider.enabled = false;
            ResetVisuals();
        }

        public void Shutdown()
        {
            gameObject.SetActive(false);
        }

        private float ReadOpacity()
        {
            return _currentOpacity;
        }

        private void OnFadeCompleted()
        {
            if (Mathf.Approximately(_currentOpacity, SolidOpacity))
            {
                RestoreOriginalMaterials();
            }
        }

        private void ApplyOpacity(float opacity)
        {
            _currentOpacity = opacity;
            _propertyBlock.SetFloat(OpacityId, opacity);

            for (int i = 0; i < _renderers.Length; i++)
            {
                _renderers[i].SetPropertyBlock(_propertyBlock);
            }
        }

        private void SwapGhostMaterials()
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                _renderers[i].sharedMaterials = _ghostMaterials[i];
            }
        }

        private void RestoreOriginalMaterials()
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                _renderers[i].SetPropertyBlock(null);
                _renderers[i].sharedMaterials = _originalMaterials[i];
            }
        }

        private void ResetVisuals()
        {
            _isGhost = false;
            _currentOpacity = SolidOpacity;

            DOTween.Kill(this);

            for (int i = 0; i < _renderers.Length; i++)
            {
                _renderers[i].SetPropertyBlock(null);
                _renderers[i].sharedMaterials = _originalMaterials[i];
            }
        }
    }
}
