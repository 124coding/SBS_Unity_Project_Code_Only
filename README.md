# ⚔️ [프로젝트명] - 2D 횡스크롤 및 턴제 RPG 프레임워크 

> **기획자의 생산성을 높이는 데이터 주도 설계(Data-Driven)와 비동기 연출 동기화에 집중한 클라이언트 코어 아키텍처입니다.**
> *🚨 본 레포지토리는 상용 에셋 저작권 보호를 위해 **순수 C# 스크립트(Scripts) 코드만 업로드된 포트폴리오용 레포지토리**입니다.*

<br>

## 🎥 게임 시연 (Play Video)
![Game_Play_GIF_링크_넣기](여기에_핵심전투_움짤_링크_삽입.gif)
* 📺 **[YouTube 전체 플레이 영상 보기](유튜브_링크_삽입)**
* 🎮 **[실행 가능한 빌드 다운로드](구글드라이브_또는_itch.io_링크_삽입)**

<br>

## 📋 프로젝트 개요 (Overview)
- **개발 기간:** 2026.05 ~ 현재 (진행 중)
- **장르:** 2D 플랫포머 탐험 + 턴제 RPG 전투
- **담당 역할:** 클라이언트 프로그래머 (코어 아키텍처 설계, 전투 엔진, AI, 물리 및 최적화 전담)
- **기술 스택:** `Unity 6000.3.14f1`, `C#`

<br>

## 💡 핵심 구현 및 트러블슈팅 (Core Highlights)

자세한 고민 과정과 코드 아키텍처는 아래 요약 및 코드 내부 주석을 통해 확인하실 수 있습니다.

### 1. 로직과 연출의 분리 및 비동기 전투 동기화 버그 해결
- **문제:** 다단 히트 스킬 연출 도중 타겟이 사망하면 타겟팅이 꼬여 데미지가 중복되거나 허공에 투사체가 날아가는 상태 동기화 에러 발생.
- **해결 (`Target Snapshot & Filtering` 패턴):** - 수치 연산 전담(`BattleLogicHandler`)과 시각적 연출 전담(`CharacterAction`)으로 관심사 완전 분리.
  - 스킬 시전 시점의 타겟 명세서를 **스냅샷(Snapshot)**으로 캡처하고, 실제 투사체 적중 시 교집합을 필터링하여 데미지를 부여함으로써 헛스윙과 중복 연산 원천 차단.
- **📁 관련 코드:** [`BattleLogicHandler.cs`](https://github.com/124coding/SBS_Unity_Project_Code_Only/blob/main/Battle/BattleLogicHandler.cs), [`CharacterAction.cs`](https://github.com/124coding/SBS_Unity_Project_Code_Only/blob/main/Character/CharacterAction.cs)

### 2. EffectPayload 기반 데이터 주도 설계(Data-Driven)와 Enemy AI
- **설계:** 데미지 공식, 타겟팅 조건, 상태이상 정보를 하나의 `EffectPayload` 구조체로 모듈화하여 확장성 확보. 기획자가 코드 수정 없이 스킬 기믹 조립 가능.
- **AI 행동 평가 (Utility AI 기초):** 적 AI가 무작위로 행동하지 않고, `EffectPayload` 내부에 설계된 `aiWeight` 값과 현재 전장 상황(적 체력, 버프 상태)을 계산하여 **가장 점수(Score)가 높은 합리적 행동을 선택**하도록 설계.
- **📁 관련 코드:** [`EffectSystem.cs`](https://github.com/124coding/SBS_Unity_Project_Code_Only/blob/main/Character/EffectSystem.cs), [`EnemyAI.cs`](https://github.com/124coding/SBS_Unity_Project_Code_Only/blob/main/Character/Enemy/EnemyAI.cs)

### 3. 2D 물리 주기 동기화 및 렌더링 최적화
- **Jittering(떨림) 해결:** 무빙 플랫폼 이동 시 화면 떨림 현상을 분석, `FixedUpdate`와 `Cinemachine` 주기를 동기화하고 발판 이동을 `Rigidbody2D.MovePosition`으로 제어하여 이동 동기화 물리 구현.
- **가비지 컬렉션(GC) 및 물리 최적화:** - `CompositeCollider2D`를 통한 맵 전체 다각망(Baking) 병합으로 물리 연산 과부하 제거.
  - 룸(Room) 단위 청크 렌더링 시스템 및 애니메이션 이벤트(`EndAction`)와 연동된 이펙트 오브젝트 풀링(Object Pooling) 구축.
- **📁 관련 코드:** [`MovingPlatform.cs`](https://github.com/124coding/SBS_Unity_Project_Code_Only/blob/main/Field/WorkObject/MovingPlatform.cs), [`RoomManager.cs`](https://github.com/124coding/SBS_Unity_Project_Code_Only/blob/main/Room/RoomManager.cs)

<br>
