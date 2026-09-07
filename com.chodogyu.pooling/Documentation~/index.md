# ChoDogyu Object Pooling Documentation

## 1. 개요

ChoDogyu Object Pooling은 Unity `GameObject` 인스턴스를 반복적으로 생성하고 제거하는 대신 기존 객체를 재사용하기 위한 Runtime 패키지입니다.

각 `GameObjectPool`은 하나의 원본 GameObject Prefab을 기준으로 동작합니다.

```text
Prefab A
→ GameObjectPool A

Prefab B
→ GameObjectPool B
```

Pool은 객체의 생성, 대여, 반환, 활성 상태, 보관 상태 및 소유권을 관리합니다.

반면 HP, Damage, Velocity, Lifetime 등 게임별 상태는 관리하지 않습니다.

---

# 2. 설계 원칙

## 2.1 하나의 Prefab과 하나의 Pool

`GameObjectPool` 하나는 하나의 Prefab을 관리합니다.

```csharp
GameObjectPool enemyPool = new(enemyPrefab);
GameObjectPool projectilePool = new(projectilePrefab);
```

Pool Manager나 전역 Singleton은 패키지에서 제공하지 않습니다.

필요한 Pool을 어디에서 생성하고 얼마나 오래 유지할지는 사용하는 프로젝트가 결정합니다.

---

## 2.2 Pool은 게임 규칙을 알지 않음

Pool이 담당하는 것은 객체 생명주기와 재사용입니다.

```text
Instantiate
Get
Release
SetActive
Parent
Prewarm
Inactive Cache
Ownership
Rental State
Clear
Dispose
```

다음과 같은 게임별 상태는 사용하는 코드가 관리합니다.

```text
Position
Rotation
Scale
Velocity
HP
Damage
Animator
Particle
Trail
Lifetime
UI
Release Timing
```

따라서 `Get()`으로 객체를 대여한 뒤 필요한 상태를 직접 초기화해야 합니다.

---

# 3. GameObjectPool 생성

가장 간단한 생성 방법:

```csharp
GameObjectPool pool = new(prefab);
```

Parent를 지정할 수 있습니다.

```csharp
GameObjectPool pool = new(prefab, poolRoot);
```

최대 비활성 보관 수까지 지정할 수 있습니다.

```csharp
GameObjectPool pool = new(prefab, poolRoot, 20);
```

전체 생성자:

```csharp
public GameObjectPool(GameObject prefab, Transform parent = null, int maxInactiveCount = 100);
```

---

# 4. Constructor Parameters

## 4.1 prefab

```csharp
GameObject prefab
```

새로운 객체가 필요한 경우 Pool이 복제할 원본 GameObject입니다.

```csharp
GameObjectPool pool = new(projectilePrefab);
```

`null`을 전달할 수 없습니다.

```csharp
GameObjectPool pool = new(null);
```

위와 같은 경우:

```text
ArgumentNullException
```

이 발생합니다.

---

## 4.2 parent

```csharp
Transform parent
```

Pool이 비활성 객체를 보관할 선택적인 Parent입니다.

```csharp
GameObjectPool pool = new(prefab, poolRoot);
```

Parent를 지정하면:

```text
새 객체 생성
→ Pool Parent 아래에서 생성

Release
→ Pool Parent로 복귀
```

합니다.

Parent를 지정하지 않으면 Scene Root를 사용합니다.

```csharp
GameObjectPool pool = new(prefab, null);
```

---

## 4.3 maxInactiveCount

```csharp
int maxInactiveCount
```

Pool 내부에 비활성 상태로 보관할 수 있는 최대 객체 수입니다.

기본값:

```text
100
```

예:

```csharp
GameObjectPool pool = new(prefab, null, 20);
```

`1`보다 작은 값은 허용하지 않습니다.

```csharp
new GameObjectPool(prefab, null, 0);
```

위와 같은 경우:

```text
ArgumentOutOfRangeException
```

이 발생합니다.

---

# 5. 객체 대여 - Get

```csharp
GameObject instance = pool.Get();
```

