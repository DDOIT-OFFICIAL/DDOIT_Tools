using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace DDOIT.Tools
{
    /// <summary>
    /// 사운드 타입 열거형.
    /// </summary>
    public enum SoundType
    {
        BGM,  // Background Music
        NAR,  // Narration
        UIS,  // UI Sound
        SFX   // Sound Effect
    }

    /// <summary>
    /// 모든 사운드를 관리하는 매니저.
    /// 통합 AudioSource 풀에서 사운드를 재생하며, Global/Scene 두 개의 SoundDatabase를 지원한다.
    /// AudioMixer를 통해 마스터/카테고리별 볼륨을 제어한다.
    /// BootstrapManager에서 Initialize() 호출 후 사용 가능.
    /// </summary>
    public class SoundManager : Singleton<SoundManager>
    {
        #region Serialized Fields

        [Header("Sound Database")]
        [Tooltip("프로젝트 전역 사운드 (BGM, UI 효과음 등) - Addressables")]
        [SerializeField] private AssetReference _globalDatabaseReference;

        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer _mixer;
        [SerializeField] private AudioMixerGroup _bgmMixerGroup;
        [SerializeField] private AudioMixerGroup _narMixerGroup;
        [SerializeField] private AudioMixerGroup _uisMixerGroup;
        [SerializeField] private AudioMixerGroup _sfxMixerGroup;

        [Header("Pool Settings")]
        [Tooltip("AudioSource 프리팹 (없으면 기본 AudioSource 생성)")]
        [SerializeField] private GameObject _audioSourcePrefab;
        [SerializeField] private int _poolSize = 20;

        #endregion

        #region Constants

        private const string MASTER_VOLUME_PARAM = "MasterVolume";

        private static readonly Dictionary<SoundType, string> VOLUME_PARAMS = new()
        {
            { SoundType.BGM, "BGMVolume" },
            { SoundType.NAR, "NARVolume" },
            { SoundType.UIS, "UISVolume" },
            { SoundType.SFX, "SFXVolume" }
        };

        #endregion

        #region Private Fields

        private SoundDatabase _globalDatabase;
        private AsyncOperationHandle<SoundDatabase> _globalDatabaseHandle;
        private SoundDatabase _sceneDatabase;
        private Queue<AudioSource> _pool;
        private Dictionary<AudioSource, SoundType> _activeSources;
        private Dictionary<string, List<AudioSource>> _trackedSources;
        private Dictionary<GameObject, List<AudioSource>> _ownerSources;
        private Dictionary<SoundType, AudioMixerGroup> _mixerGroups;
        private AudioSource _currentNarration;

        #endregion

        #region Properties

        public bool IsReady { get; private set; }

        #endregion

        #region Initialization

        /// <summary>
        /// BootstrapManager에서 호출. 풀 생성, 딕셔너리 초기화, Global SoundDatabase 로드.
        /// </summary>
        public IEnumerator Initialize()
        {
            _pool = new Queue<AudioSource>();
            _activeSources = new Dictionary<AudioSource, SoundType>();
            _trackedSources = new Dictionary<string, List<AudioSource>>();
            _ownerSources = new Dictionary<GameObject, List<AudioSource>>();

            _mixerGroups = new Dictionary<SoundType, AudioMixerGroup>
            {
                { SoundType.BGM, _bgmMixerGroup },
                { SoundType.NAR, _narMixerGroup },
                { SoundType.UIS, _uisMixerGroup },
                { SoundType.SFX, _sfxMixerGroup }
            };

            for (int i = 0; i < _poolSize; i++)
            {
                AudioSource source = CreatePooledSource(i);
                if (source != null)
                    _pool.Enqueue(source);
            }

            // Global SoundDatabase 비동기 로드
            if (_globalDatabaseReference != null && _globalDatabaseReference.RuntimeKeyIsValid())
            {
                _globalDatabaseHandle = Addressables.LoadAssetAsync<SoundDatabase>(_globalDatabaseReference);
                yield return _globalDatabaseHandle;

                if (_globalDatabaseHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    _globalDatabase = _globalDatabaseHandle.Result;
                    Debug.Log("[SoundManager] Global SoundDatabase 로드 완료");
                }
                else
                {
                    Debug.LogError("[SoundManager] Global SoundDatabase 로드 실패");
                }
            }

            IsReady = true;
        }

        protected override void OnDestroy()
        {
            if (_globalDatabaseHandle.IsValid())
                Addressables.Release(_globalDatabaseHandle);

            base.OnDestroy();
        }

        private AudioSource CreatePooledSource(int index)
        {
            GameObject obj;

            if (_audioSourcePrefab != null)
            {
                obj = Instantiate(_audioSourcePrefab, transform);
            }
            else
            {
                obj = new GameObject();
                obj.transform.SetParent(transform);
                obj.AddComponent<AudioSource>();
            }

            obj.name = $"AudioSource_{index}";

            AudioSource source = obj.GetComponent<AudioSource>();
            source.playOnAwake = false;
            obj.SetActive(false);
            return source;
        }

        #endregion

        #region Database

        /// <summary>
        /// 씬 전용 SoundDatabase 설정. 씬 로드 시 호출.
        /// </summary>
        public void SetSceneDatabase(SoundDatabase database)
        {
            _sceneDatabase = database;
        }

        /// <summary>
        /// 씬 전용 SoundDatabase 해제. 씬 언로드 시 호출.
        /// </summary>
        public void ClearSceneDatabase()
        {
            _sceneDatabase = null;
        }

        private SoundDatabase.SoundData FindSoundData(string soundName)
        {
            // Scene DB 우선 탐색 (씬별 오버라이드 가능)
            if (_sceneDatabase != null && _sceneDatabase.TryGetSoundData(soundName, out var sceneData))
                return sceneData;

            // Global DB 탐색
            if (_globalDatabase != null && _globalDatabase.TryGetSoundData(soundName, out var globalData))
                return globalData;

            Debug.LogWarning($"[SoundManager] '{soundName}' 사운드를 찾을 수 없습니다!");
            return null;
        }

        #endregion

        #region Play

        /// <summary>
        /// 이름으로 사운드 재생 (2D).
        /// </summary>
        public AudioSource PlaySound(string soundName, bool? forceLoop = null, GameObject owner = null)
        {
            var data = FindSoundData(soundName);
            if (data == null || data.clip == null) return null;

            if (data.soundType == SoundType.NAR)
                StopCurrentNarration();

            AudioSource source = AcquireSource();
            if (source == null) return null;

            source.clip = data.clip;
            source.volume = data.defaultVolume;
            source.spatialBlend = 0f;
            source.loop = forceLoop ?? data.loop;
            AssignMixerGroup(source, data.soundType);
            source.gameObject.SetActive(true);
            source.Play();

            Register(source, data, soundName, owner);
            return source;
        }

        /// <summary>
        /// 이름으로 사운드 재생 (3D - Vector3).
        /// </summary>
        public AudioSource PlaySound(string soundName, Vector3 position, bool? forceLoop = null, GameObject owner = null)
        {
            var data = FindSoundData(soundName);
            if (data == null || data.clip == null) return null;

            if (data.soundType == SoundType.NAR)
                StopCurrentNarration();

            AudioSource source = AcquireSource();
            if (source == null) return null;

            source.transform.position = position;
            source.clip = data.clip;
            source.volume = data.defaultVolume;
            source.spatialBlend = 1f;
            source.minDistance = 1f;
            source.maxDistance = 20f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.loop = forceLoop ?? data.loop;
            AssignMixerGroup(source, data.soundType);
            source.gameObject.SetActive(true);
            source.Play();

            Register(source, data, soundName, owner);
            return source;
        }

        /// <summary>
        /// 이름으로 사운드 재생 (3D - Transform).
        /// </summary>
        public AudioSource PlaySound(string soundName, Transform target, bool? forceLoop = null, GameObject owner = null)
        {
            if (target == null) return null;
            return PlaySound(soundName, target.position, forceLoop, owner);
        }

        /// <summary>
        /// UnityEvent용 간편 재생.
        /// </summary>
        public void PlaySoundSimple(string soundName)
        {
            PlaySound(soundName);
        }

        #endregion

        #region Stop

        /// <summary>
        /// 이름으로 해당 사운드의 모든 인스턴스 정지.
        /// </summary>
        public void StopSound(string soundName)
        {
            if (!_trackedSources.TryGetValue(soundName, out var sources)) return;

            var copy = new List<AudioSource>(sources);
            foreach (var source in copy)
            {
                if (source != null)
                    StopSound(source);
            }
        }

        /// <summary>
        /// AudioSource 정지 후 풀 반환.
        /// </summary>
        public void StopSound(AudioSource source)
        {
            if (source == null || !_activeSources.ContainsKey(source)) return;

            if (_currentNarration == source)
                _currentNarration = null;

            source.Stop();
            ReturnToPool(source);
        }

        /// <summary>
        /// 특정 GameObject가 재생한 모든 사운드 정지.
        /// </summary>
        public void StopSound(GameObject owner)
        {
            if (owner == null || !_ownerSources.TryGetValue(owner, out var sources)) return;

            var copy = new List<AudioSource>(sources);
            foreach (var source in copy)
            {
                if (source != null)
                    StopSound(source);
            }
        }

        /// <summary>
        /// 특정 타입의 모든 사운드 정지.
        /// </summary>
        public void StopAllSounds(SoundType type)
        {
            var toStop = new List<AudioSource>();
            foreach (var kvp in _activeSources)
            {
                if (kvp.Value == type)
                    toStop.Add(kvp.Key);
            }

            foreach (var source in toStop)
                StopSound(source);
        }

        /// <summary>
        /// 모든 사운드 정지.
        /// </summary>
        public void StopAll()
        {
            var toStop = new List<AudioSource>(_activeSources.Keys);
            foreach (var source in toStop)
                StopSound(source);
        }

        /// <summary>
        /// 현재 재생 중인 나레이션 정지.
        /// </summary>
        public void StopCurrentNarration()
        {
            if (_currentNarration != null && _currentNarration.isPlaying)
            {
                StopSound(_currentNarration);
                _currentNarration = null;
            }
        }

        #endregion

        #region Pause / Resume

        /// <summary>
        /// 모든 사운드 일시정지.
        /// </summary>
        public void PauseAll()
        {
            foreach (var kvp in _activeSources)
            {
                if (kvp.Key != null && kvp.Key.isPlaying)
                    kvp.Key.Pause();
            }
        }

        /// <summary>
        /// 특정 타입의 모든 사운드 일시정지.
        /// </summary>
        public void PauseAll(SoundType type)
        {
            foreach (var kvp in _activeSources)
            {
                if (kvp.Value == type && kvp.Key != null && kvp.Key.isPlaying)
                    kvp.Key.Pause();
            }
        }

        /// <summary>
        /// 모든 사운드 재개.
        /// </summary>
        public void ResumeAll()
        {
            foreach (var kvp in _activeSources)
            {
                if (kvp.Key != null && !kvp.Key.isPlaying)
                    kvp.Key.UnPause();
            }
        }

        /// <summary>
        /// 특정 타입의 모든 사운드 재개.
        /// </summary>
        public void ResumeAll(SoundType type)
        {
            foreach (var kvp in _activeSources)
            {
                if (kvp.Value == type && kvp.Key != null && !kvp.Key.isPlaying)
                    kvp.Key.UnPause();
            }
        }

        #endregion

        #region Mixer Volume

        /// <summary>
        /// 마스터 볼륨 설정 (0~1).
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            if (_mixer != null)
                _mixer.SetFloat(MASTER_VOLUME_PARAM, VolumeToDecibel(volume));
        }

        /// <summary>
        /// 카테고리별 볼륨 설정 (0~1).
        /// </summary>
        public void SetCategoryVolume(SoundType type, float volume)
        {
            if (_mixer != null && VOLUME_PARAMS.TryGetValue(type, out var param))
                _mixer.SetFloat(param, VolumeToDecibel(volume));
        }

        /// <summary>
        /// 현재 마스터 볼륨 조회 (0~1).
        /// </summary>
        public float GetMasterVolume()
        {
            if (_mixer != null && _mixer.GetFloat(MASTER_VOLUME_PARAM, out float dB))
                return DecibelToVolume(dB);
            return 1f;
        }

        /// <summary>
        /// 현재 카테고리 볼륨 조회 (0~1).
        /// </summary>
        public float GetCategoryVolume(SoundType type)
        {
            if (_mixer != null && VOLUME_PARAMS.TryGetValue(type, out var param)
                && _mixer.GetFloat(param, out float dB))
                return DecibelToVolume(dB);
            return 1f;
        }

        private float VolumeToDecibel(float volume)
        {
            return Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20f;
        }

        private float DecibelToVolume(float dB)
        {
            return Mathf.Pow(10f, dB / 20f);
        }

        #endregion

        #region Sound Volume

        /// <summary>
        /// 해당 이름의 모든 인스턴스 볼륨 즉시 설정.
        /// </summary>
        public void SetVolume(string soundName, float volume)
        {
            if (!_trackedSources.TryGetValue(soundName, out var sources)) return;

            volume = Mathf.Clamp01(volume);
            foreach (var source in sources)
            {
                if (source != null && source.isPlaying)
                    source.volume = volume;
            }
        }

        /// <summary>
        /// 해당 이름의 모든 인스턴스 볼륨 페이드.
        /// </summary>
        public void FadeVolume(string soundName, float targetVolume, float duration)
        {
            if (!_trackedSources.TryGetValue(soundName, out var sources)) return;

            foreach (var source in sources)
            {
                if (source != null && source.isPlaying)
                    StartCoroutine(FadeCoroutine(source, targetVolume, duration));
            }
        }

        /// <summary>
        /// 해당 이름의 모든 인스턴스 페이드 아웃 후 정지.
        /// </summary>
        public void FadeOutAndStop(string soundName, float duration)
        {
            if (!_trackedSources.TryGetValue(soundName, out var sources)) return;

            var copy = new List<AudioSource>(sources);
            foreach (var source in copy)
            {
                if (source != null && source.isPlaying)
                    StartCoroutine(FadeOutAndStopCoroutine(source, duration));
            }
        }

        /// <summary>
        /// 해당 이름의 재생 중인 인스턴스 중 첫 번째의 볼륨 조회. 없으면 -1.
        /// </summary>
        public float GetVolume(string soundName)
        {
            if (!_trackedSources.TryGetValue(soundName, out var sources)) return -1f;

            foreach (var source in sources)
            {
                if (source != null && source.isPlaying)
                    return source.volume;
            }
            return -1f;
        }

        /// <summary>
        /// 해당 이름의 인스턴스가 하나라도 재생 중인지 확인.
        /// </summary>
        public bool IsPlaying(string soundName)
        {
            if (!_trackedSources.TryGetValue(soundName, out var sources)) return false;

            foreach (var source in sources)
            {
                if (source != null && source.isPlaying)
                    return true;
            }
            return false;
        }

        #endregion

        #region Utility

        /// <summary>
        /// 사운드 이름으로 AudioClip 길이(초) 조회.
        /// </summary>
        public float GetSoundLength(string soundName)
        {
            var data = FindSoundData(soundName);
            return data?.clip != null ? data.clip.length : 0f;
        }

        /// <summary>
        /// 재생 중인 AudioSource의 남은 재생 시간.
        /// </summary>
        public float GetRemainingTime(AudioSource source)
        {
            if (source == null || source.clip == null || !source.isPlaying) return 0f;
            return Mathf.Max(0f, source.clip.length - source.time);
        }

        #endregion

        #region Private - Pool

        private AudioSource AcquireSource()
        {
            if (_pool.Count > 0)
                return _pool.Dequeue();

            return StealActiveSource();
        }

        private AudioSource StealActiveSource()
        {
            AudioSource candidate = null;
            float leastRemaining = float.MaxValue;

            // 비루프 사운드 중 남은 시간이 가장 짧은 것 탐색
            foreach (var kvp in _activeSources)
            {
                var source = kvp.Key;
                if (source == null || source.loop) continue;

                float remaining = source.clip != null
                    ? source.clip.length - source.time
                    : 0f;

                if (remaining < leastRemaining)
                {
                    leastRemaining = remaining;
                    candidate = source;
                }
            }

            // 비루프가 없으면 루프 사운드라도 강탈
            if (candidate == null)
            {
                foreach (var kvp in _activeSources)
                {
                    if (kvp.Key != null)
                    {
                        candidate = kvp.Key;
                        break;
                    }
                }
            }

            if (candidate != null)
            {
                Debug.LogWarning($"[SoundManager] 풀 소진 - '{candidate.clip?.name}' 강제 정지");
                if (_currentNarration == candidate)
                    _currentNarration = null;
                candidate.Stop();
                ReturnToPool(candidate);

                if (_pool.Count > 0)
                    return _pool.Dequeue();
            }

            Debug.LogError("[SoundManager] 사용 가능한 AudioSource가 없습니다!");
            return null;
        }

        private void AssignMixerGroup(AudioSource source, SoundType type)
        {
            if (_mixerGroups.TryGetValue(type, out var group))
                source.outputAudioMixerGroup = group;
        }

        private void Register(AudioSource source, SoundDatabase.SoundData data, string soundName, GameObject owner)
        {
            _activeSources[source] = data.soundType;

            if (!_trackedSources.ContainsKey(soundName))
                _trackedSources[soundName] = new List<AudioSource>();
            _trackedSources[soundName].Add(source);

            if (data.soundType == SoundType.NAR)
                _currentNarration = source;

            if (owner != null)
            {
                if (!_ownerSources.ContainsKey(owner))
                    _ownerSources[owner] = new List<AudioSource>();
                _ownerSources[owner].Add(source);
            }

            if (!source.loop)
                StartCoroutine(AutoReturnCoroutine(source));
        }

        private void ReturnToPool(AudioSource source)
        {
            if (source == null) return;

            source.Stop();
            source.clip = null;
            source.loop = false;
            source.spatialBlend = 0f;
            source.outputAudioMixerGroup = null;
            source.gameObject.SetActive(false);

            _activeSources.Remove(source);

            // 이름 추적 정리
            string nameToClean = null;
            foreach (var kvp in _trackedSources)
            {
                if (kvp.Value.Remove(source))
                {
                    if (kvp.Value.Count == 0)
                        nameToClean = kvp.Key;
                    break;
                }
            }
            if (nameToClean != null)
                _trackedSources.Remove(nameToClean);

            // Owner 추적 정리
            GameObject ownerToClean = null;
            foreach (var kvp in _ownerSources)
            {
                if (kvp.Value.Remove(source))
                {
                    if (kvp.Value.Count == 0)
                        ownerToClean = kvp.Key;
                    break;
                }
            }
            if (ownerToClean != null)
                _ownerSources.Remove(ownerToClean);

            _pool.Enqueue(source);
        }

        private IEnumerator AutoReturnCoroutine(AudioSource source)
        {
            while (source != null && source.isPlaying)
                yield return null;

            if (source != null && _activeSources.ContainsKey(source))
                ReturnToPool(source);
        }

        #endregion

        #region Private - Fade

        private IEnumerator FadeCoroutine(AudioSource source, float targetVolume, float duration)
        {
            if (source == null) yield break;

            float startVolume = source.volume;
            float elapsed = 0f;
            targetVolume = Mathf.Clamp01(targetVolume);

            while (elapsed < duration)
            {
                if (source == null || !source.isPlaying) yield break;

                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
                yield return null;
            }

            if (source != null)
                source.volume = targetVolume;
        }

        private IEnumerator FadeOutAndStopCoroutine(AudioSource source, float duration)
        {
            yield return FadeCoroutine(source, 0f, duration);

            if (source != null)
                StopSound(source);
        }

        #endregion
    }
}
