using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Collectables
{
    public sealed class WeightPopup : MonoBehaviour
    {
        private const int PlusFrame = 10;
        private const int PollIntervalMilliseconds = 100;

        [SerializeField] private ParticleSystem _template;
        [SerializeField] private float _digitSpacing = 0.8f;
        [SerializeField] private bool _showPlus = true;

        private readonly List<ParticleSystem> _pool = new List<ParticleSystem>(8);
        private readonly List<Vector4> _customData = new List<Vector4>(16);
        private readonly List<ParticleSystemVertexStream> _vertexStreams = new List<ParticleSystemVertexStream>(8);
        private Transform _poolRoot;

        private void Awake()
        {
            if (_template == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Template is not assigned. Bake the digit atlas (Mad Slime → Bake Digit Atlas), " +
                    "make a material with the MadSlime/DigitParticle shader, assign the atlas to it, build a " +
                    "ParticleSystem prefab with that material and Custom1.x vertex stream, then drag the prefab " +
                    "into the _template field.");
            }

            if (_digitSpacing <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(_digitSpacing), _digitSpacing, "Digit spacing must be positive.");
            }

            if (_template.main.playOnAwake == true)
            {
                throw new InvalidOperationException(
                    $"{name}: Template '{_template.name}' has Play On Awake enabled. Turn it off — " +
                    "the popup plays only when an item is absorbed.");
            }

            if (_template.main.simulationSpace != ParticleSystemSimulationSpace.Local)
            {
                throw new InvalidOperationException(
                    $"{name}: Template '{_template.name}' must use Simulation Space = Local. " +
                    "Digits are laid out in the system's local space.");
            }

            if (_template.textureSheetAnimation.enabled == true)
            {
                throw new InvalidOperationException(
                    $"{name}: Template '{_template.name}' must NOT use Texture Sheet Animation. The digit atlas " +
                    "is split by the MadSlime/DigitParticle shader via the Custom1.x vertex stream instead.");
            }

            ParticleSystemRenderer renderer = _template.GetComponent<ParticleSystemRenderer>();

            if (renderer == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Template '{_template.name}' has no ParticleSystemRenderer.");
            }

            renderer.GetActiveVertexStreams(_vertexStreams);

            if (_vertexStreams.Contains(ParticleSystemVertexStream.Custom1X) == false)
            {
                throw new InvalidOperationException(
                    $"{name}: Template '{_template.name}' Renderer has no Custom1.x vertex stream. " +
                    "Open Renderer → Custom Vertex Streams and add Custom1.x after the default ones.");
            }

            _poolRoot = new GameObject("MassPopups").transform;
        }

        public void Show(Vector3 position, int mass)
        {
            if (mass <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(mass), mass, "WeightPopup.Show requires a positive mass.");
            }

            string text = _showPlus ? "+" + mass : mass.ToString();

            ParticleSystem system = GetPooled();
            system.transform.position = position;
            EmitDigits(system, text);
            ReleaseWhenDeadAsync(system).Forget();
        }

        private void EmitDigits(ParticleSystem system, string text)
        {
            float originX = -(text.Length - 1) * 0.5f * _digitSpacing;

            for (int i = 0; i < text.Length; i++)
            {
                ParticleSystem.EmitParams parameters = new ParticleSystem.EmitParams
                {
                    position = new Vector3(originX + i * _digitSpacing, 0f, 0f)
                };

                system.Emit(parameters, 1);
                SetFrameOfLastParticle(system, FrameOf(text[i]));
            }
        }

        private void SetFrameOfLastParticle(ParticleSystem system, int frame)
        {
            int aliveCount = system.particleCount;
            system.GetCustomParticleData(_customData, ParticleSystemCustomData.Custom1);

            if (_customData.Count != aliveCount)
            {
                Debug.LogError(
                    $"{name}: particle count changed while laying out digits ({_customData.Count} != {aliveCount}). " +
                    "Check the template for extra emission modules.");
                return;
            }

            _customData[aliveCount - 1] = new Vector4(frame, 0f, 0f, 0f);
            system.SetCustomParticleData(_customData, ParticleSystemCustomData.Custom1);
        }

        private static int FrameOf(char symbol)
        {
            if (symbol == '+')
            {
                return PlusFrame;
            }

            if (symbol < '0' || symbol > '9')
            {
                throw new ArgumentOutOfRangeException(
                    nameof(symbol), symbol, $"WeightPopup cannot show the symbol '{symbol}'.");
            }

            return symbol - '0';
        }

        private ParticleSystem GetPooled()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i].gameObject.activeSelf == false)
                {
                    _pool[i].gameObject.SetActive(true);
                    return _pool[i];
                }
            }

            ParticleSystem created = Instantiate(_template, _poolRoot);
            created.gameObject.SetActive(false);
            _pool.Add(created);
            created.gameObject.SetActive(true);

            return created;
        }

        private async UniTaskVoid ReleaseWhenDeadAsync(ParticleSystem system)
        {
            try
            {
                while (system.particleCount > 0)
                {
                    await UniTask.Delay(
                        PollIntervalMilliseconds, cancellationToken: this.GetCancellationTokenOnDestroy());
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }

            system.gameObject.SetActive(false);
        }
    }
}
