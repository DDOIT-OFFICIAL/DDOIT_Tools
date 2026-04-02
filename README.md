# DDOIT Tools

> VR 교육프로그램 통합 개발 도구 모음 (Unity 6 / Meta Quest)

## 개요

DDOIT Tools는 VR 교육프로그램을 효율적으로 개발하기 위한 **공통 도구 모음**입니다.  
시나리오 시스템, UI 시스템, 오디오 관리, 씬 전환 등을 제공합니다.

## 요구 사항

| 항목 | 버전 |
|---|---|
| Unity | 6000.3.7f1 |
| Meta XR All-In-One SDK | 83.0.4 |
| Addressables | 2.8.1 |
| URP | 17.3.0 |
| TextMeshPro | 4.0+ |

## 설치

Unity Package Manager에서 Git URL로 설치합니다.

1. Unity 에디터 → `Window > Package Manager`
2. 좌측 상단 `+` 버튼 → `Add package from git URL...`
3. 아래 URL 입력:

```
https://github.com/DDOIT-OFFICIAL/DDOIT_Tools.git
```

특정 버전 설치:
```
https://github.com/DDOIT-OFFICIAL/DDOIT_Tools.git#v0.3.1
```

### 업데이트

`Packages/manifest.json`에서 버전 태그를 변경합니다:
```json
"com.ddoit.tools": "https://github.com/DDOIT-OFFICIAL/DDOIT_Tools.git#v0.4.0"
```

## 권장 도구

AI 기반 vibe 코딩(Claude Code 등)을 활용할 경우, CLI에서 Unity 에디터를 직접 제어할 수 있는 **unity-cli** 설치를 권장합니다.

- [unity-cli](https://github.com/youngwoocho02/unity-cli/tree/main)

## 주요 모듈

| 모듈 | 설명 |
|---|---|
| **Scenario** | 교육 시나리오 순차 실행 (ScenarioManager → Scenario → Step → Node) |
| **UI** | UIManager 풀링, UIPanel, UITheme, UINode |
| **Audio** | SoundManager, SoundDatabase, BGM/SFX 관리 |
| **Managers** | GameManager, SceneManager, ScreenFadeManager, BootstrapManager |
| **Font** | 다국어 Dynamic SDF (한/영/일/중/베트남어) |

## 문서

상세 가이드는 `MDs/` 폴더를 참조하세요.

- [DDOIT_Tools.md](MDs/DDOIT_Tools.md) — 전체 도구 가이드
- [Meta_XR_SDK_Unity_Guide.md](MDs/Meta_XR_SDK_Unity_Guide.md) — Meta XR SDK 사용법

## 라이선스

Internal use only. DDOIT 프로젝트 전용.
