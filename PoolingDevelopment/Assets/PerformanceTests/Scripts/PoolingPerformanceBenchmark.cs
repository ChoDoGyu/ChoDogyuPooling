using System.Collections;
using CDG.Pooling;
using Unity.Profiling;
using UnityEngine;

public sealed class PoolingPerformanceBenchmark : MonoBehaviour
{
    private static readonly ProfilerMarker InstantiateMarker = new("CDG Benchmark - Instantiate 100");
    private static readonly ProfilerMarker DestroyMarker = new("CDG Benchmark - Destroy 100");
    private static readonly ProfilerMarker PoolGetMarker = new("CDG Benchmark - Pool Get 100");
    private static readonly ProfilerMarker PoolReleaseMarker = new("CDG Benchmark - Pool Release 100");

    [SerializeField] private GameObject prefab;
    [SerializeField] private int objectsPerCycle = 100;
    [SerializeField] private int cycleCount = 100;
    [SerializeField] private Transform poolParent;

    private GameObject[] instances;
    private GameObjectPool pool;
    private bool isRunning;

    [ContextMenu("Run Instantiate Destroy Benchmark")]
    public void RunInstantiateDestroyBenchmark()
    {
        if (!Application.isPlaying || isRunning)
        {
            return;
        }

        StartCoroutine(InstantiateDestroyRoutine());
    }

    [ContextMenu("Run Pooling Benchmark")]
    public void RunPoolingBenchmark()
    {
        if (!Application.isPlaying || isRunning)
        {
            return;
        }

        StartCoroutine(PoolingRoutine());
    }

    private IEnumerator InstantiateDestroyRoutine()
    {
        isRunning = true;
        instances = new GameObject[objectsPerCycle];

        Debug.Log("[Benchmark] Instantiate / Destroy 테스트 시작");

        for (int cycle = 0; cycle < cycleCount; cycle++)
        {
            using (InstantiateMarker.Auto())
            {
                for (int i = 0; i < objectsPerCycle; i++)
                {
                    instances[i] = Instantiate(prefab);
                }
            }

            yield return null;

            using (DestroyMarker.Auto())
            {
                for (int i = 0; i < objectsPerCycle; i++)
                {
                    Destroy(instances[i]);
                    instances[i] = null;
                }
            }

            yield return null;
        }

        Debug.Log("[Benchmark] Instantiate / Destroy 테스트 종료");

        instances = null;
        isRunning = false;
    }

    private IEnumerator PoolingRoutine()
    {
        isRunning = true;
        instances = new GameObject[objectsPerCycle];

        pool = new GameObjectPool(prefab, poolParent, objectsPerCycle);
        pool.Prewarm(objectsPerCycle);

        Debug.Log("[Benchmark] Object Pooling 테스트 시작");

        for (int cycle = 0; cycle < cycleCount; cycle++)
        {
            using (PoolGetMarker.Auto())
            {
                for (int i = 0; i < objectsPerCycle; i++)
                {
                    instances[i] = pool.Get();
                }
            }

            yield return null;

            using (PoolReleaseMarker.Auto())
            {
                for (int i = 0; i < objectsPerCycle; i++)
                {
                    pool.Release(instances[i]);
                    instances[i] = null;
                }
            }

            yield return null;
        }

        Debug.Log("[Benchmark] Object Pooling 테스트 종료");

        pool.Dispose();
        pool = null;
        instances = null;
        isRunning = false;
    }

    private void OnDestroy()
    {
        pool?.Dispose();
    }
}