`Get()`은 Pool에서 사용할 GameObject를 대여합니다.

동작 순서:

```text
Inactive 객체가 존재함
→ 기존 객체 Pop
→ 대여 상태 등록
→ SetActive(true)
→ 반환

Inactive 객체가 없음
→ Prefab Instantiate
→ Pool 소유 상태 등록
→ 대여 상태 등록
→ SetActive(true)
→ 반환
```

즉 Pool은 필요한 경우 자동으로 확장됩니다.

---

# 6. 객체 반환 - Release

```csharp
pool.Release(instance);
```

정상적인 객체 반환 흐름:

```text
대여 상태 확인
→ SetActive(false)
→ MaxInactiveCount 확인
```

비활성 보관 공간이 남아 있으면:

```text
Pool Parent로 복귀
→ Inactive 상태로 보관
```

합니다.

이미 비활성 보관 한도에 도달했다면:

```text
Pool 소유 상태에서 제거
→ GameObject 제거
```

합니다.

---

# 7. Pool 소유권

`GameObjectPool`은 자신이 생성한 객체를 추적합니다.

따라서 다른 Pool에서 생성된 객체를 반환할 수 없습니다.

```text
Pool A
→ Get A

Pool B
→ Release A
```

위와 같은 경우:

```text
InvalidOperationException
```

이 발생합니다.

---

# 8. 대여 상태

Pool은 현재 어떤 객체가 `Get()`으로 대여 중인지 추적합니다.

따라서:

```text
Get
→ Release
→ 다시 Release
```

와 같은 중복 반환은 허용하지 않습니다.

두 번째 `Release()`에서:

```text
InvalidOperationException
```

이 발생합니다.

---

# 9. Prewarm

```csharp
pool.Prewarm(10);
```

`Prewarm(count)`은 Pool 내부에 최소 `count`개의 비활성 객체가 준비되어 있도록 합니다.

단순히 `count`개를 추가하는 기능이 아닙니다.

예:

```text
CountInactive = 0

Prewarm(10)

→ 10개 생성
→ CountInactive = 10
```

이미 일부 객체가 준비되어 있다면:

```text
CountInactive = 4

Prewarm(10)

→ 6개 생성
→ CountInactive = 10
```

이미 충분하다면:

```text
CountInactive = 10

Prewarm(10)

→ 추가 생성 없음
```

---

## 9.1 Prewarm 입력 범위

다음 값은 허용합니다.

```text
0 <= count <= MaxInactiveCount
```

따라서:

```csharp
pool.Prewarm(0);
```

도 정상입니다.

하지만:

```csharp
pool.Prewarm(-1);
```

또는:

```csharp
pool.Prewarm(pool.MaxInactiveCount + 1);
```

은:

```text
ArgumentOutOfRangeException
```

을 발생시킵니다.

---

# 10. MaxInactiveCount

`MaxInactiveCount`는 Pool의 최대 전체 크기가 아닙니다.

```text
MaxInactiveCount
= 비활성 상태로 보관할 최대 객체 수
```

입니다.

예:

```csharp
GameObjectPool pool = new(prefab, null, 2);
```

이 상태에서:

```text
Get
Get
Get
```

을 호출하면 3개의 객체를 정상적으로 사용할 수 있습니다.

```text
CountAll = 3
CountInUse = 3
CountInactive = 0
```

이후 세 객체를 모두 반환하면 최대 2개까지만 보관합니다.

```text
CountAll = 2
CountInUse = 0
CountInactive = 2
```

나머지 한 객체는 제거됩니다.

---

# 11. 상태 Property

## 11.1 Prefab

```csharp
pool.Prefab
```

이 Pool에서 새로운 객체를 생성할 때 사용하는 원본 GameObject입니다.

Read-Only입니다.

---

## 11.2 CountAll

```csharp
pool.CountAll
```

Pool이 현재 추적하고 있는 전체 객체 수입니다.

---

## 11.3 CountInUse

```csharp
pool.CountInUse
```

`Get()`으로 대여된 뒤 아직 `Release()`되지 않은 객체 수입니다.

---

## 11.4 CountInactive

