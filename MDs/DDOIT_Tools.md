# DDOIT Tools 가이드

> DDOIT VR 교육프로그램 통합 개발 템플릿 및 도구 모음

---

## 1. 개요

### 1.1 목적
DDOIT Tools는 VR 교육프로그램을 효율적으로 개발하기 위한 **공통 도구 모음 및 템플릿**입니다.

### 1.2 사용 방식
- **방법 A**: `DDOIT_Tools.unitypackage`를 기존 프로젝트에 임포트
- **방법 B**: `DDOIT_Template` 프로젝트를 복제하여 새 프로젝트 시작
- **방법 C**: UPM 패키지(`com.ddoit.tools`)로 소비 프로젝트에 설치

### 1.3 개발/배포 원칙
- `DDOIT_Template`은 `DDOIT_Tools` UPM 패키지의 원본 개발 프로젝트입니다.
- `Assets/DDOIT_Tools`가 실시간 개발 원본(source of truth)입니다.
- `D:\DDOIT\Packages\DDOIT_Tools`는 UPM 배포를 위한 패키지 산출물/미러입니다.
- 공통 기능 수정은 먼저 `Assets/DDOIT_Tools`에서 수행합니다.
- 배포가 필요할 때만 `Assets/DDOIT_Tools` → `D:\DDOIT\Packages\DDOIT_Tools` 방향으로 동기화합니다.
- 반대 방향 수정은 명시적인 유지보수 작업이 아닌 한 하지 않습니다.

### 1.4 기술 스택
| 항목 | 버전 |
|---|---|
| Unity | 6000.3.7f1 |
| Meta XR All-In-One SDK | 201.0.0 (Audio/Voice 모듈만 v85.0.0 유지) |
| Meta XR Movement SDK | 201.0.0 (`#v201.0.0` tag pin) |
| XR Management | 4.5.3 |
| OpenXR | 1.16.1 |
| Addressables | 2.8.1 |
| URP | 17.3.0 |
| Input System | 1.18.0 |
| 네트워킹 | Photon Fusion 2.0.9 (예정) |
| Target Platform | Android (Meta Quest) |

---

## 2. 프로젝트 구조

### 2.1 폴더 구조
```
Assets/
├── DDOIT_Tools/                    ← 공통 도구 (패키지 포함 대상)
│   ├── Fonts/                      ← TMP 폰트 (Dynamic SDF)
│   ├── Prefabs/                    ← 공통 프리팹 (UIPanel 등)
│   ├── Scenes/                     ← 템플릿 씬
│   │   └── DDOIT.unity             ← 부트스트랩 씬 (Persistent)
│   └── Scripts/                    ← 스크립트
│       ├── Scenario/               ← 시나리오 시스템 (핵심)
│       │   └── Nodes/              ← 기본 제공 노드
│       ├── Audio/                  ← 음향 관리
│       ├── Editor/                 ← 에디터 전용 스크립트
│       ├── Managers/               ← 범용 매니저
│       ├── Network/                ← 네트워크 (Photon)
│       ├── Addressables/           ← 원격 업데이트, 에셋 로딩
│       ├── VR/                     ← VR 공통 유틸
│       │   └── Movement/           ← Movement SDK 래퍼
│       ├── UI/                     ← 공통 UI 컴포넌트
│       ├── Data/                   ← ScriptableObject, 설정 데이터
│       ├── Settings/                ← 전역 설정 (DDOITSettings)
│       └── Utilities/              ← 범용 헬퍼
├── {프로젝트명}/                    ← 개별 프로젝트 고유 코드
│   ├── Managers/
│   ├── Scenarios/
│   ├── UI/
│   └── ...
└── 01. Scenes/                     ← 프로젝트 콘텐츠 씬

DDOIT_Template/                      ← 프로젝트 루트
├── AGENTS.md                        ← AI Agent 공통 규칙 (자동 참조)
├── CLAUDE.md                        ← AGENTS.md와 동일 내용
└── Assets/
    └── DDOIT_Tools/
        └── MDs/                     ← 도구 문서
            ├── AGENTS.md            ← UPM 소비 프로젝트 루트 배포용
            ├── CLAUDE.md            ← AGENTS.md와 동일 내용
            ├── DDOIT_Tools.md       ← 이 문서
            └── Meta_XR_SDK_Unity_Guide.md
```

### 2.2 Namespace 구조
```
DDOIT.Tools.{카테고리}          ← 공통 코드
DDOIT.{프로젝트명}.{카테고리}    ← 프로젝트 고유 코드
```

---

## 3. DDOIT 코딩 규칙 (심화)

> 기본 코딩 규칙(`AGENTS.md`/`CLAUDE.md`)을 기반으로, DDOIT 프로젝트에 특화된 심화 규칙입니다.

### 3.1 Namespace 적용 규칙

`AGENTS.md`/`CLAUDE.md` §5의 일반 원칙을 DDOIT에 적용한 구체적인 규칙입니다.

#### 3.1.1 이중 구조 원칙
```
DDOIT.Tools.{카테고리}          ← DDOIT_Tools/ 폴더 (공통, 패키지 포함 대상)
DDOIT.{프로젝트명}.{카테고리}    ← {프로젝트명}/ 폴더 (프로젝트 고유)
```

#### 3.1.2 공통 코드 네임스페이스 (`DDOIT.Tools.*`)
```csharp
// 오디오
namespace DDOIT.Tools.Audio
{
    public class SoundManager : MonoBehaviour { }
    public class BGMController : MonoBehaviour { }
}

// 범용 매니저
namespace DDOIT.Tools.Managers
{
    public class GameManager : MonoBehaviour { }
    public class SceneFlowManager : MonoBehaviour { }
}

// 네트워크 (Photon 등)
namespace DDOIT.Tools.Network
{
    public class NetworkManager : MonoBehaviour { }
}

// Addressables (원격 업데이트, 에셋 로딩)
namespace DDOIT.Tools.Addressables
{
    public class RemoteUpdateManager : MonoBehaviour { }
    public class AssetLoader : MonoBehaviour { }
}

// VR 공통
namespace DDOIT.Tools.VR
{
    public class VRPlayerSetup : MonoBehaviour { }
    public class HandTrackingHelper : MonoBehaviour { }
}

namespace DDOIT.Tools.VR.Movement
{
    public class MovementSDKHelper : MonoBehaviour { }
}

// UI 공통 컴포넌트
namespace DDOIT.Tools.UI
{
    public class BaseMenuUI : MonoBehaviour { }
    public class LoadingScreenUI : MonoBehaviour { }
}

// 데이터
namespace DDOIT.Tools.Data
{
    [System.Serializable]
    public class AppSettings { }
}

// 유틸리티
namespace DDOIT.Tools.Utilities
{
    public static class MathHelper { }
    public static class StringHelper { }
}
```

#### 3.1.3 프로젝트 고유 코드 네임스페이스 (`DDOIT.{프로젝트명}.*`)
```csharp
// 예시: 안전교육 프로젝트
namespace DDOIT.SafetyTraining.Managers
{
    public class TrainingSessionManager : MonoBehaviour { }
}

namespace DDOIT.SafetyTraining.Scenarios
{
    public class FireEvacuationScenario : MonoBehaviour { }
    public class ElectricalSafetyScenario : MonoBehaviour { }
}

namespace DDOIT.SafetyTraining.UI
{
    public class ScenarioSelectUI : MonoBehaviour { }
    public class TrainingResultUI : MonoBehaviour { }
}

// 예시: 의료 시뮬레이션 프로젝트
namespace DDOIT.MedicalSim.Patient
{
    public class PatientController : MonoBehaviour { }
}
```

#### 3.1.4 폴더-네임스페이스 매핑
```
Assets/
├── DDOIT_Tools/                    ← DDOIT.Tools.*
│   └── Scripts/
│       ├── Audio/                  ← DDOIT.Tools.Audio
│       ├── Managers/               ← DDOIT.Tools.Managers
│       ├── Network/                ← DDOIT.Tools.Network
│       ├── Addressables/           ← DDOIT.Tools.Addressables
│       ├── VR/                     ← DDOIT.Tools.VR
│       │   └── Movement/           ← DDOIT.Tools.VR.Movement
│       ├── UI/                     ← DDOIT.Tools.UI
│       ├── Data/                   ← DDOIT.Tools.Data
│       └── Utilities/              ← DDOIT.Tools.Utilities
└── {프로젝트명}/                    ← DDOIT.{프로젝트명}.*
    ├── Managers/
    ├── Scenarios/
    └── UI/
```

