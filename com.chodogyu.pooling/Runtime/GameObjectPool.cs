using System;
using System.Collections.Generic;
using UnityEngine;

namespace CDG.Pooling
{
    /// <summary>
    /// 하나의 GameObject Prefab에서 생성된 인스턴스를 재사용하기 위한 Object Pool입니다.
    /// Pool은 객체의 생성, 대여 및 반환 상태를 관리하며 게임별 상태 초기화는 관리하지 않습니다.
    /// </summary>
    public sealed class GameObjectPool
    {
        private readonly GameObject prefab;
        private readonly Stack<GameObject> inactiveObjects = new();

        /// <summary>
        /// 이 Pool에서 인스턴스를 생성할 때 사용하는 원본 Prefab입니다.
        /// </summary>
        public GameObject Prefab => prefab;

        /// <summary>
        /// 지정한 GameObject Prefab을 사용하는 새로운 Pool을 생성합니다.
        /// </summary>
        /// <param name="prefab">Pool에서 반복적으로 생성하고 재사용할 원본 Prefab입니다.</param>
        /// <exception cref="ArgumentNullException"><paramref name="prefab"/>이 null인 경우 발생합니다.</exception>
        public GameObjectPool(GameObject prefab)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            this.prefab = prefab;
        }

        /// <summary>
        /// Pool에서 GameObject 인스턴스를 대여합니다.
        /// 반환된 객체가 있으면 재사용하고, 없으면 원본 Prefab에서 새로운 인스턴스를 생성합니다.
        /// </summary>
        /// <returns>활성화된 GameObject 인스턴스입니다.</returns>
        public GameObject Get()
        {
            GameObject instance;

            if (inactiveObjects.Count > 0)
            {
                instance = inactiveObjects.Pop();
            }
            else
            {
                instance = UnityEngine.Object.Instantiate(prefab);
            }

            instance.SetActive(true);

            return instance;
        }

        /// <summary>
        /// 사용이 끝난 GameObject 인스턴스를 Pool에 반환합니다.
        /// 반환된 객체는 비활성화된 상태로 보관되며 이후 Get 호출에서 다시 사용됩니다.
        /// </summary>
        /// <param name="instance">이 Pool에서 대여한 후 반환할 GameObject 인스턴스입니다.</param>
        /// <exception cref="ArgumentNullException"><paramref name="instance"/>가 null인 경우 발생합니다.</exception>
        public void Release(GameObject instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            instance.SetActive(false);
            inactiveObjects.Push(instance);
        }
    }
}