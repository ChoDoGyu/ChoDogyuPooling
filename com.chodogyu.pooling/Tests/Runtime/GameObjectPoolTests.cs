using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace CDG.Pooling.Tests
{
    public class GameObjectPoolTests
    {
        private GameObject prefab;
        private GameObjectPool pool;
        private List<GameObject> instances;

        [SetUp]
        public void SetUp()
        {
            prefab = new GameObject("TestPrefab");
            prefab.SetActive(false);

            pool = new GameObjectPool(prefab);
            instances = new List<GameObject>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject instance in instances)
            {
                if (instance != null)
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                }
            }

            if (prefab != null)
            {
                UnityEngine.Object.DestroyImmediate(prefab);
            }
        }

        [Test]
        public void Constructor_WithNullPrefab_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new GameObjectPool(null));
        }

        [Test]
        public void Constructor_WithValidPrefab_StoresPrefabReference()
        {
            Assert.That(pool.Prefab, Is.SameAs(prefab));
        }

        [Test]
        public void NewPool_HasZeroCounts()
        {
            Assert.That(pool.CountAll, Is.Zero);
            Assert.That(pool.CountInUse, Is.Zero);
            Assert.That(pool.CountInactive, Is.Zero);
        }

        [Test]
        public void Get_WhenNoInactiveObject_CreatesNewActiveInstance()
        {
            GameObject instance = pool.Get();
            instances.Add(instance);

            Assert.That(instance, Is.Not.Null);
            Assert.That(instance, Is.Not.SameAs(prefab));
            Assert.That(instance.activeSelf, Is.True);
        }

        [Test]
        public void Get_NewInstance_UpdatesCounts()
        {
            GameObject instance = pool.Get();
            instances.Add(instance);

            Assert.That(pool.CountAll, Is.EqualTo(1));
            Assert.That(pool.CountInUse, Is.EqualTo(1));
            Assert.That(pool.CountInactive, Is.Zero);
        }

        [Test]
        public void Release_WithValidInstance_DeactivatesInstance()
        {
            GameObject instance = pool.Get();
            instances.Add(instance);

            pool.Release(instance);

            Assert.That(instance.activeSelf, Is.False);
        }

        [Test]
        public void Release_UpdatesCounts()
        {
            GameObject first = pool.Get();
            GameObject second = pool.Get();

            instances.Add(first);
            instances.Add(second);

            pool.Release(first);

            Assert.That(pool.CountAll, Is.EqualTo(2));
            Assert.That(pool.CountInUse, Is.EqualTo(1));
            Assert.That(pool.CountInactive, Is.EqualTo(1));
        }

        [Test]
        public void Get_AfterRelease_ReusesSameInstance()
        {
            GameObject first = pool.Get();
            instances.Add(first);

            pool.Release(first);

            GameObject second = pool.Get();

            Assert.That(second, Is.SameAs(first));
            Assert.That(second.activeSelf, Is.True);
        }

        [Test]
        public void Get_ReusedInstance_DoesNotIncreaseCountAll()
        {
            GameObject first = pool.Get();
            instances.Add(first);

            pool.Release(first);

            GameObject second = pool.Get();

            Assert.That(second, Is.SameAs(first));
            Assert.That(pool.CountAll, Is.EqualTo(1));
            Assert.That(pool.CountInUse, Is.EqualTo(1));
            Assert.That(pool.CountInactive, Is.Zero);
        }

        [Test]
        public void Release_WithNullInstance_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => pool.Release(null));
        }

        [Test]
        public void Release_WithInstanceOwnedByDifferentPool_ThrowsInvalidOperationException()
        {
            GameObjectPool otherPool = new(prefab);

            GameObject instance = pool.Get();
            instances.Add(instance);

            Assert.Throws<InvalidOperationException>(() => otherPool.Release(instance));
        }

        [Test]
        public void Release_WithAlreadyReleasedInstance_ThrowsInvalidOperationException()
        {
            GameObject instance = pool.Get();
            instances.Add(instance);

            pool.Release(instance);

            Assert.Throws<InvalidOperationException>(() => pool.Release(instance));
        }
    }
}