\# Changelog



이 패키지의 주요 변경 사항을 기록합니다.



\## \[1.0.0] - 2026-09-08



\### Added



\- GameObject 기반 `GameObjectPool`

\- `Get()` / `Release()` 객체 재사용

\- 자동 활성화 / 비활성화

\- `Prewarm()` 사전 생성

\- 필요 시 Pool 자동 확장

\- `MaxInactiveCount` 비활성 보관 제한

\- `CountAll`, `CountInUse`, `CountInactive` 상태 정보

\- Pool 소유권 검증

\- 중복 반환 및 잘못된 반환 방지

\- 선택적인 Parent 관리

\- Parent 손실 시 Scene Root 폴백

\- `Clear()` 비활성 객체 정리

\- `Dispose()` Pool 전체 종료

\- 외부에서 파괴된 객체에 대한 제한적인 상태 복구

\- Unity Test Framework 기반 Runtime 테스트

\- 재사용 동작 확인용 Benchmark 환경

