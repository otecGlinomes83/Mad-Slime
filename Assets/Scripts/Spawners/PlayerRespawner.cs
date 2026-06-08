using UnityEngine;

public sealed class PlayerRespawner : MonoBehaviour
{
    [SerializeField] private Player _player;
    [SerializeField] private Transform _spawnPoint;

    private void Start()
    {
        _player.Health.Died += OnPlayerDied;
    }

    private void OnDisable()
    {
        _player.Health.Died -= OnPlayerDied;
    }

    private void OnPlayerDied()
    {
        _player.gameObject.SetActive(false);
        _player.transform.position = _spawnPoint.position;
        _player.gameObject.SetActive(true);
        _player.Reset();
    }
}