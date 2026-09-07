# ChoDogyu Object Pooling

Unity `GameObject`의 반복적인 생성과 제거를 줄이기 위해 인스턴스를 재사용하는 경량 Object Pooling 패키지입니다.

하나의 Prefab을 기준으로 하나의 Pool을 생성하며, 객체의 대여/반환, 상태 추적, 사전 생성, 비활성 보관 수 제한, Parent 관리, Clear 및 Dispose를 제공합니다.

게임 로직이나 특정 장르에 의존하지 않으며, 독립적인 Unity Package Manager 패키지로 설치할 수 있도록 구성했습니다.

---

## 주요 기능

- `GameObject` 기반 Object Pool
- `Get()` / `Release()` 객체 대여 및 반환
- 자동 활성화 / 비활성화
- `Prewarm()` 사전 생성
- 필요 시 자동 확장
- `MaxInactiveCount` 비활성 보관 제한
- `CountAll`, `CountInUse`, `CountInactive` 상태 확인
- Pool 소유권 검증
- 중복 반환 방지
- 잘못된 반환 검증
- 선택적인 Parent 관리
- Parent 손실 시 Scene Root 폴백
- `Clear()` 비활성 객체 제거
- `Dispose()` 전체 Pool 종료
- 외부에서 Destroy된 객체에 대한 제한적인 상태 복구

---

## 저장소 구조

```text
ChoDogyuPooling/
├─ PoolingDevelopment/
│  └─ Unity 개발 및 검증 프로젝트
│
├─ com.chodogyu.pooling/
│  ├─ Runtime/
│  ├─ Tests/
│  ├─ Documentation~/
│  ├─ package.json
│  ├─ README.md
│  └─ CHANGELOG.md
│
├─ .gitignore
└─ README.md
```

### PoolingDevelopment

패키지 개발, 통합 검증 및 재사용 동작 확인을 위한 Unity 프로젝트입니다.

실제 UPM 배포 대상에는 포함되지 않습니다.

### com.chodogyu.pooling

실제 배포하는 UPM 패키지입니다.

다른 Unity 프로젝트에서는 이 폴더를 패키지로 설치하여 사용할 수 있습니다.

---

## 설치

Unity Package Manager에서 Git URL을 사용해 설치할 수 있습니다.

```text
https://github.com/<GitHub계정>/ChoDogyuPooling.git?path=/com.chodogyu.pooling#v1.0.0
```

Unity에서 다음 경로로 이동합니다.

```text
Window
→ Package Management
→ Package Manager
→ Add package from git URL...
```

위 Git URL을 입력하면 `ChoDogyu Object Pooling` 패키지가 설치됩니다.

> `<GitHub계정>` 부분은 실제 GitHub 계정명으로 변경해야 합니다.

---

## 요구 사항

- Unity 6.3 이상
- 별도의 외부 패키지 의존성 없음

개발 및 검증 환경:

```text
Unity 6.3 LTS
6000.3.9f1
```

`ChoDogyu Core`를 포함한 다른 CDG 패키지에 의존하지 않으며, Object Pooling 패키지만 독립적으로 설치할 수 있습니다.

---

## 기본 사용

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
    }

    private void Release(GameObject instance)
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
Pool 생성
→ Prewarm
→ Get
→ 게임별 상태 초기화
→ 사용
→ Release
→ Pool 종료 시 Dispose
```

---

## 설계 방향

`GameObjectPool`은 객체의 생성과 재사용 상태를 관리하지만 게임별 상태 초기화는 담당하지 않습니다.

### Pool이 담당하는 범위

```text
Prefab 기반 인스턴스 생성
Get / Release
활성 / 비활성 상태 변경
Pool Parent 복귀
객체 소유권 추적
대여 상태 추적
Prewarm
비활성 객체 보관
MaxInactiveCount 관리
Clear
Dispose
```

### 사용자 코드가 담당하는 범위

```text
Position / Rotation / Scale
Rigidbody 상태
HP / Damage
Animator 상태
Particle / Trail 상태
Lifetime
UI 상태
객체를 Release할 시점
```

즉 Object Pool은 게임 규칙을 알지 않고, 객체의 생명주기와 재사용만 관리하도록 설계했습니다.

---

## Prewarm

`Prewarm(count)`은 객체를 단순히 `count`개 추가하는 기능이 아닙니다.

Pool 내부에 최소 `count`개의 비활성 객체가 준비되어 있도록 보장합니다.

```csharp
pool.Prewarm(10);
```

예를 들어 현재 비활성 객체가 3개라면:

```text
CountInactive = 3

Prewarm(10)

