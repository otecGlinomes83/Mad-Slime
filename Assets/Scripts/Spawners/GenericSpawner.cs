using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Spawners
{
    public sealed class GenericSpawner<T> where T : MonoBehaviour
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly ObjectPool<T> _pool;
        private readonly HashSet<T> _activeInstances = new HashSet<T>();

        public int ActiveCount => _activeInstances.Count;

        public event Action<T> Spawned;
        public event Action<T> Released;

        public GenericSpawner(T prefab, Transform parent, int defaultCapacity, int maxSize)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            _prefab = prefab;
            _parent = parent;

            _pool = new ObjectPool<T>(
                createFunc: Create,
                actionOnGet: OnPoolGet,
                actionOnRelease: OnPoolRelease,
                actionOnDestroy: OnPoolDestroy,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize
            );
        }

        public T Spawn()
        {
            return _pool.Get();
        }

        public void Release(T instance)
        {
            _pool.Release(instance);
        }

        public void ReleaseAll()
        {
            T[] snapshot = new T[_activeInstances.Count];
            _activeInstances.CopyTo(snapshot);

            for (int i = 0; i < snapshot.Length; i++)
            {
                T instance = snapshot[i];

                if (instance == null)
                {
                    throw new InvalidOperationException("GenericPool.ReleaseAll encountered a null instance.");
                }

                _pool.Release(instance);
            }
        }

        private T Create()
        {
            return UnityEngine.Object.Instantiate(_prefab, _parent);
        }

        private void OnPoolGet(T instance)
        {
            instance.gameObject.SetActive(true);
            _activeInstances.Add(instance);

            Spawned?.Invoke(instance);
        }

        private void OnPoolRelease(T instance)
        {
            Released?.Invoke(instance);

            _activeInstances.Remove(instance);
            instance.gameObject.SetActive(false);
        }

        private void OnPoolDestroy(T instance)
        {
            UnityEngine.Object.Destroy(instance.gameObject);
        }
    }
}