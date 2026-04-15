# CLAUDE.md

이 파일은 Claude Code와 함께 작업한 내용을 기록하는 문서입니다.

## 프로젝트 정보
- Unity 버전: 6000.3.7f1
- Meta XR All-In-One SDK 버전: 83.0.4
- Meta XR Movement SDK 버전: 83.0.0 (GitHub)
- XR Management: 4.5.4 / OpenXR: 1.16.1
- 네트워킹: Photon Fusion 2.0.9 (예정)

## 참고 문서
- **DDOIT Tools 가이드**: 아래 경로 중 존재하는 파일을 참조
  - UPM: `Packages/com.ddoit.tools/MDs/DDOIT_Tools.md`
  - 개발자 모드: `Assets/DDOIT_Tools/MDs/DDOIT_Tools.md`
  - 템플릿 구조, 모듈별 가이드, Addressables 운영 가이드, 새 프로젝트 시작 방법
- **Meta XR SDK 가이드**: 아래 경로 중 존재하는 파일을 참조
  - UPM: `Packages/com.ddoit.tools/MDs/Meta_XR_SDK_Unity_Guide.md`
  - 개발자 모드: `Assets/DDOIT_Tools/MDs/Meta_XR_SDK_Unity_Guide.md`
  - **현재 가이드 기준 버전: v83.0.4**
  - Meta SDK 관련 기술 (Movement SDK, Avatar Retargeting, Hand Tracking, Passthrough 등) 사용 시 반드시 이 파일을 읽고 참조할 것
  - Building Blocks, 컴포넌트 사용법, 설정 방법 등이 포함되어 있음

## 메모

---

# Unity 코딩 규칙

## 1. 명명 규칙 (Nomenclature)

### 1.1 기본 명명 규칙
- **Variables (변수)**: `_variableName` - 언더스코어로 시작하는 camelCase
- **Constants (상수)**: `CONSTANTNAME` - 모두 대문자
- **Statics (정적 변수)**: `StaticName` - PascalCase
- **Classes/Structs (클래스/구조체)**: `ClassName` - PascalCase
- **Properties (프로퍼티)**: `PropertyName` - PascalCase
- **Methods (메서드)**: `MethodName()` - PascalCase
- **Arguments (인자)**: `argumentName` - camelCase
- **Temporary variables (임시 변수)**: `temporaryVariable` - camelCase

### 1.2 예제
```csharp
public class PlayerController
{
    // 변수
    private int _health;
    private string _playerName;

    // 상수
    private const string API_URL = "https://api.example.com";
    private const int MAX_HEALTH = 100;

    // 정적 변수
    private static PlayerController Instance;

    // 프로퍼티
    public int Health { get => _health; set => _health = value; }

    // 메서드 (인자: argumentName, 임시변수: temporaryVariable)
    public void TakeDamage(int damageAmount)
    {
        int temporaryHealth = _health - damageAmount;
        _health = Mathf.Max(0, temporaryHealth);
    }
}
```

---

## 2. 변수 선언 규칙

### 2.1 전역 변수 (필드)
- 모든 전역 변수는 **언더스코어(_)로 시작**해야 합니다.
- 예: `_test`, `_playerHealth`, `_enemyCount`

### 2.2 접근 제한자 (Access Modifiers)
- 다른 곳에서 사용되지 않는 변수는 **무조건 `private`**으로 선언합니다.
- 외부에서 접근이 필요한 경우 프로퍼티를 통해 캡슐화합니다.

```csharp
// ✅ 올바른 예
private int _score;

// ❌ 잘못된 예 (불필요한 public)
public int _score;
```

### 2.3 Unity Inspector 노출
- Inspector에서 값을 설정해야 하는 경우 `[SerializeField]`를 사용합니다.
- **public을 사용하지 않습니다.**

```csharp
// ✅ 올바른 예
[SerializeField] private int _maxHealth;
[SerializeField] private GameObject _weaponPrefab;

// ❌ 잘못된 예
public int _maxHealth;
```

