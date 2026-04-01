# Meta XR SDK for Unity 완벽 가이드 (v83.0.4)

> 이 문서는 Meta Horizon 개발자 문서를 기반으로 Unity에서 Meta XR SDK를 사용하는 방법을 체계적으로 정리한 한국어 가이드입니다.
> **기준 SDK 버전**: Meta XR All-in-One SDK v83.0.4 / Movement SDK v83.0.0

---

## 목차

1. [개요](#1-개요)
2. [환경 설정](#2-환경-설정)
3. [Core SDK](#3-core-sdk)
4. [Interaction SDK](#4-interaction-sdk)
5. [Passthrough & Mixed Reality](#5-passthrough--mixed-reality)
6. [Movement SDK](#6-movement-sdk)
7. [Audio SDK](#7-audio-sdk)
8. [Avatars SDK](#8-avatars-sdk)
9. [Voice SDK](#9-voice-sdk)
10. [Platform SDK](#10-platform-sdk)
11. [Multiplayer & Networking](#11-multiplayer--networking)
12. [Building Blocks](#12-building-blocks)
13. [성능 최적화](#13-성능-최적화)
14. [앱 배포](#14-앱-배포)
15. [SDK 컴포넌트 레퍼런스](#15-sdk-컴포넌트-레퍼런스)

---

## 1. 개요

### 1.1 Meta XR SDK 소개

Meta XR SDK는 Meta Quest 디바이스용 VR/MR 애플리케이션을 개발하기 위한 Unity용 종합 소프트웨어 개발 키트입니다. 컨트롤러 및 핸드 트래킹, 패스스루, 공간 앵커 등 다양한 XR 기능을 제공합니다.

### 1.2 지원 디바이스

| 디바이스 | 특징 |
|---------|------|
| **Meta Quest 2** | 스탠드얼론 VR, 컨트롤러 기반 |
| **Meta Quest Pro** | 컬러 패스스루, 페이스/아이 트래킹 |
| **Meta Quest 3** | 고해상도 컬러 패스스루, Depth API, 향상된 MR |
| **Meta Quest 3S** | Quest 3의 경량 버전 |

### 1.3 SDK 패키지 종류

| SDK | 설명 |
|-----|------|
| **Meta XR Core SDK** | VR 개발의 핵심 기능 (OVRCameraRig, OVRManager 등) |
| **Meta XR All-in-One SDK** | 모든 SDK를 포함하는 래퍼 패키지 |
| **Meta XR Interaction SDK** | 컨트롤러/핸드 인터랙션 |
| **Meta XR Movement SDK** | 바디/페이스/아이 트래킹 |
| **Meta XR Audio SDK** | 공간 오디오 및 HRTF |
| **Meta Avatars SDK** | 아바타 시스템 |
| **Meta Voice SDK** | 음성 인식 및 명령 (Wit.ai 기반) |
| **Meta XR Platform SDK** | 업적, 리더보드, 멀티플레이어 |

### 1.4 참고 링크

- [SDK 개요](https://developers.meta.com/horizon/documentation/unity/unity-sdks-overview/)
- [Meta XR All-in-One SDK (Asset Store)](https://assetstore.unity.com/packages/tools/integration/meta-xr-all-in-one-sdk-269657)

---

## 2. 환경 설정

### 2.1 Unity 버전 요구사항

- **현재 프로젝트**: Unity 6000.3.7f1 (Unity 6)
- **권장 버전**: Unity 6 (6000.0.x 이상)
- **최소 버전**: Unity 2022.3.x LTS
- **중요**: v74+부터 OpenXR Plugin 기반으로 전환됨. **v83에서는 OpenXR Plugin 사용** (Oculus XR Plugin 미사용)

### 2.2 SDK 설치 방법

#### Asset Store를 통한 설치
1. Unity Asset Store에서 "Meta XR All-in-One SDK" 검색
2. "Add to My Assets" 클릭
3. Unity Package Manager에서 import

#### UPM (Unity Package Manager)을 통한 설치
1. `Window > Package Manager` 열기
2. `+` 버튼 클릭 → "Add package from git URL..."
3. 다음 URL 입력:
   - Core SDK: `com.meta.xr.sdk.core`
   - Interaction SDK: `com.meta.xr.sdk.interaction`
   - 기타 필요한 패키지

### 2.3 Project Setup Tool 사용법

SDK 설치 후 자동으로 Project Setup Tool이 나타납니다:

1. `Meta > Tools > Project Setup Tool` 메뉴 접근
2. "Fix All" 버튼 클릭하여 권장 설정 자동 적용
3. 주요 설정 항목:
   - Color Space: **Linear** (필수)
   - Graphics API: **Vulkan** (권장)
   - Texture Compression: **ASTC**
   - Minimum API Level: **Android 12 (API 32)** 이상

### 2.4 XR Plugin Management 설정

```
Edit > Project Settings > XR Plug-in Management
```

1. Android 탭에서 **OpenXR** 체크 후 **Meta Quest feature set** 활성화
2. PC (Standalone) 탭에서도 필요시 **OpenXR** 체크
3. OpenXR > Feature Groups에서 **Meta Quest** 기능 활성화
> **v83 기준**: Oculus XR Plugin 대신 OpenXR Plugin을 사용합니다. 프로젝트 내 `Assets/XR/Loaders/OpenXRLoader.asset` 참조.

### 2.5 참고 링크

- [XR Plugin Management](https://developers.meta.com/horizon/documentation/unity/unity-xr-plugin/)
- [Meta XR Core SDK 다운로드](https://developers.meta.com/horizon/downloads/package/meta-xr-core-sdk/)

---

## 3. Core SDK

### 3.1 OVRCameraRig 설정

OVRCameraRig는 Meta XR의 핵심 카메라 프리팹으로, VR 환경에서 사용자의 시점을 관리합니다.

#### 씬에 추가하기
1. Hierarchy에서 기존 Main Camera 삭제
2. Project 창에서 "OVRCameraRig" 검색
3. OVRCameraRig 프리팹을 씬에 드래그

#### 구조
```
OVRCameraRig
├── TrackingSpace
│   ├── LeftEyeAnchor
│   ├── CenterEyeAnchor (Main Camera)
│   ├── RightEyeAnchor
│   ├── LeftHandAnchor
│   ├── RightHandAnchor
│   └── TrackerAnchor
└── OVRManager (Component)
```

### 3.2 OVRManager 구성

OVRManager는 Meta Quest의 핵심 설정을 관리하는 컴포넌트입니다.

#### 주요 설정 (Inspector)

**Tracking Origin Type:**
- `Eye Level`: 눈 높이 기준
- `Floor Level`: 바닥 기준 (권장)
- `Stage`: 고정 스테이지 기준

**Quest Features > General 탭:**
```
- Hand Tracking Support: Controllers And Hands / Hands Only
- System Keyboard Support: Supported / Required
- Passthrough Support: Supported / Required
- Scene Support: Supported / Required
- Body Tracking Support: Supported / Required
- Face Tracking Support: Supported / Required
- Eye Tracking Support: Supported / Required
```

**Permission Requests On Startup:**
- Body Tracking, Face Tracking, Eye Tracking 등 필요한 권한 체크

### 3.3 기본 VR 씬 설정 코드 예시

```csharp
using UnityEngine;

public class VRSetup : MonoBehaviour
{
    void Start()
    {
        // 트래킹 원점 설정
        OVRManager.display.RecenterPose();

        // 프레임레이트 설정 (Quest 3: 72, 90, 120Hz 지원)
        OVRManager.display.displayFrequency = 90.0f;
    }
}
```

### 3.4 참고 링크

- [OVRCameraRig 설정](https://developers.meta.com/horizon/documentation/unity/unity-ovrcamerarig/)
- [Meta XR Core SDK 샘플](https://alpha.developers.meta.com/horizon/documentation/unity/unity-core-sdk-samples/)

---

## 4. Interaction SDK

### 4.1 개요

Interaction SDK는 손과 컨트롤러를 통한 상호작용을 구현하는 SDK입니다.

#### 핵심 개념

| 개념 | 설명 |
|------|------|
| **Interactor** | 상호작용을 시작하는 주체 (손, 컨트롤러에 부착) |
| **Interactable** | 상호작용 대상 오브젝트 |
| **Hover** | 상호작용 가능 상태 |
| **Select** | 실제 상호작용 실행 |

### 4.2 Interactor 종류

#### Grab Interactors
- `HandGrabInteractor`: 손으로 잡기
- `DistanceGrabInteractor`: 원거리 잡기

#### Poke Interactors
- `HandPokeInteractor`: 손으로 찌르기/누르기
- `ControllerPokeInteractor`: 컨트롤러로 찌르기

#### Ray Interactors
- `RayInteractor`: 레이 기반 선택

> 전체 컴포넌트 목록 및 상세 설명은 [15. SDK 컴포넌트 레퍼런스](#15-sdk-컴포넌트-레퍼런스) 참조

### 4.3 Interactable 종류

```csharp
// 오브젝트에 여러 Interactable 동시 적용 가능
[RequireComponent(typeof(Rigidbody))]
public class InteractableObject : MonoBehaviour
{
    // GrabInteractable: 잡기 가능
    // PokeInteractable: 찌르기 가능
    // RayInteractable: 레이로 선택 가능
}
```

### 4.4 Hand Grab Interaction 설정

1. 잡을 오브젝트에 `HandGrabInteractable` 컴포넌트 추가
2. `Rigidbody` 컴포넌트 필수
3. Grab Types 설정:
   - **Pinch**: 엄지+검지로 집기
   - **Palm**: 손바닥으로 잡기

```csharp
// HandGrabInteractable 설정 예시
public class GrabbableSetup : MonoBehaviour
{
    void Start()
    {
        var grabInteractable = GetComponent<HandGrabInteractable>();
        if (grabInteractable != null)
        {
            // Grab 타입 설정
            grabInteractable.SupportedGrabTypes = GrabTypeFlags.Pinch | GrabTypeFlags.Palm;
        }
    }
}
```

### 4.5 Poke Interaction 설정

UI 버튼이나 표면을 손가락으로 누르는 인터랙션:

1. 버튼 오브젝트에 `PokeInteractable` 컴포넌트 추가
2. `ISurface` 컴포넌트 필요 (ex: `PlaneSurface`)
3. 선택적으로 `PointableUnityEventWrapper`로 이벤트 연결

```csharp
using Oculus.Interaction;

public class PokeButton : MonoBehaviour
{
    private PokeInteractable _pokeInteractable;

    void Start()
    {
        _pokeInteractable = GetComponent<PokeInteractable>();
        _pokeInteractable.WhenPointerEventRaised += OnPokeEvent;
    }

    void OnPokeEvent(PointerEvent evt)
    {
        if (evt.Type == PointerEventType.Select)
        {
            Debug.Log("Button Pressed!");
        }
    }
}
```

### 4.6 Ray Interaction 설정

원거리 오브젝트 선택:

1. `RayInteractable` 컴포넌트 추가
2. `MoveFromTargetProvider` 추가하여 이동 동작 정의

### 4.7 Locomotion (이동)

#### Teleport (텔레포트)
```
1. TeleportInteractor를 손/컨트롤러에 추가
2. 바닥에 TeleportInteractable 추가
3. NavMeshSurface 설정 (선택사항)
```

#### Snap Turn (스냅 턴)
```csharp
// OVRManager에서 스냅턴 설정
// 또는 LocomotionController 사용
```

### 4.8 OVRInteraction 프리팹 사용

빠른 설정을 위해 `OVRInteraction` 프리팹을 사용할 수 있습니다:

```
OVRInteraction
├── OVRHands
│   ├── LeftHand
│   │   └── HandInteractorsLeft
│   │       ├── HandGrabInteractor
│   │       ├── HandPokeInteractor
│   │       └── HandRayInteractor
│   └── RightHand
│       └── HandInteractorsRight
└── OVRControllers
```

### 4.9 참고 링크

- [Interaction SDK 시작하기](https://developers.meta.com/horizon/documentation/unity/unity-isdk-getting-started/)
- [Poke Interaction](https://developers.meta.com/horizon/documentation/unity/unity-isdk-poke-interaction/)
- [Hand Grab Interaction](https://developers.meta.com/horizon/documentation/unity/unity-isdk-hand-grab-interaction/)

---

## 5. Passthrough & Mixed Reality

### 5.1 Passthrough 개요

Passthrough는 헤드셋 카메라를 통해 실제 환경을 볼 수 있게 하는 Mixed Reality 핵심 기능입니다.

| 디바이스 | Passthrough 특징 |
|---------|-----------------|
| Quest 2 | 흑백 Passthrough |
| Quest Pro | 컬러 Passthrough |
| Quest 3/3S | 고해상도 컬러 Passthrough |

### 5.2 Passthrough 기본 설정

#### Step 1: OVRManager 설정
```
OVRCameraRig > OVRManager > Quest Features > General
├── Passthrough Support: "Supported" 또는 "Required"
└── Insight Passthrough: Enable Passthrough 체크
```

#### Step 2: OVRPassthroughLayer 추가
```csharp
// 씬에 빈 GameObject 생성 후 OVRPassthroughLayer 추가
// 또는 코드로 설정
using UnityEngine;

public class PassthroughSetup : MonoBehaviour
{
    public OVRPassthroughLayer passthroughLayer;

    void Start()
    {
        // Passthrough 레이어 설정
        passthroughLayer.textureOpacity = 1.0f;
        passthroughLayer.edgeRenderingEnabled = false;
    }
}
```

#### Step 3: 카메라 배경 설정
```csharp
// CenterEyeAnchor 카메라의 Clear Flags를 Solid Color로 설정
// Background Color를 (0, 0, 0, 0)으로 설정 (완전 투명)
Camera.main.clearFlags = CameraClearFlags.SolidColor;
Camera.main.backgroundColor = new Color(0, 0, 0, 0);
```

### 5.3 Scene API (씬 이해)

Scene API는 사용자의 실제 환경을 이해하고 활용할 수 있게 합니다.

#### Space Setup 프로세스
1. 사용자가 Settings에서 공간 스캔 실행
2. 시스템이 바닥, 천장, 벽, 가구 등을 인식
3. Scene Model 생성 및 앱에서 사용 가능

#### Scene Model 구성 요소
```
Scene Model
├── Room Layout (천장, 벽, 바닥)
├── Scene Anchors (가구, 오브젝트)
├── Scene Mesh (3D 메시)
└── Semantic Labels (FLOOR, CEILING, WALL, TABLE, COUCH 등)
```

#### OVRManager Scene 설정
```
OVRManager > Quest Features > General
├── Scene Support: "Supported" 또는 "Required"
└── Anchor Support: "Enabled"
```

#### Scene 데이터 사용 예시
```csharp
using UnityEngine;
using Meta.XR.MRUtilityKit;

public class SceneSetup : MonoBehaviour
{
    void Start()
    {
        // MRUK를 통한 Scene 데이터 접근
        MRUK.Instance?.LoadSceneFromDevice();
    }

    public void OnSceneLoaded()
    {
        var room = MRUK.Instance.GetCurrentRoom();

        // 바닥 찾기
        var floor = room.GetKeyWall(MRUKAnchor.SceneLabels.FLOOR);

        // 테이블 위에 오브젝트 배치
        var tables = room.GetRoomAnchors(MRUKAnchor.SceneLabels.TABLE);
        foreach (var table in tables)
        {
            // 테이블 위치 사용
            Vector3 tablePosition = table.transform.position;
        }
    }
}
```

### 5.4 Mixed Reality Utility Kit (MRUK)

MRUK는 Scene API를 더 쉽게 사용할 수 있게 해주는 고수준 API입니다.

#### 설치
```
Asset Store에서 "Meta MR Utility Kit" 검색하여 설치
```

#### 기본 사용법
1. 씬에 `MRUK.prefab` 추가
2. `MRUKRoom` 컴포넌트로 방 데이터 접근
3. `FindSpawnPositions()`로 콘텐츠 배치 위치 찾기

```csharp
using Meta.XR.MRUtilityKit;

public class ContentPlacer : MonoBehaviour
{
    public GameObject prefabToSpawn;

    public void PlaceOnTable()
    {
        var room = MRUK.Instance.GetCurrentRoom();

        // 테이블 위에 스폰 위치 찾기
        var spawnPositions = room.FindSpawnPositions(
            new FindSpawnPositionsParameters
            {
                Labels = MRUKAnchor.SceneLabels.TABLE,
                SurfaceType = FindSpawnPositionsParameters.SurfaceTypes.FacingUp
            }
        );

        if (spawnPositions.Count > 0)
        {
            Instantiate(prefabToSpawn, spawnPositions[0].Position, spawnPositions[0].Rotation);
        }
    }
}
```

### 5.5 Depth API

Depth API는 Quest 3에서 실시간 깊이 정보를 제공하여 정확한 오클루전(가림)을 구현합니다.

#### 설정
```
OVRManager > Quest Features > General
└── Depth Submission: "Supported"
```

#### 오클루전 활성화
```csharp
// OVRManager에서 Depth API 활성화
OVRManager.instance.enableOcclusionMesh = true;
```

#### 지원 디바이스
- Meta Quest 3
- Meta Quest 3S

### 5.6 참고 링크

- [Passthrough 시작하기](https://developers.meta.com/horizon/documentation/unity/unity-passthrough-gs/)
- [Passthrough API 개요](https://developers.meta.com/horizon/documentation/unity/unity-passthrough/)
- [Scene Overview](https://developers.meta.com/horizon/documentation/unity/unity-scene-overview/)
- [Unity-Discover 샘플](https://github.com/oculus-samples/Unity-Discover)
- [Unity-Phanto 샘플](https://github.com/oculus-samples/Unity-Phanto)

---

## 6. Movement SDK

### 6.1 개요

Movement SDK는 사용자의 신체, 얼굴, 눈 움직임을 추적하여 아바타에 적용할 수 있게 합니다.

| 기능 | Quest 2 | Quest Pro | Quest 3 |
|------|---------|-----------|---------|
| Body Tracking | O | O | O |
| Face Tracking | - | O (카메라) | O (오디오 기반) |
| Eye Tracking | - | O | - |

### 6.2 요구사항

- Unity 6 (6000.0.x) 이상 권장, 최소 Unity 2022.3.x
- Meta XR SDK v74.0 이상 (현재 프로젝트: v83.0.4)
- Horizon OS v60.0 이상 (현재 프로젝트 타겟: v83)

### 6.3 설치

#### Git URL로 설치
```
Window > Package Manager > + > Add package from git URL
URL: https://github.com/oculus-samples/Unity-Movement.git
```

#### 특정 버전 설치
```
https://github.com/oculus-samples/Unity-Movement.git#v83.0.0
```
> 현재 프로젝트는 `manifest.json`에 `#v83.0.0`으로 설정되어 있습니다.

### 6.4 주요 컴포넌트

**핵심 컴포넌트**:
- `OVRBody` - Body Tracking 데이터 제공 (Upper Body / Full Body)
- `OVRUnityHumanoidSkeletonRetargeter` - Unity Humanoid 아바타에 Body Tracking 리타게팅 (Core SDK)
- `RetargetingAnimationConstraint` - Animation Rigging 기반 리타게팅 (Movement SDK)
- `RetargetingLayer` - 리타게팅 레이어 관리 (Movement SDK)
- `OVRFaceExpressions` - Face Tracking 데이터 (Quest Pro/3)
- `OVREyeGaze` - Eye Tracking 데이터 (Quest Pro)

> 전체 컴포넌트 목록 및 상세 설명은 [15. SDK 컴포넌트 레퍼런스](#15-sdk-컴포넌트-레퍼런스) 참조

### 6.5 OVRManager 설정

```
OVRManager > Quest Features > General
├── Body Tracking Support: "Supported" 또는 "Required"
├── Face Tracking Support: "Supported" 또는 "Required"
└── Eye Tracking Support: "Supported" 또는 "Required"

OVRManager > Permission Requests On Startup
├── Body Tracking: 체크
├── Face Tracking: 체크
└── Eye Tracking: 체크

OVRManager > Movement Tracking
├── Tracking Origin Type: "Floor Level"
└── Body Tracking Fidelity: "High"
```

### 6.6 Body Tracking

손/컨트롤러와 헤드셋 움직임을 기반으로 전신 포즈를 추론합니다.

#### Body Tracking Skeleton
```csharp
using Oculus.Movement.Tracking;

public class BodyTrackingSetup : MonoBehaviour
{
    public OVRSkeleton skeleton;

    void Update()
    {
        if (skeleton != null && skeleton.IsDataValid)
        {
            // 각 뼈대의 Transform 접근
            var bones = skeleton.Bones;
            foreach (var bone in bones)
            {
                // bone.Transform.position, rotation 사용
            }
        }
    }
}
```

### 6.7 Face Tracking

#### Quest Pro
- 내부 카메라를 사용한 정확한 표정 인식
- Visual to Expressions 방식

#### Quest 3/3S
- 마이크 오디오 기반 표정 추정
- Audio to Expressions 방식

#### FACS (Facial Action Coding System)
52개 이상의 표정 블렌드쉐이프를 지원합니다:
- `BrowLowererL/R`
- `CheekPuffL/R`
- `EyesClosedL/R`
- `JawOpen`
- `LipCornerPullerL/R`
- 등...

```csharp
using Oculus.Movement.FaceTracking;

public class FaceTrackingSetup : MonoBehaviour
{
    public OVRFaceExpressions faceExpressions;

    void Update()
    {
        if (faceExpressions != null && faceExpressions.FaceTrackingEnabled)
        {
            // 특정 표정 값 가져오기
            float jawOpen = faceExpressions.GetWeight(OVRFaceExpressions.FaceExpression.JawOpen);
            float smileLeft = faceExpressions.GetWeight(OVRFaceExpressions.FaceExpression.LipCornerPullerL);
        }
    }
}
```

### 6.8 Eye Tracking (Quest Pro 전용)

```csharp
using UnityEngine;

public class EyeTrackingSetup : MonoBehaviour
{
    public OVREyeGaze leftEyeGaze;
    public OVREyeGaze rightEyeGaze;

    void Update()
    {
        if (OVRPlugin.eyeTrackingEnabled)
        {
            // 눈의 방향 및 위치 사용
            Vector3 leftEyeDirection = leftEyeGaze.transform.forward;
            Vector3 rightEyeDirection = rightEyeGaze.transform.forward;
        }
    }
}
```

### 6.9 디바이스 설정 확인

사용자 디바이스에서 트래킹이 활성화되어 있는지 확인:
```
Settings > Movement Tracking
- Body Tracking: ON
- Face Tracking: ON
- Eye Tracking: ON (Quest Pro)
```

### 6.10 Body Tracking 리타게팅

Movement SDK는 OVRBody 트래킹 데이터를 Unity Humanoid 아바타에 적용할 수 있는 리타게팅 시스템을 제공합니다.

#### 6.10.1 Animation Rigging 기반 리타게팅

v83에서는 Unity Animation Rigging 시스템 기반 리타게팅 외에도, `CharacterRetargeter` 및 `NetworkCharacterRetargeter` Building Blocks를 통한 간편한 설정을 지원합니다.

**주요 컴포넌트:**
- `RetargetingLayer`: 리타게팅 레이어 관리
- `RetargetingAnimationConstraint`: Unity Humanoid 아바타에 Body Tracking 적용
- `OVRBody`: Body Tracking 데이터 제공
- `OVRSkeleton`: 스켈레톤 데이터 구조

#### 6.10.2 기본 설정 방법

**1. Unity Humanoid 아바타 준비**
```
1. 아바타 모델을 Humanoid로 설정 (Import Settings > Rig > Animation Type: Humanoid)
2. T-Pose 확인 및 본 매핑 검증
```

**2. OVRBody 컴포넌트 추가**
```csharp
// Body Tracking 데이터 제공
public class BodyTrackingSetup : MonoBehaviour
{
    public OVRBody body;
    public OVRSkeleton skeleton;

    void Start()
    {
        // Body Tracking 활성화 확인
        if (body != null && body.IsDataValid)
        {
            Debug.Log("[BodyTrackingSetup] Body Tracking 활성화됨");
        }
    }
}
```

**3. Animation Rigging 설정**
```
1. Package Manager에서 Animation Rigging 패키지 설치
2. 아바타에 Rig Builder 컴포넌트 추가
3. RetargetingAnimationConstraint를 Rig에 추가
4. OVRBody를 Source로, 아바타를 Target으로 설정
```

#### 6.10.3 코드 예시

```csharp
using Oculus.Movement.AnimationRigging;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace MyGame.Movement
{
    /// <summary>
    /// Body Tracking 리타게팅 설정
    /// </summary>
    public class BodyRetargetingSetup : MonoBehaviour
    {
        #region Serialized Fields
        [SerializeField]
        private OVRBody _ovrBody;

        [SerializeField]
        private Animator _targetAnimator;

        [SerializeField]
        private RigBuilder _rigBuilder;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            SetupRetargeting();
        }

        private void Update()
        {
            // Body Tracking 상태 확인
            if (_ovrBody != null && _ovrBody.IsDataValid)
            {
                // 트래킹 데이터 사용 가능
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 리타게팅 설정
        /// </summary>
        private void SetupRetargeting()
        {
            if (_ovrBody == null)
            {
                Debug.LogError("[BodyRetargetingSetup] OVRBody가 없습니다");
                return;
            }

            if (_targetAnimator == null)
            {
                Debug.LogError("[BodyRetargetingSetup] Target Animator가 없습니다");
                return;
            }

            // Humanoid 아바타 확인
            if (!_targetAnimator.isHuman)
            {
                Debug.LogError("[BodyRetargetingSetup] Target은 Humanoid 아바타여야 합니다");
                return;
            }

            Debug.Log("[BodyRetargetingSetup] 리타게팅 설정 완료");
        }
        #endregion
    }
}
```

#### 6.10.4 Legacy 리타게팅 시스템

v69에는 Legacy 리타게팅 컴포넌트도 포함되어 있습니다:

**사용 가능한 컴포넌트:**
- `RetargetedBoneTargets`: 기본 본 리타게팅
- `FullBodyRetargetedBoneTargets`: 전신 리타게팅
- `BlendHandConstraints`: 손 블렌딩
- `FullBodyHandDeformation`: 전신 + 손 변형
- `CustomMappings`: 커스텀 본 매핑
- `FullBodyCustomMappings`: 전신 커스텀 매핑

이들은 Animation Rigging 없이 직접 Transform을 조작합니다.

#### 6.10.5 문제 해결

**문제: Body Tracking이 작동하지 않음**
```
해결:
1. OVRManager에서 Body Tracking Support: "Supported" 확인
2. Permission Requests On Startup > Body Tracking 체크 확인
3. 디바이스 Settings > Movement Tracking > Body Tracking: ON 확인
```

**문제: 아바타가 T-Pose에 고정됨**
```
해결:
1. OVRBody.IsDataValid 확인
2. Humanoid 아바타 본 매핑 검증
3. Animation Rigging Constraint 설정 확인
```

**문제: 본 매핑이 부정확함**
```
해결:
1. 아바타를 T-Pose로 설정
2. Unity Humanoid 본 매핑 재확인
3. Constraint의 Source/Target 매핑 검증
```

### 6.11 참고 링크

- [Movement SDK 개요](https://developers.meta.com/horizon/documentation/unity/move-overview/)
- [Movement SDK 시작하기](https://developers.meta.com/horizon/documentation/unity/move-unity-getting-started/)
- [Face Tracking](https://developers.meta.com/horizon/documentation/unity/move-face-tracking/)
- [Eye Tracking](https://developers.meta.com/horizon/documentation/unity/move-eye-tracking/)
- [Unity-Movement GitHub](https://github.com/oculus-samples/Unity-Movement)

---

## 7. Audio SDK

### 7.1 개요

Meta XR Audio SDK는 HRTF 기반 공간 오디오와 룸 어쿠스틱 시뮬레이션을 제공합니다. 이전 Oculus Spatializer 플러그인의 후속 제품입니다.

### 7.2 주요 기능

| 기능 | 설명 |
|------|------|
| **HRTF Spatialization** | 머리 관련 전달 함수 기반 3D 오디오 |
| **Ambisonic Audio** | 전방향 오디오 재생 |
| **Room Acoustics** | 반사음 및 잔향 시뮬레이션 |
| **Acoustic Ray Tracing** | 고급 음향 레이 트레이싱 |

### 7.3 설치 및 설정

#### Unity Audio 설정
```
Edit > Project Settings > Audio
├── Spatializer Plugin: "Meta XR Audio"
└── Ambisonics Decoder Plugin: "Meta XR Audio"
```

### 7.4 HRTF 공간 오디오

HRTF(Head-Related Transfer Function)는 실제 귀의 특성을 모델링하여 정확한 3D 오디오 위치를 구현합니다.

```csharp
using UnityEngine;

public class SpatialAudioSetup : MonoBehaviour
{
    public AudioSource audioSource;

    void Start()
    {
        // Audio Source에 Meta XR Audio Source 컴포넌트 추가
        // Spatialize 옵션 활성화
        audioSource.spatialize = true;
        audioSource.spatialBlend = 1.0f; // 완전한 3D 오디오
    }
}
```

#### Meta XR Audio Source 컴포넌트
- **Gain Boost**: 볼륨 증폭
- **Enable Spatialization**: 공간화 활성화
- **Enable Acoustics**: 룸 어쿠스틱 적용
- **Reverb Send**: 잔향 레벨

### 7.5 Room Acoustics (룸 어쿠스틱)

실내 환경의 반사음과 잔향을 시뮬레이션합니다.

#### Shoebox Model
단순한 직육면체 형태의 방을 시뮬레이션:

```csharp
// Audio Mixer에 Room Acoustics 스크립트 추가
// Room 설정:
// - Room Dimensions: 방 크기 (width, height, depth)
// - Wall Materials: 벽 재질 (흡음 특성)
// - Clutter: 실내 오브젝트 밀도
```

#### 벽 재질 옵션
- AcousticsCarpet
- AcousticsConcrete
- AcousticsGlass
- AcousticsWood
- AcousticsPlaster
- 등...

### 7.6 Ambisonic 오디오

전방향 음장을 캡처하여 사용자 방향에 따라 재생:

```csharp
// AudioClip을 Ambisonic 형식으로 설정
// Import Settings에서 Ambisonic 체크
```

### 7.7 Acoustic Ray Tracing

Batman: Arkham Shadow에서 사용된 고급 음향 기술로, 실시간 레이 트레이싱으로 더 정확한 음향 시뮬레이션을 제공합니다.

### 7.8 플랫폼 지원

- Meta Quest 시리즈
- PC VR (SteamVR 등)
- 기타 독립형 VR 디바이스

### 7.9 참고 링크

- [Audio SDK 개요](https://developers.meta.com/horizon/documentation/unity/meta-xr-audio-sdk-unity/)
- [Audio SDK Unity Plugin](https://developers.meta.com/horizon/documentation/unity/meta-xr-audio-sdk-unity-intro/)
- [Room Acoustics](https://developers.meta.com/horizon/documentation/unity/meta-xr-audio-sdk-unity-room-acoustics/)
- [Audio SDK 다운로드](https://developers.meta.com/horizon/downloads/package/meta-xr-audio-sdk/)

---

## 8. Avatars SDK

### 8.1 개요

Meta Avatars SDK는 사용자의 Meta 아바타를 앱에 통합하여 소셜 프레즌스를 구현합니다.

### 8.2 요구사항

- Unity 6 (6000.0.x 이상) 권장, 최소 Unity 2022.3.x
- Meta XR Core SDK v83.0.4 이상

### 8.3 설치

1. Unity Asset Store에서 "Meta Avatars SDK" 검색
2. Package Manager로 import
3. `Meta > Avatars > Configuration` 메뉴에서 설정 확인

### 8.4 프로젝트 설정

```
Edit > Project Settings > XR Plug-in Management
└── OpenXR 체크 > Meta Quest feature set 활성화

Edit > Project Settings > Player > Other Settings
└── Color Space: Linear (필수)
```

### 8.5 아바타 로딩

#### User Avatar (사용자 아바타)
사용자가 Meta 계정으로 생성한 커스텀 아바타:

```csharp
using Oculus.Avatar2;
using Oculus.Platform;

public class UserAvatarLoader : MonoBehaviour
{
    public OvrAvatarEntity avatarEntity;

    void Start()
    {
        // Platform SDK 초기화 후
        Core.AsyncInitialize().OnComplete(OnPlatformInit);
    }

    void OnPlatformInit(Message<PlatformInitialize> msg)
    {
        if (msg.IsError) return;

        // Access Token 획득
        Users.GetAccessToken().OnComplete(OnAccessToken);
    }

    void OnAccessToken(Message<string> msg)
    {
        if (msg.IsError) return;

        // Avatar 토큰 설정
        OvrAvatarEntitlement.SetAccessToken(msg.Data);

        // 현재 사용자 ID 획득
        Users.GetLoggedInUser().OnComplete(OnLoggedInUser);
    }

    void OnLoggedInUser(Message<User> msg)
    {
        if (msg.IsError) return;

        // 아바타 로딩
        avatarEntity.SetUserAvatar(msg.Data.ID);
    }
}
```

#### Preset Avatar (프리셋 아바타)
SDK에 포함된 33개의 기본 아바타:

```csharp
// 프리셋 아바타 로딩 (오프라인/프로토타이핑용)
// 위치: Oculus/Avatar2_SampleAssets/SampleAssets
avatarEntity.LoadPresetAvatar(presetIndex);
```

### 8.6 아바타 품질 설정

| 품질 | 설명 | 용도 |
|------|------|------|
| **Light** | 저폴리곤, 빠른 렌더링 | 다수의 아바타, 성능 중시 |
| **Standard** | 고품질 비주얼 | 단일/소수 아바타, 품질 중시 |

```csharp
avatarEntity.SetLODQuality(AvatarLOD.Light);
// 또는
avatarEntity.SetLODQuality(AvatarLOD.Standard);
```

### 8.7 네트워크 아바타

Multiplayer Building Blocks를 사용한 네트워크 아바타 기능을 지원합니다. v83에서는 Movement SDK의 `NetworkCharacterRetargeter` Building Block을 통해 네트워크 아바타 동기화를 보다 쉽게 구현할 수 있습니다.

### 8.8 Avatars 1.0 지원 종료

- **종료일**: 2025년 4월 27일
- Avatars 2.0으로 마이그레이션 필요

### 8.9 참고 링크

- [Avatars SDK 개요](https://developers.meta.com/horizon/documentation/unity/meta-avatars-overview/)
- [아바타 로딩](https://developers.meta.com/horizon/documentation/unity/meta-avatars-load-avatars/)
- [프로젝트 설정](https://developers.meta.com/horizon/documentation/unity/meta-avatars-config-project/)
- [네트워킹](https://developers.meta.com/horizon/documentation/unity/meta-avatars-networking/)
- [Avatars SDK 다운로드](https://developers.meta.com/horizon/downloads/package/meta-avatars-sdk/)

---

## 9. Voice SDK

### 9.1 개요

Voice SDK는 Wit.ai NLU(자연어 이해) 서비스를 기반으로 음성 명령과 음성 인식을 구현합니다.

### 9.2 주요 기능

- **음성 명령**: 자연어로 앱 제어
- **실시간 전사**: 음성을 텍스트로 변환
- **다국어 지원**: 영어 + 12개 언어 프리뷰
- **크로스 플랫폼**: Quest 및 기타 AR/VR 디바이스

### 9.3 Wit.ai 개념

| 용어 | 설명 |
|------|------|
| **Utterance** | 사용자가 말한 문장 |
| **Intent** | 사용자의 의도 (예: "불 켜줘" → turn_on_light) |
| **Entity** | 문장 내 특정 값 (예: "빨간색" → color:red) |
| **Trait** | 문장의 특성 (예: 명령문, 질문문) |

### 9.4 Wit.ai 설정

1. [wit.ai](https://wit.ai)에 가입 (무료)
2. 새 앱 생성
3. Intent, Entity 정의 및 학습 데이터 입력
4. Server Access Token 복사

### 9.5 Unity 설정

```
1. Asset Store에서 "Meta Voice SDK" 설치
2. Meta > Voice SDK > Get Started 메뉴
3. Server Access Token 붙여넣기
4. Configuration 파일 생성 및 저장
```

### 9.6 AppVoiceExperience 사용

```csharp
using Meta.WitAi;
using Meta.WitAi.Json;

public class VoiceCommandHandler : MonoBehaviour
{
    public AppVoiceExperience voiceExperience;

    void Start()
    {
        voiceExperience.VoiceEvents.OnResponse.AddListener(OnVoiceResponse);
    }

    void OnVoiceResponse(WitResponseNode response)
    {
        // Intent 확인
        string intent = response.GetFirstIntentName();

        switch (intent)
        {
            case "turn_on_light":
                TurnOnLight();
                break;
            case "change_color":
                string color = response.GetFirstEntityValue("color");
                ChangeColor(color);
                break;
        }
    }

    public void StartListening()
    {
        voiceExperience.Activate();
    }

    void TurnOnLight() { /* 조명 켜기 로직 */ }
    void ChangeColor(string color) { /* 색상 변경 로직 */ }
}
```

### 9.7 Dictation vs Voice Commands

| 기능 | 용도 | 컴포넌트 |
|------|------|---------|
| **Voice Commands** | 앱 제어, 명령 실행 | AppVoiceExperience |
| **Dictation** | 텍스트 입력 | DictationService |

음성 명령에는 `AppVoiceExperience`가 더 정확한 인식률을 제공합니다.

### 9.8 학습 개선

```
Wit.ai Understanding 탭에서:
1. 실패한 utterance 로그 확인
2. 올바른 transcription 입력
3. 동의어 추가
4. Built-in intent/entity 활용
```

### 9.9 참고 링크

- [Voice SDK 개요](https://developers.meta.com/horizon/documentation/unity/voice-sdk-overview/)
- [기본 음성 입력 튜토리얼](https://developers.meta.com/horizon/documentation/unity/unity-tutorial-basic-voice-input/)
- [Voice SDK 모범 사례](https://developers.meta.com/horizon/documentation/unity/voice-sdk-improving-results/)
- [Voice SDK 다운로드](https://developers.meta.com/horizon/downloads/package/meta-voice-sdk/)

---

## 10. Platform SDK

### 10.1 개요

Platform SDK는 Meta Quest 플랫폼의 소셜 및 게임 서비스를 제공합니다.

> **참고**: 13세 미만 대상 앱은 Platform SDK 기능을 사용할 수 없습니다.

### 10.2 주요 기능

- 업적 (Achievements)
- 리더보드 (Leaderboards)
- 인앱 구매 (IAP)
- 앱 초대 (App Invites)
- DLC
- Destinations

### 10.3 업적 (Achievements)

#### 업적 유형

| 유형 | 설명 | 예시 |
|------|------|------|
| **Simple** | 단일 이벤트로 해제 | "첫 번째 적 처치" |
| **Count** | 카운터 목표 달성 | "적 100마리 처치" |
| **Bitfield** | 특정 비트 조합 달성 | "모든 레벨 클리어" |

#### 업적 구현

```csharp
using Oculus.Platform;
using Oculus.Platform.Models;

public class AchievementManager : MonoBehaviour
{
    void Start()
    {
        Core.AsyncInitialize();
    }

    // Simple 업적 해제
    public void UnlockAchievement(string achievementName)
    {
        Achievements.Unlock(achievementName).OnComplete(OnAchievementUnlock);
    }

    // Count 업적 진행
    public void AddCount(string achievementName, ulong count)
    {
        Achievements.AddCount(achievementName, count).OnComplete(OnAchievementUpdate);
    }

    // Bitfield 업적 업데이트
    public void AddFields(string achievementName, string fields)
    {
        Achievements.AddFields(achievementName, fields).OnComplete(OnAchievementUpdate);
    }

    void OnAchievementUnlock(Message<AchievementUpdate> msg)
    {
        if (!msg.IsError)
        {
            Debug.Log("Achievement unlocked!");
        }
    }

    void OnAchievementUpdate(Message<AchievementUpdate> msg)
    {
        if (!msg.IsError)
        {
            Debug.Log($"Achievement progress: {msg.Data.JustUnlocked}");
        }
    }
}
```

### 10.4 리더보드 (Leaderboards)

```csharp
using Oculus.Platform;
using Oculus.Platform.Models;

public class LeaderboardManager : MonoBehaviour
{
    // 점수 제출
    public void SubmitScore(string leaderboardName, long score)
    {
        Leaderboards.WriteEntry(leaderboardName, score).OnComplete(OnScoreSubmit);
    }

    // 점수 제출 (추가 데이터 포함)
    public void SubmitScoreWithData(string leaderboardName, long score, byte[] extraData)
    {
        Leaderboards.WriteEntryWithSupplementaryMetric(
            leaderboardName, score, 0, extraData, false
        ).OnComplete(OnScoreSubmit);
    }

    // 리더보드 조회
    public void GetLeaderboard(string leaderboardName, int count)
    {
        Leaderboards.GetEntries(
            leaderboardName,
            count,
            LeaderboardFilterType.None,
            LeaderboardStartAt.Top
        ).OnComplete(OnLeaderboardFetch);
    }

    void OnScoreSubmit(Message<bool> msg)
    {
        if (!msg.IsError)
        {
            Debug.Log("Score submitted!");
        }
    }

    void OnLeaderboardFetch(Message<LeaderboardEntryList> msg)
    {
        if (!msg.IsError)
        {
            foreach (var entry in msg.Data)
            {
                Debug.Log($"Rank {entry.Rank}: {entry.User.DisplayName} - {entry.Score}");
            }
        }
    }
}
```

### 10.5 인앱 구매 (IAP)

```csharp
using Oculus.Platform;
using Oculus.Platform.Models;

public class IAPManager : MonoBehaviour
{
    // 상품 목록 조회
    public void GetProducts(string[] skus)
    {
        IAP.GetProductsBySKU(skus).OnComplete(OnProductsFetch);
    }

    // 구매 시작
    public void Purchase(string sku)
    {
        IAP.LaunchCheckoutFlow(sku).OnComplete(OnPurchaseComplete);
    }

    // 구매 내역 확인
    public void GetPurchases()
    {
        IAP.GetViewerPurchases().OnComplete(OnPurchasesFetch);
    }

    void OnProductsFetch(Message<ProductList> msg)
    {
        if (!msg.IsError)
        {
            foreach (var product in msg.Data)
            {
                Debug.Log($"{product.Name}: {product.FormattedPrice}");
            }
        }
    }

    void OnPurchaseComplete(Message<Purchase> msg)
    {
        if (!msg.IsError)
        {
            Debug.Log($"Purchased: {msg.Data.Sku}");
        }
    }

    void OnPurchasesFetch(Message<PurchaseList> msg)
    {
        if (!msg.IsError)
        {
            foreach (var purchase in msg.Data)
            {
                Debug.Log($"Owned: {purchase.Sku}");
            }
        }
    }
}
```

### 10.6 Developer Dashboard 설정

업적, 리더보드, IAP는 Meta Horizon Developer Dashboard에서 미리 정의해야 합니다:

1. [developer.meta.com](https://developer.meta.com) 접속
2. 앱 선택 → Platform Services
3. 각 기능 설정 (Achievements, Leaderboards, In-App Purchases)

### 10.7 참고 링크

- [Platform SDK 다운로드](https://developers.meta.com/horizon/downloads/package/meta-xr-platform-sdk/)
- [Leaderboards](https://developers.meta.com/horizon/documentation/unity/ps-leaderboards/)
- [Achievements](https://developers.meta.com/horizon/documentation/unity/ps-achievements/)

---

## 11. Multiplayer & Networking

### 11.1 개요

Meta XR SDK는 Shared Spatial Anchors를 통해 로컬 멀티플레이어 경험을 지원합니다.

### 11.2 핵심 개념

| 개념 | 설명 |
|------|------|
| **Shared Spatial Anchors** | 여러 사용자가 공유하는 공간 앵커 |
| **Group Sharing** | 그룹 기반 앵커 공유 |

### 11.3 Shared Spatial Anchors 설정

#### OVRManager 설정
```
OVRCameraRig > OVRManager > Quest Features
├── Shared Spatial Anchors: Enabled
└── Passthrough Support: Supported
```

### 11.5 네트워킹 솔루션

Meta SDK는 네트워킹 자체를 제공하지 않으므로 서드파티 솔루션이 필요합니다:

| 솔루션 | 설명 |
|--------|------|
| **Photon Fusion** | 권장, 공식 샘플에서 사용 |
| **Unity NGO (Netcode for GameObjects)** | Unity의 공식 네트워킹 |
| **Mirror** | 오픈소스 대안 |

### 11.6 Multiplayer Building Blocks

#### Auto Matchmaking (v65+)
자동으로 연결된 플레이어를 같은 방에 매칭:

```
Photon Fusion: Shared Mode 사용
Unity NGO: Unity Game Services Relay 사용
```

> **v83 기준**: Multiplayer Building Blocks 및 `NetworkCharacterRetargeter`를 사용하여 네트워크 아바타 동기화를 구현할 수 있습니다.

### 11.7 Shared Spatial Anchors 구현 예시

```csharp
using Oculus.Platform;
using UnityEngine;

public class SharedAnchorManager : MonoBehaviour
{
    public OVRSpatialAnchor anchorPrefab;

    // 앵커 생성
    public void CreateAnchor(Vector3 position, Quaternion rotation)
    {
        GameObject anchorObject = new GameObject("SharedAnchor");
        anchorObject.transform.position = position;
        anchorObject.transform.rotation = rotation;

        var anchor = anchorObject.AddComponent<OVRSpatialAnchor>();
        anchor.OnLocalize += OnAnchorLocalized;
    }

    void OnAnchorLocalized(OVRSpatialAnchor anchor, bool success)
    {
        if (success)
        {
            // 앵커 저장 및 공유
            SaveAndShareAnchor(anchor);
        }
    }

    async void SaveAndShareAnchor(OVRSpatialAnchor anchor)
    {
        // 앵커 저장
        var saveResult = await anchor.SaveAsync();
        if (!saveResult) return;

        // 앵커 UUID를 네트워크로 공유
        System.Guid uuid = anchor.Uuid;
        // 네트워킹 코드로 uuid 전송...
    }

    // 공유된 앵커 로드
    public async void LoadSharedAnchor(System.Guid uuid)
    {
        var uuids = new System.Guid[] { uuid };
        var result = await OVRSpatialAnchor.LoadUnboundAnchorsAsync(uuids);

        foreach (var unboundAnchor in result)
        {
            // 앵커 바인딩
            GameObject anchorObject = Instantiate(anchorPrefab.gameObject);
            unboundAnchor.BindTo(anchorObject.GetComponent<OVRSpatialAnchor>());
        }
    }
}
```

### 11.8 샘플 프로젝트

- [Unity-SharedSpatialAnchors](https://github.com/oculus-samples/Unity-SharedSpatialAnchors)
- [Unity-LocalMultiplayerMR](https://github.com/oculus-samples/Unity-LocalMultiplayerMR)
- [Unity-Discover](https://github.com/oculus-samples/Unity-Discover)

### 11.9 참고 링크

- [Shared Spatial Anchors](https://developers.meta.com/horizon/documentation/unity/unity-shared-spatial-anchors/)
- [Multiplayer Building Blocks](https://developers.meta.com/horizon/documentation/unity/bb-multiplayer-blocks/)

---

## 12. Building Blocks

### 12.1 개요

Building Blocks는 Meta Quest 기능을 로우코드/노코드 방식으로 빠르게 구현할 수 있는 모듈식 컴포넌트입니다.

### 12.2 요구사항

- Meta XR Core SDK 필수

### 12.3 Building Blocks 사용법

```
Unity 메뉴: Meta > Tools > Building Blocks
```

1. Building Blocks 창 열기
2. 원하는 블록 선택
3. (+) 버튼으로 씬에 추가
4. 필요시 의존성 블록 자동 추가

### 12.4 주요 Building Blocks

#### Core Blocks
| 블록 | 설명 |
|------|------|
| **Camera Rig** | OVRCameraRig 설정 |
| **Controller Tracking** | 컨트롤러 트래킹 |
| **Hand Tracking** | 핸드 트래킹 |
| **Passthrough** | 패스스루 활성화 |

#### Interaction Blocks
| 블록 | 설명 |
|------|------|
| **Grab Interaction** | 잡기 인터랙션 |
| **Poke Interaction** | 찌르기 인터랙션 |
| **Ray Interaction** | 레이 인터랙션 |
| **Locomotion** | 이동 (텔레포트, 스냅턴) |

#### Multiplayer Blocks
| 블록 | 설명 |
|------|------|
| **Auto Matchmaking** | 자동 매칭 (v65+부터 지원) |

### 12.5 블록 의존성

일부 블록은 다른 블록에 의존합니다:
```
예: Hand Tracking → Camera Rig 필요
    Grab Interaction → Hand Tracking 또는 Controller Tracking 필요
```

Building Blocks 창에서 의존성 정보가 표시되며, 필요한 블록이 자동으로 추가됩니다.

### 12.6 참고 링크

- [Building Blocks 개요](https://developers.meta.com/horizon/documentation/unity/bb-overview/)
- [Multiplayer Building Blocks](https://developers.meta.com/horizon/documentation/unity/bb-multiplayer-blocks/)

---

## 13. 성능 최적화

### 13.1 프레임 예산

| 디바이스 | 주사율 | 프레임 예산 |
|---------|--------|------------|
| Quest 2 | 72/90/120Hz | 13.9/11.1/8.3ms |
| Quest 3 | 72/90/120Hz | 13.9/11.1/8.3ms |

VR에서는 프레임 드롭이 멀미를 유발하므로, 프레임 예산 준수가 필수입니다.

### 13.2 CPU/GPU 레벨

Horizon OS v57 이상에서 추가 CPU/GPU 레벨을 사용할 수 있습니다.

```csharp
using UnityEngine;

public class PerformanceSettings : MonoBehaviour
{
    void Start()
    {
        // CPU/GPU 레벨 설정 (0-5)
        OVRManager.cpuLevel = 3;
        OVRManager.gpuLevel = 3;

        // 프레임레이트 설정
        OVRManager.display.displayFrequency = 90.0f;
    }
}
```

> **참고**: GPU 레벨 5는 Quest 2/Pro에서 Dynamic Resolution 활성화 필요

### 13.3 프로파일링 도구

#### OVR Metrics Tool
앱의 GPU 바운드 여부 확인:
```
Meta Quest Developer Hub에서 설치 및 실행
```

#### Unity GPU Profiler
GPU 성능 분석:
```
Window > Analysis > Profiler
GPU 모듈 활성화
```

#### RenderDoc Meta Fork
저수준 GPU 프로파일링:
- Snapdragon 타일 렌더러 정보 제공
- 드로우콜별 타이밍 분석

#### Perfetto
시스템 전체 프로파일링:
```
Meta Quest Developer Hub > Performance > Perfetto
```

#### Simpleperf
CPU 프로파일링:
```
Android 개발 도구로 CPU 사용 시간 샘플링
```

### 13.4 Mixed Reality 성능 고려사항

```
MR 기능 사용 시 성능 영향:
- Passthrough: GPU -17%, CPU -14%
- Depth API: 추가 GPU 사용
- Scene API: 초기 로딩 시 CPU 사용
```

### 13.5 최적화 모범 사례

#### 렌더링
```
1. Draw Call 최소화 (배칭, GPU 인스턴싱)
2. 오버드로우 감소
3. 텍스처 압축 (ASTC)
4. LOD 사용
5. Dynamic Resolution 활성화
```

#### 스크립팅
```csharp
// 나쁜 예: 매 프레임 Find 호출
void Update()
{
    var obj = GameObject.Find("Target"); // 매우 느림
}

// 좋은 예: 캐싱
private GameObject _target;
void Start()
{
    _target = GameObject.Find("Target");
}
void Update()
{
    // _target 사용
}
```

#### 물리
```
1. 물리 오브젝트 수 최소화
2. 간단한 콜라이더 사용 (Box, Sphere > Mesh)
3. Fixed Timestep 적절히 설정
```

### 13.6 Thermal Throttling 방지

모바일 VR에서 열 관리는 필수입니다:

```csharp
using UnityEngine;

public class ThermalManager : MonoBehaviour
{
    void Update()
    {
        // 열 상태 확인
        if (OVRManager.isHmdPresent)
        {
            // 열 수준에 따른 품질 조정
            // 렌더링 품질 동적 조절 등
        }
    }
}
```

### 13.7 Quest Runtime Optimizer

실시간 분석 및 최적화 인사이트 제공:
- Meta Quest Developer Hub에서 사용

### 13.8 참고 링크

- [Testing and Performance Analysis](https://developers.meta.com/horizon/documentation/unity/unity-perf/)
- [CPU and GPU Levels](https://developers.meta.com/horizon/documentation/unity/os-cpu-gpu-levels/)
- [Unity GPU Profiler](https://developers.meta.com/horizon/blog/getting-started-w-the-unity-gpu-profiler-for-oculus-quest-and-go/)
- [Perfetto Guide](https://developers.meta.com/horizon/documentation/unity/ts-perfettoguide/)

---

## 14. 앱 배포

### 14.1 빌드 설정

#### 플랫폼 설정
```
File > Build Settings
├── Platform: Android
├── Texture Compression: ASTC
└── Build System: Gradle
```

#### Player Settings
```
Edit > Project Settings > Player > Android
├── Company Name: [회사명]
├── Product Name: [앱 이름]
├── Other Settings
│   ├── Package Name: com.company.appname
│   ├── Version: 1.0.0
│   ├── Bundle Version Code: 1 (매 빌드 증가)
│   ├── Minimum API Level: Android 12 (API 32)
│   ├── Target API Level: Android 12 (API 32)
│   ├── Scripting Backend: IL2CPP
│   └── Target Architectures: ARM64
└── Publishing Settings
    └── Keystore 설정 (릴리즈용)
```

### 14.2 APK 빌드

#### 일반 빌드
```
File > Build Settings > Build
```

#### OVR Build APK (빠른 빌드)
```
Meta > Tools > OVR Build APK
- Gradle 캐시 활용으로 10-50% 빌드 시간 단축
```

### 14.3 APK 요구사항

| 항목 | 요구사항 |
|------|---------|
| APK 크기 | 최대 1GB |
| Expansion 파일 | 최대 4GB (선택) |
| 빌드 타입 | Release (Debug 불가) |
| 서명 | Android 인증서 필수 |

### 14.4 AndroidManifest.xml 요구사항

```xml
<manifest>
    <!-- 필수 설정 -->
    <uses-sdk
        android:minSdkVersion="32"
        android:targetSdkVersion="32"
        android:compileSdkVersion="32" />

    <!-- 설치 위치 (SD 카드 지원) -->
    <application
        android:installLocation="auto"
        android:debuggable="false">

        <!-- VR 전용 앱 표시 -->
        <meta-data
            android:name="com.oculus.supportedDevices"
            android:value="quest|quest2|questpro|quest3" />
    </application>
</manifest>
```

> **중요**: `android:debuggable`은 반드시 `false`이거나 설정되지 않아야 합니다.

### 14.5 디바이스 배포 테스트

#### Meta Quest Developer Hub (MQDH)
```
1. MQDH 설치 및 실행
2. 디바이스 연결
3. APK 드래그 앤 드롭으로 설치
```

#### ADB 사용
```bash
adb install -r your_app.apk
```

### 14.6 Meta Horizon Store 제출

#### Platform Tool 사용
```
Meta > Tools > Oculus Platform Tool
├── Oculus Application ID: [Developer Dashboard에서 복사]
├── Oculus App Token: [API 페이지에서 복사]
└── Upload 버튼
```

#### 버전 코드 주의
```
매 업로드마다 Bundle Version Code를 증가시켜야 합니다.
Unity: Project Settings > Player > Bundle Version Code
자동 증가 옵션 활성화 권장
```

### 14.7 Virtual Reality Check (VRC)

앱 제출 전 VRC 요구사항을 확인하세요:

| VRC | 내용 |
|-----|------|
| VRC.Quest.Packaging.1 | Manifest 형식 준수 |
| VRC.Quest.Performance.1 | 프레임레이트 유지 |
| VRC.Quest.Input.1 | 컨트롤러/핸드 입력 지원 |
| VRC.Quest.Security.1 | 보안 요구사항 준수 |

### 14.8 제한사항

```
지원되지 않는 기능:
- Google Play Services (Firebase, GCM 등)
- 2D 폰 디스플레이 관련 기능
- 푸시 알림
- 외부 앱 인증
```

### 14.9 제출 프로세스

1. Developer Dashboard에서 앱 등록
2. 스토어 리스팅 정보 입력 (스크린샷, 설명, 카테고리)
3. APK 업로드
4. 리뷰 제출
5. Meta 검토 (보통 1-2주)
6. 승인 후 출시

### 14.10 참고 링크

- [Build Configuration Overview](https://developers.meta.com/horizon/documentation/unity/unity-build/)
- [Upload Apps to Horizon Store](https://developers.meta.com/horizon/documentation/unity/unity-platform-tool/)
- [Application Manifests](https://developers.meta.com/horizon/resources/publish-mobile-manifest/)
- [APKs and Expansion Files](https://developers.meta.com/horizon/resources/publish-apk/)
- [Submitting Your App](https://developers.meta.com/horizon/resources/publish-submit/)

---

## 15. SDK 컴포넌트 레퍼런스

### 15.1 핵심 컴포넌트 상세

이 섹션에서는 자주 사용되는 Meta XR SDK의 핵심 컴포넌트들을 상세히 설명합니다.

#### Core SDK - Movement

| 컴포넌트 | 위치 | 역할 | 주요 프로퍼티/메서드 |
|---------|------|------|-------------------|
| **OVRBody** | `Core/Scripts/Movement/` | Body Tracking 데이터 제공자 | `ProvidedSkeletonType`, `BodyState`, `GetSkeletonType()`, `GetSkeletonPoseData()` |
| **OVRSkeleton** | `Core/Scripts/Util/` | Skeleton 생성 및 애니메이션 적용 | `Bones`, `SkeletonType`, `IsDataValid`, `GetSkeletonPoseData()` |
| **OVRUnityHumanoidSkeletonRetargeter** | `Core/Scripts/Movement/` | Unity Humanoid 아바타에 Body Tracking 리타게팅 | `AnimatorTargetSkeleton`, `CustomBoneIdToHumanBodyBone` |
| **OVRFaceExpressions** | `Core/Scripts/` | Face Tracking 데이터 제공 (Quest Pro/3) | `FaceTrackingEnabled`, `GetWeight()`, 52개 표정 블렌드쉐이프 |
| **OVREyeGaze** | `Core/Scripts/Movement/` | Eye Tracking 데이터 제공 (Quest Pro) | `transform.forward` (시선 방향) |
| **OVRSkeletonMetadata** | `Core/Scripts/Movement/` | 리타게팅용 메타데이터 | Bone-to-Bone 페어링 정보 |
| **OVRHumanBodyBonesMappings** | `Core/Scripts/Movement/` | Body Tracking ↔ Humanoid 본 매핑 | Upper Body/Full Body 매핑 데이터 |

**사용 예시 - Body Tracking**:
```csharp
// GameObject 구조
GameObject (Humanoid Avatar)
├── Animator (Humanoid)
├── OVRBody (데이터 소스)
└── OVRUnityHumanoidSkeletonRetargeter (리타게터)
```

#### Core SDK - VR 기본

| 컴포넌트 | 위치 | 역할 | 주요 프로퍼티/메서드 |
|---------|------|------|-------------------|
| **OVRCameraRig** | `Core/Scripts/` | VR 카메라 시스템 | `TrackingSpace`, `LeftEyeAnchor`, `CenterEyeAnchor`, `RightEyeAnchor` |
| **OVRManager** | `Core/Scripts/` | Meta Quest 핵심 설정 관리 | `TrackingOriginType`, `cpuLevel`, `gpuLevel`, `display.displayFrequency` |
| **OVRInput** | `Core/Scripts/` | 컨트롤러 입력 처리 | `Get()`, `GetDown()`, `GetUp()`, `Controller` enum |
| **OVRPassthroughLayer** | `Core/Scripts/` | Passthrough MR 기능 | `textureOpacity`, `edgeRenderingEnabled` |

#### Movement SDK - Animation Rigging Retargeting

| 컴포넌트 | 위치 | 역할 | 주요 프로퍼티/메서드 |
|---------|------|------|-------------------|
| **RetargetingLayer** | `Movement/Runtime/Scripts/AnimationRigging/` | 리타게팅 레이어 관리 | `SkeletonType`, `ProcessSkeleton()`, `GetCustomBoneIdToJointPair()` |
| **RetargetingAnimationConstraint** | `Movement/Runtime/Scripts/AnimationRigging/` | Unity Humanoid 아바타에 Body Tracking 적용 (Constraint 기반) | `OVRSkeletonData`, `RetargetingLayer`, Animation Rigging Constraint 인터페이스 구현 |
| **RetargetingAnimationJob** | `Movement/Runtime/Scripts/AnimationRigging/` | Job System 기반 리타게팅 처리 | IAnimationJob 구현 |
| **IRetargetingProcessor** | `Movement/Runtime/Scripts/AnimationRigging/` | 리타게팅 전후처리 인터페이스 | `PreprocessSkeleton()`, `PostprocessSkeleton()` |

**사용 예시 - Animation Rigging Retargeting**:
```csharp
// GameObject 구조
GameObject (Character)
├── Animator (Humanoid)
├── RigBuilder
│   └── Rig (Rig Layer)
│       └── RetargetingAnimationConstraint
│           └── OVRBody 참조
└── OVRBody
```

#### Movement SDK - Retargeting Processors

| 컴포넌트 | 위치 | 역할 |
|---------|------|------|
| **RetargetingProcessor** | `Movement/Runtime/Scripts/RetargetingProcessing/` | 리타게팅 전후처리 베이스 클래스 |
| **RetargetingBlendHandProcessor** | `Movement/Runtime/Scripts/RetargetingProcessing/` | 손 블렌딩 처리 |
| **RetargetingHandDeformationProcessor** | `Movement/Runtime/Scripts/RetargetingProcessing/` | 손 변형 처리 |
| **RetargetingProcessorCorrectBones** | `Movement/Runtime/Scripts/RetargetingProcessing/` | 본 교정 프로세서 |
| **RetargetingProcessorCorrectHand** | `Movement/Runtime/Scripts/RetargetingProcessing/` | 손 교정 프로세서 |

> **v83 참고**: `NetworkCharacterRetargeter` Building Block이 v83에 포함되어 있습니다. Photon Fusion 또는 Unity Netcode와 함께 네트워크 아바타 동기화를 구현할 수 있습니다.

#### Interaction SDK - Core

| 컴포넌트 | 위치 | 역할 | 주요 프로퍼티/메서드 |
|---------|------|------|-------------------|
| **HandGrabInteractor** | `Interaction/Runtime/Scripts/Grab/` | 손으로 잡기 인터랙터 | Pinch/Palm Grab 지원 |
| **HandGrabInteractable** | `Interaction/Runtime/Scripts/Grab/` | 손으로 잡을 수 있는 오브젝트 | `SupportedGrabTypes`, `Rigidbody` 필요 |
| **DistanceGrabInteractor** | `Interaction/Runtime/Scripts/DistanceGrab/` | 원거리 잡기 인터랙터 | 레이 기반 원거리 Grab |
| **HandPokeInteractor** | `Interaction/Runtime/Scripts/Poke/` | 손으로 찌르기 인터랙터 | UI 버튼 누르기 |
| **PokeInteractable** | `Interaction/Runtime/Scripts/Poke/` | 찌를 수 있는 오브젝트 | `ISurface` 필요 |
| **RayInteractor** | `Interaction/Runtime/Scripts/Ray/` | 레이 기반 선택 인터랙터 | 원거리 오브젝트 선택 |

#### Audio SDK

| 컴포넌트 | 위치 | 역할 |
|---------|------|------|
| **Meta XR Audio Source** | `Audio/Runtime/Scripts/` | 공간 오디오 소스 | HRTF Spatialization, Room Acoustics |

#### Platform SDK

| 컴포넌트 | 사용 방법 | 역할 |
|---------|----------|------|
| **Achievements** | `Oculus.Platform.Achievements` | 업적 시스템 (Simple/Count/Bitfield) |
| **Leaderboards** | `Oculus.Platform.Leaderboards` | 리더보드 시스템 |
| **IAP** | `Oculus.Platform.IAP` | 인앱 구매 |

---

### 15.2 전체 컴포넌트 목록

#### Core SDK - Movement 관련

```
Scripts/Movement/
├── OVRBody.cs - Body Tracking 데이터 소스 제공, Upper Body/Full Body 지원
├── OVRSkeleton.cs - Skeleton 생성 및 애니메이션, Bone Transform 관리
├── OVRUnityHumanoidSkeletonRetargeter.cs - Unity Humanoid 아바타에 Body Tracking 리타게팅
├── OVRSkeletonMetadata.cs - 리타게팅용 메타데이터, Bone 페어링 정보
├── OVRHumanBodyBonesMappings.cs - Body Tracking ↔ Humanoid 본 매핑 데이터
├── OVRHumanBodyBonesMappingsInterface.cs - 본 매핑 인터페이스
├── OVRFace.cs - Face Tracking 기본 인터페이스
├── OVRCustomFace.cs - 커스텀 Face Tracking 구현
├── OVRFaceExpressions.cs - Face Tracking 표정 데이터 (52개 블렌드쉐이프)
└── OVREyeGaze.cs - Eye Tracking 데이터 제공 (Quest Pro)
```

#### Core SDK - 기본 VR 시스템

```
Scripts/
├── OVRCameraRig.cs - VR 카메라 시스템, TrackingSpace 관리
├── OVRManager.cs - Meta Quest 핵심 설정 관리
├── OVRInput.cs - 컨트롤러 입력 처리
├── OVRHand.cs - Hand Tracking 데이터
├── OVRSkeleton.cs - Skeleton 생성 및 애니메이션
├── OVRSkeletonRenderer.cs - Skeleton 렌더링
├── OVRBoundary.cs - Guardian Boundary 관리
├── OVRPassthroughLayer.cs - Passthrough MR 기능
├── OVRSpatialAnchor.cs - Spatial Anchors 관리
├── OVRSceneManager.cs - Scene API 관리
├── OVRSceneRoom.cs - Scene Room 데이터
└── OVROverlay.cs - Overlay 렌더링
```

#### Movement SDK - Animation Rigging

```
Runtime/Scripts/AnimationRigging/
├── RetargetingLayer.cs - 리타게팅 레이어 관리
├── RetargetingAnimationConstraint.cs - Unity Humanoid Body Tracking 적용 (Constraint)
├── RetargetingAnimationJob.cs - Job System 기반 리타게팅
├── RetargetingAnimationRig.cs - Animation Rig 통합
├── IRetargetingProcessor.cs - 리타게팅 프로세서 인터페이스
└── RetargetedBoneMappings.cs - 본 매핑 데이터

Legacy/
├── RetargetedBoneTargets.cs - Legacy 본 리타게팅
└── FullBodyRetargetedBoneTargets.cs - Legacy 전신 리타게팅
```

#### Movement SDK - Retargeting Processing

```
Runtime/Scripts/RetargetingProcessing/
├── RetargetingProcessor.cs - 리타게팅 프로세서 베이스
├── RetargetingBlendHandProcessor.cs - 손 블렌딩 처리
├── RetargetingHandDeformationProcessor.cs - 손 변형 처리
├── RetargetingProcessorCorrectBones.cs - 본 교정
└── RetargetingProcessorCorrectHand.cs - 손 교정
```

> **v83 기준**: `CharacterRetargeter`, `NetworkCharacterRetargeter` Building Blocks가 포함되어 있습니다. Retargeting Behavior Flags, Visemes 지원도 v83에서 추가됨.

#### Movement SDK - Face Tracking

```
Runtime/Scripts/Tracking/FaceTrackingData/
├── ARKitFace.cs - ARKit Face 리타게팅
├── CorrectivesFace.cs - Correctives Face 시스템
├── BlendshapeModifier.cs - 블렌드쉐이프 수정자
├── CorrectivesModule.cs - Correctives 모듈
└── Legacy/
    ├── BlendshapeMapping.cs - Legacy 블렌드쉐이프 매핑
    ├── CorrectiveShapesDriver.cs - Legacy Correctives 드라이버
    └── FaceTrackingSystem.cs - Legacy Face Tracking 시스템
```

> **v83 기준**: Face Tracking은 ARKitFace, CorrectivesFace 컴포넌트를 사용합니다. Quest 3/3S에서는 Audio-to-Expression(A2E) 방식이 지원됩니다.

#### Movement SDK - Utilities

```
Runtime/Scripts/Utils/
├── FollowTransformDirection.cs - Transform 방향 추적
├── HMDRemountRestartTracking.cs - HMD 재장착 시 트래킹 재시작
├── MirrorTransforms.cs - Transform 미러링
└── 기타 유틸리티 스크립트들
```

#### Interaction SDK - Grab

```
Runtime/Scripts/Grab/
├── HandGrabAPI.cs - Hand Grab API
├── HandGrabInteractor.cs - 손 잡기 인터랙터
├── HandGrabInteractable.cs - 손으로 잡을 수 있는 오브젝트
├── FingerPinchGrabAPI.cs - Pinch Grab API
├── FingerPalmGrabAPI.cs - Palm Grab API
├── FingerRawPinchAPI.cs - Raw Pinch API
├── ControllerPinchInjector.cs - 컨트롤러 Pinch 입력
├── GrabbingRule.cs - Grab 규칙
└── GrabTypeFlags.cs - Grab 타입 플래그 (Pinch/Palm)
```

#### Interaction SDK - Distance Grab

```
Runtime/Scripts/DistanceGrab/
├── DistantPointDetector.cs - 원거리 포인트 감지
├── IDistanceInteractor.cs - Distance Interactor 인터페이스
└── Visuals/ - Distance Grab 시각화 컴포넌트들
    ├── DistantInteractionLineVisual.cs - 라인 시각화
    ├── DistantInteractionTubeVisual.cs - 튜브 시각화
    ├── InteractorReticle.cs - 조준점 시각화
    └── ReticleDataIcon.cs - 아이콘 데이터
```

#### Interaction SDK - Poke

```
Runtime/Scripts/Poke/
├── HandPokeInteractor.cs - 손 찌르기 인터랙터
├── ControllerPokeInteractor.cs - 컨트롤러 찌르기 인터랙터
└── PokeInteractable.cs - 찌를 수 있는 오브젝트
```

#### Interaction SDK - Ray

```
Runtime/Scripts/Ray/
├── RayInteractor.cs - 레이 기반 선택 인터랙터
└── RayInteractable.cs - 레이로 선택할 수 있는 오브젝트
```

---

### 15.3 컴포넌트 조합 패턴

실제 개발 시나리오별로 필요한 컴포넌트 조합을 정리합니다.

#### 패턴 1: 기본 Avatar Retargeting

**목표**: Unity Humanoid 아바타에 Body Tracking 적용

**필요 컴포넌트**:
- `OVRBody` - Body Tracking 데이터 소스
- `OVRUnityHumanoidSkeletonRetargeter` - Humanoid 리타게터

**GameObject 구조**:
```
AvatarGameObject
├── Animator (Humanoid Rig)
├── OVRBody
│   └── ProvidedSkeletonType: UpperBody 또는 FullBody
└── OVRUnityHumanoidSkeletonRetargeter
    └── (자동으로 OVRBody에서 데이터 수신)
```

**OVRManager 설정**:
- Body Tracking Support: Supported
- Permission Requests On Startup: Body Tracking 체크

---

#### 패턴 2: Animation Rigging 기반 Retargeting (Movement SDK)

**목표**: Animation Rigging을 사용한 고급 리타게팅

**필요 컴포넌트**:
- `OVRBody` - Body Tracking 데이터
- `RigBuilder` - Unity Animation Rigging
- `Rig` - Rig Layer
- `RetargetingAnimationConstraint` - Body Tracking 적용 Constraint
- `RetargetingLayer` - 리타게팅 레이어 (선택)

**GameObject 구조**:
```
AvatarGameObject
├── Animator (Humanoid Rig)
├── OVRBody
└── RigBuilder
    └── Rig (Rig Layer)
        └── RetargetingAnimationConstraint
            └── OVRBody 참조
```

**설정 방법**:
1. Package Manager에서 Animation Rigging 패키지 설치
2. 아바타에 RigBuilder 컴포넌트 추가
3. Rig Layer 생성
4. RetargetingAnimationConstraint 추가 및 OVRBody 연결

---

#### 패턴 3: 네트워크 멀티플레이어 Avatar

**목표**: 네트워크 동기화된 아바타 시스템

> **v83 기준**: `NetworkCharacterRetargeter` Building Block을 사용하거나, Photon Fusion과 직접 통합하여 구현할 수 있습니다.

**필요 컴포넌트**:
- `OVRBody` - Body Tracking 데이터 (로컬 플레이어만)
- `OVRUnityHumanoidSkeletonRetargeter` 또는 `CharacterRetargeter` - 리타게터
- `NetworkCharacterRetargeter` (Movement SDK v83) 또는 Photon Fusion NetworkTransform

**GameObject 구조**:
```
LocalPlayer (로컬)
├── Animator
├── OVRBody
├── OVRUnityHumanoidSkeletonRetargeter
└── NetworkObject (Fusion)
    └── NetworkTransform (Animator 파라미터 동기화)

RemotePlayer (원격)
├── Animator
└── NetworkObject (Fusion)
    └── NetworkTransform (애니메이션 데이터 수신)
```

---

#### 패턴 4: Face + Eye Tracking 추가 (Quest Pro)

**목표**: Body + Face + Eye 통합 트래킹

**필요 컴포넌트**:
- `OVRBody` - Body Tracking
- `OVRFaceExpressions` - Face Tracking
- `OVREyeGaze` - Eye Tracking
- `OVRUnityHumanoidSkeletonRetargeter` - Body 리타게팅
- `ARKitFace` 또는 `CorrectivesFace` - Face 리타게팅 (선택)

**GameObject 구조**:
```
AvatarGameObject
├── Animator (Humanoid + Face Blendshapes)
├── OVRBody (Body Tracking)
├── OVRUnityHumanoidSkeletonRetargeter
├── OVRFaceExpressions (Face Tracking)
│   └── 52개 표정 블렌드쉐이프
├── ARKitFace (Face 리타게팅 - 선택)
└── LeftEye, RightEye
    └── OVREyeGaze 각각
```

**OVRManager 설정**:
- Body Tracking Support: Supported
- Face Tracking Support: Supported
- Eye Tracking Support: Supported
- Permission Requests: 모두 체크

---

#### 패턴 5: Grab Interaction 구현

**목표**: 손으로 오브젝트 잡기

**필요 컴포넌트**:
- `HandGrabInteractor` (손에 부착)
- `HandGrabInteractable` (잡을 오브젝트에 부착)
- `Rigidbody` (물리 시뮬레이션)

**GameObject 구조**:
```
OVRCameraRig
└── Hands
    ├── LeftHand
    │   └── HandGrabInteractor
    └── RightHand
        └── HandGrabInteractor

GrabbableObject
├── HandGrabInteractable
│   └── SupportedGrabTypes: Pinch | Palm
└── Rigidbody
    └── IsKinematic: false
```

---

#### 패턴 6: Poke Interaction (UI 버튼)

**목표**: 손으로 UI 버튼 누르기

**필요 컴포넌트**:
- `HandPokeInteractor` (손에 부착)
- `PokeInteractable` (버튼에 부착)
- `PlaneSurface` 또는 `ISurface` (표면 정의)

**GameObject 구조**:
```
OVRCameraRig
└── Hands
    ├── LeftHand
    │   └── HandPokeInteractor
    └── RightHand
        └── HandPokeInteractor

UIButton
├── PokeInteractable
├── PlaneSurface (표면)
└── PointableUnityEventWrapper (이벤트)
```

---

#### 패턴 7: Ray Interaction (원거리 선택)

**목표**: 레이로 원거리 오브젝트 선택

**필요 컴포넌트**:
- `RayInteractor` (손/컨트롤러에 부착)
- `RayInteractable` (선택할 오브젝트에 부착)

**GameObject 구조**:
```
OVRCameraRig
└── Hands
    ├── LeftHand
    │   └── RayInteractor
    └── RightHand
        └── RayInteractor

SelectableObject
└── RayInteractable
```

---

#### 패턴 8: MR Passthrough + Scene API

**목표**: Mixed Reality 환경 인식

**필요 컴포넌트**:
- `OVRPassthroughLayer` - Passthrough 활성화
- `OVRSceneManager` - Scene 데이터 관리
- `MRUK` (MR Utility Kit) - 고수준 Scene API

**GameObject 구조**:
```
Scene
├── OVRCameraRig
│   └── CenterEyeAnchor (Background: Transparent)
├── PassthroughLayer (OVRPassthroughLayer)
├── SceneManager (OVRSceneManager)
└── MRUK (선택)
```

**OVRManager 설정**:
- Passthrough Support: Supported
- Scene Support: Supported
- Anchor Support: Enabled

---

### 15.4 자주 사용하는 API 패턴

#### Body Tracking 데이터 읽기

```csharp
using UnityEngine;

public class BodyTrackingReader : MonoBehaviour
{
    [SerializeField] private OVRBody _ovrBody;
    [SerializeField] private OVRSkeleton _ovrSkeleton;

    void Update()
    {
        // Body State 확인
        if (_ovrBody != null)
        {
            var bodyState = _ovrBody.BodyState;

            // 특정 관절 위치 읽기
            if (bodyState.IsActive)
            {
                // BodyState에서 Joint 정보 접근
            }
        }

        // Skeleton Bones 접근
        if (_ovrSkeleton != null && _ovrSkeleton.IsDataValid)
        {
            var bones = _ovrSkeleton.Bones;
            foreach (var bone in bones)
            {
                Transform boneTransform = bone.Transform;
                // bone 위치/회전 사용
            }
        }
    }
}
```

#### Face Tracking 표정 읽기

```csharp
using UnityEngine;

public class FaceTrackingReader : MonoBehaviour
{
    [SerializeField] private OVRFaceExpressions _faceExpressions;

    void Update()
    {
        if (_faceExpressions != null && _faceExpressions.FaceTrackingEnabled)
        {
            // 특정 표정 값 가져오기
            float jawOpen = _faceExpressions.GetWeight(OVRFaceExpressions.FaceExpression.JawOpen);
            float smileLeft = _faceExpressions.GetWeight(OVRFaceExpressions.FaceExpression.LipCornerPullerL);
            float smileRight = _faceExpressions.GetWeight(OVRFaceExpressions.FaceExpression.LipCornerPullerR);

            // 블렌드쉐이프에 적용
            SkinnedMeshRenderer faceMesh = GetComponent<SkinnedMeshRenderer>();
            faceMesh.SetBlendShapeWeight(0, jawOpen * 100f);
        }
    }
}
```

#### Hand Grab Interaction 이벤트

```csharp
using Oculus.Interaction;
using UnityEngine;

public class GrabEventHandler : MonoBehaviour
{
    [SerializeField] private HandGrabInteractable _grabInteractable;

    void Start()
    {
        _grabInteractable.WhenPointerEventRaised += OnGrabEvent;
    }

    void OnGrabEvent(PointerEvent evt)
    {
        switch (evt.Type)
        {
            case PointerEventType.Hover:
                Debug.Log("[GrabEventHandler] Hovering");
                break;
            case PointerEventType.Unhover:
                Debug.Log("[GrabEventHandler] Unhovered");
                break;
            case PointerEventType.Select:
                Debug.Log("[GrabEventHandler] Grabbed");
                break;
            case PointerEventType.Unselect:
                Debug.Log("[GrabEventHandler] Released");
                break;
        }
    }
}
```

---

## 부록: 유용한 리소스

### 공식 문서
- [Meta Horizon Developers](https://developers.meta.com/horizon/)
- [Unity Development Overview](https://developers.meta.com/horizon/documentation/unity/unity-development-overview/)

### 공식 샘플 프로젝트
- [Unity-Movement](https://github.com/oculus-samples/Unity-Movement) - 바디/페이스/아이 트래킹
- [Unity-Discover](https://github.com/oculus-samples/Unity-Discover) - MR API 쇼케이스
- [Unity-Phanto](https://github.com/oculus-samples/Unity-Phanto) - Scene Mesh 활용
- [Unity-SharedSpatialAnchors](https://github.com/oculus-samples/Unity-SharedSpatialAnchors) - 멀티플레이어
- [Unity-PassthroughCameraApiSamples](https://github.com/oculus-samples/Unity-PassthroughCameraApiSamples) - 카메라 API

### 커뮤니티
- [Meta Community Forums](https://communityforums.atmeta.com/)
- [Unity XR Forums](https://discussions.unity.com/c/xr/93)

### 도구
- [Meta Quest Developer Hub](https://developer.oculus.com/downloads/package/oculus-developer-hub-win/)
- [Meta XR Simulator](https://developers.meta.com/horizon/documentation/unity/xrsim-getting-started/)

---

> **문서 작성일**: 2026년 3월 (v83.0.4 기준으로 작성)
> **기준 SDK 버전**: Meta XR All-in-One SDK v83.0.4 / Movement SDK v83.0.0
> **기준 Unity 버전**: Unity 6 (6000.3.7f1)
>
> ⚠️ **주의**: 이 가이드는 v83.0.4 기준으로 작성되었습니다. 다른 버전 사용 시 일부 내용이 다를 수 있습니다.

이 문서는 Meta Horizon 공식 개발자 문서를 기반으로 작성되었습니다. 최신 정보는 항상 [공식 문서](https://developers.meta.com/horizon/documentation/unity/)를 참조하세요.
