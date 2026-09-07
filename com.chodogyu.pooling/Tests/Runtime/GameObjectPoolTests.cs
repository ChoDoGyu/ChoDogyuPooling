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
            while (pool != null && pool.CountInactive > 0)
            {
                GameObject instance = pool.Get();

                if (!instances.Contains(instance))
                {
                    instances.Add(instance);
                }
            }

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
        public void Get_WithoutParent_CreatesInstanceWithoutParent()
        {
            GameObject instance = pool.Get();
            instances.Add(instance);

            Assert.That(instance.transform.parent, Is.Null);
        }

        [Test]
        public void Get_WithParent_CreatesInstanceUnderParent()
        {
            GameObject parentObject = new("PoolRoot");
            instances.Add(parentObject);

            GameObjectPool poolWithParent = new(prefab, parentObject.transform);

            GameObject instance = poolWithParent.Get();
            instances.Add(instance);

            Assert.That(instance.transform.parent, Is.SameAs(parentObject.transform));
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
        public void Release_WithParent_ReturnsInstanceToPoolParent()
        {
            GameObject poolParent = new("PoolRoot");
            GameObject temporaryParent = new("TemporaryRoot");

            instances.Add(poolParent);
            instances.Add(temporaryParent);

            GameObjectPool poolWithParent = new(prefab, poolParent.transform);

            GameObject instance = poolWithParent.Get();
            instances.Add(instance);

            instance.transform.SetParent(temporaryParent.transform, true);

            poolWithParent.Release(instance);

            Assert.That(instance.transform.parent, Is.SameAs(poolParent.transform));
        }

        [Test]
        public void Release_WithoutParent_ReturnsInstanceToSceneRoot()
        {
            GameObject temporaryParent = new("TemporaryRoot");
            instances.Add(temporaryParent);

            GameObject instance = pool.Get();
            instances.Add(instance);

            instance.transform.SetParent(temporaryParent.transform, true);

            pool.Release(instance);

            Assert.That(instance.transform.parent, Is.Null);
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

        [Test]
        public void Prewarm_WithNegativeCount_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => pool.Prewarm(-1));
        }

        [Test]
        public void Prewarm_WithZeroCount_DoesNothing()
        {
            pool.Prewarm(0);

            Assert.That(pool.CountAll, Is.Zero);
            Assert.That(pool.CountInUse, Is.Zero);
            Assert.That(pool.CountInactive, Is.Zero);
        }

        [Test]
        public void Prewarm_CreatesRequestedInactiveObjects()
        {
            pool.Prewarm(3);

            Assert.That(pool.CountAll, Is.EqualTo(3));
            Assert.That(pool.CountInUse, Is.Zero);
            Assert.That(pool.CountInactive, Is.EqualTo(3));
        }

        [Test]
        public void Prewarm_WhenInactiveObjectsAlreadyExist_CreatesOnlyMissingAmount()
        {
            pool.Prewarm(2);
            pool.Prewarm(5);

            Assert.That(pool.CountAll, Is.EqualTo(5));
            Assert.That(pool.CountInUse, Is.Zero);
            Assert.That(pool.CountInactive, Is.EqualTo(5));
        }

        [Test]
        public void Prewarm_WhenInactiveCountAlreadyMeetsTarget_DoesNotCreateAdditionalObjects()
        {
            pool.Prewarm(5);
            pool.Prewarm(3);

            Assert.That(pool.CountAll, Is.EqualTo(5));
            Assert.That(pool.CountInUse, Is.Zero);
            Assert.That(pool.CountInactive, Is.EqualTo(5));
        }

        [Test]
        public void Get_AfterPrewarm_ReusesPrewarmedInstanceWithoutIncreasingCountAll()
        {
            pool.Prewarm(1);

            GameObject instance = pool.Get();
            instances.Add(instance);

            Assert.That(instance, Is.Not.Null);
            Assert.That(pool.CountAll, Is.EqualTo(1));
            Assert.That(pool.CountInUse, Is.EqualTo(1));
            Assert.That(pool.CountInactive, Is.Zero);
        }

        [Test]
        public void Get_AfterPrewarm_ActivatesPrewarmedInstance()
        {
            pool.Prewarm(1);

            GameObject instance = pool.Get();
            instances.Add(instance);

            Assert.That(instance.activeSelf, Is.True);
        }

        [Test]
        public void Prewarm_WithParent_CreatesInactiveInstanceUnderPoolParent()
        {
            GameObject parentObject = new("PoolRoot");
            instances.Add(parentObject);

            GameObjectPool poolWithParent = new(prefab, parentObject.transform);

            poolWithParent.Prewarm(1);

            Assert.That(parentObject.transform.childCount, Is.EqualTo(1));

            GameObject prewarmedInstance = parentObject.transform.GetChild(0).gameObject;

            Assert.That(prewarmedInstance.activeSelf, Is.False);
            Assert.That(prewarmedInstance.transform.parent, Is.SameAs(parentObject.transform));
        }

        [Test]
        public void Constructor_WithDefaultMaxInactiveCount_UsesOneHundred()
        {
            Assert.That(pool.MaxInactiveCount, Is.EqualTo(100));
        }

        [Test]
        public void Constructor_WithCustomMaxInactiveCount_StoresSpecifiedValue()
        {
            GameObjectPool customPool = new(prefab, maxInactiveCount: 10);

            Assert.That(customPool.MaxInactiveCount, Is.EqualTo(10));
        }

        [Test]
        public void Constructor_WithMaxInactiveCountLessThanOne_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameObjectPool(prefab, maxInactiveCount: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameObjectPool(prefab, maxInactiveCount: -1));
        }

        [Test]
        public void Get_CanExceedMaxInactiveCountWhileObjectsAreInUse()
        {
            GameObjectPool limitedPool = new(prefab, maxInactiveCount: 2);

            GameObject first = limitedPool.Get();
            GameObject second = limitedPool.Get();
            GameObject third = limitedPool.Get();

            instances.Add(first);
            instances.Add(second);
            instances.Add(third);

            Assert.That(limitedPool.CountAll, Is.EqualTo(3));
            Assert.That(limitedPool.CountInUse, Is.EqualTo(3));
            Assert.That(limitedPool.CountInactive, Is.Zero);
        }

        [Test]
        public void Release_WhenInactiveCountIsAtMaximum_RemovesAdditionalInstanceFromPool()
        {
            GameObjectPool limitedPool = new(prefab, maxInactiveCount: 1);

            GameObject first = limitedPool.Get();
            GameObject second = limitedPool.Get();

            instances.Add(first);
            instances.Add(second);

            limitedPool.Release(first);
            limitedPool.Release(second);

            Assert.That(limitedPool.CountAll, Is.EqualTo(1));
            Assert.That(limitedPool.CountInUse, Is.Zero);
            Assert.That(limitedPool.CountInactive, Is.EqualTo(1));
        }

        [Test]
        public void Get_AfterMaximumExceeded_ReusesRetainedInstance()
        {
            GameObjectPool limitedPool = new(prefab, maxInactiveCount: 1);

            GameObject first = limitedPool.Get();
            GameObject second = limitedPool.Get();

            instances.Add(first);
            instances.Add(second);

            limitedPool.Release(first);
            limitedPool.Release(second);

            GameObject reused = limitedPool.Get();

            Assert.That(reused, Is.SameAs(first));
            Assert.That(limitedPool.CountAll, Is.EqualTo(1));
            Assert.That(limitedPool.CountInUse, Is.EqualTo(1));
            Assert.That(limitedPool.CountInactive, Is.Zero);
        }

        [Test]
        public void Prewarm_WithCountEqualToMaxInactiveCount_CreatesMaximumInactiveObjects()
        {
            GameObjectPool limitedPool = new(prefab, maxInactiveCount: 3);

            limitedPool.Prewarm(3);

            Assert.That(limitedPool.CountAll, Is.EqualTo(3));
            Assert.That(limitedPool.CountInUse, Is.Zero);
            Assert.That(limitedPool.CountInactive, Is.EqualTo(3));
        }

        [Test]
        public void Prewarm_WithCountGreaterThanMaxInactiveCount_ThrowsArgumentOutOfRangeException()
        {
            GameObjectPool limitedPool = new(prefab, maxInactiveCount: 3);

            Assert.Throws<ArgumentOutOfRangeException>(() => limitedPool.Prewarm(4));

            Assert.That(limitedPool.CountAll, Is.Zero);
            Assert.That(limitedPool.CountInUse, Is.Zero);
            Assert.That(limitedPool.CountInactive, Is.Zero);
        }

        [Test]
        public void Clear_RemovesAllInactiveObjects()
        {
            pool.Prewarm(3);

            pool.Clear();

            Assert.That(pool.CountAll, Is.Zero);
            Assert.That(pool.CountInUse, Is.Zero);
            Assert.That(pool.CountInactive, Is.Zero);
        }

        [Test]
        public void Clear_DoesNotRemoveInUseObjects()
        {
            GameObject first = pool.Get();
            GameObject second = pool.Get();

            instances.Add(first);
            instances.Add(second);

            pool.Release(first);

            pool.Clear();

            Assert.That(pool.CountAll, Is.EqualTo(1));
            Assert.That(pool.CountInUse, Is.EqualTo(1));
            Assert.That(pool.CountInactive, Is.Zero);
            Assert.That(second, Is.Not.Null);
            Assert.That(second.activeSelf, Is.True);
        }

        [Test]
        public void Get_AfterClear_CreatesNewInstanceAndPoolRemainsUsable()
        {
            pool.Prewarm(1);

            pool.Clear();

            GameObject instance = pool.Get();
            instances.Add(instance);

            Assert.That(instance, Is.Not.Null);
            Assert.That(instance.activeSelf, Is.True);
            Assert.That(pool.CountAll, Is.EqualTo(1));
            Assert.That(pool.CountInUse, Is.EqualTo(1));
            Assert.That(pool.CountInactive, Is.Zero);
        }

        [Test]
        public void NewPool_IsNotDisposed()
        {
            Assert.That(pool.IsDisposed, Is.False);
        }

        [Test]
        public void Dispose_RemovesAllOwnedObjectsAndMarksPoolDisposed()
        {
            GameObject first = pool.Get();
            instances.Add(first);

            pool.Prewarm(2);

            pool.Dispose();

            Assert.That(pool.IsDisposed, Is.True);
            Assert.That(pool.CountAll, Is.Zero);
            Assert.That(pool.CountInUse, Is.Zero);
            Assert.That(pool.CountInactive, Is.Zero);
        }

        [Test]
        public void Dispose_WhenCalledMultipleTimes_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                pool.Dispose();
                pool.Dispose();
            });

            Assert.That(pool.IsDisposed, Is.True);
        }

        [Test]
        public void Get_AfterDispose_ThrowsObjectDisposedException()
        {
            pool.Dispose();

            Assert.Throws<ObjectDisposedException>(() => pool.Get());
        }

        [Test]
        public void Prewarm_AfterDispose_ThrowsObjectDisposedException()
        {
            pool.Dispose();

            Assert.Throws<ObjectDisposedException>(() => pool.Prewarm(1));
        }

        [Test]
        public void Release_AfterDispose_ThrowsObjectDisposedException()
        {
            GameObject instance = pool.Get();
            instances.Add(instance);

            pool.Dispose();

            Assert.Throws<ObjectDisposedException>(() => pool.Release(instance));
        }

        [Test]
        public void Clear_AfterDispose_ThrowsObjectDisposedException()
        {
            pool.Dispose();

            Assert.Throws<ObjectDisposedException>(() => pool.Clear());
        }

        [Test]
        public void Properties_AfterDispose_RemainReadable()
        {
            pool.Prewarm(2);

            pool.Dispose();

            Assert.That(pool.IsDisposed, Is.True);
            Assert.That(pool.CountAll, Is.Zero);
            Assert.That(pool.CountInUse, Is.Zero);
            Assert.That(pool.CountInactive, Is.Zero);
            Assert.That(pool.MaxInactiveCount, Is.EqualTo(100));
            Assert.That(pool.Prefab, Is.SameAs(prefab));
        }

        [Test]
        public void GetAndRelease_WithMultiplePrewarmedObjects_ReusesExistingInstances()
        {
            pool.Prewarm(3);

            GameObject first = pool.Get();
            GameObject second = pool.Get();
            GameObject third = pool.Get();

            instances.Add(first);
            instances.Add(second);
            instances.Add(third);

            List<GameObject> originalInstances = new()
    {
        first,
        second,
        third
    };

            pool.Release(first);
            pool.Release(second);
            pool.Release(third);

            GameObject reusedFirst = pool.Get();
            GameObject reusedSecond = pool.Get();
            GameObject reusedThird = pool.Get();

            List<GameObject> reusedInstances = new()
    {
        reusedFirst,
        reusedSecond,
        reusedThird
    };

            CollectionAssert.AreEquivalent(originalInstances, reusedInstances);

            Assert.That(pool.CountAll, Is.EqualTo(3));
            Assert.That(pool.CountInUse, Is.EqualTo(3));
            Assert.That(pool.CountInactive, Is.Zero);
        }

        [Test]
        public void GetAndRelease_Repeatedly_MaintainsCountRelationship()
        {
            pool.Prewarm(3);

            for (int i = 0; i < 10; i++)
            {
                GameObject instance = pool.Get();

                Assert.That(pool.CountAll, Is.EqualTo(pool.CountInUse + pool.CountInactive));

                pool.Release(instance);

                Assert.That(pool.CountAll, Is.EqualTo(pool.CountInUse + pool.CountInactive));
            }

            Assert.That(pool.CountAll, Is.EqualTo(3));
            Assert.That(pool.CountInUse, Is.Zero);
            Assert.That(pool.CountInactive, Is.EqualTo(3));
        }

        [Test]
        public void Release_AfterClear_ReturnsExistingInUseInstanceNormally()
        {
            GameObject first = pool.Get();
            GameObject second = pool.Get();

            instances.Add(first);
            instances.Add(second);

            pool.Release(first);

            pool.Clear();

            pool.Release(second);

            Assert.That(pool.CountAll, Is.EqualTo(1));
            Assert.That(pool.CountInUse, Is.Zero);
            Assert.That(pool.CountInactive, Is.EqualTo(1));
            Assert.That(second.activeSelf, Is.False);
        }

        [Test]
        public void Prewarm_AfterClear_CreatesNewInactiveObjectsNormally()
        {
            pool.Prewarm(5);

            pool.Clear();

            pool.Prewarm(3);

            Assert.That(pool.CountAll, Is.EqualTo(3));
            Assert.That(pool.CountInUse, Is.Zero);
            Assert.That(pool.CountInactive, Is.EqualTo(3));
        }

        [Test]
        public void Get_AfterInactiveCapacityTrim_ReusesRetainedObjectsAndExpandsWhenNeeded()
        {
            GameObjectPool limitedPool = new(prefab, maxInactiveCount: 2);

            GameObject first = limitedPool.Get();
            GameObject second = limitedPool.Get();
            GameObject third = limitedPool.Get();

            instances.Add(first);
            instances.Add(second);
            instances.Add(third);

            limitedPool.Release(first);
            limitedPool.Release(second);
            limitedPool.Release(third);

            Assert.That(limitedPool.CountAll, Is.EqualTo(2));
            Assert.That(limitedPool.CountInUse, Is.Zero);
            Assert.That(limitedPool.CountInactive, Is.EqualTo(2));

            GameObject reusedFirst = limitedPool.Get();
            GameObject reusedSecond = limitedPool.Get();
            GameObject expanded = limitedPool.Get();

            instances.Add(expanded);

            Assert.That(reusedFirst, Is.Not.Null);
            Assert.That(reusedSecond, Is.Not.Null);
            Assert.That(expanded, Is.Not.Null);

            Assert.That(limitedPool.CountAll, Is.EqualTo(3));
            Assert.That(limitedPool.CountInUse, Is.EqualTo(3));
            Assert.That(limitedPool.CountInactive, Is.Zero);
        }

        [Test]
        public void Pools_WithSamePrefab_MaintainIndependentStates()
        {
            GameObjectPool firstPool = new(prefab);
            GameObjectPool secondPool = new(prefab);

            GameObject firstInstance = firstPool.Get();
            GameObject secondInstance = secondPool.Get();

            instances.Add(firstInstance);
            instances.Add(secondInstance);

            Assert.That(firstInstance, Is.Not.SameAs(secondInstance));

            Assert.That(firstPool.CountAll, Is.EqualTo(1));
            Assert.That(firstPool.CountInUse, Is.EqualTo(1));
            Assert.That(firstPool.CountInactive, Is.Zero);

            Assert.That(secondPool.CountAll, Is.EqualTo(1));
            Assert.That(secondPool.CountInUse, Is.EqualTo(1));
            Assert.That(secondPool.CountInactive, Is.Zero);

            firstPool.Release(firstInstance);

            Assert.That(firstPool.CountInUse, Is.Zero);
            Assert.That(firstPool.CountInactive, Is.EqualTo(1));

            Assert.That(secondPool.CountInUse, Is.EqualTo(1));
            Assert.That(secondPool.CountInactive, Is.Zero);
        }
    }
}