---

## 3. 캡슐화 (Encapsulation)

public 접근이 필요한 경우 프로퍼티를 활용하여 캡슐화합니다.

### 3.1 기본 프로퍼티
```csharp
private int _health;

public int Health
{
    get => _health;
    set => _health = Mathf.Clamp(value, 0, 100);
}
```

### 3.2 읽기 전용 프로퍼티
```csharp
private string _playerName;

public string PlayerName
{
    get => _playerName;
}
```

### 3.3 Auto Property (간단한 경우)
```csharp
// private 필드가 필요 없는 경우
public int Score { get; private set; }
```

---

## 4. 코드 구조화 (#region)

코드의 가독성을 위해 `#region`으로 섹션을 구분합니다.

### 4.1 권장 구조
```csharp
namespace Game.Player
{
    public class PlayerController : MonoBehaviour
    {
        #region Constants
        private const int MAX_HEALTH = 100;
        private const string SAVE_KEY = "PlayerData";
        #endregion

        #region Serialized Fields
        [SerializeField] private int _startingHealth;
        [SerializeField] private GameObject _weaponPrefab;
        #endregion

        #region Private Fields
        private int _currentHealth;
        private Transform _transform;
        #endregion

        #region Properties
        public int CurrentHealth
        {
            get => _currentHealth;
            private set => _currentHealth = value;
        }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _transform = transform;
        }

        private void Start()
        {
            InitializePlayer();
        }

        private void Update()
        {
            HandleInput();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 플레이어가 데미지를 받습니다.
        /// </summary>
        /// <param name="damageAmount">받을 데미지 양</param>
        public void TakeDamage(int damageAmount)
        {
            _currentHealth -= damageAmount;
            if (_currentHealth <= 0)
            {
                Die();
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 플레이어를 초기화합니다.
        /// </summary>
        private void InitializePlayer()
        {
            _currentHealth = _startingHealth;
        }

        /// <summary>
        /// 플레이어 사망 처리를 합니다.
        /// </summary>
        private void Die()
        {
            // 사망 로직
        }

        /// <summary>
        /// 사용자 입력을 처리합니다.
        /// </summary>
        private void HandleInput()
        {
            // 입력 처리
        }
        #endregion

        #region Event Handlers
        private void OnCollisionEnter(Collision collision)
        {
            // 충돌 처리
        }
        #endregion
    }
}
```

---

## 5. Namespace 규칙

### 5.1 기본 원칙
- 모든 스크립트에 **namespace를 적용**합니다.
- **기능 단위**로 namespace를 구분합니다.
- **폴더 구조와 네임스페이스를 일치**시킵니다.

### 5.2 Namespace 구조
```
{루트}.{카테고리}.{세부기능}
```
- **루트**: 조직명, 프로젝트명 등 최상위 식별자
- **카테고리**: 기능 분류 (Player, UI, Audio 등)
- **세부기능**: 카테고리 내 하위 분류 (필요 시)

### 5.3 카테고리 분류 예시
```csharp
// 플레이어
namespace MyProject.Player
{
    public class PlayerController : MonoBehaviour { }
    public class PlayerStats { }
}

namespace MyProject.Player.Inventory
{
    public class InventoryManager { }
    public class InventoryItem { }
}

// 적/NPC
namespace MyProject.Enemy
{
    public class EnemyController : MonoBehaviour { }
}

namespace MyProject.Enemy.AI
{
    public class EnemyAI { }
    public class PatrolBehavior { }
}

// UI
namespace MyProject.UI
{
    public class MainMenuUI : MonoBehaviour { }
    public class GameHUD : MonoBehaviour { }
}

// 오디오
namespace MyProject.Audio
{
    public class SoundManager : MonoBehaviour { }
}

// 매니저
namespace MyProject.Managers
{
    public class GameManager : MonoBehaviour { }
}

// 데이터
namespace MyProject.Data
{
    [System.Serializable]
    public class PlayerData { }
    [System.Serializable]
    public class GameSettings { }
}

// 유틸리티
namespace MyProject.Utilities
{
    public static class MathHelper { }
    public static class StringHelper { }
}
```

