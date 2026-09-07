# Texas Bingo Multiplayer

싱글플레이로 제작한 Texas Bingo를 PUN2 기반 멀티플레이 게임으로 확장한 프로젝트입니다.

각 플레이어가 자신의 5×5 보드에 카드를 배치하고, 같은 위치의 Poker Hand를 비교하여 더 높은 족보를 완성한 플레이어가 승점을 획득합니다. 캐릭터별 능력과 Event Card를 추가하여 디지털 게임에 맞는 전략 요소를 강화했습니다.

본 폴더에는 빙고 보드 구조, 카드 배치 흐름, 셀 효과, 카드 공개 연출 및 결과 UI 관련 코드를 정리했습니다.

## 개발 개요

- 장르: 턴제 전략 멀티플레이 보드게임
- 개발 환경: Unity, C#, PUN2
- 담당 분야: 멀티플레이 구조 전환, Turn Flow, 보드 및 카드 시스템, 캐릭터 능력 연동
- 주요 라이브러리: Photon PUN2, DOTween

## 주요 구현

### 1. Bingo Board 역할 분리

보드의 데이터, 진행 상태와 화면 표현을 분리하여 구성했습니다.

- `BingoBoard`: Cell과 Line 데이터 및 족보 결과 관리
- `BingoBoardRuntime`: 선택, 미리보기, 배치 확정과 Cell 효과 처리
- `BingoBoardView`: 사용자 입력과 화면 연출 연결
- `BingoCellView`: Cell 상태에 따른 이미지와 Animation 갱신

보드 데이터가 View를 직접 참조하지 않고, Runtime에서 발생한 `OnCellChanged` event를 View가 받아 화면을 갱신하도록 구성했습니다.

### 2. 카드 배치 상태 관리

카드 선택부터 최종 배치까지의 상태를 단계별로 분리했습니다.

- 배치할 Cell 선택 및 Preview 표시
- 이전 선택을 변경할 때 기존 Preview 해제
- 준비 완료 시 Cell을 `Ready` 상태로 전환
- 네트워크 배치 요청 이후 `Commit` 처리
- Commit Animation이 끝난 뒤 보드와 Line 데이터 갱신
- 배치 가능한 Cell이 없을 경우 자동으로 준비 완료

`CellChangeReason`을 통해 Preview, Ready, Commit, Effect 등의 변경 원인을 View에 전달하여 상태별 연출을 분리했습니다.

### 3. 빙고 라인과 Poker Hand 갱신

카드가 확정되면 해당 Cell이 속한 가로, 세로, 대각선 Line에 카드 정보를 전달합니다.

- 카드 한 장이 영향을 주는 Line만 갱신
- 카드 추가 시 가능한 Poker Hand 후보 재계산
- 5장의 카드가 완성되면 최종 Rank와 Kicker 결정
- Line 완성 event를 통해 캐릭터 능력과 후속 기능 연결
- 12개 Line의 결과를 플레이어별로 비교

보드 전체를 매번 다시 계산하지 않고, 카드가 추가된 Line의 분석 상태를 누적해서 갱신하도록 구성했습니다.

### 4. 캐릭터 능력과 Cell 효과

캐릭터의 능력 발동 시점에 따라 필요한 game event와 Turn Phase에 기능을 연결했습니다.

- 카드 공개 시 발동하는 Passive Ability
- 플레이어가 선택하여 사용하는 Active Ability
- 빙고 Line 완성 시 발동하는 Ability
- Turn Phase에 따른 능력 설치와 상태 초기화
- 모래폭풍과 석화에 의한 Cell 배치 제한
- Turn End에서 임시 Cell 효과 해제

발동 시점은 보드와 게임 흐름에서 관리하고, 실제 능력 동작은 캐릭터 객체가 담당하도록 분리했습니다.

### 5. 비동기 연출 완료 대기

네트워크 Turn Flow가 화면 연출보다 먼저 진행되지 않도록 Phase 작업 등록 구조를 적용했습니다.

- Animation 시작 전에 Phase Job 등록
- DOTween `OnComplete`에서 작업 완료 전달
- Ready와 Commit 연출이 끝난 뒤 다음 흐름 진행
- 카드 공개 및 제거 연출 완료 시 Phase 대기 해제
- 여러 연출이 진행되는 동안 Turn Phase 전환 방지

이를 통해 데이터 처리는 완료됐지만 카드 또는 토큰 연출이 남아 있는 상태에서 다음 Phase로 넘어가는 문제를 방지했습니다.

### 6. 카드 공개와 선택 연출

한 장 또는 여러 장의 카드가 공개되는 상황을 공통 흐름으로 처리했습니다.

- Deck에서 다음 카드 추출
- 단일·복수 카드 공개 연출 구분
- 여러 카드 중 실제 배치할 카드 선택
- 카드 선택 변경 시 위치 교환 연출
- 캐릭터 능력에 따른 실제 카드와 표시 카드 분리
- Event Card에 대한 별도 공개 규칙 적용

실제 카드 정보와 플레이어에게 표시되는 정보를 분리하여 속임수 계열 능력의 시각적 표현을 지원했습니다.

### 7. 결과 비교 UI

게임 종료 후 12개 빙고 Line을 순서대로 비교하는 결과 화면을 구현했습니다.

- 플레이어별 보드와 캐릭터 정보 표시
- 현재 비교 중인 Line 강조
- Line별 Poker Hand 결과 출력
- 승리한 Line과 이전 비교 결과 갱신
- 모든 Line을 순차적으로 보여주는 결과 연출

## 폴더 구성

- `BingoBoard`
  - 보드·Cell·Line 데이터
  - Runtime 상태 처리
  - View와 Animation 연결
- `Card`
  - Playing, Event, Mission Card 데이터
  - 카드 외형을 관리하는 Skin 데이터
- `Manager`
  - 카드와 UI 관련 공통 관리
- `UI`
  - 카드 공개·선택 연출
  - 플레이어별 결과 비교 UI

## 참고

이 저장소에는 포트폴리오 검토를 위해 프로젝트에서 직접 담당한 코드 일부만 포함되어 있습니다.  
네트워크 제어, Turn Phase 및 캐릭터 능력과 관련된 일부 연관 클래스는 포함되지 않아 단독 실행을 목적으로 하지 않습니다.