#### 3.1.5 분리 기준
| 판단 기준 | DDOIT.Tools.* | DDOIT.{프로젝트명}.* |
|---|---|---|
| 다른 프로젝트에서도 재사용 가능한가? | O | X |
| 특정 교육 콘텐츠에 종속되는가? | X | O |
| unitypackage에 포함되어야 하는가? | O | X |

### 3.2 공통 코드 수정 금지 원칙
- `DDOIT.Tools.*` 네임스페이스의 코드를 **개별 프로젝트에서 직접 수정하지 않음**
- 확장이 필요하면 **상속 또는 래퍼**를 프로젝트 고유 네임스페이스에 작성
- 공통 코드 자체의 개선은 **DDOIT_Template 프로젝트에서만** 수행

```csharp
// 예시: 공통 SoundManager를 확장하는 경우
namespace DDOIT.SafetyTraining.Audio
{
    public class TrainingSoundManager : DDOIT.Tools.Audio.SoundManager
    {
        // 프로젝트 고유 오디오 로직
    }
}
```

### 3.3 VR 교육프로그램 전용 규칙

#### 3.3.1 시나리오 클래스 명명
교육 시나리오 클래스는 `{주제}Scenario` 형식으로 명명합니다.
```csharp
namespace DDOIT.SafetyTraining.Scenarios
{
    public class FireEvacuationScenario : MonoBehaviour { }
    public class ElectricalSafetyScenario : MonoBehaviour { }
    public class ChemicalHandlingScenario : MonoBehaviour { }
}
```

#### 3.3.2 Addressable 에셋 키 명명
교육 콘텐츠의 Addressable 에셋 키는 다음 형식을 따릅니다.
```
{그룹}_{프로젝트약어}_{에셋유형}_{식별자}

예시:
Scenario_ST_Prefab_FireEvacuation
Media_ST_Video_IntroGuide
Config_ST_Data_ScenarioList
```

#### 3.3.3 디버그 로그 접두사
`CLAUDE.md` §8의 로그 규칙을 따르되, 공통/프로젝트 구분을 명확히 합니다.
```csharp
// 공통 코드
Debug.Log("[DDOIT.SoundManager] BGM 재생 시작");
Debug.Log("[DDOIT.RemoteUpdateManager] 업데이트 확인 중...");

// 프로젝트 고유 코드
Debug.Log("[SafetyTraining.ScenarioManager] 시나리오 로드: FireEvacuation");
```

---

## 4. 모듈별 가이드

### 4.1 Scenario (`DDOIT.Tools`)

**용도**: 교육 시나리오를 순차적으로 실행하는 **핵심 시스템**. 디자이너가 코딩 없이 인스펙터에서 노드를 조합하여 교육 흐름을 구성할 수 있다.

#### 4.1.1 계층 구조

```
ScenarioManager          ← 진입점. 시퀀스 시작/종료
└── Scenario             ← Step 컨테이너. 다음 Scenario로 분기 가능
    └── Step             ← Node 컨테이너. 조건 그룹(AND+OR) 충족 시 종료 + 분기
        ├── SoundNode              ← 사운드 재생
        ├── TransformNode          ← 오브젝트 이동/회전/스케일 (Duration/Speed/Instant)
        ├── TeleportNode           ← 플레이어 텔레포트 (페이드 포함)
        ├── ToggleNode             ← GameObject/Component/Particle/Script On/Off
        ├── AnimatorNode           ← Animator 파라미터 설정 (Trigger/Bool/Int/Float)
        ├── UINode                 ← UI 패널 표시 (버튼 이벤트)
        ├── TriggerConditionNode   ← 트리거 감지 (Enter/Exit/Stay 조건)
        └── TimerConditionNode     ← 시간 경과 (조건)
```

- **ScenarioManager**: `StartSequence()`로 Entry Scenario를 시작. 모든 Scenario를 초기화.
- **Scenario**: 하위 Step을 순차 실행. `_nextScenario`로 다음 시나리오 연결. 모든 Step 완료 시 `EndTrigger()`.
- **Step**: 하위 Node를 활성화하고 `Init()` 호출. **조건 그룹 시스템**으로 분기 가능 (§4.1.7). 조건 그룹이 0개이면 자동 진행하지 않고 `Step.EndTrigger()` 호출 전까지 대기.
- **ScenarioNode**: 모든 노드의 추상 베이스 클래스. 각 노드는 `ConditionGroup`(int) 프로퍼티로 그룹 소속을 가짐 (0 = 미소속).

#### 4.1.2 실행 흐름

```
ScenarioManager.StartSequence()
  → Scenario.StartTrigger()
    → Step.StartTrigger()
      → _onStart UnityEvent 발동
      → node.Init() → OnInit()          ← 각 노드 초기화
      → (조건 그룹별 AND 대기, 그룹 간 OR)
      → 한 그룹 완료 → 그룹 index 기록
      → node.Release() → OnRelease()    ← 각 노드 정리
      → _onRelease UnityEvent 발동
    → Step.EndTrigger()
      → _onEnd UnityEvent 발동
      → 완료 그룹의 _groupTargetStep/Scenario로 분기 (없으면 다음 Step)
      → gameObject.SetActive(false)     ← OnDisable() 발동 (안전장치)
    → 다음 Step 시작 ...
  → Scenario.EndTrigger()
    → 다음 Scenario 시작 (있으면)
```

#### 4.1.3 노드 라이프사이클

| 단계 | 메서드 | 호출 시점 | 용도 |
|---|---|---|---|
| 초기화 | `Init()` → `OnInit()` | Step 시작 시 | 코루틴 시작, 사운드 재생 등 |
| 종료 | `Release()` → `OnRelease()` | Step 종료 시 (비활성화 전) | 시나리오 흐름 내 정리 |
| 비활성화 | `OnDisable()` | `gameObject.SetActive(false)` 시 | 코루틴 강제 중단 (안전장치) |

- `OnRelease()`는 **시나리오 흐름**에서의 정리 (예: 원래 위치 복귀, 이벤트 해제)
- `OnDisable()`는 **Unity 라이프사이클**에서의 안전장치 (예: 씬 전환 시 코루틴 중단)
- 두 콜백의 역할이 다르므로 모두 유지

#### 4.1.4 기본 제공 노드

| 노드 | 용도 | 조건 그룹 소속 가능 |
|---|---|---|
| **SoundNode** | SoundDatabase의 사운드 재생 | O (재생 완료 시 충족) |
| **TransformNode** | 오브젝트 이동/회전/스케일 (Duration/Speed/Instant 모드, 항목 독립 제어) | O (활성화된 모든 항목 완료 시 충족) |
| **TeleportNode** | 플레이어 텔레포트 (Fade→Teleport→FadeClear) | — (즉시 완료, `_onEnd` 이벤트) |
| **WalkingStickNode** | `PlayerRig.EnableWalkingStick()` / `DisableWalkingStick()` 호출. 활성화 시점의 HMD 높이로 stick 길이 자동 결정 | — (즉시 완료, `_onEnd` 이벤트) |
| **ToggleNode** | GameObject / Component / ParticleSystem / IToggleable 스크립트 On/Off | — (즉시 완료, `_onEnd` 이벤트) |
| **AnimatorNode** | Animator의 Trigger/Bool/Int/Float 파라미터 설정 | — (즉시 완료, `_onEnd` 이벤트) |
| **UINode** | UIManager를 통한 UI 패널 표시 | O (버튼 클릭 시 충족) |
| **TriggerConditionNode** | 특정 태그 객체의 트리거 감지 (Enter/Exit/Stay) | O (전용 조건 노드) |
| **TimerConditionNode** | 지정 시간 경과 | O (전용 조건 노드) |

> **IToggleable 인터페이스**: `Go()` / `Stop()` 두 메서드를 구현하면 ToggleNode의 Script 모드에서 해당 스크립트를 On/Off할 수 있다.

**SoundNode 재생 정책**:

- `SoundNode`는 `SoundManager`가 존재하고 초기화된 상태에서만 사운드 재생을 시도한다.
- 사운드 이름이 비어 있거나, `SoundDatabase`에서 찾을 수 없거나, 실제 `AudioClip` 재생에 실패하면 Error 로그를 남기고 조건을 완료하지 않는다.
- 조건 그룹에 속한 `SoundNode`는 일반 사운드 재생이 끝났을 때 조건을 충족한다.
- Loop 사운드는 자연 종료 시점이 없으므로 조건 그룹 자동 완료용으로 사용하지 않는다.
- `Step 종료 시 정지` 옵션을 켜면 Step 종료, 노드 Release, 비활성화 시 해당 노드가 시작한 사운드를 정지한다. 기본값은 기존 동작 호환을 위해 꺼져 있다.

