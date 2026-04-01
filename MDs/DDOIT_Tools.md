# DDOIT Tools 가이드

> DDOIT VR 교육프로그램 통합 개발 템플릿 및 도구 모음

---

## 1. 개요

### 1.1 목적
DDOIT Tools는 VR 교육프로그램을 효율적으로 개발하기 위한 **공통 도구 모음 및 템플릿**입니다.

### 1.2 사용 방식
- **방법 A**: `DDOIT_Tools.unitypackage`를 기존 프로젝트에 임포트
- **방법 B**: `DDOIT_Template` 프로젝트를 복제하여 새 프로젝트 시작

### 1.3 기술 스택
| 항목 | 버전 |
|---|---|
| Unity | 6000.3.7f1 |
| Meta XR All-In-One SDK | 83.0.4 |
| Meta XR Movement SDK | 83.0.0 (GitHub) |
| XR Management | 4.5.4 |
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
│       └── Utilities/              ← 범용 헬퍼
├── {프로젝트명}/                    ← 개별 프로젝트 고유 코드
│   ├── Managers/
│   ├── Scenarios/
│   ├── UI/
│   └── ...
└── 01. Scenes/                     ← 프로젝트 콘텐츠 씬

DDOIT_Template/                      ← 프로젝트 루트
├── CLAUDE.md                        ← 코딩 규칙 (자동 참조)
└── Assets/
    └── DDOIT_Tools/
        └── MDs/                     ← 도구 문서
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

> 기본 코딩 규칙(`CLAUDE.md`)을 기반으로, DDOIT 프로젝트에 특화된 심화 규칙입니다.

### 3.1 Namespace 적용 규칙

`CLAUDE.md` §5의 일반 원칙을 DDOIT에 적용한 구체적인 규칙입니다.

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
    └── Step             ← Node 컨테이너. 조건 충족 시 자동 완료
        ├── SoundNode           ← 사운드 재생
        ├── TransformNode       ← 오브젝트 이동/회전
        ├── UINode              ← UI 패널 표시 (버튼 이벤트)
        ├── TriggerConditionNode ← 트리거 감지 (조건)
        └── TimerConditionNode   ← 시간 경과 (조건)
```

- **ScenarioManager**: `StartSequence()`로 Entry Scenario를 시작. 모든 Scenario를 초기화.
- **Scenario**: 하위 Step을 순차 실행. `_nextScenario`로 다음 시나리오 연결. 모든 Step 완료 시 `EndTrigger()`.
- **Step**: 하위 Node를 활성화하고 `Init()` 호출. `_isStepCondition`이 켜진 모든 Node가 충족되면 `EndTrigger()`. 조건 노드가 없으면 **대기** (외부에서 `EndTrigger()` 호출 필요).
- **ScenarioNode**: 모든 노드의 추상 베이스 클래스.

#### 4.1.2 실행 흐름

```
ScenarioManager.StartSequence()
  → Scenario.StartTrigger()
    → Step.StartTrigger()
      → node.Init() → OnInit()          ← 각 노드 초기화
      → (조건 노드 충족 대기)
      → 모든 조건 충족
      → node.Release() → OnRelease()    ← 각 노드 정리
      → _onRelease UnityEvent 발동
    → Step.EndTrigger()
      → _onEnd UnityEvent 발동
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

