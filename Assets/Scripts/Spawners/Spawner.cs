using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Spawners
{
    public abstract class Spawner<T> : MonoBehaviour where T : MonoBehaviour
    {
        [SerializeField] private T _prefab;
        [SerializeField] private int _defaultCapacity = 16;
        [SerializeField] private int _maxSize = 64;

        private ObjectPool<T> _pool;
        private readonly HashSet<T> _activeInstances = new HashSet<T>();

        public int ActiveCount => _activeInstances.Count;

        protected virtual void Awake()
        {
            _pool = new ObjectPool<T>(
                createFunc: Create,
                actionOnGet: OnPoolGet,
                actionOnRelease: OnPoolRelease,
                actionOnDestroy: OnPoolDestroy,
                defaultCapacity: _defaultCapacity,
                maxSize: _maxSize
            );
        }

        protected virtual void OnDisable()
        {
            ReleaseAll();
        }

        public T Spawn()
        {
            return _pool.Get();
        }

        public void Release(T instance)
        {
            _pool.Release(instance);
        }

        protected virtual T Create()
        {
            return Instantiate(_prefab, transform);
        }

        protected virtual void OnSpawned(T instance)
        {
        }

        protected virtual void OnReleased(T instance)
        {
        }

        private void OnPoolGet(T instance)
        {
            instance.gameObject.SetActive(true);
            _activeInstances.Add(instance);

            OnSpawned(instance);
        }

        private void OnPoolRelease(T instance)
        {
            OnReleased(instance);

            _activeInstances.Remove(instance);
            instance.gameObject.SetActive(false);
        }

        private void OnPoolDestroy(T instance)
        {
            Destroy(instance.gameObject);
        }

        private void ReleaseAll()
        {
            T[] snapshot = new T[_activeInstances.Count];
            _activeInstances.CopyTo(snapshot);

            for (int i = 0; i < snapshot.Length; i++)
            {
                T instance = snapshot[i];

                if (instance == null)
                {
                    _activeInstances.Remove(instance);
                    continue;
                }

                _pool.Release(instance);
            }
        }
    }
}