**Global SoundDatabase 정책**:

- `GlobalSDB.asset`은 기본 UI 효과음을 제공하는 패키지 내장 SoundDatabase이다.
- 기본 UI mp3 파일은 `Assets/DDOIT_Tools/Audio/` 또는 UPM 소비 프로젝트의 `Packages/com.ddoit.tools/Audio/` 안에 함께 존재해야 한다.
- UPM 소비 프로젝트에서 `Init Project`를 실행하면 Setup이 `Packages/com.ddoit.tools/Prefabs/GlobalSDB.asset`을 Addressables `DDOIT` 그룹에 `DDOIT/GlobalSDB` 주소로 등록한다.
- 새 프로젝트에서 Addressables Groups 창을 한 번도 열지 않았더라도 `Init Project`가 Addressables 설정과 `DDOIT` 그룹을 필요 시 생성한다.
- 이 방식은 `GlobalSDB.asset`을 `Assets/` 하위로 복제하지 않는다. 패키지 내부 기본 DB를 그대로 사용하며, 프로젝트별 사운드는 씬 전용 `SoundDatabase`가 우선한다.

#### 4.1.5 커스텀 노드 만들기

```csharp
namespace DDOIT.Tools
{
    public class MyCustomNode : ScenarioNode
    {
        [SerializeField] private float _value;

        protected override void OnInit()
        {
            // 초기화 로직
            // 조건 노드라면 완료 시 SetConditionMet() 호출
        }

        protected override void OnRelease()
        {
            // Step 종료 시 정리 (선택 — 필요한 경우만 override)
        }

        private void OnDisable()
        {
            // 코루틴 중단 등 안전장치 (코루틴 사용 시 필수)
        }
    }
}
```

**필수 사항**:
- `ScenarioNode` 상속
- `OnInit()` 구현 (abstract)
- 조건 노드로 사용 시 완료 시점에 `SetConditionMet()` 호출
- 코루틴 사용 시 `OnDisable()`에서 정리

**선택 사항**:
- `OnRelease()` override (시나리오 흐름 내 정리가 필요한 경우)
- 커스텀 에디터 작성 (`UnityEditor.Editor` 상속, `_isStepCondition`과 `_onRelease` 프로퍼티 포함)

#### 4.1.6 에디터 도구

| 에디터 | 기능 |
|---|---|
| **ScenarioManagerEditor** | 흐름 미리보기 + **Scenario 분기 시각화**, 시나리오 목록, 런타임 상태 표시 |
| **ScenarioEditor** | Step 목록 + **분기 트리 시각화**, 자동 넘버링, 조건 노드 수/진행 표시 |
| **StepEditor** | 노드 목록, **메모 편집**, 조건 충족 상태 (✓/○), 노드 추가 버튼 (9종) |
| **TransformNodeEditor** | Translate/Rotate/Scale 독립 토글, 모드별(Duration/Speed/Instant) 필드 표시 |
| **TeleportNodeEditor** | 목적지 Transform 설정, `_onEnd` 이벤트 |
| **WalkingStickNodeEditor** | 활성화 toggle, 동작 안내 HelpBox, `_onEnd` 이벤트 |
| **ToggleNodeEditor** | 모드별(GameObject/Component/Particle/Script) 대상 필드, Activate 토글 |
| **AnimatorNodeEditor** | 파라미터 타입별 입력 필드(Trigger/Bool/Int/Float) |
| **TriggerConditionNodeEditor** | 외부 Collider 설정, Collider 타입 버튼 (Box/Sphere/Capsule), 감지 모드(Enter/Exit/Stay) |
| **UINodeEditor** | UI 요소 플래그별 조건부 필드, Theme 기본값 안내, 버튼 이벤트 섹션, 작성 누락/분기 경고 |
| **SoundNodeEditor** | 사운드 이름 드롭다운, Step 종료 시 정지 옵션, 오디오 미리듣기, 미선택/누락/Loop 조건 경고 |
| **TimerConditionNodeEditor** | 대기 시간 설정, 0 이하 경고 |
| **ConditionGroupDrawer** | ScenarioNode의 `_conditionGroup` 필드를 그룹 번호 버튼 UI로 표시 |

#### 4.1.7 조건 그룹 시스템 (Step 분기)

Step은 **조건 그룹**을 통해 여러 완료 경로와 각 경로별 분기를 지원한다.

**구조**:
- 각 ScenarioNode는 `ConditionGroup` 값을 가짐 (0 = 그룹 미소속)
- Step의 `_conditionGroupCount`로 그룹 개수 설정 (기본 0, **최대 7**)
- 같은 그룹 내 모든 노드가 충족(AND) → 그룹 완료
- 그룹 중 하나라도 완료(OR) → Step 종료

**분기 대상**:
- `_defaultTargetStep` / `_defaultTargetScenario`: 조건 그룹이 없거나 그룹 미소속 진행 시 기본 분기
- `_groupTargetSteps[]` / `_groupTargetScenarios[]`: 그룹 인덱스별 분기 (개별 설정)
- 그룹 완료 시 해당 Step/Scenario로 이동. 미설정이면 Scenario의 다음 Step으로.

**예시**:
```
Step_RoomA (_conditionGroupCount = 2)
├── [그룹 1] TriggerConditionNode (포털 A 통과) → _groupTargetSteps[0] = Step_Forest
└── [그룹 2] TriggerConditionNode (포털 B 통과) → _groupTargetSteps[1] = Step_Cave
```

조건 그룹이 0개면 자동 진행하지 않는다. 해당 Step은 `Step.EndTrigger()`가 직접 호출될 때까지 대기한다.
따라서 시나리오 제작자는 Step을 다음으로 넘겨야 하는 지점에 조건 그룹을 지정하거나, UnityEvent/스크립트에서 `EndTrigger()`를 명시적으로 호출해야 한다.

**외부 marker (UINode 버튼 분기, v0.18.0+)**:

Step에 매개변수 없는 그룹 충족 메서드 7개(`MarkConditionGroup1` ~ `MarkConditionGroup7`)가 노출된다. UINode의 `_onButtonA` / `_onButtonB` UnityEvent 슬롯에서 Step을 target으로 끌어다 놓고 해당 메서드를 선택하면, 버튼 클릭 시 그룹 충족 marker가 Step에 보고된다.

- 그룹 내 노드 + 외부 marker는 **AND**로 검사 (모두 충족 시 그룹 완료)
- 그룹에 노드가 없고 외부 marker만 있으면 외부 marker 단독으로 충족
- StepEditor가 자동 가시화: 그룹 row에 `◆ UINode 'X' ▸ 버튼 A`처럼 표시 (Scene scan 기반)

**예시 (UI 분기 시나리오)**:
```
Step_Question (_conditionGroupCount = 2)
├── [그룹 1] UINode (질문 패널) ─┐
│                                ├── _onButtonA → Step.MarkConditionGroup1()
│                                └── _onButtonB → Step.MarkConditionGroup2()
├── _groupTargetSteps[0] = Step_AnswerA
└── _groupTargetSteps[1] = Step_AnswerB
```
UINode 자체의 `_conditionGroup`은 그대로 두면 "어느 버튼이든 충족하는 공통 그룹"으로 동작.

#### 4.1.8 Step 메모 및 분기 시각화

- **Step 메모**: 각 Step에 `_memo` 문자열(멀티라인). StepEditor에서 편집 가능. 시나리오 설계 의도 기록용.
- **Scenario 편집기**: Step들이 트리 형태로 시각화되어 `_groupTargetStep`에 따른 분기 흐름이 그려짐.
- **ScenarioManager 편집기**: Scenario 간 `_nextScenario` / `_groupTargetScenario` 분기가 네트워크 그래프로 표시됨.

---

### 4.2 Audio (`DDOIT.Tools.Audio`)

**용도**: BGM, SFX, 환경음 등 음향 일괄 관리

**핵심 클래스**:
- `SoundManager` — 전역 오디오 재생/정지/볼륨 제어