### 5.4 공통 코드와 프로젝트 코드 분리
**공통 모듈(UPM 라이브러리)과 제품 프로젝트는 네임스페이스 루트를 다르게 사용합니다.**

```
{조직}.{공통모듈}.{카테고리}        ← 공통 모듈 (UPM으로 배포, 타 프로젝트에 import됨)
{제품명}.{세부}.{카테고리}          ← 제품 프로젝트 (조직 prefix 생략)
```

**근거**:
- 공통 모듈은 다른 프로젝트에 import되는 라이브러리이므로, 조직 prefix로 **네임스페이스 충돌을 방지**.
- 제품 프로젝트는 최종 결과물이므로 조직 prefix가 군더더기. 제품명 자체가 루트로 충분하며, 네임스페이스 깊이(depth)도 줄어 가독성 향상.

**예시 (DDOIT 생태계)**:
```csharp
// 공통 모듈 (UPM 패키지)
namespace DDOIT.Tools.Managers  // ScenarioManager, SoundManager, UIManager ...
namespace DDOIT.Tools.Scenario  // ScenarioNode, Step, Scenario, UINode ...

// 제품 프로젝트 (ResQ 시리즈)
namespace ResQ.Fire.Managers    // 화재편 고유 매니저
namespace ResQ.Fire.Scenarios   // 화재편 고유 시나리오 노드
namespace ResQ.Cpr.UI           // CPR편 고유 UI
```

**네임스페이스 충돌 대응**: `ResQ.Fire.Managers.ScenarioManager`와 `DDOIT.Tools.Managers.ScenarioManager`가 동일 파일에서 충돌할 경우, using alias로 해결:
```csharp
using ToolsScenarioManager = DDOIT.Tools.Managers.ScenarioManager;
using FireScenarioManager = ResQ.Fire.Managers.ScenarioManager;
```

### 5.5 폴더-네임스페이스 매핑 원칙

프로젝트 루트 폴더는 **CEW Init이 생성하는 에셋 타입별 구조**(`01. Scenes`, `02. Scripts`, ...)를 유지하되, **스크립트 내부는 제품명 서브폴더 기준으로 네임스페이스를 매핑**합니다.

```
Assets/
├── DDOIT_Tools/                    ← 공통 모듈 (UPM 개발 프로젝트에만 존재)
│   └── Scripts/
│       ├── Managers/               → DDOIT.Tools.Managers
│       ├── Scenario/               → DDOIT.Tools.Scenario
│       └── UI/                     → DDOIT.Tools.UI
│
├── 01. Scenes/
│   ├── DDOIT/                      ← 공통 모듈 제공 씬 (Bootstrap, InitScene)
│   └── Fire/                       ← 제품 씬 (ResQ 화재편)
│
├── 02. Scripts/
│   └── Fire/                       ← 제품명 서브폴더
│       ├── Managers/               → ResQ.Fire.Managers
│       ├── Scenarios/              → ResQ.Fire.Scenarios
│       └── UI/                     → ResQ.Fire.UI
│
├── 03. Prefabs/
│   └── Fire/                       ← 제품명 서브폴더
│
└── 05. SO/
    └── Fire/                       ← 제품명 서브폴더
```

**규칙 요약**:
- 에셋 타입 루트(`01. Scenes`, `02. Scripts` 등)는 CEW Init 구조 그대로 유지.
- 그 하위에 **제품명 서브폴더** 생성 (예: `Fire/`, `Cpr/`, `Earthquake/`).
- 스크립트 네임스페이스는 `{제품명}.{카테고리}` 형식으로 제품명 서브폴더 기준 매핑.
- 여러 제품이 한 프로젝트에 공존할 경우(예: ResQ 통합 앱), 제품명 서브폴더로 자연스럽게 분리.

> 프로젝트별 구체적 적용은 해당 프로젝트의 가이드 문서를 참조하세요.

