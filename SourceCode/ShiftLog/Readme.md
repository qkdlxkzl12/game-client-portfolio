\# ShiftLog



잠입과 조사를 중심으로 진행되는 2D 스토리 기반 어드벤처 게임입니다.  

본 폴더에는 프로젝트에서 담당한 UI 시스템과 입력·게임 진행 관련 코드 일부를 정리했습니다.



\## 개발 개요



\- 장르: 2D 스토리 기반 어드벤처

\- 개발 환경: Unity, C#

\- 담당 분야: UI 시스템 설계 및 구현, 게임 시스템 개발

\- 주요 라이브러리: DOTween



\## 주요 구현



\### 1. Popup UI 공통 구조



`UIManager`와 `UI\_Popup`을 기반으로 팝업의 생성, 열기, 닫기 및 현재 상태를 공통으로 관리했습니다.



\- `ShowPopupUI<T>()`를 통한 타입 기반 팝업 생성 및 재사용

\- `ShowPopupUI<T, D>()`와 `IInitializablePopup<T>`를 통한 데이터 전달 및 초기화

\- 팝업별 열기·닫기 연출을 `CreateOpenSequence()`와 `CreateCloseSequence()`로 분리

\- 연출 중 상호작용 차단 및 중복 열기·닫기 요청 처리

\- `IsTracked`, `UseBlinder`, `AffectedCancel` 설정을 통한 팝업별 관리 방식 구분

\- 일반 팝업 위에 획득 알림이나 선택지처럼 별도로 동작하는 UI를 표시할 수 있도록 확장



\### 2. Binder UI



인벤토리, 증거 수집함, 설정을 하나의 바인더 형태로 관리하는 통합 UI를 구현했습니다.



\- `UI\_BinderSection` 추상 클래스로 각 Section의 초기화와 열람 동작을 공통화

\- Inventory, ClueCollection, Setting Section을 독립된 클래스로 분리

\- 각 페이지의 왼쪽·오른쪽 영역과 현재 Section 상태를 기준으로 콘텐츠 관리

\- DOTween Sequence를 이용한 페이지 넘김 및 인덱스 전환 연출

\- 새로운 Section을 추가할 때 기존 페이지 전환 로직의 변경을 줄일 수 있도록 구성



\### 3. Incident Board UI



수집한 증거를 배치하고 연결하여 사건의 질문과 해답을 완성하는 증거 보드 UI를 구현했습니다.



\- 질문, 증거, 해답을 `UI\_BoardItem` 기반의 개별 타입으로 구성

\- 증거 카드의 Drag \& Drop과 배치 가능 여부 판정

\- 질문 카드에 제출된 증거의 정답·오답 검증

\- 증거 조합 결과에 따른 다음 질문과 해답 생성

\- 보드 위 항목 사이의 핀과 연결선 생성 및 제거 연출

\- 보드 확대·축소와 Drag 이동 시 표시 가능한 영역을 벗어나지 않도록 위치 보정



\### 4. 행동 기반 입력 처리



물리 키와 게임 행동을 분리하기 위해 `KeyInputSystem`을 구현했습니다.



\- `\[Flags]` 기반 `EUserAction`으로 동시에 입력된 행동 수집

\- 행동과 `KeyCode`의 매핑을 통한 입력 바인딩

\- `IsPress()`를 통해 단일 입력과 Hold 입력을 구분

\- 입력을 키 값이 아닌 Move, Interaction, Cancel 등의 행동 단위로 전달



`GameManager.HandleInput()`에서는 수집된 행동을 현재 게임 상태에 따라 해석하도록 구성했습니다.



\- Popup, Dialogue, Interaction 상태에 따른 입력 우선순위 처리

\- 동일한 키를 사용하는 Setting과 Cancel 기능의 실행 조건 구분

\- 대화 진행 중 아이템 획득 UI와 선택지 등의 예외 처리

\- UI, 순간이동, Sequence 진행 상태에 따른 플레이어 이동 제한



\## 폴더 구성



\- `UI`

&#x20; - Popup UI 공통 구조와 개별 UI

&#x20; - Binder UI

&#x20; - Incident Board UI

\- `System`

&#x20; - 대화 시스템

&#x20; - 행동 기반 입력 시스템

\- `GameManager.cs`

&#x20; - 입력 해석과 게임 진행 흐름

\- `Util.cs`

&#x20; - 공통 유틸리티 기능



\## 참고



이 저장소에는 포트폴리오 검토를 위해 프로젝트에서 직접 담당한 코드 일부만 포함되어 있습니다.  

따라서 프로젝트 전체 리소스와 외부 의존성이 포함되지 않아 단독 실행을 목적으로 하지 않습니다.

