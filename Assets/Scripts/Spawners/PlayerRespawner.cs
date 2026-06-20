using UnityEngine;

public sealed class PlayerRespawner : MonoBehaviour
{
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private Transform _spawnPoint;

    public void Respawn()
    {
        _playerTransform.gameObject.SetActive(false);
        _playerTransform.transform.position = _spawnPoint.position;
        _playerTransform.gameObject.SetActive(true);
    }
}