---

## 6. 주석 규칙

### 6.1 XML 문서화 주석
모든 public 메서드와 중요한 private 메서드에는 `///` summary를 작성합니다.

```csharp
/// <summary>
/// 플레이어의 체력을 회복합니다.
/// </summary>
/// <param name="amount">회복할 체력 양</param>
/// <returns>실제로 회복된 체력 양</returns>
public int Heal(int amount)
{
    int previousHealth = _currentHealth;
    _currentHealth = Mathf.Min(_currentHealth + amount, MAX_HEALTH);
    return _currentHealth - previousHealth;
}
```

### 6.2 복잡한 로직 설명
```csharp
/// <summary>
/// A* 알고리즘을 사용하여 최단 경로를 찾습니다.
/// </summary>
/// <param name="start">시작 위치</param>
/// <param name="end">목표 위치</param>
/// <returns>경로 노드 리스트</returns>
private List<Node> FindPath(Vector3 start, Vector3 end)
{
    // 열린 리스트와 닫힌 리스트 초기화
    List<Node> openList = new List<Node>();
    HashSet<Node> closedList = new HashSet<Node>();

    // 경로 탐색 로직...
    return null;
}
```

---

## 7. 추가 권장 코딩 규칙

### 7.1 성능 최적화
```csharp
// ✅ 올바른 예: 캐싱 활용
private Transform _transform;

private void Awake()
{
    _transform = transform; // 한 번만 가져오기
}

private void Update()
{
    _transform.position += Vector3.forward; // 캐시된 값 사용
}

// ❌ 잘못된 예: 매 프레임 GetComponent 호출
private void Update()
{
    transform.position += Vector3.forward; // 내부적으로 GetComponent 호출
}
```

### 7.2 Null 체크
```csharp
/// <summary>
/// 타겟에게 데미지를 입힙니다.
/// </summary>
public void DealDamage(GameObject target, int damage)
{
    if (target == null)
    {
        Debug.LogWarning("Target is null!");
        return;
    }

    var health = target.GetComponent<Health>();
    if (health != null)
    {
        health.TakeDamage(damage);
    }
}
```

### 7.3 매직 넘버 사용 금지
```csharp
// ❌ 잘못된 예
if (_health < 20)
{
    // 로직
}

// ✅ 올바른 예
private const int LOW_HEALTH_THRESHOLD = 20;

if (_health < LOW_HEALTH_THRESHOLD)
{
    // 로직
}
```

### 7.4 이벤트 시스템 활용
```csharp
// UnityEvent 사용
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [SerializeField] private UnityEvent _onDeath;

    /// <summary>
    /// 사망 처리를 합니다.
    /// </summary>
    private void Die()
    {
        _onDeath?.Invoke();
    }
}

// C# 이벤트 사용
public class GameManager : MonoBehaviour
{
    public event System.Action OnGameOver;

    /// <summary>
    /// 게임 종료를 처리합니다.
    /// </summary>
    private void EndGame()
    {
        OnGameOver?.Invoke();
    }
}
```

### 7.5 ScriptableObject 활용
```csharp
namespace MyGame.Data
{
    /// <summary>
    /// 무기의 기본 데이터를 담는 ScriptableObject입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWeaponData", menuName = "Game/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        #region Serialized Fields
        [SerializeField] private string _weaponName;
        [SerializeField] private int _damage;
        [SerializeField] private float _attackSpeed;
        #endregion

        #region Properties
        public string WeaponName => _weaponName;
        public int Damage => _damage;
        public float AttackSpeed => _attackSpeed;
        #endregion
    }
}
```

### 7.6 String 비교 최적화
```csharp
// ❌ 잘못된 예
if (gameObject.tag == "Player")
{
    // 로직
}

// ✅ 올바른 예
if (gameObject.CompareTag("Player"))
{
    // 로직 (더 빠르고 안전함)
}
```

