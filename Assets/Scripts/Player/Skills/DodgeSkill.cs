using UnityEngine;

public sealed class DodgeSkill : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float _dodgeChance = 0.15f;

    public bool TryDodge()
    {
        return UnityEngine.Random.value < _dodgeChance;
    }
}