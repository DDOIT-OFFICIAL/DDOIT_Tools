using UnityEditor;
using UnityEngine;

namespace DDOIT.Tools.Editor
{
    [CustomEditor(typeof(SoundNode))]
    public class SoundNodeEditor : UnityEditor.Editor
    {
        private SerializedProperty _isStepCondition;
        private SerializedProperty _onRelease;
        private SerializedProperty _soundName;

        private AudioClip _previewClip;
        private bool _isPlaying;

        private void OnEnable()
        {
            _isStepCondition = serializedObject.FindProperty("_isStepCondition");
            _onRelease = serializedObject.FindProperty("_onRelease");
            _soundName = serializedObject.FindProperty("_soundName");
        }

        private void OnDisable()
        {
            StopPreview();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_isStepCondition, new GUIContent("Step 조건"));
            EditorGUILayout.PropertyField(_onRelease);
            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField("사운드 설정", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_soundName, new GUIContent("사운드 이름"));

            if (string.IsNullOrEmpty(_soundName.stringValue))
            {
                EditorGUILayout.HelpBox("사운드가 선택되지 않았습니다.", MessageType.Warning);
            }
            else
            {
                DrawPreviewButtons();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPreviewButtons()
        {
            // 현재 사운드 이름에 해당하는 AudioClip 찾기
            var clip = FindClip(_soundName.stringValue);

            if (clip == null)
            {
                EditorGUILayout.HelpBox(
                    $"'{_soundName.stringValue}' 클립을 찾을 수 없습니다. SoundDatabase를 확인하세요.",
                    MessageType.Warning);
                return;
            }

            // 클립이 변경되면 기존 재생 중지
            if (_previewClip != clip)
            {
                StopPreview();
                _previewClip = clip;
            }

            EditorGUILayout.BeginHorizontal();

            // 재생 중이면 초록색 표시
            bool wasEnabled = GUI.enabled;
            if (_isPlaying)
                GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);

            if (GUILayout.Button(_isPlaying ? "재생 중..." : "재생", GUILayout.Height(24)))
            {
                StopPreview();
                StartPreview(clip);
            }

            GUI.backgroundColor = Color.white;

            GUI.enabled = _isPlaying;
            if (GUILayout.Button("정지", GUILayout.Height(24)))
            {
                StopPreview();
            }
            GUI.enabled = wasEnabled;

            EditorGUILayout.EndHorizontal();

            // 재생 중이면 Inspector 지속 갱신 (버튼 상태 업데이트)
            if (_isPlaying)
            {
                if (!IsPreviewPlaying())
                    _isPlaying = false;

                Repaint();
            }
        }

        private static AudioClip FindClip(string soundName)
        {
            if (string.IsNullOrEmpty(soundName)) return null;

            var guids = AssetDatabase.FindAssets("t:SoundDatabase");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var db = AssetDatabase.LoadAssetAtPath<SoundDatabase>(path);
                if (db == null) continue;

                var clip = db.GetClip(soundName);
                if (clip != null) return clip;
            }

            return null;
        }

        #region Editor Audio Preview

        private static System.Type GetAudioUtilClass()
        {
            return typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
        }

        private static void PlayClipInternal(AudioClip clip)
        {
            StopClipInternal();

            var audioUtil = GetAudioUtilClass();
            if (audioUtil == null) return;

            var playMethod = audioUtil.GetMethod(
                "PlayPreviewClip",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
                null,
                new System.Type[] { typeof(AudioClip), typeof(int), typeof(bool) },
                null);

            playMethod?.Invoke(null, new object[] { clip, 0, false });
        }

        private static void StopClipInternal()
        {
            var audioUtil = GetAudioUtilClass();
            if (audioUtil == null) return;

            var stopMethod = audioUtil.GetMethod(
                "StopAllPreviewClips",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

            stopMethod?.Invoke(null, null);
        }

        private static bool IsPreviewPlaying()
        {
            var audioUtil = GetAudioUtilClass();
            if (audioUtil == null) return false;

            var method = audioUtil.GetMethod(
                "IsPreviewClipPlaying",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

            if (method == null) return false;
            return (bool)method.Invoke(null, null);
        }

        private void StopPreview()
        {
            StopClipInternal();
            _isPlaying = false;
        }

        private void StartPreview(AudioClip clip)
        {
            PlayClipInternal(clip);
            _isPlaying = true;
        }

        #endregion
    }
}