```csharp
pool.CountInactive
```

Pool 내부에서 재사용을 기다리고 있는 비활성 객체 수입니다.

---

## 11.5 MaxInactiveCount

```csharp
pool.MaxInactiveCount
```

Pool에 비활성 상태로 보관할 수 있는 최대 객체 수입니다.

Read-Only입니다.

---

## 11.6 IsDisposed

```csharp
pool.IsDisposed
```

Pool이 Dispose되어 더 이상 사용할 수 없는 상태인지 나타냅니다.

---

# 12. Count 불변식

정상적인 Pool 상태에서는 다음 관계가 유지됩니다.

```text
CountAll = CountInUse + CountInactive
```

또한:

```text
CountInactive <= MaxInactiveCount
```

를 유지합니다.

외부 Destroy와 같은 지원되지 않는 사용으로 일시적으로 추적 상태가 실제 Unity 객체 상태와 달라질 수 있지만, 해당 상태를 Pool이 발견한 경우 가능한 범위에서 다시 정리합니다.

---

# 13. Parent 관리

Pool Parent는 선택 기능입니다.

```csharp
GameObjectPool pool = new(prefab, poolRoot);
```

새로운 객체는 지정한 Parent 아래에서 생성됩니다.

대여 중인 객체는 사용자 코드에서 자유롭게 Reparent할 수 있습니다.

```csharp
GameObject instance = pool.Get();

instance.transform.SetParent(otherParent);
```

이후:

```csharp
pool.Release(instance);
```

하면 다시 Pool Parent 아래로 복귀합니다.

Pool은 Parent를 복구하지만 Transform의 위치, 회전, 크기를 초기화하지는 않습니다.

---

# 14. Parent가 없는 경우

Parent를 지정하지 않으면:

```csharp
GameObjectPool pool = new(prefab);
```

새로운 객체는 Scene Root에 생성됩니다.

Release 시에도 Scene Root로 이동합니다.

---

# 15. Pool Parent가 파괴된 경우

Pool을 생성한 이후 외부에서 Parent를 파괴할 수 있습니다.

```text
Pool 생성
→ Parent 존재

Parent 외부 Destroy
→ Parent 손실
```

이 경우 Pool 자체는 Dispose되지 않습니다.

이후 새로운 객체 생성이나 Release가 발생하면 Scene Root를 사용합니다.

```text
Destroyed Parent
→ Scene Root fallback
```

Pool Parent는 객체 정리를 위한 편의 기능이며 Pool 자체의 생존 조건이 아닙니다.

---

# 16. Clear

```csharp
pool.Clear();
```

현재 Pool 내부에서 재사용을 기다리고 있는 모든 비활성 객체를 제거합니다.

대여 중인 객체에는 영향을 주지 않습니다.

예:

```text
Before

CountAll = 5
CountInUse = 2
CountInactive = 3
```

`Clear()` 이후:

```text
CountAll = 2
CountInUse = 2
CountInactive = 0
```

Pool은 계속 사용할 수 있습니다.

```csharp
pool.Clear();

GameObject instance = pool.Get();
```

도 정상입니다.

`Clear()`는 여러 번 호출할 수 있습니다.

---

# 17. Dispose

```csharp
pool.Dispose();
```

Pool이 소유하고 있는 모든 GameObject를 제거하고 Pool 사용을 종료합니다.

Dispose 대상:

```text
Inactive 객체
+
InUse 객체
```

모두 포함됩니다.

Dispose 이후:

```text
CountAll = 0
CountInUse = 0
CountInactive = 0
IsDisposed = true
```

상태가 됩니다.

---

# 18. Dispose 이후 사용

Dispose 이후 다음 메서드는 사용할 수 없습니다.

```text
Get
Release
Prewarm
Clear
```

호출하면:

```text
ObjectDisposedException
```

이 발생합니다.

예:

```csharp
pool.Dispose();

pool.Get();
```

결과:

```text
ObjectDisposedException
```

`Dispose()` 자체는 여러 번 호출해도 안전합니다.

```csharp
pool.Dispose();
pool.Dispose();
pool.Dispose();
```

