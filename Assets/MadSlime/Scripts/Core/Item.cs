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
        private Outline[] _outlines;

        private Vector3 _defaultScale;
        private Material[][] _originalMaterials;
        private Material[][] _ghostMaterials;
        private MaterialPropertyBlock _propertyBlock;
        private float _ghostTargetOpacity;
        private float _currentOpacity;
        private bool _isGhost;
        private bool _isHighlighted;
        private Color _highlightColor;
        private float _outlineWidth;

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

                _originalMaterials[i] = originalSet;
                _ghostMaterials[i] = BuildMaterialSet(_ghostMaterial, originalSet.Length);
            }

            _outlines = new Outline[_renderers.Length];

            for (int i = 0; i < _renderers.Length; i++)
            {
                Outline outline = _renderers[i].GetComponent<Outline>();

                if (outline == null)
                {
                    outline = _renderers[i].gameObject.AddComponent<Outline>();
                }

                outline.enabled = false;
                _outlines[i] = outline;
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
                SwapMaterials(_ghostMaterials);
                DisableOutlines();
                targetOpacity = _ghostTargetOpacity;
            }
            else
            {
                targetOpacity = SolidOpacity;
            }

            DOTween.Kill(this);

            DOTween.To(ReadOpacity, ApplyOpacity, targetOpacity, _ghostFadeConfig.FadeDuration)
                .SetEase(_ghostFadeConfig.Ease)
                .SetTarget(this)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(OnFadeCompleted);
        }

        public void SetHighlighted(bool isHighlighted, Color color, float width)
        {
            if (_isHighlighted == isHighlighted)
            {
                return;
            }

            _isHighlighted = isHighlighted;
            _highlightColor = color;
            _outlineWidth = width;

            if (_isGhost == true)
            {
                return;
            }

            ApplyOutline();
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
            if (Mathf.Approximately(_currentOpacity, SolidOpacity) == false)
            {
                return;
            }

            RestoreOriginalMaterials();
            ApplyOutline();
        }

        private void ApplyOutline()
        {
            for (int i = 0; i < _outlines.Length; i++)
            {
                _outlines[i].OutlineColor = _highlightColor;
                _outlines[i].OutlineWidth = _outlineWidth;
                _outlines[i].enabled = _isHighlighted;
            }
        }

        private void DisableOutlines()
        {
            for (int i = 0; i < _outlines.Length; i++)
            {
                _outlines[i].enabled = false;
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

        private void SwapMaterials(Material[][] materialSets)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                _renderers[i].sharedMaterials = materialSets[i];
            }
        }

        private Material[] BuildMaterialSet(Material material, int length)
        {
            Material[] set = new Material[length];

            for (int i = 0; i < length; i++)
            {
                set[i] = material;
            }

            return set;
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
            _isHighlighted = false;
            _currentOpacity = SolidOpacity;

            DOTween.Kill(this);
            DisableOutlines();

            for (int i = 0; i < _renderers.Length; i++)
            {
                _renderers[i].SetPropertyBlock(null);
                _renderers[i].sharedMaterials = _originalMaterials[i];
            }
        }
    }
}
