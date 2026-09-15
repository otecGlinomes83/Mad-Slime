using System;
using Interfaces;
using Skills;
using UnityEngine;

namespace Items
{
    public sealed class Item : MonoBehaviour, IAttractable
    {
        [SerializeField] private ItemDefinition _definition;
        [SerializeField] private Collider _collider;
        [SerializeField] private Material _ghostMaterial;

        private Vector3 _defaultScale;
        private Renderer[] _renderers;
        private Material[][] _originalMaterials;
        private Material[][] _ghostMaterials;
        private bool _isGhost;

        public ItemDefinition Definition => _definition;
        public Transform Self => transform;

        public int Mass
        {
            get
            {
                if (_definition == null)
                {
                    throw new InvalidOperationException(
                        $"{name}: Mass requested but Definition is null. Assign a definition on the prefab or via SetDefinition.");
                }

                return _definition.BaseMass;
            }
        }

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

            _defaultScale = new Vector3(
                Mathf.Abs(transform.localScale.x),
                Mathf.Abs(transform.localScale.y),
                Mathf.Abs(transform.localScale.z));

            _renderers = GetComponentsInChildren<Renderer>(true);

            if (_renderers.Length == 0)
            {
                throw new InvalidOperationException(
                    $"{name}: no Renderers found in children. An item prefab must contain a model with at least one Renderer.");
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

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (isGhost)
                {
                    _renderers[i].sharedMaterials = _ghostMaterials[i];
                }
                else
                {
                    _renderers[i].sharedMaterials = _originalMaterials[i];
                }
            }
        }

        public void Initialize(Vector3 position, float scale)
        {
            transform.position = position;
            transform.localScale = _defaultScale * scale;
            _collider.enabled = true;
            SetGhost(false);
        }

        public void Collect()
        {
            _collider.enabled = false;
            SetGhost(false);
        }

        public void Shutdown()
        {
            gameObject.SetActive(false);
        }
    }
}