**사용 예시**:
```csharp
using DDOIT.Tools.Audio;

// 효과음 재생
SoundManager.Instance.PlaySFX("ButtonClick");
```

---

### 4.3 Managers (`DDOIT.Tools.Managers`)

**용도**: 프로젝트 공통 매니저 (게임 흐름, 씬 전환 등)

**핵심 클래스**:
- `GameManager` — 앱 라이프사이클 관리. 씬 로딩 완료를 대기한 후 시나리오 시작 (`WaitAndStartScenario`)
- `SceneManager` — Addressables 또는 경로 기반 콘텐츠 씬 전환. 전환 시작 시 활성 `UIPanel`을 모두 닫아 이전 콘텐츠 UI가 다음 씬에 남지 않게 한다.
- `UIManager` — UI 패널 풀링 및 표시/숨김 관리. 중복 초기화와 Pool 상태 오염을 방어한다 (§4.7 참조)

---

### 4.4 Addressables (`DDOIT.Tools.Addressables`)

**용도**: 원격 에셋 업데이트 및 동적 에셋 로딩

**배경**:
- 오프라인 설치 후 고객 VR 장비에 원격으로 콘텐츠 업데이트 필요
- Unity Addressables를 통해 에셋 번들 단위 업데이트 구현

**핵심 클래스**:
- `RemoteUpdateManager` — 원격 카탈로그 확인 및 에셋 다운로드
- `AssetLoader` — Addressable 에셋 로드/언로드 래퍼

**워크플로우**:
```
1. 빌드 시: Addressable 그룹 설정 → 에셋 번들 빌드 → 서버 업로드
2. 런타임: 카탈로그 체크 → 변경분 다운로드 → 에셋 로드
```

**사용 예시**:
```csharp
using DDOIT.Tools.Addressables;

// 에셋 로드
var prefab = await AssetLoader.LoadAsync<GameObject>("ScenarioPrefab_Fire");

// 원격 업데이트 확인
bool hasUpdate = await RemoteUpdateManager.Instance.CheckForUpdates();
if (hasUpdate)
{
    await RemoteUpdateManager.Instance.DownloadUpdates();
}
```

---

### 4.5 VR (`DDOIT.Tools.VR` / `DDOIT.Tools.Player` / `DDOIT.Tools.Locomotion`)

**용도**: Meta Quest VR 공통 기능 + Player rig + Locomotion 시스템

#### 4.5.1 VR 유틸 (`DDOIT.Tools.VR`)

- `VRPlayerSetup` — VR 플레이어 리그 초기 설정
- `HandTrackingHelper` — 핸드트래킹 유틸

**관련 모듈**: `DDOIT.Tools.Player`, `DDOIT.Tools.Locomotion`
- `PlayerRig` — Meta XR 리그와 DDOIT 이동 보조 기능을 연결하는 플레이어 진입점
- `WalkingStickLocomotor` — Interaction SDK locomotion 흐름을 활용한 DDOIT 이동 보조 기능

#### 4.5.2 PlayerRig (`DDOIT.Tools.Player`)

ISDK `FirstPersonLocomotor` 기반 Player rig의 외부 진입점 (구 `PlayerController` 461 LoC를 v0.15.0에서 폐기하고 신규 도입).

UPM 패키지의 `package.json`은 `com.meta.xr.sdk.all@201.0.0`, `com.unity.inputsystem@1.18.0`, TextMeshPro, Addressables, UGUI를 직접 요구한다. 따라서 `DDOIT.Tools.Runtime`이 컴파일되기 전에 Meta/Oculus/Input System/UI 어셈블리가 준비되며, Meta/Interaction SDK 타입을 직접 참조하는 Locomotion 스크립트와 Input System을 사용하는 `PlayerRig` 디버그 입력도 첫 컴파일부터 정상 컴파일된다. `DDOIT Tools > Setup`은 Meta XR이 정확히 201.0.0인지 계속 검증한다.

**API**:
- `Singleton<PlayerRig>` — `PlayerRig.Instance` / `HasInstance`
- `HeadTransform` (read-only Transform) — `OVRCameraRig/TrackingSpace/CenterEyeAnchor` 노출. UI lookAt/follow 등에 사용
- `Teleport(Vector3 position)` / `Teleport(Vector3 position, Quaternion rotation)` — `_playerOrigin` (OVRCameraRig root) 위치 이동
- `ApplyDefaultControllerLocomotionProfile()` — 왼쪽 스틱 이동, 오른쪽 스틱 45도 스냅턴, comfort tunneling 비활성 표준값 적용
- `SetComfortTunnelingEnabled(bool enabled)` — `SmoothMovementTunneling`, `WallPenetrationTunneling` 활성 상태 일괄 변경
- `EnableWalkingStick()` / `DisableWalkingStick()` — `_walkingStickRoot.SetActive` 토글, `IsWalkingStickMode {get; private set}` 갱신
- `_enableDebugKeyboard` Inspector 토글 — 활성 시 스페이스바로 EnableWalkingStick/DisableWalkingStick 토글

**Inspector 슬롯 wiring** (DDOIT 씬에서):
- `_headTransform` ← `OVRCameraRig/TrackingSpace/CenterEyeAnchor`
- `_playerOrigin` ← `OVRCameraRig` root (Teleport 시 이동 대상)
- `_walkingStickRoot` ← `OVRCameraRig/OVRInteractionComprehensive/Locomotor/WalkingStickGroup` (단일 토글 컨테이너)
- `_leftControllerSlideInteractor` ← 왼쪽 `ControllerSlideInteractor` (기본 active)
- `_rightControllerTurnerInteractor` ← 오른쪽 `ControllerTurnerInteractor` (기본 active, Snap turn)
- `_leftControllerStepInteractor`, `_rightControllerStepInteractor` ← 기본 inactive
- `_leftTeleportControllerInteractor`, `_rightTeleportControllerInteractor` ← 기본 inactive
- `_smoothMovementTunneling`, `_wallPenetrationTunneling` ← 기본 inactive

**Scene Hierarchy** (DDOIT 씬 — Bootstrap 이중 씬 구조의 일부):
```
OVRCameraRig (root) + PlayerRig 컴포넌트
├ TrackingSpace/CenterEyeAnchor      ← _headTransform
└ OVRInteractionComprehensive
   ├ LeftInteractions/Interactors/Controller/LocomotionControllerInteractorGroup
   │  ├ ControllerSlideInteractor      ← 왼쪽 스틱 이동 active
   │  ├ ControllerTurnerInteractor     ← inactive
   │  ├ ControllerStepInteractor       ← inactive
   │  └ TeleportControllerInteractor   ← inactive
   ├ RightInteractions/Interactors/Controller/LocomotionControllerInteractorGroup
   │  ├ ControllerTurnerInteractor     ← 오른쪽 스틱 45도 Snap turn active
   │  ├ ControllerSlideInteractor      ← inactive
   │  ├ ControllerStepInteractor       ← inactive
   │  └ TeleportControllerInteractor   ← inactive
   └ Locomotor
      ├ PlayerController              ← FirstPersonLocomotor + IsGroundedActiveState
      │  layer = Water (self-cast 방지)
      │  ISDK CharacterController._layerMask = 7 (Default+TFX+IgnoreRaycast)
      │  speedFactor=30, groundDamping=40, acceleration=5, ...
      ├ SmoothMovementTunneling        ← 기본 inactive
      ├ WallPenetrationTunneling       ← 기본 inactive
      └ WalkingStickGroup             ← _walkingStickRoot
         ├ WalkingStickLocomotor      ← LocomotionEventsConnection→FirstPersonLocomotor
         ├ HandWalkingStick (L)
         └ HandWalkingStick (R)
```

**Critical 설정** (생략 시 ground 검출/이동 실패):
- `OVRManager._trackingOriginType = FloorLevel` (EyeLevel 시 OVRCameraRig가 HMD 위치로 jump → tracking-space head.y ≈ 0 → stick 길이 0)
- `OVRRightHandVisual` 하위 `OpenXRRightHand` 활성 (OculusHand_R 비활성) — 좌측 동일
- DDOIT 기본 컨트롤러 프로파일: 왼쪽 스틱 이동, 오른쪽 스틱 Snap turn, Step/Teleport/tunneling 비활성
- `FirstPersonLocomotor` velocity 이동은 grounded 상태에서만 속도가 붙는다. DDOIT 씬은 player/system 씬이므로 지면 collider를 포함하지 않는다. 실제 콘텐츠/체험 씬이 `Default` 등 Player CharacterController layer mask에 포함된 레이어의 바닥/지형 collider를 제공해야 한다.
- `SmoothMovementTunneling`, `WallPenetrationTunneling`은 기본 비활성이지만, 옵션으로 다시 켤 경우 `TunnelingEffect`의 eye/camera 슬롯이 유지되어야 함
- `EventSystem`은 DDOIT 씬에만 (환경 씬에 두지 않음)
- `DDOIT Tools > Setup > Optimize Project`는 위 PlayerRig/Locomotion 기본값을 다시 보정한다