### 7.7 코루틴 관리
```csharp
private Coroutine _damageCoroutine;

/// <summary>
/// 지속 데미지를 시작합니다.
/// </summary>
public void StartDamageOverTime(int damagePerSecond, float duration)
{
    // 기존 코루틴이 있으면 중지
    if (_damageCoroutine != null)
    {
        StopCoroutine(_damageCoroutine);
    }

    _damageCoroutine = StartCoroutine(DamageOverTimeCoroutine(damagePerSecond, duration));
}

/// <summary>
/// 시간에 따라 데미지를 입히는 코루틴입니다.
/// </summary>
private IEnumerator DamageOverTimeCoroutine(int damagePerSecond, float duration)
{
    float elapsedTime = 0f;

    while (elapsedTime < duration)
    {
        TakeDamage(damagePerSecond);
        yield return new WaitForSeconds(1f);
        elapsedTime += 1f;
    }

    _damageCoroutine = null;
}
```

### 7.8 레이어 및 태그 상수화
```csharp
namespace MyGame.Constants
{
    /// <summary>
    /// 게임에서 사용하는 태그 상수입니다.
    /// </summary>
    public static class Tags
    {
        public const string PLAYER = "Player";
        public const string ENEMY = "Enemy";
        public const string GROUND = "Ground";
    }

    /// <summary>
    /// 게임에서 사용하는 레이어 상수입니다.
    /// </summary>
    public static class Layers
    {
        public const int PLAYER = 6;
        public const int ENEMY = 7;
        public const int GROUND = 8;
    }
}

// 사용 예
if (collision.gameObject.CompareTag(Tags.PLAYER))
{
    // 로직
}
```

### 7.9 Single Responsibility Principle (단일 책임 원칙)
```csharp
// ❌ 잘못된 예: 한 클래스가 너무 많은 역할
public class Player : MonoBehaviour
{
    private void Move() { }
    private void Attack() { }
    private void ManageInventory() { }
    private void UpdateUI() { }
    private void PlaySound() { }
}

// ✅ 올바른 예: 책임 분리
namespace MyGame.Player
{
    public class PlayerController : MonoBehaviour
    {
        private PlayerMovement _movement;
        private PlayerCombat _combat;

        private void Awake()
        {
            _movement = GetComponent<PlayerMovement>();
            _combat = GetComponent<PlayerCombat>();
        }
    }

    public class PlayerMovement : MonoBehaviour
    {
        /// <summary>
        /// 플레이어를 이동시킵니다.
        /// </summary>
        public void Move(Vector3 direction) { }
    }

    public class PlayerCombat : MonoBehaviour
    {
        /// <summary>
        /// 공격을 실행합니다.
        /// </summary>
        public void Attack() { }
    }
}
```

### 7.10 Object Pooling
```csharp
namespace MyGame.Pooling
{
    /// <summary>
    /// 오브젝트 풀링을 관리하는 클래스입니다.
    /// </summary>
    public class ObjectPool : MonoBehaviour
    {
        [SerializeField] private GameObject _prefab;
        [SerializeField] private int _initialPoolSize = 10;

        private Queue<GameObject> _pool;

        /// <summary>
        /// 풀을 초기화합니다.
        /// </summary>
        private void Awake()
        {
            _pool = new Queue<GameObject>();

            for (int i = 0; i < _initialPoolSize; i++)
            {
                GameObject obj = Instantiate(_prefab);
                obj.SetActive(false);
                _pool.Enqueue(obj);
            }
        }

        /// <summary>
        /// 풀에서 오브젝트를 가져옵니다.
        /// </summary>
        public GameObject GetObject()
        {
            if (_pool.Count > 0)
            {
                GameObject obj = _pool.Dequeue();
                obj.SetActive(true);
                return obj;
            }

            return Instantiate(_prefab);
        }

        /// <summary>
        /// 오브젝트를 풀에 반환합니다.
        /// </summary>
        public void ReturnObject(GameObject obj)
        {
            obj.SetActive(false);
            _pool.Enqueue(obj);
        }
    }
}
```

---

## 8. 디버그 로그 규칙