| 노드 | 용도 | 조건 노드 가능 |
|---|---|---|
| **SoundNode** | SoundDatabase의 사운드 재생 | O (재생 완료 시 충족) |
| **TransformNode** | 오브젝트 이동/회전 (Duration/Speed 모드) | O (이동+회전 완료 시 충족) |
| **UINode** | UIManager를 통한 UI 패널 표시 | O (버튼 클릭 시 충족, B1/B2 타입만) |
| **TriggerConditionNode** | 특정 태그 객체의 트리거 진입 감지 | O (전용 조건 노드) |
| **TimerConditionNode** | 지정 시간 경과 | O (전용 조건 노드) |

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
| **ScenarioManagerEditor** | 흐름 미리보기 (Scenario 체인 시각화), 시나리오 목록, 런타임 상태 표시 |
| **ScenarioEditor** | Step 목록, 자동 넘버링, 조건 노드 수/진행 표시 |
| **StepEditor** | 노드 목록, 조건 충족 상태 (✓/○), 노드 추가 버튼 |
| **TransformNodeEditor** | 이동/회전 설정 조건부 UI, 모드별 필드 표시 |
| **TriggerConditionNodeEditor** | 외부 Collider 설정, Collider 타입 버튼 (Box/Sphere/Capsule) |
| **UINodeEditor** | UIType별 조건부 필드, 버튼 이벤트 섹션, 빈 필드 자동 숨김 안내 |
| **SoundNodeEditor** | 사운드 이름 드롭다운, 오디오 미리듣기 (Play/Stop), 미선택 경고 |
| **TimerConditionNodeEditor** | 대기 시간 설정, 0 이하 경고 |

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
- `SceneFlowManager` — 씬 전환 흐름 제어
- `UIManager` — UI 패널 풀링 및 표시/숨김 관리 (§4.7 참조)

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

### 4.5 VR (`DDOIT.Tools.VR`)

**용도**: Meta Quest VR 공통 기능 래퍼

**핵심 클래스**:
- `VRPlayerSetup` — VR 플레이어 리그 초기 설정
- `HandTrackingHelper` — 핸드트래킹 유틸

