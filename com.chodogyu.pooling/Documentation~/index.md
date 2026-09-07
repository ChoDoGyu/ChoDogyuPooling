\# ChoDogyu Object Pooling Documentation



\## 개요



ChoDogyu Object Pooling은 Unity GameObject 인스턴스를 반복적으로 생성하고 제거하는 대신 기존 인스턴스를 재사용하기 위한 Runtime 패키지입니다.



각 `GameObjectPool`은 하나의 Prefab을 기준으로 동작합니다.



Pool은 객체의 생명주기와 저장 상태를 관리하지만 게임별 상태 초기화는 담당하지 않습니다.



\---



\## GameObjectPool 생성



```csharp

GameObjectPool pool = new(prefab);

