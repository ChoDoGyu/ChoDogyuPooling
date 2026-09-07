# ChoDogyu Object Pooling

Unity `GameObject`의 반복적인 생성과 제거를 줄이기 위해 기존 인스턴스를 재사용하는 경량 Object Pooling 패키지입니다.

하나의 Prefab을 기준으로 하나의 `GameObjectPool`을 생성하며, 객체의 생성, 대여, 반환, 상태 추적, 사전 생성, 비활성 보관 및 Pool 종료까지의 생명주기를 관리합니다.

게임별 상태 초기화나 객체를 반환할 시점은 Pool이 결정하지 않으며, 패키지를 사용하는 코드에서 직접 관리합니다.

---

## 주요 기능

- `GameObject` 기반 Object Pool
- `Get()`을 통한 객체 대여
- `Release()`를 통한 객체 반환
- 대여 시 자동 활성화
- 반환 시 자동 비활성화
- 반환 시 Pool Parent 복귀
- `Prewarm()`을 통한 사전 생성
- 필요한 경우 자동 확장
- `MaxInactiveCount`를 통한 비활성 보관 수 제한
- `CountAll`, `CountInUse`, `CountInactive` 상태 확인
- Pool 소유권 검증
- 중복 반환 방지
- 잘못된 반환 검증
- 선택적인 Parent 관리
- Parent 손실 시 Scene Root 폴백
- `Clear()`를 통한 비활성 객체 제거
- `Dispose()`를 통한 Pool 전체 정리
- 외부에서 파괴된 객체에 대한 제한적인 상태 복구

---

## 요구 사항

- Unity 6.3 이상
- 별도의 외부 패키지 의존성 없음

개발 및 검증 환경:

```text
Unity 6.3 LTS
6000.3.9f1
```

`ChoDogyu Core`를 포함한 다른 CDG 패키지에 의존하지 않습니다.

---

## 설치

Unity Package Manager에서 Git URL을 사용해 설치할 수 있습니다.

```text
https://github.com/<GitHub계정>/ChoDogyuPooling.git?path=/com.chodogyu.pooling#v1.0.0
```

Unity에서:

```text
Window
→ Package Management
→ Package Manager
→ Add package from git URL...
```

을 선택한 뒤 위 URL을 입력합니다.

> `<GitHub계정>`은 실제 GitHub 계정명으로 변경해야 합니다.

---

## 기본 사용법

```csharp
using CDG.Pooling;
using UnityEngine;

public sealed class ProjectileSpawner : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform poolRoot;

    private GameObjectPool pool;

    private void Start()
    {
        pool = new GameObjectPool(projectilePrefab, poolRoot, 20);

        pool.Prewarm(10);
    }

    private void Spawn()
    {
        GameObject instance = pool.Get();

        instance.transform.position = transform.position;
        instance.transform.rotation = transform.rotation;

        // HP, Damage, Velocity, Lifetime 등
        // 게임별 상태는 사용하는 코드에서 직접 초기화합니다.
    }

    private void Return(GameObject instance)
    {
        pool.Release(instance);
    }

    private void OnDestroy()
    {
        pool?.Dispose();
    }
}
```

기본적인 사용 흐름은 다음과 같습니다.

```text
GameObjectPool 생성
→ Prewarm
→ Get
→ 게임별 상태 초기화
→ 객체 사용
→ Release
→ Pool 자체가 필요 없어지면 Dispose
```

---

## 책임 범위

`GameObjectPool`은 객체의 생명주기와 재사용 상태를 관리합니다.

### Pool이 담당하는 것

- Prefab 기반 인스턴스 생성
- 객체 대여
- 객체 반환
- 대여 상태 추적
- 소유권 추적
- 활성 / 비활성 상태 변경
- 비활성 객체 보관
- Pool Parent 복귀
- Prewarm
- 비활성 보관 수 제한
- Clear
- Dispose

### 사용하는 코드가 담당하는 것

- Position
- Rotation
- Scale
- Rigidbody 상태
- Velocity
- HP
- Damage
- Animator 상태
- Particle 상태
- Trail 상태
- Lifetime
- UI 내용
- 객체를 Release할 시점

Pool은 특정 게임이나 시스템의 상태를 알지 않습니다.

따라서 재사용된 객체의 게임별 상태는 `Get()` 이후 사용하는 코드에서 직접 초기화해야 합니다.

---

## GameObjectPool 생성

가장 간단한 생성 방법:

```csharp
GameObjectPool pool = new(prefab);
```

Parent와 최대 비활성 보관 수를 지정할 수도 있습니다.

```csharp
GameObjectPool pool = new(prefab, poolRoot, 20);
```

기본 `MaxInactiveCount`는 `100`입니다.

---

## Get

```csharp
GameObject instance = pool.Get();
```

Pool 내부에 반환되어 있던 객체가 있으면 해당 객체를 재사용합니다.

사용 가능한 비활성 객체가 없다면 원본 Prefab에서 새로운 객체를 생성합니다.

반환되는 객체는 활성화된 상태입니다.

```text
Inactive 객체 존재
→ 기존 객체 재사용

Inactive 객체 없음
→ 새로운 객체 Instantiate
```

---

## Release

```csharp
pool.Release(instance);
```

정상적으로 반환된 객체는:

```text
SetActive(false)
→ Pool Parent로 복귀
→ 재사용을 위해 보관
```

됩니다.

비활성 보관 수가 `MaxInactiveCount`에 도달한 경우에는 반환된 객체를 더 이상 보관하지 않고 제거합니다.

---

## Prewarm

```csharp
pool.Prewarm(10);
```

`Prewarm(count)`은 객체를 단순히 `count`개 추가하는 기능이 아닙니다.

Pool 내부에 **최소 count개의 비활성 객체가 준비되어 있도록 보장**합니다.

예:

```text
현재 CountInactive = 3

Prewarm(10)

→ 7개 추가 생성
→ CountInactive = 10
```

이미 충분한 비활성 객체가 존재하면 추가 생성하지 않습니다.

```text
현재 CountInactive = 10

Prewarm(10)

→ 추가 생성 없음
```

`count`는 `MaxInactiveCount`를 초과할 수 없습니다.

---

## MaxInactiveCount

`MaxInactiveCount`는 Pool에서 동시에 사용할 수 있는 객체 수를 제한하지 않습니다.

```csharp
GameObjectPool pool = new(prefab, null, 10);
```

이 경우:

- 동시에 10개보다 많은 객체를 `Get()`할 수 있습니다.
- 필요한 경우 새로운 객체가 자동 생성됩니다.
- 반환된 비활성 객체는 최대 10개까지만 보관됩니다.
- 보관 한도를 초과한 반환 객체는 제거됩니다.

즉:

```text
MaxInactiveCount
≠ 최대 생성 수
≠ 최대 대여 수

MaxInactiveCount
= 최대 비활성 보관 수
```

입니다.

---

## 상태 정보

현재 Pool 상태를 Read-Only Property로 확인할 수 있습니다.

```csharp
pool.CountAll;
pool.CountInUse;
pool.CountInactive;
pool.MaxInactiveCount;
pool.IsDisposed;
```

### CountAll

Pool이 현재 추적하고 있는 전체 객체 수입니다.

### CountInUse

`Get()`으로 대여된 뒤 아직 반환되지 않은 객체 수입니다.

### CountInactive

Pool 내부에서 재사용을 기다리고 있는 비활성 객체 수입니다.

정상적인 상태에서는 다음 관계를 유지합니다.

```text
CountAll = CountInUse + CountInactive
```

그리고:

```text
CountInactive <= MaxInactiveCount
```

를 유지합니다.

---

## Parent 관리

Pool 생성 시 선택적으로 Parent를 지정할 수 있습니다.

```csharp
GameObjectPool pool = new(prefab, poolRoot);
```

새로운 객체는 해당 Parent 아래에서 생성됩니다.

대여된 객체는 사용 중 다른 Transform 아래로 자유롭게 이동할 수 있습니다.

```csharp
instance.transform.SetParent(otherParent);
```

이후:

```csharp
pool.Release(instance);
```

하면 다시 Pool Parent로 복귀합니다.

Parent를 지정하지 않은 경우 반환된 객체는 Scene Root로 이동합니다.

Pool 생성 이후 지정했던 Parent가 외부에서 파괴된 경우에도 Pool 자체는 계속 사용할 수 있습니다.

이후 생성되거나 반환되는 객체는 Scene Root를 사용합니다.

---

## Clear

```csharp
pool.Clear();
```

현재 Pool 내부에서 재사용을 기다리고 있는 **비활성 객체만 제거**합니다.

현재 대여 중인 객체는 유지됩니다.

```text
Before

CountAll = 5
CountInUse = 2
CountInactive = 3

Clear()

After

CountAll = 2
CountInUse = 2
CountInactive = 0
```