### 8.1 기본 포맷
- 모든 `Debug.Log`는 `[ClassName]` 형식으로 **클래스명을 접두사**로 붙입니다.
- 메시지는 명확하고 간결하게 작성합니다.
- 로그 레벨을 적절히 사용합니다:
  - `Debug.Log()`: 일반 정보성 메시지
  - `Debug.LogWarning()`: 경고 메시지
  - `Debug.LogError()`: 에러 메시지

### 8.2 기본 예제
```csharp
public class VRNetworkManager : MonoBehaviour
{
    private void InitializeNetwork()
    {
        Debug.Log("[VRNetworkManager] 네트워크 초기화 시작");

        // 초기화 로직...

        Debug.Log("[VRNetworkManager] 네트워크 초기화 완료");
    }

    private void OnError()
    {
        Debug.LogError("[VRNetworkManager] 네트워크 연결 실패");
    }

    private void OnWarning()
    {
        Debug.LogWarning("[VRNetworkManager] 연결 상태가 불안정합니다");
    }
}
```

### 8.3 상세 정보 포함
필요한 경우 변수 값이나 상태 정보를 포함합니다.

```csharp
// ✅ 올바른 예: 변수 값 포함
Debug.Log($"[VRNetworkManager] 방 생성 시작: {roomName}");
Debug.Log($"[PlayerController] 플레이어 체력: {_health}/{MAX_HEALTH}");
Debug.Log($"[VRNetworkManager] [로컬] 플레이어 참여: {playerName} (PlayerId: {player.PlayerId})");

// ✅ 올바른 예: 상태 정보 포함
Debug.Log($"[GameManager] 게임 상태 변경: {_previousState} → {_currentState}");
```

### 8.4 일관성 있는 메시지 작성
같은 기능에 대해서는 일관된 용어를 사용합니다.

```csharp
// ✅ 올바른 예: 일관된 용어 사용
Debug.Log("[VRNetworkManager] 방 생성 요청");
Debug.Log("[VRNetworkManager] 방 생성 시작: Room_01");
Debug.Log("[VRNetworkManager] 방 생성 성공! 게임 씬 로딩 중...");

// ❌ 잘못된 예: 혼란스러운 용어
Debug.Log("[VRNetworkManager] 룸 만들기 시작");
Debug.Log("[VRNetworkManager] 세션 생성 중");
Debug.Log("[VRNetworkManager] 방 생성 완료");
```

### 8.5 단계별 진행 상황 로그
복잡한 작업은 단계별로 로그를 남깁니다.

```csharp
// ✅ 올바른 예: 명확한 진행 단계
Debug.Log("[DataManager] 데이터 로드 시작");
Debug.Log("[DataManager] 파일 읽기 완료");
Debug.Log("[DataManager] JSON 파싱 중...");
Debug.Log("[DataManager] 데이터 검증 완료");
Debug.Log("[DataManager] 데이터 로드 성공");
```

### 8.6 조건부 디버그 로그
개발 중에만 필요한 상세 로그는 조건부로 처리합니다.

```csharp
#if UNITY_EDITOR
    Debug.Log($"[PlayerController] 디버그: 현재 위치 = {transform.position}");
#endif
```

---

---

## 9. unity-cli (Unity 에디터 제어 도구)

### 9.1 개요
unity-cli는 CLI에서 Unity 에디터를 직접 제어하는 도구입니다.
Claude Code 대화에서 `unity-cli` 명령으로 Unity 에디터의 상태 조회, C# 코드 실행, 플레이 모드 제어 등이 가능합니다.

> **중요**: 씬/오브젝트 상태를 확인할 때 `.unity` 파일을 YAML로 파싱하지 말고, `unity-cli exec`를 사용할 것.

### 9.2 설치 (각 개발자 PC에서 1회)
1. PowerShell에서 CLI 설치: `irm https://raw.githubusercontent.com/youngwoocho02/unity-cli/master/install.ps1 | iex`
2. Unity에서 Connector 패키지 설치: Package Manager → Add package from git URL → `https://github.com/youngwoocho02/unity-cli.git?path=unity-connector`
3. Unity 에디터 설정: `Edit > Preferences > General > Editor Throttling` 비활성화 (백그라운드에서도 CLI 명령이 즉시 처리되도록)