는 정상입니다.

---

# 19. Release 예외 규칙

## 19.1 실제 null

```csharp
pool.Release(null);
```

결과:

```text
ArgumentNullException
```

---

## 19.2 다른 Pool의 객체

```text
Pool A → Get
Pool B → Release
```

결과:

```text
InvalidOperationException
```

---

## 19.3 중복 Release

```text
Get
→ Release
→ Release
```

결과:

```text
InvalidOperationException
```

---

## 19.4 현재 대여 중이지 않은 객체

Pool이 소유하고 있더라도 현재 `Get()`으로 대여된 상태가 아니라면 반환할 수 없습니다.

결과:

```text
InvalidOperationException
```

---

# 20. 외부 Destroy

Pool이 관리하는 객체를 외부에서 직접 `Destroy()`하는 것은 정상적인 사용 방식이 아닙니다.

정상 흐름:

```text
Get
→ 사용
→ Release
```

을 사용해야 합니다.

그러나 발견 가능한 일부 잘못된 상태에서는 Pool이 내부 추적 정보를 복구합니다.

---

## 20.1 Inactive 객체 외부 Destroy

예:

```text
Get A
→ Release A
→ A 외부 Destroy
→ Get
```

다음 `Get()`에서 파괴된 Inactive 참조를 발견하면:

```text
Inactive Stack에서 제거
→ 소유 상태에서 제거
→ 사용 가능한 다른 객체 검색
→ 없으면 새로운 객체 생성
```

합니다.

`Clear()`에서도 파괴된 Inactive 객체 참조를 정리할 수 있습니다.

---

## 20.2 InUse 객체 외부 Destroy

예:

```text
Get A
→ A 외부 Destroy
→ Release A
```

Pool은 해당 객체가 이미 파괴된 것을 확인하면:

```text
Pool 추적 상태에서 A 제거
→ InvalidOperationException
```

을 발생시킵니다.

이를 통해 잘못된 사용을 알리면서 내부 상태를 가능한 범위에서 복구합니다.

---

## 20.3 자동 감시하지 않음

Pool은 모든 객체에 별도의 감시 Component를 추가하지 않습니다.

또한 매 프레임 모든 소유 객체를 검색하지 않습니다.

따라서:

```text
Get
→ 외부 Destroy
→ Release도 하지 않음
```

처럼 Pool이 해당 상태를 다시 확인할 기회가 없다면 즉시 감지하지 않습니다.

외부 Destroy는 지원되는 정상적인 객체 반환 방식이 아닙니다.

---

# 21. 사용자 상태 초기화

Pool은 게임별 상태를 자동으로 초기화하지 않습니다.

예를 들어 Projectile을 재사용한다면:

```csharp
GameObject projectile = pool.Get();

projectile.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
```

처럼 사용자 코드에서 위치와 회전을 다시 설정해야 합니다.

필요한 경우 다음 상태도 직접 초기화합니다.

```text
Rigidbody Velocity
Angular Velocity
Damage
Lifetime
Animator
Particle
Trail
HP
UI
```

어떤 상태를 초기화해야 하는지는 해당 GameObject의 역할에 따라 달라집니다.

---

# 22. 반환 시점

Pool은 객체를 언제 반환해야 하는지 판단하지 않습니다.

예:

```text
Projectile Lifetime 종료
Enemy 사망
Effect 재생 종료
UI 임시 요소 사용 종료
```

등의 판단은 사용하는 시스템이 담당합니다.

판단이 끝나면:

```csharp
pool.Release(instance);
```

를 호출합니다.

---

# 23. 권장 사용 예제

```csharp
using CDG.Pooling;
using UnityEngine;

public sealed class ExampleSpawner : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private Transform poolRoot;

    private GameObjectPool pool;

    private void Start()
    {
        pool = new GameObjectPool(prefab, poolRoot, 20);

        pool.Prewarm(10);
    }

    public GameObject Spawn(Vector3 position, Quaternion rotation)
    {
        GameObject instance = pool.Get();

        instance.transform.SetPositionAndRotation(position, rotation);

        return instance;
    }

    public void Despawn(GameObject instance)
    {
        pool.Release(instance);
    }

    private void OnDestroy()
    {
        pool?.Dispose();
    }
}
```

