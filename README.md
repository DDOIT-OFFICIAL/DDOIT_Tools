# DDOIT Tools

DDOIT Tools는 Meta Quest 기반 VR 교육프로그램을 빠르게 구성하기 위한 Unity UPM 패키지입니다.
시나리오 실행, 공통 매니저, UI, 오디오, 플레이어/이동 보조, 에디터 자동 설정 도구를 함께 제공합니다.

> 이 저장소는 UPM 배포용 미러입니다. 공통 기능 개발 원본은 `DDOIT_Template/Assets/DDOIT_Tools`입니다.

## Current Version

| 항목 | 값 |
|---|---|
| Package | `com.ddoit.tools` |
| Latest tag | `v0.19.0` |
| Unity | `6000.0+` |
| Verified Unity | `6000.3.7f1` |
| Target | Android / Meta Quest |
| Meta XR All-in-One SDK | `203.0.0` |

## Install

Unity Package Manager에서 Git URL로 설치합니다.

```text
https://github.com/DDOIT-OFFICIAL/DDOIT_Tools.git#v0.19.0
```

`Packages/manifest.json`에 직접 추가할 수도 있습니다.

```json
{
  "dependencies": {
    "com.ddoit.tools": "https://github.com/DDOIT-OFFICIAL/DDOIT_Tools.git#v0.19.0"
  }
}
```

`main` 브랜치 직접 설치도 가능하지만, 소비 프로젝트에서는 재현 가능한 태그 설치를 권장합니다.

## Dependencies

`com.ddoit.tools`의 `package.json`은 `com.meta.xr.sdk.all@203.0.0`을 직접 요구합니다. 따라서 Meta/Oculus 어셈블리가 `DDOIT.Tools.Runtime`보다 먼저 준비되고 Meta Locomotion 스크립트도 첫 컴파일부터 항상 컴파일됩니다. Setup은 Meta XR 버전을 정확히 203.0.0으로 계속 검증합니다.

Input System, XR Management, OpenXR, Lottie Player는 `DDOIT Tools > Setup` 창의 `필수 패키지 설치/업데이트` 버튼에서 설치/검증합니다. 특히 OpenXR은 최초 설치 전에 Asset Import Worker를 일시 정지할 수 있도록 Setup 관리 대상으로 유지합니다.

| 구분 | 패키지 | 기준 |
|---|---|---|
| UPM 하드 의존성 + Setup exact 검증 | Meta XR All-in-One SDK | `203.0.0` exact |
| UPM 하드 의존성 | TextMeshPro | `4.0.0+` |
| UPM 하드 의존성 | Addressables | `2.8.1+` |
| Setup 필수 설치/검증 | Input System | `1.18.0+` |
| Setup 필수 설치/검증 | XR Management | `4.5.4+` |
| Setup 필수 설치/검증 | OpenXR Plugin | `1.17.1+` |
| Setup 필수 설치/검증 | Lottie Player | 설치 필요 |
| Setup 권장 도구 | Unity CLI Connector | AI/CLI 기반 Unity 제어용 |

Meta XR All-in-One SDK는 Unity Asset Store에서 먼저 내 에셋에 추가되어 있어야 Package Manager 설치가 정상 동작합니다.

## Quick Start

1. Package Manager에 위 Git URL을 추가합니다.
2. Unity 상단 메뉴에서 `DDOIT Tools > Setup`을 엽니다.
3. `필수 패키지 설치/업데이트`로 필수 패키지를 확인/설치합니다.
4. `Initialize Project`로 프로젝트 표준 폴더와 기본 씬을 생성합니다.
5. Quest 프로젝트라면 `Optimize Project`를 실행합니다.
6. 시나리오 제작은 `DDOIT Tools > Tools Window`와 Inspector의 커스텀 인스펙터를 사용합니다.

## Editor Menus

| 메뉴 | 용도 |
|---|---|
| `DDOIT Tools > Setup` | 패키지 업데이트, 의존성 점검, 프로젝트 초기화, Quest 최적화 |
| `DDOIT Tools > Tools Window` | 시나리오/설정/도구 통합 창 |
| `DDOIT Tools > Scene Switcher` | DDOIT/InitScene/Addressable 씬 전환 메뉴 생성 |

## Setup Features

### Package Update

Setup 창 상단의 `최신 릴리스 확인/업데이트` 버튼으로 GitHub의 최신 안정 태그를 확인하고 새 버전으로 갱신할 수 있습니다.

