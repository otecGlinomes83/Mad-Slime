using Detection;
using Interfaces;
using Skills;
using UnityEngine;

namespace NPC.Enemy
{
    [RequireComponent(typeof(Wander))]
    [RequireComponent(typeof(Chaser))]
    [RequireComponent(typeof(Attacker))]
    [RequireComponent(typeof(TargetSensor))]
    public sealed class Enemy : MonoBehaviour
    {
        [SerializeField] private TargetSensor _attackTargetSensor;
        [SerializeField] private ItemTier _tier;

        private Wander _wander;
        private TargetSensor _targetSensor;
        private Attacker _attacker;
        private Chaser _chaser;

        private void Awake()
        {
            _wander = GetComponent<Wander>();
            _attacker = GetComponent<Attacker>();
            _targetSensor = GetComponent<TargetSensor>();
            _chaser = GetComponent<Chaser>();
        }

        private void Update()
        {
            if (_targetSensor.TryDetect(out ITarget target))
            {
                if (target.Tier > _tier)
                {
                    _wander.Tick();
                    return;
                }

                if (_attackTargetSensor.TryDetect(out ITarget attackTarget))
                {
                    _attacker.TryAttack(attackTarget);
                }

                _wander.Stop();
                _chaser.Tick(target.Transform.position);
            }
            else
            {
                _wander.Tick();
            }
        }
    }
}