#### 4.5.3 WalkingStick Locomotion (`DDOIT.Tools.Locomotion`)

Meta Interaction SDK Sample의 WalkingStick 시스템을 v0.15.0에서 포팅한 모듈. ISDK `FirstPersonLocomotor`의 LocomotionEvent 흐름을 사용 (Push=Relative, Slide=Velocity, Jump=Action).

**핵심 클래스** (Sample 1:1 미러, 알고리즘/상수/AnimationCurve 무변경):
- `WalkingStick` — 개별 막대. `OnEnable`에서 `_walkingStickLocomotor.RegisterStick(this)` 호출
- `WalkingStickLocomotor` — Push/Slide/Jump 판정 + LocomotionEvent broadcaster (669 LoC)
- `WalkingStickVisual` — TubeRenderer 기반 시각화 (5 TubePoint)
- `IsGroundedActiveState` — `CharacterController.IsGrounded`를 `IActiveState`로 노출 (Sample에는 별도 클래스, DDOIT 자체 작성)

**Prefab**:
- `Prefabs/Locomotion/WalkingStickLocomotor.prefab` — `WalkingStickLocomotor` + `LocomotionEventsConnection` (handlers는 씬 wiring)
- `Prefabs/Locomotion/HandWalkingStick.prefab` — Hand Tracking 전용 (Audio 자식 제거됨, Sample의 ControllerHandWalkingStick은 미포팅)

**LocomotionEvent 흐름**:
```
Hand → HandJoint → WalkingStick (개별)
   ↓ register
WalkingStickLocomotor (Push/Slide/Jump 판정)
   ↓ WhenLocomotionPerformed
LocomotionEventsConnection (handlers → FirstPersonLocomotor)
   ↓ HandleLocomotionEvent
FirstPersonLocomotor (damping/acceleration/gravity)
   ↓ ISDK CharacterController.Move
Player Transform 갱신
```

**StickLength 계산** (`WalkingStickLocomotor.CalculateStickLength`):
- 첫 stick `RegisterStick` 시점에 `pose.tracking.head.y * 0.68f` 1회 계산
- 즉 **EnableWalkingStick 호출 시점의 사용자 자세(HMD 높이)에 따라 stick 길이 자동 결정**
- 토글(Disable→Enable)할 때마다 새 길이 갱신 — 체험자 키 변화 대응

**WalkingStick 토글 운영 패턴**:
- 시나리오: `WalkingStickNode`에서 `_enable=true/false` 설정
- 코드: `PlayerRig.Instance.EnableWalkingStick()` / `.DisableWalkingStick()`
- 디버그: `_enableDebugKeyboard=true` 후 스페이스바 (Game window focus 필요)

> Meta XR SDK 상세 사용법은 `Meta_XR_SDK_Unity_Guide.md` 참조

---

### 4.6 Network (`DDOIT.Tools.Network`)

**용도**: 멀티플레이어 네트워크 공통 래퍼 (Photon Fusion 예정)

**핵심 클래스**:
- `NetworkManager` — 세션 생성/참여, 연결 관리

**상태**: Photon Fusion 2.0.9 도입 예정

---

### 4.7 UI (`DDOIT.Tools.UI`)

**용도**: VR 환경에서 정보 표시용 UI 패널을 풀링 방식으로 관리하는 시스템

#### 4.7.1 아키텍처

```
UIManager (Singleton)
├── Queue<UIPanel> pool
├── List<UIPanel> activePanels
└── UIPanel prefab
    ├── World Space Canvas
    ├── InSceneOverlayCanvasRenderer
    ├── Title / Context / ContextSub (TMP)
    ├── ImageA / ImageSub (Image)
    ├── VideoSurface (RawImage) + VideoPlayer
    ├── ButtonA / ButtonB
    └── SmoothFollowCanvas
```

- **UIManager**: `OpenUI(UIData, UITheme)`로 패널을 열고 `CloseUI(UIPanel)`로 닫는다. 초기화 전 호출이나 프리팹 누락은 `null`을 반환하고 오류 로그를 남긴다. `Initialize()` 중복 호출은 기존 Pool을 재생성하지 않고 무시한다. `CloseUI()`/`CloseAllUI()`는 초기화 전에도 안전하게 반환하며, 풀이 비면 기존 활성 패널을 빼앗지 않고 새 패널을 동적으로 생성한다.
- **UIPanel**: 하나의 패널 프리팹 안에 모든 UI 요소를 가지고 있다. `UIData`의 bool 플래그에 따라 필요한 요소만 켜고, 값이 비어 있는 텍스트/이미지/영상 요소는 런타임에서 숨긴다. 실제 표시 요소가 없는 TitleRow/ButtonRow는 부모 Row까지 꺼서 빈 레이아웃 공간을 남기지 않는다.
- **UINode**: 시나리오 흐름에서 `UIData`, `UITheme`, 배치 모드, 버튼 이벤트를 설정하는 노드다. 실제 표시할 콘텐츠가 하나도 없으면 런타임에서 패널을 열지 않고 오류를 기록한다.
- **SmoothFollowCanvas**: 비고정형 UI에서 CenterEyeAnchor 또는 `PlayerRig.HeadTransform`을 추적한다. Yaw(좌우 회전) 중심으로 따라가며 Pitch/Roll은 UI 안정성을 위해 직접 따라가지 않는다.
- **SceneManager 연동**: 콘텐츠 씬 전환이 시작되면 `SceneManager`가 활성 `UIPanel`을 모두 닫는다. 씬 전환 중 유지해야 하는 별도 로딩 UI는 일반 `UIPanel` 풀과 분리해서 설계해야 한다.

#### 4.7.2 UI 요소 플래그

현재 코드에는 `UIType` enum이 없다. UI 구성은 `UIData` 내부의 bool 플래그로 결정한다.

| 플래그 | 데이터 필드 | 런타임 동작 |
|---|---|---|
| `useTitle` | `title`, `titleIcon` | 제목 텍스트와 제목 아이콘 영역 사용 |
| `useContext` | `context` | 본문 텍스트 사용 |
| `useContextSub` | `contextSub` | 하단 본문 텍스트 사용 |
| `useImageA` | `image` | 첫 번째 이미지 사용 |
| `useImageSub` | `imageSub` | 두 번째 이미지 사용 |
| `useVideo` | `video` | 비디오 영역 사용 |
| `useButtonA` | `buttonLabelA`, `_onButtonA` | 버튼 A 표시 및 클릭 이벤트 연결 |
| `useButtonB` | `buttonLabelB`, `_onButtonB` | 버튼 B 표시 및 클릭 이벤트 연결 |

플래그가 켜져 있어도 실제 데이터가 비어 있으면 텍스트/이미지/영상 요소는 자동으로 숨겨진다. 예를 들어 `useImageA=true`인데 `image=null`이면 ImageA는 표시되지 않는다. 버튼은 예외다. `useButtonA=true`이면 `buttonLabelA`가 비어 있어도 버튼 오브젝트 자체는 표시된다. Title Icon은 `useTitle=true`일 때만 표시되며, TitleRow와 ButtonRow는 실제 표시 요소가 있을 때만 레이아웃에 남는다.

#### 4.7.3 UIData (콘텐츠 데이터)

