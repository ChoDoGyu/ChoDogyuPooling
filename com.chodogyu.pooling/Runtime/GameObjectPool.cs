using System;
using System.Collections.Generic;
using UnityEngine;

namespace CDG.Pooling
{
    /// <summary>
    /// 하나의 GameObject Prefab에서 생성된 인스턴스를 재사용하기 위한 Object Pool입니다.
    /// Pool은 객체의 생성, 대여 및 반환 상태를 관리하며 게임별 상태 초기화는 관리하지 않습니다.
    /// </summary>
    public sealed class GameObjectPool : IDisposable
    {
        private readonly GameObject prefab;
        private readonly Transform parent;
        private readonly int maxInactiveCount;
        private readonly Stack<GameObject> inactiveObjects = new();
        private readonly HashSet<GameObject> ownedObjects = new();
        private readonly HashSet<GameObject> inUseObjects = new();

        private bool isDisposed;

        /// <summary>
        /// 이 Pool에서 인스턴스를 생성할 때 사용하는 원본 Prefab입니다.
        /// </summary>
        public GameObject Prefab => prefab;

        /// <summary>
        /// 이 Pool이 현재 소유하고 있는 전체 GameObject 수입니다.
        /// </summary>
        public int CountAll => ownedObjects.Count;

        /// <summary>
        /// Get으로 대여된 후 아직 Release되지 않은 GameObject 수입니다.
        /// </summary>
        public int CountInUse => inUseObjects.Count;

        /// <summary>
        /// Release되어 Pool 내부에서 재사용을 기다리고 있는 GameObject 수입니다.
        /// </summary>
        public int CountInactive => inactiveObjects.Count;

        /// <summary>
        /// Pool 내부에 비활성 상태로 보관할 수 있는 최대 GameObject 수입니다.
        /// 동시에 대여할 수 있는 객체 수를 제한하지 않습니다.
        /// </summary>
        public int MaxInactiveCount => maxInactiveCount;

        /// <summary>
        /// 이 Pool이 Dispose되어 더 이상 사용할 수 없는 상태인지 나타냅니다.
        /// </summary>
        public bool IsDisposed => isDisposed;

