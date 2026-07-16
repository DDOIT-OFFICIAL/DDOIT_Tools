# DDOIT Tools

DDOIT Tools는 Meta Quest 기반 VR 교육 프로그램 제작을 위한 Unity UPM 패키지입니다.
시나리오 실행, 공통 매니저, UI, 오디오, 플레이어/이동 보조, 프로젝트 초기화와 Quest 최적화 도구를 제공합니다.

> 이 저장소는 UPM 배포용 미러입니다. 공통 기능 개발 원본은 `D:\DDOIT\Projects\DDOIT_Template\Assets\DDOIT_Tools`입니다.

## Current Version

| 항목 | 값 |
|---|---|
| Package | `com.ddoit.tools` |
| Latest tag | `v0.19.2` |
| Unity | `6000.0+` |
| Verified Unity | `6000.3.7f1` |
| Target | Android / Meta Quest |
| Meta XR All-in-One SDK | `201.0.0` |
| Meta XR Movement SDK | `https://github.com/oculus-samples/Unity-Movement.git#v201.0.0` |
| XR Management / OpenXR | `4.5.3` / `1.16.1` |

## Install

Unity Package Manager에서 Git URL로 설치합니다.

```text
https://github.com/DDOIT-OFFICIAL/DDOIT_Tools.git#v0.19.2
```

`Packages/manifest.json`에 직접 추가할 수도 있습니다.

```json
{
  "dependencies": {
    "com.ddoit.tools": "https://github.com/DDOIT-OFFICIAL/DDOIT_Tools.git#v0.19.2"
  }
}
```

소비 프로젝트에서는 재현 가능한 tag 설치를 권장합니다. `main` 직접 설치는 테스트/검증용으로만 사용하세요.

## Dependencies

`package.json` 직접 의존성은 패키지 설치 직후 첫 컴파일에 필요한 항목만 둡니다.
`DDOIT Tools > Setup`은 이 항목들도 다시 검증하지만, 주 역할은 Movement/OpenXR/XR Management/Lottie 같은 프로젝트 설정성 의존성을 원버튼으로 설치하고 최적화하는 것입니다.

| 구분 | 패키지 | 기준 |
|---|---|---|
| UPM 직접 의존성 + Setup exact 검증 | Meta XR All-in-One SDK | `201.0.0` exact |
| UPM 직접 의존성 + Setup 검증 | Input System | `1.18.0+` |
| UPM 직접 의존성 + Setup 검증 | TextMeshPro | `4.0.0+` |
| UPM 직접 의존성 + Setup 검증 | Addressables | `2.8.1+` |
| UPM 직접 의존성 | Unity UI / UGUI | `2.0.0+` |
| Setup 필수 설치/검증 | Meta XR Movement SDK | `#v201.0.0` exact Git reference |
| Setup 필수 설치/검증 | XR Management | `4.5.3` exact |
| Setup 필수 설치/검증 | OpenXR Plugin | `1.16.1` exact |
| Setup 필수 설치/검증 | Lottie Player | 설치 필요 |
| Setup 권장 도구 | Unity CLI Connector | AI/CLI 기반 Unity 제어용 |

Meta XR All-in-One SDK는 Unity Asset Store에서 먼저 내 에셋에 추가되어 있어야 Package Manager 설치가 정상 동작합니다.

## Quick Start

1. Package Manager에서 Git URL을 추가합니다.
2. Unity 상단 메뉴에서 `DDOIT Tools > Setup`을 엽니다.
3. `필수 패키지 설치/업데이트`로 의존성을 확인/설치합니다.
4. `Initialize Project`로 표준 폴더, DDOIT/InitScene, AI Agent 문서를 생성합니다.
5. Quest 프로젝트라면 `Optimize Project`를 실행합니다.
6. 시나리오 제작은 `DDOIT Tools > Tools Window`와 Inspector의 커스텀 인스펙터를 사용합니다.

## Setup Features

### Package Update

Git URL로 설치된 `com.ddoit.tools`에서 최신 안정 tag를 조회하고, 사용자가 승인하면 해당 tag URL로 패키지를 갱신합니다.
Package Manager에 Git URL을 반복 입력하거나 lock 파일을 직접 지우는 절차를 줄이기 위한 기능입니다.

### Initialize Project

- `01. Scenes`부터 `15. Videos`까지 표준 폴더 생성
- `999. Resources/AssetStore` 생성
- `Assets/01. Scenes/DDOIT/DDOIT.unity` 복사
- `Assets/01. Scenes/DDOIT/InitScene.unity` 복사
- `AGENTS.md`, `CLAUDE.md` 프로젝트 루트 배포

### Optimize Project

Quest VR 기준 프로젝트 설정을 preflight 확인 후 적용합니다.

- DDOIT URP 에셋을 `Assets/Settings/DDOIT`로 복사
- Graphics/Quality Render Pipeline 설정
- Linear Color Space
- Android Graphics APIs: Vulkan, OpenGLES3
- IL2CPP / ARM64 / Managed Stripping Medium
- Min SDK: Android API Level 32
- Fixed Timestep: `0.01389`
- Audio DSP Buffer: `256`
- Texture Compression: ASTC
- Build Compression: LZ4
- Overlay UI / `OVROverlayCanvas Rendering` 레이어 등록
- XR Plug-in Management Android/Standalone OpenXR loader 자동 등록
- OpenXR feature baseline 및 target API patch warning 자동 정리
- Meta XR Project Setup / XR Project Validation의 fixable 항목 자동 적용 시도

## Documentation

자세한 내용은 패키지의 `MDs/` 폴더를 기준으로 확인합니다.

| 문서 | 용도 |
|---|---|
| `MDs/DDOIT_Tools.md` | 전체 구조, 모듈별 사용법, 개발 원칙 |
| `MDs/Meta_XR_SDK_Unity_Guide.md` | Meta XR SDK 기반 Unity 개발 가이드 |
| `MDs/AGENTS.md` | AI Agent 프로젝트 운영/코딩 규칙 |
| `MDs/CLAUDE.md` | Claude Code용 동일 규칙 문서 |

## Development Policy

- `DDOIT_Template/Assets/DDOIT_Tools`가 실시간 개발 원본입니다.
- 이 저장소는 UPM 배포를 위한 패키지 산출물입니다.
- 공통 기능 수정은 먼저 `DDOIT_Template`에서 진행합니다.
- 배포 직전에 `Assets/DDOIT_Tools` 내용을 이 저장소로 미러링하고 tag를 발행합니다.

## License

Internal use only. DDOIT 프로젝트 전용.
