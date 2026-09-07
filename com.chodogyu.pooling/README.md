# ChoDogyu Object Pooling

Unity GameObject의 반복적인 생성과 제거를 줄이기 위해 인스턴스를 재사용하는 경량 Object Pooling 패키지입니다.

하나의 Prefab을 기준으로 하나의 `GameObjectPool`을 생성하며, 객체의 생성, 대여, 반환, 비활성 보관 및 Pool 종료까지의 생명주기를 관리합니다.

게임별 상태 초기화나 반환 시점 판단은 Pool이 담당하지 않으며, 사용하는 코드에서 직접 관리합니다.

## 주요 기능

- GameObject 기반 Object Pool
- `Get()`을 통한 객체 대여
- `Release()`를 통한 객체 반환
- 자동 활성화 / 비활성화
- `Prewarm()`을 통한 사전 생성
- 필요 시 자동 확장
- 최대 비활성 보관 수 제한
- Pool 소유권 검증
- 중복 반환 방지
- 대여 / 반환 상태 추적
- 선택적인 Parent 관리
- `Clear()`를 통한 비활성 객체 제거
- `Dispose()`를 통한 Pool 전체 정리
- 외부에서 파괴된 객체에 대한 제한적인 상태 복구

## 요구 사항

- Unity 6.3 이상
- 별도의 외부 패키지 의존성 없음

## 설치

Unity Package Manager에서 Git URL을 사용해 설치할 수 있습니다.

Git 저장소의 패키지 경로를 사용하는 경우 프로젝트 환경에 맞는 Git URL을 등록합니다.

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