→ 7개 추가 생성
→ CountInactive = 10
```

이미 10개 이상의 비활성 객체가 존재한다면 추가로 생성하지 않습니다.

---

## MaxInactiveCount

`MaxInactiveCount`는 Pool 내부에 비활성 상태로 보관할 수 있는 최대 객체 수입니다.

```csharp
GameObjectPool pool = new(prefab, null, 10);
```

이 경우:

- 동시에 10개보다 많은 객체를 `Get()`할 수 있습니다.
- 필요한 경우 Pool은 자동으로 새로운 객체를 생성합니다.
- 반환된 객체는 최대 10개까지만 비활성 상태로 보관합니다.
- 보관 한도를 초과한 반환 객체는 제거됩니다.

따라서 `MaxInactiveCount`는 동시 사용 가능한 최대 객체 수가 아니라 **비활성 캐시 크기 제한**입니다.

---

## 상태 정보

다음 Read-Only Property를 통해 Pool 상태를 확인할 수 있습니다.

```csharp
pool.CountAll;
pool.CountInUse;
pool.CountInactive;
pool.MaxInactiveCount;
pool.IsDisposed;
```

정상적인 Pool 상태에서는 다음 관계를 유지합니다.

```text
CountAll = CountInUse + CountInactive
```

또한:

```text
CountInactive <= MaxInactiveCount
```

를 유지합니다.

---

## Clear와 Dispose

### Clear

```csharp
pool.Clear();
```

현재 Pool 내부에서 재사용을 기다리고 있는 비활성 객체만 제거합니다.

현재 `Get()`으로 대여 중인 객체에는 영향을 주지 않으며, `Clear()` 이후에도 Pool을 계속 사용할 수 있습니다.

### Dispose

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

Dispose는 여러 번 호출해도 안전합니다.

---

## 잘못된 Release 검증

다음과 같은 잘못된 사용은 예외로 처리됩니다.

- null 객체 반환
- 다른 Pool이 소유한 객체 반환
- 이미 반환된 객체를 다시 반환
- 현재 대여 중이지 않은 객체 반환
- Dispose된 Pool 사용

이를 통해 Pool 내부 상태가 잘못된 Release로 인해 손상되는 것을 방지합니다.

---

## 외부 Destroy 처리

Pool이 관리하는 객체를 직접 `Destroy()`하는 것은 정상적인 사용 방식이 아닙니다.

권장 흐름은 항상 다음과 같습니다.

```text
Get
→ 사용
→ Release
```

다만 외부에서 객체가 파괴된 경우 발견 가능한 범위에서는 내부 추적 상태를 복구합니다.

예를 들어:

```text
파괴된 Inactive 객체
→ 다음 Get 또는 Clear에서 추적 제거

파괴된 InUse 객체
→ Release 시 추적 상태 제거 후 예외 발생
```

모든 외부 파괴를 실시간으로 감시하기 위한 추가 Component나 지속적인 검색 로직은 포함하지 않습니다.

---

## Parent 관리

Pool 생성 시 선택적으로 Parent를 지정할 수 있습니다.

```csharp
GameObjectPool pool = new(prefab, poolRoot);
```

새로운 객체는 해당 Parent 아래에서 생성됩니다.

대여된 객체는 사용 중 다른 Transform 아래로 이동할 수 있습니다.

```csharp
instance.transform.SetParent(otherParent);
```

이후 `Release()`하면 다시 Pool Parent 아래로 복귀합니다.

Pool Parent가 없는 경우 Scene Root로 이동합니다.

Pool 사용 중 지정했던 Parent가 파괴된 경우에도 Pool 자체는 계속 사용할 수 있으며, 이후 객체는 Scene Root를 사용합니다.

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

Unity Test Framework 기반 Runtime Test를 통해 주요 상태 전이를 검증했습니다.

```text
56 Passed
0 Failed
```

주요 검증 항목:

- Get / Release
- 반환 객체 재사용
- 다중 객체 상태 전이
- Pool 소유권 검증
- 다른 Pool 객체 Release 방지
- 중복 Release 방지
- Count 상태 검증
- Parent 관리
- Prewarm
- MaxInactiveCount
- Clear
- Dispose
- 외부 Destroy Edge Case
- Parent 손실 Edge Case
- 다중 Pool 독립성
- 반복적인 Get / Release 상태 전이

---

## 재사용 동작 검증

개발 프로젝트에 별도의 Benchmark 환경을 구성하여 반복적인 객체 사용 흐름을 확인했습니다.

검증 조건:

```text
Objects Per Cycle = 100
Cycle Count = 100

100 × 100
= 총 10,000회 사용
```

일반적인 Instantiate / Destroy 방식에서는 객체가 반복적으로 생성되고 제거됩니다.

```text
Instantiate
→ Destroy
→ Instantiate
→ Destroy
→ ...
```

Pooling 방식에서는 100개의 객체를 Prewarm한 뒤 같은 객체를 반복적으로 대여하고 반환합니다.

```text
Prewarm
→ Get / Release
→ Get / Release
→ Get / Release
→ ...
→ Dispose
```

반복 구간에서 객체 수가 계속 증가하지 않고 기존 인스턴스가 재사용되는 것을 확인했습니다.

정량적인 CPU 또는 GC 성능 수치는 별도로 제시하지 않으며, 재사용 구조와 객체 생명주기 동작을 검증하는 용도로 사용했습니다.

---

## UPM 설치 및 제거 검증

완전히 새로운 Unity 6.3 프로젝트에서 실제 사용자 환경을 가정하여 다음 과정을 검증했습니다.

```text
Git URL 설치
→ 성공

Core 없이 독립 설치
→ 성공

CDG.Pooling 참조
→ 성공

GameObjectPool 실행
→ 성공

Package Runtime Tests
→ 56 Passed / 0 Failed

Package 제거
→ 성공

잔여 CDG 의존성
→ 없음

Git URL 재설치
→ 성공

v1.0.0 Tag 고정 설치
→ 성공
```

이를 통해 개발 프로젝트의 로컬 경로에 의존하지 않고 독립적인 UPM 패키지로 설치 및 제거할 수 있음을 확인했습니다.

---

## 버전

현재 정식 버전:

```text
v1.0.0
```

패키지의 주요 변경 사항은 다음 파일에서 확인할 수 있습니다.

```text
com.chodogyu.pooling/CHANGELOG.md
```

---

## 패키지 문서

패키지 내부의 빠른 사용 안내:

```text
com.chodogyu.pooling/README.md
```

세부 사용 규칙 및 설계 문서:

```text
com.chodogyu.pooling/Documentation~/index.md
```

---

## License

별도의 라이선스 정책이 지정되지 않은 경우 저장소의 라이선스 정책을 따릅니다.