```csharp
[System.Serializable]
public struct UIData
{
    public bool useTitle;
    public bool useContext;
    public bool useImageA;
    public bool useImageSub;
    public bool useVideo;
    public bool useButtonA;
    public bool useButtonB;
    public bool useContextSub;

    public string title;
    public Sprite titleIcon;
    public string context;
    public Sprite image;
    public Sprite imageSub;
    public VideoClip video;
    public string buttonLabelA;
    public string buttonLabelB;
    public string contextSub;

    public bool HasVisibleTitle =>
        useTitle && (!string.IsNullOrWhiteSpace(title) || titleIcon != null);
    public bool HasVisibleContext =>
        useContext && !string.IsNullOrWhiteSpace(context);
    public bool HasVisibleImageA => useImageA && image != null;
    public bool HasVisibleImageSub => useImageSub && imageSub != null;
    public bool HasVisibleVideo => useVideo && video != null;
    public bool HasVisibleButtonA => useButtonA;
    public bool HasVisibleButtonB => useButtonB;
    public bool HasVisibleContextSub =>
        useContextSub && !string.IsNullOrWhiteSpace(contextSub);
    public bool HasVisibleContent =>
        HasVisibleTitle || HasVisibleContext || HasVisibleImageA ||
        HasVisibleImageSub || HasVisibleVideo || HasVisibleButtonA ||
        HasVisibleButtonB || HasVisibleContextSub;
}
```

`UINode`에서는 별도 `_titleIcon` 필드가 Inspector에 노출된다. 런타임에서는 `UINode.OnInit()`이 `UIData`를 복사한 뒤 `data.titleIcon = _titleIcon`으로 주입한다. 제목 Bold 옵션이 켜져 있으면 제목 문자열을 `<b>...</b>`로 감싼 뒤 패널에 전달한다.

`HasVisibleContent`는 런타임에서 패널을 열어도 되는지 판단하는 공통 기준이다. Title은 제목 텍스트 또는 제목 아이콘 중 하나가 있어야 보이는 요소로 인정된다. Context/ContextSub는 공백이 아닌 문자열이 있어야 하며, Image/Video는 실제 Asset 참조가 있어야 한다. Button A/B는 라벨이 비어 있어도 버튼 자체가 표시되므로 보이는 요소로 인정하지만, 라벨 누락은 작성 경고로 남긴다.

#### 4.7.4 배치 모드

| 모드 | `_isFixed` | `UILookAtMode` | 설명 |
|---|---|---|
| **월드 고정** | true | `None` | UINode Transform의 위치/회전을 그대로 사용 |
| **월드 고정 + 1회 바라보기** | true | `LookOnce` | 패널을 열 때 플레이어를 한 번 바라본 뒤 고정 |
| **월드 고정 + 계속 바라보기** | true | `LookAlways` | 패널 위치는 고정하고 회전만 플레이어를 계속 추적 |
| **플레이어 추적** | false | 무시 | `SmoothFollowCanvas`가 CenterEyeAnchor 또는 HeadTransform을 따라감 |

플레이어 추적 모드에서는 패널을 열 때 `SmoothFollowCanvas` target을 먼저 비운 뒤 새 target을 찾는다. CenterEyeAnchor를 우선 사용하고, 없으면 `PlayerRig.HeadTransform`을 사용한다. 새 target을 찾으면 첫 위치/회전을 즉시 snap한 뒤 다음 프레임부터 보간한다. target을 찾지 못하면 이전 target을 재사용하지 않고 SmoothFollow를 비활성화하며 경고를 남긴다.

#### 4.7.5 UINode (시나리오 노드)

UINode는 시나리오 흐름에서 UI를 표시하는 ScenarioNode:

1. `OnInit()`에서 `UIManager` 존재 여부를 확인한다.
2. `UIData`를 복사하고 제목 Bold/Title Icon을 반영한다.
3. `UIManager.Instance.OpenUI(data, _theme)`로 패널을 연다.
4. `SetPlacement()`로 고정형/추적형 배치를 적용한다.
5. 버튼이 켜져 있으면 `UIPanel.OnButtonClicked`에 연결한다.
6. `OnRelease()` 또는 `OnDisable()`에서 열린 패널을 `UIManager.CloseUI()`로 반환한다.

`UINode`가 Step 조건 노드로 쓰이면 버튼 클릭 시 `SetConditionMet()`을 호출한다. 버튼 UnityEvent에는 일반 로직뿐 아니라 부모 Step의 `MarkConditionGroup1` ~ `MarkConditionGroup7`을 연결할 수 있어 분기형 UI를 만들 수 있다.

UINode 버튼은 기본적으로 **1회 선택**으로 동작한다. 한 번 클릭되면 UIPanel이 A/B 버튼을 즉시 비활성화하고, 같은 패널에서 추가 클릭 이벤트를 다시 발생시키지 않는다. 버튼별 UnityEvent 실행 중 Step이 종료되어 UINode가 Release되면 남은 공통 `_onEnd`와 자체 조건 충족 처리는 실행하지 않는다.

작성 실수를 줄이기 위해 `UINodeEditor`는 다음 경고를 표시한다.

- 활성화된 UI 요소가 하나도 없거나, 실제 표시할 콘텐츠가 없음
- 켜진 Title/텍스트/이미지/영상 요소에 실제 표시 값이 없음
- Title Icon이 지정되어 있지만 Title 요소가 꺼져 있음
- 버튼 라벨이 비어 있음. 단, 버튼 자체는 보이므로 빈 UI 차단 조건은 아님
- 예상 패널 높이가 `UIPanel` 기준 높이 1080px을 넘을 가능성. 긴 텍스트, 이미지 2개, 비디오, 버튼을 한 패널에 과밀하게 넣은 경우 작성 보조 경고를 표시함
- 버튼 이벤트가 없거나 Step 조건 완료 경로가 불명확함
- UINode의 조건 그룹과 버튼 이벤트의 `MarkConditionGroupN` 연결이 충돌할 가능성
- Theme이 `Default`일 때 실제 적용 기준을 안내하고, 기본 Theme을 찾지 못하면 경고함

초보 개발자는 `UINodeEditor`의 경고를 "즉시 고장"으로 해석하기보다 "실행하면 의도와 다르게 보일 가능성"으로 해석하면 된다. 예를 들어 `useImageA`가 켜져 있는데 `image`가 비어 있으면 런타임에서 ImageA 영역은 숨겨진다. 단, 전체 UIData 기준으로 실제 표시할 제목/본문/이미지/영상/버튼이 하나도 없으면 `UINode`는 패널을 열지 않는다. 버튼은 라벨이 비어 있어도 버튼 오브젝트 자체가 표시되므로 빈 UI로 보지 않는다. 대신 라벨이 비어 있으면 사용자가 버튼 의미를 알기 어려우므로 Inspector 경고가 표시된다.

#### 4.7.6 사용 예시

```csharp
// 코드에서 직접 UI 열기
var data = new UIData
{
    useTitle = true,
    useContext = true,
    useButtonA = true,
    title = "확인",
    context = "작업을 완료했습니까?",
    buttonLabelA = "예"
};

UIPanel panel = UIManager.Instance.OpenUI(data);

// 직접 OpenUI를 호출한 코드는 반환된 패널을 보관하고,
// 더 이상 필요 없을 때 반드시 CloseUI로 반환해야 한다.
UIManager.Instance.CloseUI(panel);
```

시나리오에서는 보통 직접 코드 호출보다 UINode를 Step 하위에 배치하고 Inspector에서 `UIData`, Theme, 배치, 버튼 이벤트를 설정한다.

#### 4.7.7 Theme와 Global Settings

- `UITheme`은 배경 상단/하단 색, edge 색, 텍스트 색을 정의한다.
- `UINode._theme`이 비어 있으면 `UIManager.DefaultTheme`이 적용된다.
- `UINodeEditor`의 Theme 드롭다운에서 `기본 (...)` 또는 `Default`는 `_theme = null` 상태를 뜻한다. 이 값은 "Theme 없음"이 아니라 "런타임에 UIManager 기본 Theme을 사용"한다는 의미다.
- 현재 씬에서 `UIManager.DefaultTheme`을 찾을 수 있으면 Inspector는 실제 적용될 Theme 이름을 안내한다.
- 현재 씬에 UIManager가 없으면 Inspector는 패키지 기본 Theme 에셋을 참고 이름으로 보여줄 수 있다. 단, 최종 런타임 Theme은 DDOIT 씬의 UIManager 설정이 결정한다.
- UIManager 기본 Theme과 기본 Theme 에셋을 모두 찾지 못하는 경우에만 Theme 적용 누락 위험으로 본다.
- 패널은 재사용 전에 프리팹 기본 색/머티리얼 상태로 되돌아간 뒤 새 Theme를 적용한다. Theme 배경 머티리얼 인스턴스는 패널마다 하나만 만들고 재사용하며, 패널 파괴 시 정리한다.
- `UIGlobalSettings`는 현재 런타임 레이아웃 권한자가 아니다. Tools Window에서 설정 값을 편집하고 필요 시 프리팹에 수동 적용하는 용도다.
- UPM 소비 프로젝트에서는 Editor 도구가 `Assets/Settings/DDOIT`의 프로젝트 로컬 설정을 우선 찾고, 없으면 `Packages/com.ddoit.tools/Data`의 패키지 기본값을 사용한다.