`Clear()` 이후에도 Pool은 계속 사용할 수 있습니다.

여러 번 호출해도 안전합니다.

---

## Dispose

```csharp
pool.Dispose();
```

Pool이 소유하고 있는 모든 객체를 제거하고 Pool 사용을 종료합니다.

현재 대여 중인 객체도 Dispose 대상입니다.

Dispose 이후에는 다음 메서드를 사용할 수 없습니다.

```text
Get
Release
Prewarm
Clear
```

호출하면 `ObjectDisposedException`이 발생합니다.

`Dispose()` 자체는 여러 번 호출해도 안전합니다.

Dispose 이후에도 다음 Read-Only Property는 확인할 수 있습니다.

```text
Prefab
CountAll
CountInUse
CountInactive
MaxInactiveCount
IsDisposed
```

---

## 잘못된 Release

Pool 내부 상태를 보호하기 위해 잘못된 반환은 예외로 처리합니다.

### 실제 null 반환

```csharp
pool.Release(null);
```

`ArgumentNullException`

### 다른 Pool이 소유한 객체 반환

```text
Pool A에서 Get
→ Pool B에 Release
```

`InvalidOperationException`

### 이미 반환한 객체 재반환

```text
Get
→ Release
→ 다시 Release
```

`InvalidOperationException`

### 현재 대여 상태가 아닌 객체 반환

`InvalidOperationException`

### Dispose된 Pool 사용

`ObjectDisposedException`

---

## 외부 Destroy 처리

Pool이 관리하는 객체를 직접 `Destroy()`하는 것은 정상적인 사용 방법이 아닙니다.

정상적인 흐름은 항상 다음과 같습니다.

```text
Get
→ 사용
→ Release
```

다만 외부에서 객체가 파괴된 경우 발견 가능한 범위에서는 내부 상태를 복구합니다.

### Inactive 객체가 외부에서 파괴된 경우

다음 `Get()` 또는 `Clear()` 과정에서 파괴된 참조를 발견하면 추적 상태에서 제거합니다.

`Get()`에서 재사용할 객체가 더 이상 없다면 새로운 객체를 생성합니다.

### InUse 객체가 외부에서 파괴된 경우

해당 객체를 `Release()`하려 하면 내부 추적 상태를 정리한 뒤 `InvalidOperationException`을 발생시킵니다.

모든 외부 Destroy를 실시간으로 감시하지는 않습니다.

추가적인 감시용 `MonoBehaviour`나 지속적인 전체 검색 로직은 v1.0에 포함하지 않습니다.

---

## Thread Safety

`GameObjectPool`은 Unity `GameObject`와 `Transform`을 직접 사용합니다.

Unity Main Thread에서 사용하는 것을 전제로 하며 Thread Safe하지 않습니다.

---

## Public API

### Constructor

```csharp
public GameObjectPool(GameObject prefab, Transform parent = null, int maxInactiveCount = 100);
```

### Properties

```csharp
public GameObject Prefab { get; }
public int CountAll { get; }
public int CountInUse { get; }
public int CountInactive { get; }
public int MaxInactiveCount { get; }
public bool IsDisposed { get; }
```

### Methods

```csharp
public GameObject Get();
public void Release(GameObject instance);
public void Prewarm(int count);
public void Clear();
public void Dispose();
```

---

## 테스트

Unity Test Framework 기반 Runtime Test를 통해 주요 동작과 상태 전이를 검증했습니다.

v1.0 기준:

```text
56 Passed
0 Failed
```

주요 검증 범위:

- 생성자 입력 검증
- Get
- Release
- 반환 객체 재사용
- 다중 객체 상태 전이
- Pool 소유권 추적
- 다른 Pool 객체 반환 방지
- 중복 Release 방지
- Count 상태
- Parent 관리
- Prewarm
- 자동 확장
- MaxInactiveCount
- Clear
- Dispose
- 외부 Destroy 상태 복구
- Parent 손실 상태 복구
- 다중 Pool 독립성
- 반복적인 Get / Release 상태 전이

---

## 버전

현재 정식 버전:

```text
v1.0.0
```

변경 사항은 `CHANGELOG.md`에서 확인할 수 있습니다.

자세한 사용 규칙과 설계 설명은 다음 문서에서 확인할 수 있습니다.

```text
Documentation~/index.md
```

---

## License

별도의 라이선스 정책이 지정되지 않은 경우 저장소의 라이선스 정책을 따릅니다.