**하위 모듈**: `DDOIT.Tools.VR.Movement`
- `MovementSDKHelper` — Meta Movement SDK 래퍼 (바디트래킹, 리타게팅)

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
UIManager (Singleton)        ← Queue<UIPanel> 풀 관리
├── UIPanel (Canvas)         ← 풀링 대상. World Space + OVROverlayCanvas
│   ├── Title (TMP)
│   ├── Context (TMP)
│   ├── ContextSub (TMP)
│   ├── Image / ImageSub
│   ├── Video (VideoPlayer)
│   └── ButtonA / ButtonB
└── SmoothFollowCanvas       ← 비고정형 UI의 카메라 추적
```

- **UIManager**: `OpenUI(UIData)` → 풀에서 UIPanel 꺼내 표시. `CloseUI(UIPanel)` → 풀 반환. 풀 고갈 시 가장 오래된 패널 강제 회수.
- **UIPanel**: 모든 UI 요소를 포함한 단일 Canvas. UIType에 따라 필요한 요소만 `SetActive(true)`. 빈 데이터(null Sprite, 빈 string)는 자동 숨김. 표시/숨김 시 EaseOutBack 스케일 애니메이션 재생.
- **SmoothFollowCanvas**: CenterEyeAnchor 기준 Yaw(좌우 회전)만 추적. Pitch/Roll 무시하여 VR 멀미 방지. 눈높이 유지.

#### 4.7.2 UIType (레이아웃 유형)

| UIType | 구성 | 설명 |
|---|---|---|
| T1 | Title | 제목만 |
| C1 | Context | 본문만 |
| T1C1 | Title + Context | 제목 + 본문 |
| T1C2 | Title + Context + ContextSub | 제목 + 본문 2개 |
| T1C1P1 | Title + Context + Image | 제목 + 본문 + 이미지 |
| T1C1P2 | Title + Context + Image + ImageSub | 제목 + 본문 + 이미지 2개 |
| T1C1V1 | Title + Context + Video | 제목 + 본문 + 비디오 |
| T1C1B1 | Title + Context + ButtonA | 제목 + 본문 + 버튼 1개 |
| T1C1B2 | Title + Context + ButtonA + ButtonB | 제목 + 본문 + 버튼 2개 |

> T=Title, C=Context, P=Picture, V=Video, B=Button. 빈 텍스트/이미지 필드는 자동으로 숨겨짐.

#### 4.7.3 UIData (콘텐츠 데이터)

```csharp
[System.Serializable]
public struct UIData
{
    public UIType type;
    public string title;
    public string context;
    public string contextSub;
    public Sprite image;
    public Sprite imageSub;
    public VideoClip video;
    public string buttonLabelA;
    public string buttonLabelB;
}
```

#### 4.7.4 배치 모드

| 모드 | isFixed | 설명 |
|---|---|---|
| **고정형** | true | 지정 Transform 위치에 고정. `lookAtPlayer=true` 시 플레이어를 바라봄 |
| **추적형** | false | SmoothFollowCanvas 활성화. CenterEyeAnchor 기준 Yaw만 추적 |

#### 4.7.5 UINode (시나리오 노드)

UINode는 시나리오 흐름에서 UI를 표시하는 ScenarioNode:
- `OnInit()`: UIManager.OpenUI → SetPlacement → 버튼 이벤트 연결
- `OnRelease()`: 패널 닫기
- B1/B2 타입에서 `IsStepCondition=true`이면 버튼 클릭 시 Step 조건 충족
- 버튼별 UnityEvent (`_onButtonA`, `_onButtonB`)로 커스텀 동작 할당 가능

#### 4.7.6 사용 예시

```csharp
// 코드에서 직접 UI 열기
var data = new UIData
{
    type = UIType.T1C1B1,
    title = "확인",
    context = "작업을 완료했습니까?",
    buttonLabelA = "예"
};
UIManager.Instance.OpenUI(data);
```

시나리오에서는 UINode를 Step 하위에 배치하고 인스펙터에서 UIData를 설정.

#### 4.7.7 프리팹 구성

- `UIPanel.prefab`: World Space Canvas + OVROverlayCanvas (Animated UI 프리셋)
- 모든 자식 오브젝트: Layer 3 (Overlay UI)
- RectTransform.localScale: (0.0005, 0.0005, 0.0005)
- CanvasScaler.dynamicPixelsPerUnit: 10

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

## 5. 새 프로젝트 시작 가이드

### 5.1 방법 A: unitypackage 임포트
```
1. Unity에서 새 프로젝트 생성 (Unity 6, URP)
2. Meta XR All-In-One SDK 설치
3. Assets > Import Package > DDOIT_Tools.unitypackage 임포트
4. 프로젝트 고유 폴더 생성: Assets/{프로젝트명}/
5. 네임스페이스: DDOIT.{프로젝트명}.{카테고리} 사용
```

### 5.2 방법 B: 템플릿 복제
```
1. DDOIT_Template 프로젝트 폴더 복제
2. 프로젝트명 변경 (Unity Hub에서)
3. Assets/{프로젝트명}/ 폴더 생성하여 고유 코드 작성
4. 기존 DDOIT_Tools/ 폴더는 수정하지 않음 (업데이트 호환성 유지)
```

### 5.3 개별 프로젝트 코드 작성 규칙
- 기본 코딩 규칙은 `CLAUDE.md`를 따름
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
- [x] Texture Compression: ASTC
- [x] Graphics API: Vulkan (권장)

> 상세 빌드 설정은 `Meta_XR_SDK_Unity_Guide.md` §14 참조

---

## 8. 버전 관리

### 8.1 Git 브랜치 전략
```
main                    ← 안정 버전
├── 0.1/kbs             ← 개발 브랜치 (담당자 이니셜)
├── 0.1/feature-name    ← 기능 브랜치
└── ...
```

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

| 문서 | 경로 | 내용 |
|---|---|---|
| 코딩 규칙 | `CLAUDE.md` (프로젝트 루트) | 명명 규칙, 네임스페이스, 코드 구조화 |
| Meta XR SDK 가이드 | `Assets/DDOIT_Tools/MDs/Meta_XR_SDK_Unity_Guide.md` | SDK 설치, 설정, 기능별 사용법 |
| 이 문서 | `Assets/DDOIT_Tools/MDs/DDOIT_Tools.md` | 템플릿 구조, 모듈별 가이드 |

---

**문서 버전**: 0.3.0
**최종 업데이트**: 2026-03-29
