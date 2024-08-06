using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace ZetanStudio
{
    public class SimplePool<T> where T : Component
    {
        private readonly Transform poolRoot;

        private readonly ObjectPool<T> pool;

        private readonly HashSet<T> instances = new HashSet<T>();

        public int CountAll => pool.CountAll;
        public int CountActive => pool.CountActive;
        public int CountInactive => pool.CountInactive;

        public T Get(Transform parent = null, bool worldPositionStays = false)
        {
            var go = pool.Get();
            go.transform.SetParent(parent, worldPositionStays);
            return go;
        }
        public T Get(Vector3 position, Quaternion rotation, Transform parent = null, bool worldPositionStays = false)
        {
            var go = pool.Get();
            go.transform.SetPositionAndRotation(position, rotation);
            go.transform.SetParent(parent, worldPositionStays);
            return go;
        }
        public T[] Get(int amount, Transform parent = null, bool worldPositionStays = false)
        {
            var results = new T[amount];
            for (int i = 0; i < results.Length; i++)
            {
                results[i] = Get(parent, worldPositionStays);
            }
            return results;
        }
        public void Put(T element)
        {
            pool.Release(element);
        }
        public void Put(IEnumerable<T> elements)
        {
            foreach (var element in elements)
            {
                Put(element);
            }
        }
        public bool Contains(T element) => instances.Contains(element);
        public void Clear()
        {
            pool.Clear();
        }
        public SimplePool(T model, Transform poolRoot = null, int capacity = 100)
        {
            this.poolRoot = poolRoot;
            pool = new ObjectPool<T>(() => Instantiate(model), OnGetObject, OnPutObject, OnDestroyObject, maxSize: capacity); ;
        }

        private T Instantiate(T model)
        {
            var instance = Object.Instantiate(model);
            instances.Add(instance);
            return instance;
        }
        private void OnGetObject(T go)
        {
            UtilityZT.SetActive(go, true);
        }
        private void OnPutObject(T go)
        {
            if (poolRoot) go.transform.SetParent(poolRoot, false);
            UtilityZT.SetActive(go, false);
        }
        private void OnDestroyObject(T go)
        {
            Object.Destroy(go);
        }
    }
}