# Meta XR SDK for Unity - AI Agent Development Guide

> 목적: 이 문서는 DDOIT 프로젝트를 다루는 AI/CLI Agent가 Unity에서 Meta XR 기능을 빠르고 정확하게 구현하기 위한 실무 가이드다.
> 사람에게 SDK를 설명하는 튜토리얼이 아니라, Agent가 어떤 패키지/컴포넌트/API/검증 명령을 선택해야 하는지 알려주는 작업 지침이다.

기준 환경:

| 항목 | 현재 기준 |
|---|---:|
| Unity | 6000.3.7f1 |
| Meta XR All-in-One SDK | 201.0.0 |
| Core / Interaction / Interaction OVR / Platform / Haptics / MRUK | 201.0.0 |
| Audio / Voice | 85.0.0 |
| Movement SDK | 201.0.0 (`#v201.0.0` tag pin) |
| XR Management | 4.5.3 |
| OpenXR | 1.16.1 |
| Input System | 1.18.0 |
| Unity CLI Connector | 0.3.22 |

중요: Audio/Voice가 85.0.0인 것은 누락이 아니다. All-in-One 201.0.0의 실제 dependency 구성이 그렇다.

---

## 1. AI Agent 작업 원칙

### 1.1 정보 우선순위

Meta XR 관련 판단은 다음 순서로 한다.

1. 현재 프로젝트 파일
   - `Packages/manifest.json`
   - `Packages/packages-lock.json`
   - `ProjectSettings/ProjectVersion.txt`
   - `Assets/DDOIT_Tools/MDs/DDOIT_Tools.md`
2. 로컬 설치 패키지
   - `Library/PackageCache/com.meta.xr.sdk.*`
   - `Library/PackageCache/com.meta.xr.mrutilitykit@*`
3. Meta 공식 Unity 문서
   - `https://developers.meta.com/horizon/develop/unity`
4. Unity-Movement GitHub 저장소
   - `https://github.com/oculus-samples/Unity-Movement`

문서와 코드가 충돌하면 로컬 설치본을 먼저 믿는다. 단, release note의 known issue는 구현/테스트 판단에 반영한다.

### 1.2 Unity 씬 확인 규칙

씬/오브젝트 상태는 `.unity` YAML을 직접 파싱하지 말고 `unity-cli exec`로 확인한다.

```powershell
unity-cli status
unity-cli exec "return UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name;"
unity-cli exec "return string.Join(`"`n`", UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Select(g => g.name));"
```

YAML을 읽어야 하는 경우는 prefab/scene merge conflict 분석처럼 Unity Editor 접근이 불가능할 때로 제한한다.

### 1.3 PackageCache 수정 금지

`Library/PackageCache`는 Unity Package Manager 캐시다. 여기를 직접 수정하지 않는다.

수정이 필요하면 다음 중 하나를 선택한다.

- 프로젝트 코드에서 wrapper/adapter 작성
- `Packages/` 아래 embedded package로 전환
- forked Git package 사용

일반 기능 구현은 wrapper/adapter가 기본 선택이다.

### 1.4 샘플과 본체 구분

Meta XR 작업에서 다음을 반드시 구분한다.

- Meta XR SDK UPM 본체
- Meta 공식 sample repository
- Unity-Movement package sample
- DDOIT가 포팅한 sample-derived code

예: `WalkingStick`은 현재 DDOIT 코드에는 있지만, Meta Interaction SDK UPM 본체의 일반 Building Block이라고 쓰면 안 된다.

---

## 2. 빠른 버전/경로 확인

### 2.1 현재 package 확인

```powershell
Get-Content Packages\manifest.json

$lock = Get-Content Packages\packages-lock.json -Raw | ConvertFrom-Json
$lock.dependencies.'com.meta.xr.sdk.all'
$lock.dependencies.'com.meta.xr.sdk.movement'

Get-ChildItem -Directory Library\PackageCache |
  Where-Object { $_.Name -like 'com.meta*' -or $_.Name -like 'com.oculus*' } |
  Select-Object Name
