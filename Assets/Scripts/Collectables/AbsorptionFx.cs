using UnityEngine;

namespace Collectables
{
    public sealed class AbsorptionFx : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _template;
        [SerializeField] private Color _particleColor = new Color(0.55f, 1f, 0.45f);
        [SerializeField] private int _burstCount = 12;
        [SerializeField] private float _burstSpeed = 3f;
        [SerializeField] private float _lifetime = 0.4f;

        private ParticleSystem _runtimeSystem;

        public void Play(Vector3 position)
        {
            ParticleSystem system = GetRuntimeSystem();
            system.transform.position = position;
            system.Emit(_burstCount);
        }

        private ParticleSystem GetRuntimeSystem()
        {
            if (_template != null)
            {
                if (_runtimeSystem == null)
                {
                    _runtimeSystem = Instantiate(_template, transform);
                }

                return _runtimeSystem;
            }

            if (_runtimeSystem == null)
            {
                _runtimeSystem = CreateDefaultSystem();
            }

            return _runtimeSystem;
        }

        private ParticleSystem CreateDefaultSystem()
        {
            GameObject systemObject = new GameObject("AbsorptionBurst");
            systemObject.transform.SetParent(transform);

            ParticleSystem system = systemObject.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = system.main;
            main.startColor = _particleColor;
            main.startLifetime = _lifetime;
            main.startSpeed = _burstSpeed;
            main.startSize = 0.15f;
            main.playOnAwake = false;
            main.loop = false;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;

            return system;
        }
    }
}