#### 4.7.8 UINode 작성 체크리스트

UINode를 새로 만들 때는 다음 순서로 확인하면 된다.

1. Step 하위에 UINode를 만든다.
2. 필요한 UI 요소 버튼만 켠다. 예: 제목과 본문만 필요하면 `Title`, `Context`만 켠다.
3. 켠 요소의 실제 데이터를 입력한다. 예: `Image`를 켰으면 Sprite를 넣고, `Video`를 켰으면 VideoClip을 넣는다.
4. 실제 표시할 콘텐츠가 하나 이상 있는지 확인한다. 제목은 텍스트나 제목 아이콘이 있어야 하고, 텍스트/이미지/영상 요소는 실제 값이 있어야 한다. 버튼은 켜져 있으면 표시 콘텐츠로 인정되지만, 라벨은 별도로 입력해야 한다.
5. Theme을 직접 고르지 않을 경우 `Default`로 둔다. 이 경우 DDOIT 씬의 UIManager 기본 Theme이 적용된다.
6. 긴 본문, 하단 본문, 이미지 2개, 비디오, 버튼을 한 UINode에 모두 넣으면 패널 높이가 과밀해질 수 있다. `UINodeEditor`의 높이 경고가 뜨면 Step을 나누거나 미디어 수를 줄인다.
7. 버튼을 켰다면 라벨을 입력한다. 버튼은 기본 1회 선택이므로 같은 패널에서 반복 클릭을 받아야 하는 용도로 사용하지 않는다.
8. 버튼 클릭으로 Step이 끝나야 하면 다음 중 하나를 명확히 설정한다.
   - UINode 자체를 조건 그룹에 넣어 어느 버튼이든 클릭 시 조건 충족
   - 버튼 이벤트에서 부모 Step의 `EndTrigger()` 호출
   - 버튼 이벤트에서 부모 Step의 `MarkConditionGroup1()` ~ `MarkConditionGroup7()` 호출
9. 분기형 UI라면 UINode 자체 조건 그룹과 버튼 이벤트 marker를 동시에 섞지 않는 편이 안전하다. 버튼 A/B가 서로 다른 분기로 가야 하면 UINode 조건 그룹은 `없음`으로 두고 버튼 이벤트에서 각각 `MarkConditionGroupN()`만 호출하는 구성이 가장 명확하다.

#### 4.7.9 Video

`UIPanel`은 `VideoClip`을 직접 `RawImage`에 넣지 않는다. 패널별 Runtime `RenderTexture`를 만들고 다음 흐름으로 연결한다.

```text
VideoClip -> VideoPlayer.targetTexture -> RenderTexture -> RawImage.texture
```

영상 크기를 알 수 있으면 `VideoClip.width/height` 기준으로 RenderTexture를 만들고, 알 수 없으면 1280x720을 사용한다. 너무 큰 영상은 가장 긴 변이 2048을 넘지 않도록 제한한다. 패널이 닫히거나 비활성화되면 VideoPlayer, RawImage texture, RenderTexture를 정리한다.

#### 4.7.10 렌더링과 입력

- `UIPanel.prefab`: World Space Canvas + `InSceneOverlayCanvasRenderer`
- Interaction: `PointableCanvas` + `PokeInteractable` + `RayInteractable`
- Poke와 Ray는 같은 `Surface/ClippedPlaneSurface`를 공유한다.
- Controller/hand ray는 `OVRInteractionComprehensive`의 ray interactor를 통해 UI 버튼을 선택할 수 있다.
- Runtime rendering: 원본 Canvas 계층은 Layer 30 (`DDOIT UI Render Source`)로 이동되고 내부 카메라가 RenderTexture로 렌더링한다.
- 표시용 Mesh는 Layer 0에 남고 `ZTest Always`, render queue 4500으로 월드 지오메트리에 잘리지 않게 표시된다.
- 손/컨트롤러/ray renderer는 런타임에서 UI mesh보다 위에 보이도록 보정된다.
- Editor prefab children: Layer 3 (`Overlay UI`)
- RectTransform.localScale: (0.0005, 0.0005, 0.0005)
- CanvasScaler.dynamicPixelsPerUnit: 10

#### 4.7.11 Addressables 미디어 정책

현재 `UIData`의 이미지/영상은 직접 `Sprite`/`VideoClip` 참조를 사용한다. 이 방식은 단순하고 안정적이므로 기본 정책으로 유지한다.

대형 이미지, 여러 씬에서 공유되는 영상, 원격 업데이트가 필요한 미디어가 실제 요구사항으로 커질 경우에만 선택적 Addressables 참조를 추가 검토한다. 기존 직접 참조 필드를 제거하는 전면 전환은 기존 UINode 직렬화 데이터와 비동기 수명 관리에 영향을 주므로 현재는 보류한다.

---

### 4.8 Data (`DDOIT.Tools.Data`)

**용도**: 설정, 구성 데이터를 ScriptableObject 기반으로 관리

**핵심 클래스**:
- `AppSettings` — 앱 전역 설정 데이터

---

### 4.9 Utilities (`DDOIT.Tools.Utilities`)

**용도**: 범용 유틸리티 (수학, 문자열, 확장 메서드 등)

**핵심 클래스**:
- `MathHelper` — 수학 관련 유틸
- `StringHelper` — 문자열 관련 유틸

---

### 4.10 Font System (다국어 폰트)

**용도**: 한국어, 영어, 베트남어, 일본어, 중국어(간체/번체)를 지원하는 TMP 폰트 시스템

#### 4.10.1 폰트 구성

| 폰트 | 파일 | 지원 언어 | 용도 |
|---|---|---|---|
| **Pretendard** | Regular / Bold | 한국어, 영어 | 메인 폰트 |
| **NotoSans** | Regular / Bold | 영어, 베트남어 | 베트남어 메인 |
| **NotoSansKR** | Regular / Bold | 한국어 | Fallback |
| **NotoSansJP** | Regular / Bold | 일본어 | Fallback |
| **NotoSansSC** | Regular / Bold | 중국어 간체 | Fallback |
| **NotoSansTC** | Regular / Bold | 중국어 번체 | Fallback |
| **Freesentation** | Light (300) / ExtraBold (800) | 한국어, 영어 | 보조 폰트 |

#### 4.10.2 Font Asset 방식

- **Dynamic SDF**: 런타임에 필요한 글리프만 생성. 빌드 크기 최소화, 대규모 문자셋(CJK)에 적합
- **Render Mode**: SDFAA (SDF Anti-Aliased)
- TMP Font Weight 슬롯으로 Regular/Bold 연결 가능 (Bold는 `<b>` 태그로 전환)

#### 4.10.3 Fallback 체인

TMP의 Fallback Font Asset 기능으로 다국어 지원:
```
Pretendard (메인)
├── NotoSansKR (한국어 보충)
├── NotoSansJP (일본어)
├── NotoSansSC (중국어 간체)
└── NotoSansTC (중국어 번체)
```

메인 폰트에 없는 글리프 → Fallback 리스트 순서대로 검색 → 자동 렌더링.

#### 4.10.4 파일 위치

```
Assets/DDOIT_Tools/Fonts/
├── Pretendard-Regular.ttf + SDF.asset
├── Pretendard-Bold.ttf + SDF.asset
├── NotoSans-Regular.ttf + SDF.asset
├── NotoSans-Bold.ttf + SDF.asset
├── NotoSansKR-Regular.ttf + SDF.asset
├── NotoSansKR-Bold.ttf + SDF.asset
├── NotoSansJP-Regular.ttf + SDF.asset
├── NotoSansJP-Bold.ttf + SDF.asset
├── NotoSansSC-Regular.ttf + SDF.asset  (간체)
├── NotoSansSC-Bold.ttf + SDF.asset
├── NotoSansTC-Regular.ttf + SDF.asset  (번체)
├── NotoSansTC-Bold.ttf + SDF.asset
├── Freesentation-3Light.ttf + SDF.asset
└── Freesentation-8ExtraBold.ttf + SDF.asset
```

---

### 4.11 DDOIT Tools Window (CEW)

**용도**: 에디터 작업을 가속하는 통합 윈도우.