        /// <summary>
        /// 지정한 GameObject Prefab을 사용하는 새로운 Pool을 생성합니다.
        /// Parent를 지정하면 새 인스턴스는 해당 Transform 아래에서 생성되고 반환 시 다시 해당 Parent로 복귀합니다.
        /// </summary>
        /// <param name="prefab">Pool에서 반복적으로 생성하고 재사용할 원본 Prefab입니다.</param>
        /// <param name="parent">Pool에서 생성된 객체를 보관할 선택적인 부모 Transform입니다.</param>
        /// <param name="maxInactiveCount">Pool 내부에 비활성 상태로 보관할 최대 GameObject 수입니다.</param>
        /// <exception cref="ArgumentNullException"><paramref name="prefab"/>이 null인 경우 발생합니다.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxInactiveCount"/>가 1보다 작은 경우 발생합니다.</exception>
        public GameObjectPool(GameObject prefab, Transform parent = null, int maxInactiveCount = 100)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            if (maxInactiveCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxInactiveCount));
            }

            this.prefab = prefab;
            this.parent = parent;
            this.maxInactiveCount = maxInactiveCount;
        }

        /// <summary>
        /// Pool에서 GameObject 인스턴스를 대여합니다.
        /// 반환된 객체가 있으면 재사용하고, 없으면 원본 Prefab에서 새로운 인스턴스를 생성합니다.
        /// </summary>
        /// <returns>활성화된 GameObject 인스턴스입니다.</returns>
        /// <exception cref="ObjectDisposedException">이 Pool이 이미 Dispose된 경우 발생합니다.</exception>
        public GameObject Get()
        {
            ThrowIfDisposed();

            GameObject instance;

            if (inactiveObjects.Count > 0)
            {
                instance = inactiveObjects.Pop();
            }
            else
            {
                instance = UnityEngine.Object.Instantiate(prefab, parent);
                ownedObjects.Add(instance);
            }

            inUseObjects.Add(instance);
            instance.SetActive(true);

            return instance;
        }

        /// <summary>
        /// 사용이 끝난 GameObject 인스턴스를 Pool에 반환합니다.
        /// 최대 비활성 보관 수에 여유가 있으면 Pool에 보관하고, 한도에 도달한 경우 객체를 제거합니다.
        /// </summary>
        /// <param name="instance">이 Pool에서 대여한 후 반환할 GameObject 인스턴스입니다.</param>
        /// <exception cref="ArgumentNullException"><paramref name="instance"/>가 null인 경우 발생합니다.</exception>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="instance"/>가 이 Pool에서 생성되지 않았거나 현재 대여 중인 객체가 아닌 경우 발생합니다.
        /// </exception>
        /// <exception cref="ObjectDisposedException">이 Pool이 이미 Dispose된 경우 발생합니다.</exception>
        public void Release(GameObject instance)
        {
            ThrowIfDisposed();

            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (!ownedObjects.Contains(instance))
            {
                throw new InvalidOperationException("이 GameObject는 해당 Pool에서 생성된 객체가 아닙니다.");
            }

            if (!inUseObjects.Remove(instance))
            {
                throw new InvalidOperationException("이 GameObject는 현재 해당 Pool에서 대여 중인 객체가 아닙니다.");
            }

            instance.SetActive(false);

            if (inactiveObjects.Count < maxInactiveCount)
            {
                instance.transform.SetParent(parent, true);
                inactiveObjects.Push(instance);
                return;
            }

            ownedObjects.Remove(instance);
            UnityEngine.Object.Destroy(instance);
        }

        /// <summary>
        /// 지정한 수만큼의 비활성 GameObject가 Pool에 준비되어 있도록 인스턴스를 미리 생성합니다.
        /// 이미 충분한 수의 비활성 객체가 있다면 추가로 생성하지 않습니다.
        /// </summary>
        /// <param name="count">Pool에 준비할 최소 비활성 GameObject 수입니다.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="count"/>가 0보다 작거나 <see cref="MaxInactiveCount"/>보다 큰 경우 발생합니다.
        /// </exception>
        /// <exception cref="ObjectDisposedException">이 Pool이 이미 Dispose된 경우 발생합니다.</exception>
        public void Prewarm(int count)
        {
            ThrowIfDisposed();

            if (count < 0 || count > maxInactiveCount)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            while (inactiveObjects.Count < count)
            {
                GameObject instance = UnityEngine.Object.Instantiate(prefab, parent);

                instance.SetActive(false);

                ownedObjects.Add(instance);
                inactiveObjects.Push(instance);
            }
        }

        /// <summary>
        /// Pool 내부에서 재사용을 기다리고 있는 모든 비활성 GameObject를 제거합니다.
        /// 현재 대여 중인 객체에는 영향을 주지 않으며 Clear 이후에도 Pool을 계속 사용할 수 있습니다.
        /// </summary>
        /// <exception cref="ObjectDisposedException">이 Pool이 이미 Dispose된 경우 발생합니다.</exception>
        public void Clear()
        {
            ThrowIfDisposed();

            while (inactiveObjects.Count > 0)
            {
                GameObject instance = inactiveObjects.Pop();

                ownedObjects.Remove(instance);
                UnityEngine.Object.Destroy(instance);
            }
        }

        /// <summary>
        /// 이 Pool이 소유한 모든 GameObject를 제거하고 Pool 사용을 종료합니다.
        /// Dispose 이후에는 Get, Release, Prewarm, Clear를 호출할 수 없습니다.
        /// </summary>
        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            foreach (GameObject instance in ownedObjects)
            {
                if (instance != null)
                {
                    UnityEngine.Object.Destroy(instance);
                }
            }

            inactiveObjects.Clear();
            inUseObjects.Clear();
            ownedObjects.Clear();

            isDisposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(GameObjectPool));
            }
        }
    }
}