```

### 2.2 현재 관측 PackageCache 경로

해시 suffix는 Unity가 package를 다시 resolve하면 바뀔 수 있다.

```text
Library/PackageCache/com.meta.xr.sdk.all@9edae415c95a
Library/PackageCache/com.meta.xr.sdk.core@9a2eddde53b7
Library/PackageCache/com.meta.xr.sdk.interaction@a03d9b382ceb
Library/PackageCache/com.meta.xr.sdk.interaction.ovr@63917e649e42
Library/PackageCache/com.meta.xr.mrutilitykit@f5cfbb0224ff
Library/PackageCache/com.meta.xr.sdk.platform@e8696127fbfb
Library/PackageCache/com.meta.xr.sdk.haptics@ab22382f01e2
Library/PackageCache/com.meta.xr.sdk.audio@baafb66e77db
Library/PackageCache/com.meta.xr.sdk.voice@f4180b2de811
Library/PackageCache/com.meta.xr.sdk.movement@2d2fe7d8ce45
```

경로가 없으면 다음으로 찾는다.

```powershell
Get-ChildItem -Directory Library\PackageCache | Where-Object { $_.Name -like 'com.meta.xr*' }
```

### 2.3 Movement SDK pinning

현재 manifest는 Unity-Movement v201.0.0 tag를 추적한다.

```json
"com.meta.xr.sdk.movement": "https://github.com/oculus-samples/Unity-Movement.git#v201.0.0"
```

현재 lock hash:

```text
2d2fe7d8ce451e69be0599eee2f597b5935f8e04
```

재현성을 위해 DDOIT_Template와 UPM 배포본 모두 아래 tag pin을 유지한다.

```json
"com.meta.xr.sdk.movement": "https://github.com/oculus-samples/Unity-Movement.git#v201.0.0"
```

이 값을 GitHub main URL로 되돌리지 않는다. Package Manager가 다시 resolve되면 cache suffix는 바뀔 수 있지만 lock hash와 manifest URL은 유지되어야 한다.

---

## 3. 로컬 패키지 소스맵

이 섹션은 AI Agent가 Meta XR 기능을 구현할 때 우선 열어볼 로컬 소스의 지도다. 문서 내용이 의심되거나 SDK 동작을 단정하기 어렵다면, 이 표의 패키지와 파일을 먼저 확인한다.

### 3.1 전수 판독 범위

2026-07-16 기준으로 `Library/PackageCache`의 Meta 관련 패키지 전체 `.cs` 파일을 판독 대상으로 삼았다. 판독은 모든 파일을 실제로 열어 파일 수, 라인 수, 타입 선언, 네임스페이스, `MonoBehaviour`, `ScriptableObject`, `MenuItem`, `CreateAssetMenu`, `Obsolete`, Building Block 관련 표면을 추출하는 방식으로 수행했다. 그 다음 구현 판단에 직접 쓰이는 핵심 런타임/에디터 파일은 별도로 수동 확인했다.

| PackageCache package | Version | `.cs` | Lines | Type decl. | Content hash |
|---|---:|---:|---:|---:|---|
| `com.meta.xr.mrutilitykit@f5cfbb0224ff` | 201.0.0 | 128 | 43,325 | 344 | `46526CADF3628F59` |
| `com.meta.xr.sdk.all@9edae415c95a` | 201.0.0 | 0 | 0 | 0 | - |
| `com.meta.xr.sdk.audio@baafb66e77db` | 85.0.0 | 34 | 12,640 | 131 | `972F7E57C9ED8BD7` |
| `com.meta.xr.sdk.core@9a2eddde53b7` | 201.0.0 | 866 | 212,971 | 2,864 | `B4DD82C6C01543FB` |
| `com.meta.xr.sdk.haptics@ab22382f01e2` | 201.0.0 | 12 | 2,340 | 23 | `3D7C3FE7C27E4FED` |
| `com.meta.xr.sdk.interaction.ovr@63917e649e42` | 201.0.0 | 61 | 7,194 | 96 | `2EB4F3C6EB0E566B` |
| `com.meta.xr.sdk.interaction@a03d9b382ceb` | 201.0.0 | 689 | 121,346 | 1,525 | `745999BEE0A528AE` |
| `com.meta.xr.sdk.movement@2d2fe7d8ce45` | 201.0.0 | 167 | 47,896 | 467 | `B689E6921EEF0822` |
| `com.meta.xr.sdk.platform@e8696127fbfb` | 201.0.0 | 207 | 21,673 | 427 | `50C0A066ADC9E523` |
| `com.meta.xr.sdk.voice@f4180b2de811` | 85.0.0 | 671 | 89,149 | 1,033 | `782FA7E1F187E287` |
| **Total** | - | **2,835** | **558,534** | **6,910** | - |

`com.meta.xr.sdk.all`은 wrapper package라 직접 C# 구현이 없다. 실제 구현은 Core, Interaction, MRUK, Platform, Haptics, Audio, Voice 패키지와 Git 기반 Movement 패키지에 있다.

### 3.2 패키지 표면 요약

| Package | Mono files | Editor files | SO files | Obsolete files | Menu files | Asset menu files | BB-related files |
|---|---:|---:|---:|---:|---:|---:|---:|
| MRUK | 38 | 23 | 7 | 11 | 1 | 1 | 14 |
| Audio | 10 | 15 | 8 | 1 | 2 | 2 | 0 |
| Core | 175 | 432 | 43 | 80 | 20 | 10 | 260 |
| Haptics | 4 | 2 | 1 | 0 | 0 | 0 | 0 |
| Interaction OVR | 28 | 25 | 0 | 7 | 1 | 0 | 17 |
| Interaction | 338 | 118 | 18 | 85 | 19 | 13 | 19 |
| Movement | 48 | 60 | 7 | 0 | 7 | 0 | 4 |
| Platform | 12 | 8 | 3 | 13 | 1 | 0 | 2 |
| Voice | 94 | 163 | 27 | 18 | 6 | 2 | 0 |

AI가 새 기능을 만들 때는 Mono files와 Building Block 관련 파일을 먼저 보고, 프로젝트 설정/자동 설치/메뉴 작업은 Editor files를 확인한다. Obsolete files가 많은 MRUK/Interaction/Core는 이름만 보고 API를 고르면 안 된다.

### 3.3 핵심 파일 확인 지점

| 구현 영역 | 먼저 열 파일 | 구현 판단 |
|---|---|---|
| 기본 Quest rig | `Scripts/OVRManager.cs`, `Scripts/OVRCameraRig.cs`, `CameraRigBBBlockData.cs`, `CameraRigSetupRules.cs` | `OVRCameraRig`는 tracking space와 eye/hand/controller anchors의 기준이다. Camera Rig Building Block은 없으면 rig를 만들고 `OVRManager.trackingOriginType`을 `FloorLevel`로 맞춘다. |
| Passthrough | `Scripts/OVRPassthroughLayer.cs`, `PassthroughBuildingBlockRules.cs` | `OVRManager.isInsightPassthroughEnabled`가 전제다. underlay는 카메라 clear background가 필요하다. surface projected/flexible layering 계열은 deprecated 표면이 많으므로 신규 설계의 기본값으로 삼지 않는다. |
| Depth/Occlusion | `Scripts/EnvironmentDepth/EnvironmentDepthManager.cs`, `OcclusionBlockData.cs` | `EnvironmentDepthManager.IsSupported`와 `IsDepthAvailable`을 확인한다. occlusion은 shader keyword와 material 적용이 필요하며, 지원 조건이 맞지 않으면 Building Block 설치가 취소된다. |
| MRUK scene data | `Core/Scripts/MRUK.cs`, `MRUKRoom.cs`, `MRUKAnchor.cs` | room/anchor/label 기반으로 공간 데이터를 다룬다. 단일 `FloorAnchor`/`CeilingAnchor`가 아니라 `FloorAnchors`/`CeilingAnchors` 리스트를 기준으로 작성한다. |
| Environment raycast | `Core/Scripts/EnvironmentRaycastManager.cs` | singleton 성격이다. 지원 여부를 확인하고, 필요할 때만 enable한다. Editor over Link에서는 Spatial Data over Meta Quest Link 설정이 필요할 수 있다. |
| Interaction input refs | `Runtime/Scripts/Input/Hands/HandRef.cs`, `Runtime/Scripts/Input/Controllers/ControllerRef.cs` | 두 Ref가 직접 `IActiveState`를 구현한다. 신규 wiring에서 `HandActiveState`/`ControllerActiveState`를 만들지 않는다. |
| Grab/hand grab | `GrabInteractable.cs`, `HandGrabInteractable.cs` | Rigidbody와 Collider 전제가 강하다. hand grab은 grab pose, grab rules, movement provider를 함께 본다. |
| Poke/ray UI | `PokeInteractable.cs`, `RayInteractable.cs` | `ISurfacePatch` 또는 `ISurface` 의존성이 핵심이다. UI/표면 상호작용은 surface 컴포넌트 누락을 먼저 의심한다. |
| Movement retargeting | `CharacterRetargeter.cs`, `MetaSourceDataProvider.cs`, `MSDKUtilityEditor.cs`, `InstallMovementBuildingBlock.cs` | Building Block 메뉴는 선택된 모델에 `CharacterRetargeter`와 `MetaSourceDataProvider`를 붙이고 config/metadata를 만든다. 모델이 humanoid rig인지 먼저 확인한다. |
| Networked movement | `NetworkCharacterRetargeter.cs`, Movement `Runtime/Native/Scripts/Networking` | 네트워크 retargeting은 별도 sync 계층이므로 Photon Fusion/NGO 선택을 먼저 확정한다. |
| Haptics | `Runtime/HapticSource.cs`, `HapticClip.cs`, `HapticClipPlayer.cs`, `Haptics.cs` | 신규 custom haptics는 Haptics SDK를 쓴다. `OVRHaptics` 직접 사용은 금지한다. |
| Audio/Voice/Platform | `MetaXRAudioSource.cs`, Voice SDK `Scripts/Runtime`, Platform `Scripts/Core.cs`, `Scripts/Request.cs` | Audio/Voice는 현재 85.0.0 표면이 기준이다. Platform은 `Core.Initialize`/`Core.AsyncInitialize` 흐름을 우선한다. |

### 3.4 AI가 활용할 주요 메뉴

Unity Editor 자동화나 사용자 안내가 필요할 때는 아래 메뉴 경로를 우선한다.

- Core: `Meta/About Meta XR SDK`, `Meta/Guides/*`, `Meta/Tools/Android Manifest Tool`, `Meta/Tools/OVR Build/*`, `Meta/Tools/Quest Runtime Optimizer`, `Window/Analysis/Adreno Offline Compiler Utility`
- Interaction: `Meta/Interaction SDK/* Recorder`, `Meta/Interaction SDK/OpenXR Migration Tool`, `Meta/Interaction SDK/Update Interaction SDK Plugin`, Quick Actions 계열
- Movement: `GameObject/Movement SDK/Body Tracking/Add Character Retargeter`, `GameObject/Movement SDK/Networking/Add Networked Character Retargeter`, `Assets/Movement SDK/Body Tracking/Open Retargeting Configuration Editor`, `Meta/Samples/Build Movement SDK Samples`
- MRUK: `Meta/Tools/MRUK Test Reports`
- Audio: `Meta/Audio/Acoustics/*`, `Meta/Audio/Tools/Update XR Audio Plugin`
- Voice: `Meta/Voice SDK/*`, `Assets/Create/Voice SDK/*`
- Platform: `Meta/Platform/Edit Settings`, `Export Tools/Export Horizon Platform Package`

### 3.5 Obsolete/Deprecated 우선 회피 목록

아래 항목은 로컬 소스에서 obsolete 또는 deprecated 표면으로 확인됐다. 신규 코드의 기본 선택지에서 제외한다.

- MRUK `SceneDecorator` 계열: Scene Decorator는 deprecated다.
- MRUK `SpaceMap`: `SpaceMapGPU` 흐름을 우선한다.
- MRUK `MRUKRoom.FloorAnchor`, `MRUKRoom.CeilingAnchor`: `FloorAnchors`, `CeilingAnchors`를 사용한다.
- MRUK `MRUKAnchor.AnchorLabels`, `HasPlane`, `HasVolume`, `IsLocal`: `Label`, `PlaneRect.HasValue`, `VolumeBounds.HasValue`, `HasValidHandle`을 사용한다.
- Interaction `HandActiveState`, `ControllerActiveState`: `HandRef`, `ControllerRef`를 `IActiveState`로 사용한다.
- Interaction `OneGrabFreeTransformer`, `TwoGrabFreeTransformer`: `GrabFreeTransformer`를 사용한다.
- Core `OVRPassthroughLayer` surface projected/flexible layering 관련 API: 신규 설계에서는 일반 background passthrough와 Depth/Occlusion 조합을 우선한다.
- Core `OVRHaptics`, `OVRHapticsClip`: Haptics SDK 또는 Interaction/System Haptics 흐름을 사용한다.
- Platform `StandalonePlatform`: Platform Settings의 standalone 사용 설정과 `Core.Initialize`/`Core.AsyncInitialize` 흐름을 사용한다.

### 3.6 소스 재확인 명령

새 Meta XR 기능을 구현하기 전, AI Agent는 아래처럼 로컬 설치본을 확인한다.

```powershell
$paths = (Get-ChildItem -Directory Library\PackageCache |
  Where-Object { $_.Name -like 'com.meta.xr*' -or $_.Name -like 'com.meta.xr.mrutilitykit*' }).FullName

rg --line-number --glob '*.cs' "class CharacterRetargeter|class HandGrabInteractable|class EnvironmentDepthManager" $paths
rg --line-number --glob '*.cs' "\[\s*(System\.)?Obsolete|MenuItem\(|CreateAssetMenu\(" $paths
```

특정 파일을 열 때는 인코딩을 명시한다.

```powershell
Get-Content -LiteralPath "Library\PackageCache\com.meta.xr.sdk.core@9a2eddde53b7\Scripts\OVRManager.cs" -Encoding UTF8 -TotalCount 200
```

---

## 4. 구현 시작 전 선택 가이드

### 4.1 필요한 SDK 선택

| 구현 목표 | 우선 사용할 패키지/기능 |
|---|---|
| 기본 Quest VR rig | Core SDK, `OVRCameraRig`, `OVRManager` |
| 손/컨트롤러 grab/poke/ray | Interaction SDK + Interaction OVR |
| teleport/snap turn/smooth locomotion | Interaction SDK `Oculus.Interaction.Locomotion` |
| DDOIT 지팡이 이동 | `DDOIT.Tools.Locomotion`, `PlayerRig` |
| passthrough | Core SDK `OVRPassthroughLayer` |
| depth/occlusion | Core SDK `Meta.XR.EnvironmentDepth` |
| room/anchor/scene-aware MR | MRUK |
| environment raycast/instant placement | MRUK `EnvironmentRaycastManager` |
| body retargeting | Movement SDK `CharacterRetargeter` |
| networked character retargeting | Movement SDK `NetworkCharacterRetargeter` + Photon Fusion/NGO |
| face expression | Core `OVRFaceExpressions`, Movement face samples |
| eye gaze | Core `OVREyeGaze` |
| spatial audio | Audio SDK |
| voice command/TTS/dictation | Voice SDK |
| custom haptics | Haptics SDK |
| achievements/IAP/leaderboard | Platform SDK |

### 4.2 Building Block부터 쓸지 직접 구성할지

기본 판단:

- 빠른 scene bootstrap, prototype, sample-like setup: Building Blocks 우선
- DDOIT 템플릿 구조에 편입할 shared feature: 직접 prefab/script 구성
- 반복 재사용되는 사내 기능: DDOIT_Tools wrapper 작성
- Meta package 내부 동작 변경 필요: wrapper/fork 검토

Building Blocks는 빠르지만, 생성되는 prefab과 dependency를 반드시 git diff로 확인한다.

---

## 5. Core SDK 작업 가이드

### 5.1 주요 컴포넌트

| 컴포넌트 | 용도 |
|---|---|
| `OVRManager` | Meta XR runtime 설정, permission, tracking, feature toggle |
| `OVRCameraRig` | Quest용 XR camera rig |
| `OVRPlugin` | native runtime bridge |
| `OVRPassthroughLayer` | passthrough layer |
| `OVRSceneManager` | Scene API integration |
| `OVRBody` | body tracking base source |
| `OVRFaceExpressions` | face expression weights |
| `OVREyeGaze` | eye gaze |
| `EnvironmentDepthManager` | depth/occlusion/raycast 기반 |

### 5.2 기본 rig 구성

기본 Quest scene에는 Main Camera 대신 `OVRCameraRig`를 둔다.

권장 구조:

```text
OVRCameraRig
├── TrackingSpace
│   ├── LeftEyeAnchor
│   ├── CenterEyeAnchor
│   ├── RightEyeAnchor
│   ├── LeftHandAnchor
│   └── RightHandAnchor
└── OVRManager
```

DDOIT 템플릿에서는 별도 Player wrapper가 있으면 `PlayerRig`와 충돌하지 않도록 루트/카메라 계층을 먼저 확인한다.

```powershell
unity-cli exec "return UnityEngine.Object.FindAnyObjectByType<OVRManager>() != null;"
unity-cli exec "return UnityEngine.Object.FindAnyObjectByType<OVRCameraRig>() != null;"
```

### 5.3 Project Setup

기본 설정은 Unity Editor 메뉴에서 확인한다.

```text
Meta > Tools > Project Setup Tool
```

권장 baseline:

- XR backend: OpenXR
- Color Space: Linear
- Graphics API: Vulkan
- Texture Compression: ASTC
- target platform: Android/Quest

OpenXR provider 옆 경고가 있으면 먼저 console과 Project Setup Tool 메시지를 확인한다. 단순 target API 경고는 package 내부 metadata일 수 있으므로 기능 오류로 단정하지 않는다.

### 5.4 Core known implementation notes

- `OVRHaptics` / `OVRHapticsClip`은 obsolete다. 신규 코드에서 직접 사용하지 않는다.
- `OVRPassthroughLayer.PassthroughLayerResumed` C# event는 obsolete다. 가능한 `passthroughLayerResumed` UnityEvent 또는 최신 흐름을 사용한다.
- Meta Quest Link에서는 passthrough resumed event가 device build와 다르게 보일 수 있다.
- `Meta > Tools > Quest Runtime Optimizer`는 Quest 성능 분석에 사용할 수 있다.

### 5.5 Core source 검색

```powershell
rg --line-number "class OVRManager|class OVRCameraRig|class OVRPassthroughLayer" "Library\PackageCache\com.meta.xr.sdk.core@*\Scripts"
rg --line-number "targetApiVersion|MetaXRFeature|XR_META" "Library\PackageCache\com.meta.xr.sdk.core@*\Scripts\OpenXRFeatures"
rg --line-number "Quest Runtime Optimizer|RuntimeOptimizer" "Library\PackageCache\com.meta.xr.sdk.core@*"
```

---

## 6. Interaction SDK 작업 가이드

### 6.1 Namespace

자주 쓰는 namespace:

```csharp
using Oculus.Interaction;
using Oculus.Interaction.Input;
using Oculus.Interaction.Locomotion;
```

### 6.2 기본 interaction 타입

| 목표 | 주요 타입 |
|---|---|
| grab | `GrabInteractor`, `GrabInteractable` |
| hand grab | `HandGrabInteractor`, `HandGrabInteractable` |
| distance grab | Distance Grab 관련 Quick Action/Interactor |
| poke | `PokeInteractor`, `PokeInteractable` |
| ray | `RayInteractor`, `RayInteractable` |
| UI | `PointableCanvasModule`, Poke/Ray Canvas Quick Action |
| teleport | `TeleportInteractor`, `TeleportInteractable` |
| locomotion | `FirstPersonLocomotor`, `LocomotionEventsConnection` |

### 6.3 Active State 선택

새 wiring에서는 아래를 우선한다.

- hand active 여부: `HandRef`
- controller active 여부: `ControllerRef`
- custom condition: `IActiveState` 구현체 작성

`HandRef`와 `ControllerRef`는 현재 설치본에서 `IActiveState`를 직접 구현한다.

```powershell
rg --line-number "class HandRef|class ControllerRef|IActiveState" "Library\PackageCache\com.meta.xr.sdk.interaction@*"
```

### 6.4 Grab 구현 패턴

일반적인 작업 순서:

1. scene에 Interaction rig 또는 OVRInteractionComprehensive 계열 prefab이 있는지 확인
2. target object에 collider와 rigidbody 필요 여부 결정
3. `Grabbable`/`GrabInteractable`/hand grab 관련 component 부착
4. hand/controller interactor와 layer/filter 확인
5. play mode 또는 device에서 grab event 확인

검증:

```powershell
unity-cli exec "return UnityEngine.Object.FindObjectsByType<Oculus.Interaction.GrabInteractable>(FindObjectsSortMode.None).Length;"
```

### 6.5 Poke/Ray UI 구현 패턴

World Space Canvas 기준:

1. Canvas를 World Space로 설정
2. Graphic Raycaster/EventSystem 상태 확인
3. Interaction SDK canvas module 또는 Quick Action 사용
4. button hit area와 collider/plane sizing 확인
5. 손 poke와 controller ray를 별도 테스트

Quick Action script 위치:

```text
Library/PackageCache/com.meta.xr.sdk.interaction@.../Editor/QuickActions/Scripts
```

### 6.6 Locomotion 구현 패턴

기본 이동은 `Oculus.Interaction.Locomotion` namespace를 따른다.

주요 타입:

- `FirstPersonLocomotor`
- `PlayerLocomotor`
- `LocomotionEventsConnection`
- `LocomotionEvent`
- `ILocomotionEventBroadcaster`
- `ILocomotionEventHandler`
- `TeleportInteractor`
- `TeleportInteractable`

커스텀 locomotion을 만들 때는 직접 transform을 이동시키기보다 `LocomotionEvent`를 발행해 `FirstPersonLocomotor`에 연결하는 패턴을 우선한다.

---

## 7. DDOIT PlayerRig / WalkingStick

### 7.1 PlayerRig

위치:

```text
Assets/DDOIT_Tools/Scripts/Player/PlayerRig.cs
```

역할:

- DDOIT player rig의 외부 진입점
- `PlayerRig.Instance`
- `EnableWalkingStick()`
- `DisableWalkingStick()`
- `IsWalkingStickMode`

### 7.2 WalkingStick

위치:

```text
Assets/DDOIT_Tools/Scripts/Locomotion/WalkingStick.cs
Assets/DDOIT_Tools/Scripts/Locomotion/WalkingStickLocomotor.cs
Assets/DDOIT_Tools/Scripts/Locomotion/WalkingStickVisual.cs
Assets/DDOIT_Tools/Scripts/Locomotion/IsGroundedActiveState.cs
```

prefab:

```text
Assets/DDOIT_Tools/Prefabs/Locomotion/WalkingStickLocomotor.prefab
Assets/DDOIT_Tools/Prefabs/Locomotion/HandWalkingStick.prefab
```

Scenario node:

```text
Assets/DDOIT_Tools/Scripts/Scenario/Nodes/WalkingStickNode.cs
```

### 7.3 동작 흐름

```text
Scenario
  -> WalkingStickNode
  -> PlayerRig.EnableWalkingStick()
  -> WalkingStickGroup active
  -> WalkingStickLocomotor
  -> LocomotionEventsConnection
  -> FirstPersonLocomotor
```

### 7.4 디버깅 체크

WalkingStick이 동작하지 않으면 순서대로 본다.

1. `PlayerRig` instance가 scene에 있는가
2. `_walkingStickRoot`가 올바른 GameObject를 가리키는가
3. `WalkingStickGroup` active 상태가 바뀌는가
4. left/right `HandWalkingStick`의 hand/joint reference가 연결되어 있는가
5. `WalkingStickLocomotor`가 `LocomotionEventsConnection`으로 event를 보내는가
6. `LocomotionEventsConnection` handler가 `FirstPersonLocomotor`를 가리키는가
7. damping/grounded 관련 UnityEvent wiring이 누락되지 않았는가

상세 문서는 `Assets/DDOIT_Tools/MDs/DDOIT_Tools.md` 4.5 절을 우선 참조한다.

---

## 8. Passthrough / Depth / MRUK 작업 가이드

### 8.1 Passthrough

주요 타입:

- `OVRPassthroughLayer`
- `OVRManager`
- `OVROverlay`
- `OVRPassthroughColorLut`

기본 구성:

1. scene에 `OVRManager`/`OVRCameraRig` 확인
2. passthrough 관련 project setting/permission 확인
3. camera clear flags/background 확인
4. `OVRPassthroughLayer` 추가
5. device build에서 실제 passthrough 확인

Link 테스트와 Quest device build 결과가 다를 수 있으므로 MR 기능은 실기기 테스트를 우선한다.

### 8.2 Environment Depth

namespace:

```csharp
using Meta.XR.EnvironmentDepth;
```

주요 타입:

- `EnvironmentDepthManager`
- `DepthProvider`
- `DepthProviderOpenXR`
- `OcclusionShadersMode`

새 occlusion 구현은 `EnvironmentDepthManager` component 기반으로 만든다.

### 8.3 MRUK

주요 타입:

- `MRUK`
- `MRUKRoom`
- `MRUKAnchor`
- `MRUKTrackable`
- `EnvironmentRaycastManager`

MRUK는 room/anchor/scene-aware MR 작업의 기본 선택이다.

### 8.4 EnvironmentRaycastManager

위치:

```text
Library/PackageCache/com.meta.xr.mrutilitykit@.../Core/Scripts/EnvironmentRaycastManager.cs
```

주요 API:

- `EnvironmentRaycastManager.IsSupported`
- `EnvironmentRaycastManager.Raycast(...)`
- `EnvironmentRaycastManager.PlaceBox(...)`
- `EnvironmentRaycastHit`
- `EnvironmentRaycastHitStatus`

사용 전 체크:

- target device가 environment raycast를 지원하는가
- scene permission이 있는가
- `EnvironmentDepthManager` fallback이 필요한가
- Editor/Link가 아니라 device build에서 확인했는가

### 8.5 MRUK Building Blocks

현재 설치본의 MRUK Building Blocks:

```text
AnchorPrefabSpawner
EffectMesh
FindSpawnPositions
InstantContentPlacement
MRUtilityKit
RoomGuardian
SceneDebugger
```

scene-aware 배치 작업은 `InstantContentPlacement`와 `EnvironmentRaycastManager`부터 검토한다.

---

## 9. Movement SDK 작업 가이드

### 9.1 역할

Movement SDK는 tracking data를 character rig에 적용하기 위한 패키지다.

대표 작업:

- body tracking retargeting
- face expression retargeting
- eye gaze integration
- AI motion synthesis
- networked character sync

### 9.2 샘플 폴더

```text
Library/PackageCache/com.meta.xr.sdk.movement@.../Samples~/FaceTrackingSamples
Library/PackageCache/com.meta.xr.sdk.movement@.../Samples~/BodyTrackingSamples
Library/PackageCache/com.meta.xr.sdk.movement@.../Samples~/AdvancedSamples
```

샘플은 직접 복사하기 전에 package version과 dependency를 확인한다.

### 9.3 Retargeting 주요 타입

```text
Runtime/Native/Scripts/Retargeting/CharacterRetargeter.cs
Runtime/Native/Scripts/Retargeting/CharacterRetargeterConfig.cs
Runtime/Native/Scripts/Retargeting/MetaSourceDataProvider.cs
Runtime/Native/Scripts/Networking/NetworkCharacterRetargeter.cs
Runtime/Native/Scripts/AIMotionSynthesizer/AIMotionSynthesizerSourceDataProvider.cs
```

주요 타입:

- `CharacterRetargeter`
- `CharacterRetargeterConfig`
- `SkeletonRetargeter`
- `MetaSourceDataProvider`
- `NetworkCharacterRetargeter`
- `AIMotionSynthesizerSourceDataProvider`
- `RootMotionMode`

### 9.4 Character Retargeter 작업 순서

일반적인 순서:

1. character prefab/humanoid rig 확인
2. Movement SDK sample 중 유사 scene 확인
3. Building Block 또는 editor utility로 `CharacterRetargeter` 구성
4. `MetaSourceDataProvider`가 `OVRBody` 기반 tracking source를 받는지 확인
5. retargeting config/mapping 생성
6. T-pose/debug skeleton으로 mapping 확인
7. Quest device에서 body tracking permission과 runtime 동작 확인

Editor automation이 필요하면 v201 기준으로 `MSDKUtilityEditor.AddCharacterRetargeter(GameObject)`와 `MSDKUtilityEditor.VerifyAndOpenRetargetingEditor(CharacterRetargeter)`를 검토한다. `RunDefaultRetargetingSetup(...)`는 v201 Movement SDK에 없으므로 사용하지 않는다.

```powershell
rg --line-number "AddCharacterRetargeter|VerifyAndOpenRetargetingEditor|CharacterRetargeter|MetaSourceDataProvider" "Library\PackageCache\com.meta.xr.sdk.movement@*"
```

### 9.5 Networked Character

네트워크 아바타/캐릭터는 직접 `Transform` 동기화로 시작하지 말고 `NetworkCharacterRetargeter`와 공식 networking sample/blocks를 먼저 본다.

지원 흐름:

- Photon Fusion Building Blocks
- Unity NGO Building Blocks
- custom network framework integration via Movement networking interfaces

Core package 위치:

```text
Library/PackageCache/com.meta.xr.sdk.core@.../Editor/BuildingBlocks/BlockData/MultiplayerBlocks/PhotonFusion
Library/PackageCache/com.meta.xr.sdk.core@.../Editor/BuildingBlocks/BlockData/MultiplayerBlocks/NGO
```

Photon Fusion 사용 시 확인:

- `com.exitgames.photonfusion` 설치 여부
- Fusion AppId
- Fusion AssembliesToWeave에 Meta block assembly 포함 여부
- DDOIT 프로젝트의 Fusion 버전 정책

### 9.6 Face / Eye

Core와 Movement를 함께 본다.

Core:

- `OVRFaceExpressions`
- `OVREyeGaze`

Movement:

- Face Tracking Samples
- retargeting/blendshape sample

Device capability를 먼저 확인한다. eye tracking은 모든 Quest device에서 가능한 기능이 아니다.

### 9.7 사용하지 말아야 할 과거 이름

현재 설치본 기준 신규 구현에서 아래 이름을 기반 설계로 잡지 않는다.

- `RetargetingAnimationConstraint`
- `IRetargetingProcessor`
- 직접 rig transform overwrite loop

우선 선택:

- `CharacterRetargeter`
- `SkeletonRetargeter`
- `MetaSourceDataProvider`
- `NetworkCharacterRetargeter`

---

## 10. Building Blocks 작업 가이드

### 10.1 Core BlockData

현재 Core package의 BlockData folder:

```text
AIBlocks
AlertViewHUD
CameraRig
ControllerButtonsMapper
ControllerTracking
Cube
EyeGaze
HandTracking
Haptics
Movement
MultiplayerBlocks
Occlusion
Passthrough
PassthroughCameraAccess
PassthroughCameraVisualizer
PassthroughWindow
RoomMesh
RoomModel
SampleSpatialAnchorController
Shared
SharedSpatialAnchorCore
SpatialAnchorCore
SpatialAudio
SurfaceProjectedPassthrough
VoiceBlocks
```

### 10.2 Building Block 사용 규칙

사용 전:

```powershell
Get-ChildItem "Library\PackageCache\com.meta.xr.sdk.core@*\Editor\BuildingBlocks\BlockData" -Directory
```

사용 후:

- 생성된 GameObject 이름 확인
- 추가된 prefab/scriptable object 확인
- Project Setup Tool 변경 확인
- git diff 확인
- scene 저장 여부 확인

### 10.3 Movement Block

Movement 관련 block entry:

```text
Editor/BuildingBlocks/BlockData/Movement/CharacterRetargeter
```

이 block은 Movement SDK package가 필요하다. Movement package가 resolve되지 않으면 block 사용을 중단하고 package 상태를 먼저 고친다.

---

## 11. Audio / Voice / Haptics / Platform

### 11.1 Audio

현재 버전: 85.0.0

사용 목표:

- spatial audio
- HRTF
- room acoustics
- ambisonics

Audio 관련 API는 local Audio 85 package 기준으로 확인한다.

### 11.2 Voice

현재 버전: 85.0.0

사용 목표:

- voice command
- dictation
- TTS
- Wit/Conduit sample

Voice 기능은 platform permission, network, service configuration이 얽힐 수 있으므로 sample scene과 official setup 문서를 함께 확인한다.

### 11.3 Haptics

현재 버전: 201.0.0

사용 목표:

- custom haptic clip
- Haptics Studio clip playback

신규 코드에서 `OVRHaptics`를 직접 사용하지 않는다. default interaction feedback은 Interaction SDK/System Haptics 흐름을 먼저 보고, media clip이 필요하면 Haptics SDK를 사용한다.

### 11.4 Platform

현재 버전: 201.0.0

사용 목표:

- achievements
- leaderboards
- IAP
- cloud storage
- group presence
- matchmaking

Platform 기능은 Dashboard 설정과 App ID가 필요하다. 코드만 작성해서 끝나는 기능으로 처리하지 않는다.

---

## 12. 성능/프로파일링

### 12.1 Quest Runtime Optimizer

메뉴:

```text
Meta > Tools > Quest Runtime Optimizer
```

source:

```text
Library/PackageCache/com.meta.xr.sdk.core@.../Editor/Utils/ToolingSupport/RuntimeOptimizer/RuntimeOptimizer.cs
```

사용 시점:

- Quest device frame time 분석
- CPU/GPU bottleneck 확인
- draw call/object cost 확인
- MR/Interaction scene 성능 점검

### 12.2 기본 성능 원칙

- Quest build는 device에서 확인한다.
- Editor play mode 성능만으로 판단하지 않는다.
- Passthrough/MRUK/Depth는 device capability와 permission을 같이 확인한다.
- Interaction object가 많으면 collider/layer/filter를 정리한다.
- per-frame `FindAnyObjectByType`, `FindObjectsByType`, `GetComponent` 반복 호출을 피한다.
- object pooling과 Addressables 정책은 `DDOIT_Tools.md`의 프로젝트 규칙을 따른다.

---

## 13. 구현 후 검증 체크리스트

### 13.1 공통

```powershell
unity-cli status
unity-cli console --type error --stacktrace none
```

필요 시 compile:

```powershell
unity-cli editor refresh --compile
```

### 13.2 Scene object 확인

```powershell
unity-cli exec "return UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name;"
unity-cli exec "return UnityEngine.Object.FindAnyObjectByType<OVRManager>() != null;"
unity-cli exec "return UnityEngine.Object.FindAnyObjectByType<OVRCameraRig>() != null;"
```

### 13.3 Interaction 확인

```powershell
unity-cli exec "return UnityEngine.Object.FindObjectsByType<Oculus.Interaction.GrabInteractable>(FindObjectsSortMode.None).Length;"
unity-cli exec "return UnityEngine.Object.FindObjectsByType<Oculus.Interaction.Locomotion.FirstPersonLocomotor>(FindObjectsSortMode.None).Length;"
```

### 13.4 MRUK 확인

```powershell
unity-cli exec "return UnityEngine.Object.FindAnyObjectByType<MRUK>() != null;"
unity-cli exec "return UnityEngine.Object.FindAnyObjectByType<EnvironmentRaycastManager>() != null;"
```

타입 namespace 문제가 나면 `--usings`를 사용하거나 fully qualified name을 확인한다.

### 13.5 DDOIT WalkingStick 확인

```powershell
unity-cli exec "return UnityEngine.Object.FindAnyObjectByType<DDOIT.Tools.Player.PlayerRig>() != null;"
unity-cli exec "return DDOIT.Tools.Player.PlayerRig.HasInstance;"
```

---

## 14. 문제 해결 빠른 표

| 증상 | 먼저 볼 것 |
|---|---|
| Meta type compile error | package resolve 상태, asmdef reference, namespace |
| OpenXR 관련 경고 | Project Setup Tool, OpenXR Feature, console |
| Passthrough 안 보임 | OVRManager, passthrough layer, camera clear, permission, device build |
| Depth/Occlusion 안 됨 | `EnvironmentDepthManager`, device support, permission |
| MRUK room 없음 | scene permission, room capture, MRUK prefab/source |
| Grab 안 됨 | collider, rigidbody, interactable/interactor, layer/filter |
| Ray/Poke UI 안 됨 | Canvas mode, event system, PointableCanvasModule, hit area, `RayInteractable`/`PokeInteractable` surface |
| UI가 손/레이를 가림 | DDOIT 기본 UI는 `OVROverlayCanvas`가 아니라 `InSceneOverlayCanvasRenderer`를 사용한다. `OVROverlayCanvas`가 켜져 있으면 compositor layer가 손/레이 위에 합성될 수 있으므로 `UIPanel.prefab`에서는 비활성 상태가 정상이다. |
| Locomotion 안 됨 | `FirstPersonLocomotor`, `LocomotionEventsConnection`, event broadcaster |
| WalkingStick 안 됨 | `PlayerRig`, `_walkingStickRoot`, hand refs, locomotion event wiring |
| Body retargeting 안 됨 | body tracking permission, `MetaSourceDataProvider`, mapping/T-pose |
| Networked character 안 됨 | Fusion/NGO dependency, network prefab registration, ownership |
| Haptics warning | obsolete `OVRHaptics` 직접 사용 여부 |

---

## 15. 금지/주의 목록

신규 작업에서 피할 것:

- `Library/PackageCache` 직접 수정
- `.unity` YAML만 보고 scene 상태 단정
- `OVRHaptics` 신규 사용
- `HandActiveState`/`ControllerActiveState` 신규 wiring
- `RetargetingAnimationConstraint` 기반 신규 설계
- Scene Decorator 기반 신규 MRUK 설계
- sample project 코드를 DDOIT namespace/asmdef 검토 없이 그대로 복사
- Quest device build 없이 MR/Depth/Movement 기능 완료 선언

---

## 16. 공식 자료

Meta:

- Meta Unity development: https://developers.meta.com/horizon/develop/unity
- SDK overview: https://developers.meta.com/horizon/documentation/unity/unity-sdks-overview/
- UPM packages: https://developers.meta.com/horizon/documentation/unity/unity-package-manager/
- All-in-One: https://developers.meta.com/horizon/downloads/package/meta-xr-sdk-all-in-one-upm/
- Core SDK: https://developers.meta.com/horizon/downloads/package/meta-xr-core-sdk/
- Interaction SDK: https://developers.meta.com/horizon/documentation/unity/unity-isdk-interaction-sdk-overview/
- MRUK: https://developers.meta.com/horizon/documentation/unity/unity-mr-utility-kit-overview/
- Haptics: https://developers.meta.com/horizon/downloads/package/meta-haptics-sdk-unity/

Movement:

- Movement overview: https://developers.meta.com/horizon/documentation/unity/move-overview/
- Movement getting started: https://developers.meta.com/horizon/documentation/unity/move-unity-getting-started/
- Movement building blocks: https://developers.meta.com/horizon/documentation/unity/move-building-blocks/
- Movement networking: https://developers.meta.com/horizon/documentation/unity/move-networking/
- Unity Movement GitHub: https://github.com/oculus-samples/Unity-Movement
- Unity Movement changelog: https://github.com/oculus-samples/Unity-Movement/blob/main/CHANGELOG.md

DDOIT:

- `Assets/DDOIT_Tools/MDs/DDOIT_Tools.md`
- `Assets/DDOIT_Tools/Scripts/Player/PlayerRig.cs`
- `Assets/DDOIT_Tools/Scripts/Locomotion/`
- `Assets/DDOIT_Tools/Scripts/Scenario/Nodes/WalkingStickNode.cs`