---

# 24. Public API

## Constructor

```csharp
public GameObjectPool(GameObject prefab, Transform parent = null, int maxInactiveCount = 100);
```

## Properties

```csharp
public GameObject Prefab { get; }

public int CountAll { get; }

public int CountInUse { get; }

public int CountInactive { get; }

public int MaxInactiveCount { get; }

public bool IsDisposed { get; }
```

## Methods

```csharp
public GameObject Get();

public void Release(GameObject instance);

public void Prewarm(int count);

public void Clear();

public void Dispose();
```

---

# 25. v1.0에서 제공하지 않는 기능

ChoDogyu Object Pooling v1.0은 최소한의 GameObject Pool 책임에 집중합니다.

다음 기능은 포함하지 않습니다.

```text
ComponentPool<T>
Generic ObjectPool<T>
Pool Manager
Singleton
IPoolable
자동 시간 기반 Release
Addressables
Async Pooling
Editor Tool
게임별 Reset Logic
```

필요한 기능은 사용하는 프로젝트에서 Object Pooling 패키지 위에 별도 계층으로 구성할 수 있습니다.

---

# 26. Thread Safety

`GameObjectPool`은 Unity `GameObject`, `Transform`, `Instantiate`, `Destroy` 등의 UnityEngine API를 사용합니다.

Unity Main Thread 사용을 전제로 합니다.

```text
Thread Safe
→ 아님
```

멀티스레드에서 동시에 `Get()`, `Release()`, `Clear()` 등을 호출하는 사용 방식은 지원하지 않습니다.

---

# 27. 테스트

패키지의 Runtime 동작은 Unity Test Framework 기반 자동 테스트로 검증합니다.

v1.0 기준:

```text
56 Passed
0 Failed
```

검증 범위에는 다음이 포함됩니다.

```text
Constructor Validation
Get
Release
Reuse
Ownership
Rental State
Count
Parent
Prewarm
MaxInactiveCount
Clear
Dispose
External Destroy Edge Cases
Destroyed Parent Edge Cases
Multiple Pool Independence
Repeated State Transition
```

---

# 28. 재사용 검증

개발 프로젝트에서는 별도의 Benchmark 환경을 사용해 재사용 흐름을 검증했습니다.

```text
Objects Per Cycle = 100
Cycle Count = 100

총 사용 횟수 = 10,000
```

일반적인 생성/제거 방식:

```text
Instantiate
→ Destroy
→ Instantiate
→ Destroy
→ ...
```

Pooling 방식:

```text
Prewarm
→ Get / Release
→ Get / Release
→ Get / Release
→ ...
→ Dispose
```

Pooling 반복 구간에서는 기존 100개의 객체가 재사용되며, 객체 수가 지속적으로 증가하지 않는 것을 확인했습니다.

이 검증은 정량적인 CPU 또는 GC 성능 수치를 제시하기 위한 Benchmark가 아니라 객체 재사용 구조가 실제로 유지되는지 확인하기 위한 개발 검증 환경입니다.

---

# 29. 설치 및 배포 검증

완전히 새로운 Unity 6.3 프로젝트에서 실제 Git 기반 설치를 검증했습니다.

검증 항목:

```text
Git URL 설치
Core 없이 독립 설치
Public API 사용
Package Runtime Tests
Package 제거
잔여 CDG Dependency 확인
Git URL 재설치
v1.0.0 Tag 고정 설치
```

패키지 테스트 결과:

```text
56 Passed
0 Failed
```

---

# 30. 지원 버전

패키지 기준 최소 Unity 버전:

```text
Unity 6000.3
```

실제 개발 및 검증 환경:

```text
Unity 6.3 LTS
6000.3.9f1
```

---

# 31. 버전

현재 정식 버전:

```text
v1.0.0
```

주요 변경 사항은 패키지의 `CHANGELOG.md`를 참고합니다.