- Git URL로 설치된 패키지만 실제 업데이트 가능
- 현재 버전과 최신 안정 버전을 비교한 뒤 사용자 확인 후 업데이트
- Package Manager에 새 Git URL을 다시 입력하거나 lock 파일을 삭제할 필요 없음
- `Assets/DDOIT_Tools` 개발 원본 모드에서는 최신 릴리스 조회만 제공

### Initialize Project

표준 프로젝트 폴더를 생성하고 패키지의 기본 씬을 소비 프로젝트로 복사합니다.

- `01. Scenes`부터 `15. Videos`까지 표준 폴더 생성
- `999. Resources/AssetStore` 생성
- `Assets/01. Scenes/DDOIT/DDOIT.unity` 복사
- `Assets/01. Scenes/DDOIT/InitScene.unity` 복사
- `AGENTS.md`, `CLAUDE.md` 프로젝트 루트 배포

### Optimize Project

Quest VR 기준 프로젝트 설정을 preflight 확인 후 적용합니다.

- DDOIT URP 에셋을 `Assets/Settings/DDOIT`로 복사
- 기존 설정 에셋은 GUID를 보존한 채 내용 갱신
- Graphics/Quality Render Pipeline 설정
- Linear Color Space
- Android Graphics APIs: Vulkan, OpenGLES3
- IL2CPP / ARM64 / Managed Stripping Medium
- Min SDK: Android API Level 32
- Fixed Timestep: `0.01389`
- Audio DSP Buffer: `256`
- Texture Compression: ASTC
- Build Compression: LZ4
- Overlay UI / OVROverlayCanvas Rendering 레이어 등록
- XR Plug-in Management Android/Standalone OpenXR loader 자동 등록

Android/Standalone OpenXR loader가 꺼져 있으면 자동 등록합니다.
Meta XR 203.0.0과 OpenXR 1.17.1 조합에서 재시작마다 복귀하는 OpenXR target API patch warning은 에디터 시작 시 자동 정리합니다. `Use OpenXR Predicted Time` 상태는 자동 변경하지 않고 preflight 경고로 표시합니다.

## Main Modules

| 모듈 | 설명 |
|---|---|
| Scenario | `ScenarioManager > Scenario > Step > ScenarioNode` 기반 교육 흐름 실행 |
| Scenario Editors | 시나리오 목록, Step 흐름, 조건 그룹, 분기 미리보기 커스텀 인스펙터 |
| UI | `UIManager`, `UIPanel`, `UINode`, 공통 UI 표시 흐름 |
| Audio | `SoundManager`, `SoundDatabase`, BGM/SFX 관리 |
| Managers | Bootstrap, Game, Scene, ScreenFade, UI, Sound 매니저 |
| Player / Locomotion | PlayerRig, WalkingStick 계열 이동 보조 기능 |
| Addressables | 원격 에셋/시나리오 로딩 기반 유틸 |
| Data / Settings | ScriptableObject 기반 공통 설정과 데이터 |

## Documentation

상세 사용법은 패키지의 `MDs/` 폴더를 기준으로 확인합니다.

| 문서 | 용도 |
|---|---|
| [`MDs/DDOIT_Tools.md`](MDs/DDOIT_Tools.md) | 전체 구조, 모듈별 사용법, 개발 원칙 |
| [`MDs/Meta_XR_SDK_Unity_Guide.md`](MDs/Meta_XR_SDK_Unity_Guide.md) | Meta XR SDK 기반 Unity 개발 가이드 |
| [`MDs/AGENTS.md`](MDs/AGENTS.md) | AI Agent용 프로젝트 운영/코딩 규칙 |
| [`MDs/CLAUDE.md`](MDs/CLAUDE.md) | Claude Code용 동일 규칙 문서 |

`DDOIT Tools > Setup`의 문서 배포 기능은 `AGENTS.md`와 `CLAUDE.md`를 소비 프로젝트 루트로 복사합니다.

## Development Policy

- `DDOIT_Template/Assets/DDOIT_Tools`가 실시간 개발 원본입니다.
- 이 저장소는 UPM 배포를 위한 패키지 산출물입니다.
- 공통 기능 수정은 먼저 `DDOIT_Template`에서 진행합니다.
- 배포 시 `Assets/DDOIT_Tools` 내용을 이 저장소로 미러링하고 태그를 발행합니다.
- 소비 프로젝트에서는 태그 기반 Git URL 설치를 권장합니다.

## License

Internal use only. DDOIT 프로젝트 전용.