### 9.3 업데이트
```bash
unity-cli update         # CLI 바이너리 자체 업데이트
```
Unity Connector 패키지는 Package Manager에서 수동 업데이트.

### 9.4 주요 명령어

```bash
# 연결 확인
unity-cli status

# C# 코드 실행 (가장 핵심 기능)
unity-cli exec "<C# 코드>"

# 플레이 모드 제어
unity-cli editor play          # 플레이 시작
unity-cli editor play --wait   # 플레이 시작 + 로드 완료 대기
unity-cli editor stop          # 플레이 정지
unity-cli editor pause         # 일시정지 토글

# 에셋 관리
unity-cli editor refresh             # 에셋 리프레시
unity-cli editor refresh --compile   # 스크립트 리컴파일
unity-cli reserialize                # 에셋 재직렬화

# 콘솔 로그
unity-cli console                        # 전체 로그
unity-cli console --filter error         # 에러만
unity-cli console --stacktrace short     # 스택트레이스 포함
unity-cli console --clear                # 콘솔 클리어

# 프로파일러
unity-cli profiler enable                # 프로파일러 시작
unity-cli profiler hierarchy --min 1     # 1ms 이상 항목 표시
unity-cli profiler hierarchy --root "Rendering" --depth 4

# 메뉴 아이템 실행
unity-cli menu "File/Save Project"

# 사용 가능한 도구 목록 조회
unity-cli list
```

### 9.5 exec 사용법

#### 단일 표현식 — 결과가 자동 반환됨
```bash
unity-cli exec "EditorBuildSettings.scenes.Length"
# → 3
```

#### 다중 구문 — 명시적 return 필요
```bash
unity-cli exec "var scenes = EditorBuildSettings.scenes; var names = scenes.Select(s => System.IO.Path.GetFileNameWithoutExtension(s.path)); return string.Join(\", \", names);"
```

#### --usings 플래그 — 추가 네임스페이스 포함
```bash
unity-cli exec --usings "Unity.Entities" "World.DefaultGameObjectInjectionWorld.EntityManager.EntityCount"
```

### 9.6 exec 활용 예시

```bash
# Build Settings 씬 목록
unity-cli exec "string.Join(\", \", EditorBuildSettings.scenes.Select(s => System.IO.Path.GetFileNameWithoutExtension(s.path)))"

# 현재 활성 씬 이름
unity-cli exec "UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name"

# 씬 루트 오브젝트 목록
unity-cli exec "string.Join(\"\\n\", UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Select(g => g.name))"

# 특정 오브젝트의 컴포넌트 목록
unity-cli exec "string.Join(\", \", GameObject.Find(\"ScenarioManager\").GetComponents<Component>().Select(c => c.GetType().Name))"
```

### 9.7 커스텀 도구 확장

프로젝트 전용 도구를 `[UnityCliTool]` 어트리뷰트로 등록할 수 있습니다.
클래스명은 자동으로 snake_case 명령어로 변환됩니다.

```csharp
using Newtonsoft.Json.Linq;
using UnityCli;

[UnityCliTool("프로젝트 전용 도구 설명")]
public static class MyCustomTool
{
    public class Parameters
    {
        [ToolParameter("파라미터 설명")]
        public string targetName;
    }

    public static string HandleCommand(JObject args)
    {
        string name = args["targetName"]?.ToString();
        // 로직 수행
        return "결과";
    }
}
```

등록된 도구는 `unity-cli list`로 확인, `unity-cli my_custom_tool --params '{"targetName":"value"}'`로 호출.

