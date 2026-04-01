using System.Collections.Generic;
using UnityEngine;

namespace DDOIT.Tools
{
    /// <summary>
    /// 사운드 데이터를 저장하는 ScriptableObject.
    /// Inspector에서 사운드를 등록하면 Dictionary 캐싱으로 O(1) 조회.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSoundDatabase", menuName = "DDOIT/Sound Database")]
    public class SoundDatabase : ScriptableObject
    {
        [System.Serializable]
        public class SoundData
        {
            public string soundName;
            public AudioClip clip;
            [Range(0f, 1f)] public float defaultVolume = 1f;
            public bool loop;
            [HideInInspector] public SoundType soundType;
        }

        [Header("Background Music")]
        [SerializeField] private SoundData[] _bgmSounds;

        [Header("Narration")]
        [SerializeField] private SoundData[] _narSounds;

        [Header("UI Sounds")]
        [SerializeField] private SoundData[] _uisSounds;

        [Header("Sound Effects")]
        [SerializeField] private SoundData[] _sfxSounds;

        private Dictionary<string, SoundData> _lookupCache;

        private void OnEnable()
        {
            BuildCache();
        }

        /// <summary>
        /// Dictionary 캐시를 빌드한다. OnEnable에서 자동 호출되며,
        /// 런타임 중 데이터 변경 시 수동 호출 가능.
        /// </summary>
        public void BuildCache()
        {
            _lookupCache = new Dictionary<string, SoundData>();
            CacheArray(_bgmSounds, SoundType.BGM);
            CacheArray(_narSounds, SoundType.NAR);
            CacheArray(_uisSounds, SoundType.UIS);
            CacheArray(_sfxSounds, SoundType.SFX);
        }

        private void CacheArray(SoundData[] array, SoundType type)
        {
            if (array == null) return;

            foreach (var sound in array)
            {
                if (string.IsNullOrEmpty(sound.soundName)) continue;

                sound.soundType = type;

                if (_lookupCache.ContainsKey(sound.soundName))
                {
                    Debug.LogWarning($"[SoundDatabase] 중복 사운드 이름: '{sound.soundName}'");
                    continue;
                }

                _lookupCache.Add(sound.soundName, sound);
            }
        }

        /// <summary>
        /// 이름으로 사운드 데이터 조회 (O(1)). 실패 시 경고 로그 출력.
        /// </summary>
        public SoundData GetSoundData(string soundName)
        {
            if (_lookupCache == null) BuildCache();

            if (_lookupCache.TryGetValue(soundName, out var data))
                return data;

            Debug.LogWarning($"[SoundDatabase] '{soundName}' 사운드를 찾을 수 없습니다!");
            return null;
        }

        /// <summary>
        /// 이름으로 사운드 데이터 조회 (O(1)). 실패해도 경고 없음.
        /// 복수 DB 탐색 시 사용.
        /// </summary>
        public bool TryGetSoundData(string soundName, out SoundData data)
        {
            if (_lookupCache == null) BuildCache();
            return _lookupCache.TryGetValue(soundName, out data);
        }

        /// <summary>
        /// 이름으로 AudioClip 조회.
        /// </summary>
        public AudioClip GetClip(string soundName)
        {
            return GetSoundData(soundName)?.clip;
        }
    }
}
