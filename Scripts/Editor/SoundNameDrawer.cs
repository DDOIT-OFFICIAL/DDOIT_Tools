using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DDOIT.Tools.Editor
{
    [CustomPropertyDrawer(typeof(SoundNameAttribute))]
    public class SoundNameDrawer : PropertyDrawer
    {
        private static List<SoundEntry> _cachedSounds;
        private static bool _cacheInvalid = true;

        internal static void InvalidateCache() => _cacheInvalid = true;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            if (_cacheInvalid || _cachedSounds == null)
            {
                _cachedSounds = CollectAllSounds();
                _cacheInvalid = false;
            }

            var sounds = _cachedSounds;

            if (sounds.Count == 0)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);
            var rect = EditorGUI.PrefixLabel(position, label);

            string buttonText = string.IsNullOrEmpty(property.stringValue)
                ? "(선택)"
                : property.stringValue;

            if (EditorGUI.DropdownButton(rect, new GUIContent(buttonText), FocusType.Keyboard))
                ShowDropdown(rect, property, sounds);

            EditorGUI.EndProperty();
        }

        private void ShowDropdown(Rect rect, SerializedProperty property, List<SoundEntry> sounds)
        {
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("(없음)"), string.IsNullOrEmpty(property.stringValue), () =>
            {
                Undo.RecordObject(property.serializedObject.targetObject, "Change Sound");
                property.stringValue = "";
                property.serializedObject.ApplyModifiedProperties();
            });

            // DB별로 그룹화 (Global DB 상단 고정)
            var dbGroups = sounds.GroupBy(s => s.dbName)
                .OrderByDescending(g => g.Key.Contains("Global"))
                .ThenBy(g => g.Key);

            foreach (var group in dbGroups)
            {
                menu.AddSeparator("");
                foreach (var sound in group)
                {
                    string path = $"{group.Key}/{sound.type}/{sound.name}";
                    bool selected = sound.name == property.stringValue;
                    var captured = sound;
                    menu.AddItem(new GUIContent(path), selected, () =>
                    {
                        Undo.RecordObject(property.serializedObject.targetObject, "Change Sound");
                        property.stringValue = captured.name;
                        property.serializedObject.ApplyModifiedProperties();
                    });
                }
            }

            menu.DropDown(rect);
        }

        #region Sound Collection

        private struct SoundEntry
        {
            public string name;
            public string type;
            public string dbName;
        }

        private static List<SoundEntry> CollectAllSounds()
        {
            var result = new List<SoundEntry>();
            var guids = AssetDatabase.FindAssets("t:SoundDatabase");

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var db = AssetDatabase.LoadAssetAtPath<SoundDatabase>(path);
                if (db == null) continue;

                using var so = new SerializedObject(db);
                CollectFromArray(so, "_bgmSounds", "BGM", db.name, result);
                CollectFromArray(so, "_narSounds", "NAR", db.name, result);
                CollectFromArray(so, "_uisSounds", "UIS", db.name, result);
                CollectFromArray(so, "_sfxSounds", "SFX", db.name, result);
            }

            return result;
        }

        private static void CollectFromArray(SerializedObject so, string arrayName, string type,
            string dbName, List<SoundEntry> result)
        {
            var array = so.FindProperty(arrayName);
            if (array == null || !array.isArray) return;

            for (int i = 0; i < array.arraySize; i++)
            {
                var element = array.GetArrayElementAtIndex(i);
                var nameProperty = element.FindPropertyRelative("soundName");
                if (nameProperty == null || string.IsNullOrEmpty(nameProperty.stringValue)) continue;

                result.Add(new SoundEntry
                {
                    name = nameProperty.stringValue,
                    type = type,
                    dbName = dbName
                });
            }
        }

        #endregion
    }

    public class SoundNameCachePostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            foreach (var path in imported)
            {
                if (path.EndsWith(".asset") &&
                    AssetDatabase.LoadAssetAtPath<SoundDatabase>(path) != null)
                {
                    SoundNameDrawer.InvalidateCache();
                    return;
                }
            }

            foreach (var path in deleted)
            {
                if (path.EndsWith(".asset"))
                {
                    SoundNameDrawer.InvalidateCache();
                    return;
                }
            }
        }
    }
}
