using UnityEditor;
using UnityEngine;

using DDOIT.Tools.Data;
using DDOIT.Tools.Scenario.Nodes;

namespace DDOIT.Tools.Editor
{
    [CustomEditor(typeof(SoundNode))]
    [CanEditMultipleObjects]
    public class SoundNodeEditor : UnityEditor.Editor
    {
        #region Private Fields

        private SerializedProperty _conditionGroup;
        private SerializedProperty _soundName;
        private SerializedProperty _stopOnRelease;
        private SerializedProperty _onEnd;

        private AudioClip _previewClip;
        private bool _isPlaying;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            _conditionGroup = serializedObject.FindProperty("_conditionGroup");
            _soundName = serializedObject.FindProperty("_soundName");
            _stopOnRelease = serializedObject.FindProperty("_stopOnRelease");
            _onEnd = serializedObject.FindProperty("_onEnd");
        }

        private void OnDisable()
        {
            StopPreview();
        }

        #endregion

        #region Inspector

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (ConditionGroupDrawer.DrawMultiObjectExecutionOnly(serializedObject))
                return;

            bool executionDisabled = ConditionGroupDrawer.DrawExecutionToggle(serializedObject, (MonoBehaviour)target);
            EditorGUILayout.Space(4);

            ConditionGroupDrawer.Draw(_conditionGroup, (MonoBehaviour)target);
            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField("사운드 설정", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_soundName, new GUIContent("사운드 이름"));
            EditorGUILayout.PropertyField(_stopOnRelease, new GUIContent("Step 종료 시 정지"));

            if (string.IsNullOrEmpty(_soundName.stringValue))
            {
                if (!executionDisabled)
                    EditorGUILayout.HelpBox("사운드가 선택되지 않았습니다.", MessageType.Warning);
            }
            else
            {
                DrawSoundInfoAndPreview();
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(_onEnd, new GUIContent("재생 완료 이벤트"));

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSoundInfoAndPreview()
        {
            SoundDatabase.SoundData data = FindSoundData(_soundName.stringValue);

            if (data == null)
            {
                EditorGUILayout.HelpBox(
                    $"'{_soundName.stringValue}' 사운드를 찾을 수 없습니다. SoundDatabase를 확인하세요.",
                    MessageType.Warning);
                return;
            }

            if (data.clip == null)
            {
                EditorGUILayout.HelpBox(
                    $"'{_soundName.stringValue}' 사운드는 등록되어 있지만 AudioClip이 비어 있습니다.",
                    MessageType.Warning);
                return;
            }

            DrawAuthoringWarnings(data);
            DrawPreviewButtons(data.clip);
        }

        private void DrawAuthoringWarnings(SoundDatabase.SoundData data)
        {
            if (_conditionGroup.intValue > 0 && data.loop)
            {
                EditorGUILayout.HelpBox(
                    "Loop 사운드는 자연 종료 시점이 없으므로 Step 조건을 자동 완료할 수 없습니다. " +
                    "조건 완료가 필요하면 loop가 아닌 사운드를 사용하거나 다른 조건 노드/이벤트로 완료를 처리하세요.",
                    MessageType.Error);
            }

            if (!_stopOnRelease.boolValue && data.loop)
            {
                EditorGUILayout.HelpBox(
                    "Step 종료 시 정지가 꺼져 있으므로 이 loop 사운드는 Step이 끝난 뒤에도 계속 재생될 수 있습니다.",
                    MessageType.Info);
            }
        }

        private void DrawPreviewButtons(AudioClip clip)
        {
            if (_previewClip != clip)
            {
                StopPreview();
                _previewClip = clip;
            }

            EditorGUILayout.BeginHorizontal();

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

            if (_isPlaying)
            {
                if (!IsPreviewPlaying())
                    _isPlaying = false;

                Repaint();
            }
        }

        private static SoundDatabase.SoundData FindSoundData(string soundName)
        {
            if (string.IsNullOrEmpty(soundName)) return null;

            string[] guids = AssetDatabase.FindAssets("t:SoundDatabase");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                SoundDatabase db = AssetDatabase.LoadAssetAtPath<SoundDatabase>(path);
                if (db == null) continue;

                db.BuildCache();
                if (db.TryGetSoundData(soundName, out SoundDatabase.SoundData data))
                    return data;
            }

            return null;
        }

        #endregion

        #region Editor Audio Preview

        private static System.Type GetAudioUtilClass()
        {
            return typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
        }

        private static void PlayClipInternal(AudioClip clip)
        {
            StopClipInternal();

            System.Type audioUtil = GetAudioUtilClass();
            if (audioUtil == null) return;

            System.Reflection.MethodInfo playMethod = audioUtil.GetMethod(
                "PlayPreviewClip",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
                null,
                new System.Type[] { typeof(AudioClip), typeof(int), typeof(bool) },
                null);

            playMethod?.Invoke(null, new object[] { clip, 0, false });
        }

        private static void StopClipInternal()
        {
            System.Type audioUtil = GetAudioUtilClass();
            if (audioUtil == null) return;

            System.Reflection.MethodInfo stopMethod = audioUtil.GetMethod(
                "StopAllPreviewClips",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

            stopMethod?.Invoke(null, null);
        }

        private static bool IsPreviewPlaying()
        {
            System.Type audioUtil = GetAudioUtilClass();
            if (audioUtil == null) return false;

            System.Reflection.MethodInfo method = audioUtil.GetMethod(
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
