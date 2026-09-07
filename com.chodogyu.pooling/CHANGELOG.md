# Changelog

ChoDogyu Object Pooling 패키지의 주요 변경 사항을 기록합니다.

## [1.0.0] - 2026-09-08

첫 정식 배포 버전입니다.

### Added

#### GameObject Pool

- 하나의 Prefab을 기준으로 동작하는 `GameObjectPool` 추가
- `Get()`을 통한 객체 대여
- `Release()`를 통한 객체 반환
- 비활성 객체 우선 재사용
- 사용 가능한 객체가 없을 경우 자동 확장
- 대여 시 자동 활성화
- 반환 시 자동 비활성화

#### 상태 및 소유권 관리

- Pool이 생성한 객체의 소유권 추적
- 현재 대여 중인 객체 상태 추적
- 다른 Pool이 소유한 객체 반환 방지
- 중복 Release 방지
- 잘못된 반환 상태 검증
- `CountAll` 상태 정보 제공
- `CountInUse` 상태 정보 제공
- `CountInactive` 상태 정보 제공

#### Parent 관리

- 선택적인 Pool Parent 지원
- 새 객체를 Pool Parent 아래에서 생성
- 반환 시 객체를 Pool Parent로 복귀
- 사용 중 자유로운 Reparent 허용
- Parent 미지정 시 Scene Root 사용
- Pool Parent 손실 시 Scene Root 폴백

#### Prewarm

- `Prewarm()`을 통한 사전 객체 생성
- 필요한 최소 비활성 객체 수를 보장하는 방식으로 동작
- 이미 충분한 객체가 존재하는 경우 추가 생성 방지

#### 비활성 보관 수 제한

- `MaxInactiveCount` 추가
- 최대 비활성 보관 수 설정 지원
- 최대 보관 수와 동시 대여 가능 객체 수 분리
- 비활성 보관 한도를 초과한 반환 객체 제거
- `Prewarm()`이 `MaxInactiveCount`를 초과하지 않도록 검증

#### Clear

- `Clear()` 추가
- 현재 비활성 상태인 객체만 제거
- 현재 대여 중인 객체 유지
- Clear 이후 Pool 재사용 지원
- 반복 Clear 호출 지원

#### Dispose

- `IDisposable` 구현
- `Dispose()`를 통한 Pool 전체 정리
- 비활성 객체와 대여 중 객체 모두 제거
- Dispose 이후 Pool 사용 방지
- `IsDisposed` 상태 정보 제공
- 반복 Dispose 호출 지원

#### Edge Case 처리

- 외부에서 파괴된 Inactive 객체의 제한적인 상태 복구
- 외부에서 파괴된 InUse 객체 Release 시 추적 상태 복구
- 실제 null과 Destroy된 Unity Object 참조 구분
- Pool Parent가 외부에서 파괴된 경우 Scene Root 폴백
- Parent와 비활성 객체가 함께 파괴된 경우 다음 Get에서 복구

#### Tests

- Unity Test Framework 기반 Runtime Test 추가
- Get / Release 동작 검증
- 객체 재사용 검증
- 소유권 검증
- 대여 상태 검증
- Count 상태 검증
- Parent 동작 검증
- Prewarm 검증
- MaxInactiveCount 검증
- Clear 검증
- Dispose 검증
- 외부 Destroy Edge Case 검증
- Parent 손실 Edge Case 검증
- 다중 Pool 독립성 검증
- 반복적인 상태 전이 검증
- v1.0 기준 56개 Runtime Test 통과

#### Documentation

- 패키지 README 추가
- 세부 사용 및 설계 문서 추가
- Public API 사용법 문서화
- Pool과 사용자 코드의 책임 범위 문서화
- 외부 Destroy 및 Thread Safety 정책 문서화