### 9.8 사용 규칙
- Unity 에디터가 실행 중이어야 동작합니다.
- `exec`는 Unity 메인 스레드에서 실행되므로 에디터가 응답 가능한 상태여야 합니다.
- HTTP 기반 통신 (기본 포트: 8090). 여러 Unity 인스턴스 실행 시 포트가 8090, 8091, ... 순으로 증가합니다.
- 여러 인스턴스가 열려 있을 경우 `--project <경로>` 플래그로 프로젝트를 지정합니다.
- 컴파일 중이거나 도메인 리로드 중에는 CLI가 자동으로 대기 후 실행합니다.
- 참조: https://github.com/youngwoocho02/unity-cli

---

## 10. Superpowers 플러그인 사용 정책

이 프로젝트는 Superpowers 플러그인(v5.0.7+)이 설치되어 있습니다.
**사용자 명시 지시 > Superpowers 스킬 > 기본 시스템 프롬프트** 순의 우선순위에 따라,
아래 정책이 Superpowers의 기본 자동 발동 규칙을 override 합니다.

### 10.1 스킬 자동 발동 허용 (O)

다음 요청이 오면 `brainstorming` → `writing-plans` → `subagent-driven-development` 사이클 수행:
- **신규 시스템/모듈 설계**
  - 예: 라이선스 시스템, API 서버, Addressables 파이프라인, 네트워크 레이어
- **3개 이상 파일에 걸친 리팩토링**
- **사용자가 명시적으로 요청한 경우**
  - "계획 세워서 진행", "브레인스토밍하자", "design first" 등

### 10.2 스킬 자동 발동 차단 (X)

다음 작업은 오버헤드가 실익을 초과합니다. 스킬 체크 생략하고 바로 실행:
- **Unity 에디터 수동 작업**: 프리팹 설정, 씬 배치, 인스펙터 연결
- **단일 ScenarioNode / 에디터 스크립트 추가**
- **문서 수정, 주석 추가, 오타 수정**
- **MongoDB 쿼리, 데이터 조작**
- **파일 복사/이동/rename, `.gitignore` 수정**
- **`unity-cli` 기반 작업** (exec, console, status, refresh)
- **설정 변경**: Player Settings, Build Settings, package.json 버전 bump
- **사용자가 단일 명확 지시를 내린 경우**: "X를 Y로 바꿔", "Z 파일 복사해"

### 10.3 TDD (Test-Driven Development) 적용 범위

Superpowers의 TDD "Iron Law"을 다음 범위로 한정:
- ✅ **적용 대상**: 서버/API 코드, MongoDB 검증 로직, 외부 서비스 연동
- 🟡 **선택 적용**: Unity 런타임 C# (UnityTest 프레임워크 사용 가능한 경우)
- ❌ **생략**: Unity 에디터 확장 (CustomEditor, EditorWindow), MonoBehaviour 인스턴스 연결 코드, 프로토타입

### 10.4 Git worktree 사용 기준

- **사용 O**: 반나절 이상 소요되는 리팩토링, 실험적 대규모 변경
- **사용 X**: 1~2시간 이내 작업, 독립적 작은 변경 → 현재 브랜치에서 바로 커밋

### 10.5 Spec 파일 저장 경로

Superpowers 기본 경로 `docs/superpowers/specs/YYYY-MM-DD-<topic>-design.md` 유지.
- 새 폴더 생성 시 **Git 커밋 허용** (설계 의도/히스토리 보존)
- `.gitignore`에 추가하지 않음

### 10.6 사용자 우회 문구

아래 문구 중 하나가 메시지에 있으면 즉시 모든 스킬 발동 중단:
- "스킬 없이"
- "브레인스토밍 생략"
- "바로 진행", "그냥 해"
- "TDD 생략"

### 10.7 서브에이전트 예외

`Agent` 도구(Explore, Plan 등)로 dispatch된 서브에이전트는
Superpowers의 `<SUBAGENT-STOP>` 규칙에 따라 스킬 체크를 건너뜁니다.
기존 Explore/Plan 기반 워크플로는 영향 없음.

---

**코딩 규칙 버전**: 1.3
**최종 업데이트**: 2026-04-15