**메뉴 경로**: `DDOIT Tools / Tools Window`

#### 4.11.1 탭 구성

| 탭 | 기능 |
|---|---|
| **Scene Setup** | Init Scene 자동 생성 (Stage / InitTr / GameManager / ScenarioManager + Scenario_01) |
| **UI Theme** | `UIGlobalSettings`/`UITheme` 에셋 편집 + UIPanel에 일괄 적용 |
| **Settings** | `DDOITSettings` SO 편집 (teleportFadeDuration 등) |

#### 4.11.2 DDOITSettings

전역 런타임 설정 ScriptableObject. `Resources` 또는 `AssetDatabase`에서 자동 탐색 후 `DDOITSettings.Instance`로 접근.

```csharp
public class DDOITSettings : ScriptableObject
{
    public float teleportFadeDuration = 1f;   // TeleportNode 페이드 총 소요 시간
}
```

에셋 경로: `Assets/DDOIT_Tools/DDOITSettings.asset` (Settings 탭에서 자동 생성)

---

## 5. 새 프로젝트 시작 가이드

### 5.1 방법 A: UPM 패키지 설치 (권장)
```
1. Unity에서 새 프로젝트 생성 (Unity 6, URP)
2. Package Manager > Add package from git URL
   https://github.com/DDOIT-OFFICIAL/DDOIT_Tools.git#v0.19.20
3. Unity 상단 메뉴에서 DDOIT Tools > Setup 실행
4. 필수 패키지 설치/업데이트 실행
5. Init Project 실행
6. Quest 프로젝트라면 Optimize Project 실행
7. 프로젝트 고유 폴더 생성: Assets/02. Scripts/{제품명}/
8. 네임스페이스: {제품명}.{카테고리} 사용
```

`com.ddoit.tools`의 `package.json`은 `com.meta.xr.sdk.all@201.0.0`, `com.unity.inputsystem@1.18.0`, TextMeshPro, Addressables, UGUI를 직접 요구한다. 따라서 UPM이 Meta/Oculus/Input System/UI 어셈블리를 먼저 준비하고 `DDOIT.Tools.Runtime`, Locomotion 스크립트, `PlayerRig` 디버그 입력은 첫 컴파일부터 정상적으로 컴파일된다. Setup은 Meta XR 버전을 정확히 201.0.0으로 계속 검증하며, Meta XR Movement SDK는 `https://github.com/oculus-samples/Unity-Movement.git#v201.0.0` tag pin을 요구한다. XR Management 4.5.3, OpenXR 1.16.1, Lottie Player는 Setup에서 설치/검증한다. 특히 OpenXR은 최초 설치 전에 Asset Import Worker를 일시 정지할 수 있도록 Setup 관리 대상으로 유지한다.

#### 5.1.1 Git UPM 패키지 업데이트

`DDOIT Tools > Setup` 창 상단의 `최신 릴리스 확인/업데이트` 버튼은 GitHub의 안정 릴리스 태그를 조회하고 현재 설치 버전보다 높은 버전이 있으면 업데이트를 제안한다. 확인 대화상자에서 승인하면 Unity Package Manager의 `Client.Add()`를 사용해 새 태그 URL로 교체한다. `packages-lock.json`을 직접 삭제하거나 Package Manager에 Git URL을 다시 입력할 필요가 없다.

- Git URL로 설치된 `com.ddoit.tools`에서만 실제 업데이트가 활성화된다.
- `Assets/DDOIT_Tools` 개발 원본 모드에서는 최신 릴리스 조회만 가능하다.
- 다른 패키지 설치 작업과 동시에 실행되지 않도록 Setup의 패키지 버튼이 잠긴다.
- 업데이트 중 스크립트 재컴파일과 도메인 리로드가 발생할 수 있다.

### 5.2 방법 B: 템플릿 복제
```
1. DDOIT_Template 프로젝트 폴더 복제
2. 프로젝트명 변경 (Unity Hub에서)
3. Assets/{프로젝트명}/ 폴더 생성하여 고유 코드 작성
4. 기존 DDOIT_Tools/ 폴더는 수정하지 않음 (업데이트 호환성 유지)
```

### 5.3 방법 C: unitypackage 임포트 (보조/레거시)
```
1. Unity에서 새 프로젝트 생성 (Unity 6, URP)
2. Assets > Import Package > DDOIT_Tools.unitypackage 임포트
3. DDOIT Tools > Setup 실행
4. 필수 패키지 설치/업데이트 실행
5. Init Project 및 Optimize Project 실행
```

### 5.4 개별 프로젝트 코드 작성 규칙
- 기본 코딩 규칙은 `AGENTS.md`/`CLAUDE.md`를 따름
- DDOIT 심화 규칙은 이 문서의 §3을 따름

---

## 6. Addressables 운영 가이드

### 6.1 배포 환경
```
[빌드 PC] → APK 설치 (오프라인)
         → 에셋 번들 업로드 (원격 서버)

[고객 VR 장비] → APK 실행 → 서버에서 업데이트 확인 → 변경분 다운로드
```

### 6.2 Addressable 그룹 설계 기준
| 그룹 | 내용 | 업데이트 빈도 |
|---|---|---|
| Local_Static | 앱 코어, 공통 UI | 거의 없음 (APK에 포함) |
| Remote_Scenarios | 교육 시나리오 프리팹/에셋 | 자주 (원격 업데이트) |
| Remote_Media | 영상, 음성, 이미지 | 자주 (원격 업데이트) |
| Remote_Config | 설정, 텍스트 데이터 | 필요 시 |

### 6.3 주의사항
- 원격 에셋 서버 URL은 `AppSettings`에서 관리
- 오프라인 환경 대비: 로컬 캐시 우선 사용, 네트워크 실패 시 폴백 처리
- 에셋 번들 빌드 시 버전 태깅 권장

---

## 7. 빌드 설정

### 7.1 Android (Meta Quest) 빌드 체크리스트
- [x] Build Target: Android
- [x] Min API Level: Android 12 (API 32)
- [x] Target API Level: 32
- [x] Scripting Backend: IL2CPP
- [x] Target Architecture: ARM64
- [x] XR Plugin: OpenXR + Meta Quest feature set
- [x] XR Plug-in Management: Android/Standalone OpenXR loader 활성
- [x] Texture Compression: ASTC
- [x] Graphics API: Vulkan (권장)

> Meta XR 관련 검증은 `Meta_XR_SDK_Unity_Guide.md`의 성능/검증/문제 해결 섹션을 참조

---

## 8. 버전 관리

### 8.1 Git 브랜치 전략
```
main                    ← 안정 버전
├── feature/name        ← 임시 기능 브랜치
├── fix/name            ← 임시 수정 브랜치
└── ...
```
작업 완료 후에는 `main`으로 병합하고 불필요한 임시 브랜치는 정리합니다.

### 8.2 DDOIT_Tools 버전
템플릿/패키지 버전은 **Semantic Versioning**을 따릅니다:
```
MAJOR.MINOR.PATCH
예: 1.0.0, 1.1.0, 1.1.1
```
- **MAJOR**: 호환성이 깨지는 변경
- **MINOR**: 하위 호환되는 기능 추가
- **PATCH**: 버그 수정

---

## 9. 참고 문서

| 문서 | 경로 (UPM / 개발자 모드) | 내용 |
|---|---|---|
| AI Agent / 코딩 규칙 | `AGENTS.md` / `CLAUDE.md` (프로젝트 루트) | 프로젝트 운영 원칙, 명명 규칙, 네임스페이스, 코드 구조화 |
| Meta XR SDK 가이드 | `Packages/com.ddoit.tools/MDs/Meta_XR_SDK_Unity_Guide.md` / `Assets/DDOIT_Tools/MDs/Meta_XR_SDK_Unity_Guide.md` | SDK 설치, 설정, 기능별 사용법 |
| 이 문서 | `Packages/com.ddoit.tools/MDs/DDOIT_Tools.md` / `Assets/DDOIT_Tools/MDs/DDOIT_Tools.md` | 템플릿 구조, 모듈별 가이드 |
| 패키지 배포용 AI Agent 문서 | `Packages/com.ddoit.tools/MDs/AGENTS.md` / `Packages/com.ddoit.tools/MDs/CLAUDE.md` | 소비 프로젝트 루트 자동 배포용 |

---

**문서 버전**: 0.4.0
**DDOIT_Tools 패키지 버전**: v0.19.20
**최종 업데이트**: 2026-07-22
