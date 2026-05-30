using System;
using UnityEngine;

public class PlayerScaler : MonoBehaviour
{
    [SerializeField] private PlayerMass _playerMass;
    [SerializeField] private Transform _transfromToScale;
    [SerializeField] private ItemDetector _itemDetector;
    [SerializeField] private AttractableDetector _attractableDetector;

    private void OnEnable()
    {
        _playerMass.Changed += OnMassChanged;
    }

    private void OnDisable()
    {
        _playerMass.Changed -= OnMassChanged;
    }
    
    private void OnMassChanged(int previous, int current)
    {
    
    }
}