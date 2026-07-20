using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

using DDOIT.Tools.Settings;
using DDOIT.Tools.UI;

namespace DDOIT.Tools.Editor
{
    internal static class DDOITEditorAssetLocator
    {
        private const string DEV_ROOT = "Assets/DDOIT_Tools";
        private const string DEV_DATA_PATH = DEV_ROOT + "/Data";
        private const string DEV_PANEL_PREFAB_PATH = DEV_ROOT + "/Prefabs/UIPanel.prefab";
        private const string UPM_ROOT = "Packages/com.ddoit.tools";
        private const string UPM_DATA_PATH = UPM_ROOT + "/Data";
        private const string UPM_PANEL_PREFAB_PATH = UPM_ROOT + "/Prefabs/UIPanel.prefab";
        private const string PROJECT_SETTINGS_PATH = "Assets/Settings/DDOIT";

        public static string WritableSettingsFolder
        {
            get
            {
                if (IsDevelopmentSourceMode && AssetDatabase.IsValidFolder(DEV_DATA_PATH))
                    return DEV_DATA_PATH;

                return PROJECT_SETTINGS_PATH;
            }
        }

        public static UIGlobalSettings FindUIGlobalSettings()
        {
            return FindPreferredAssetAtKnownPaths<UIGlobalSettings>("UIGlobalSettings.asset")
                   ?? FindPreferredAssetByType<UIGlobalSettings>();
        }

        public static DDOITSettings FindDDOITSettings()
        {
            return FindPreferredAssetAtKnownPaths<DDOITSettings>("DDOITSettings.asset")
                   ?? FindPreferredAssetByType<DDOITSettings>();
        }

        public static UITheme FindDefaultTheme()
        {
            return FindPreferredAssetAtKnownPaths<UITheme>("UITheme_Blue.asset")
                   ?? FindUIThemes().FirstOrDefault(theme => theme.name == "UITheme_Blue");
        }

        public static UITheme[] FindUIThemes()
        {
            return FindAssetsWithPaths<UITheme>("t:UITheme")
                .GroupBy(item => item.asset.name)
                .Select(group => group
                    .OrderBy(item => GetPathPriority(item.path))
                    .ThenBy(item => item.path)
                    .First())
                .OrderBy(item => GetPathPriority(item.path))
                .ThenBy(item => item.asset.name)
                .Select(item => item.asset)
                .ToArray();
        }

        public static GameObject FindUIPanelPrefab(out string path)
        {
            foreach (string candidatePath in GetPanelPrefabCandidatePaths())
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(candidatePath);
                if (prefab != null)
                {
                    path = candidatePath;
                    return prefab;
                }
            }

            path = null;
            return null;
        }

        public static void EnsureWritableSettingsFolder()
        {
            EnsureAssetFolder(WritableSettingsFolder);
        }

        private static bool IsDevelopmentSourceMode =>
            AssetDatabase.IsValidFolder(DEV_ROOT + "/Scripts") &&
            AssetDatabase.IsValidFolder(DEV_ROOT + "/Prefabs");

        private static T FindPreferredAssetAtKnownPaths<T>(string fileName)
            where T : Object
        {
            foreach (string folder in GetDataCandidateFolders())
            {
                string path = $"{folder}/{fileName}";
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                    return asset;
            }

            return null;
        }

        private static T FindPreferredAssetByType<T>()
            where T : Object
        {
            return FindAssetsWithPaths<T>($"t:{typeof(T).Name}")
                .OrderBy(item => GetPathPriority(item.path))
                .ThenBy(item => item.path)
                .Select(item => item.asset)
                .FirstOrDefault();
        }

        private static IEnumerable<(T asset, string path)> FindAssetsWithPaths<T>(string filter)
            where T : Object
        {
            foreach (string guid in AssetDatabase.FindAssets(filter))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                    yield return (asset, path);
            }
        }

        private static IEnumerable<string> GetDataCandidateFolders()
        {
            if (IsDevelopmentSourceMode)
            {
                yield return DEV_DATA_PATH;
                yield return PROJECT_SETTINGS_PATH;
                yield return UPM_DATA_PATH;
                yield break;
            }

            yield return PROJECT_SETTINGS_PATH;
            yield return DEV_DATA_PATH;
            yield return UPM_DATA_PATH;
        }

        private static IEnumerable<string> GetPanelPrefabCandidatePaths()
        {
            yield return DEV_PANEL_PREFAB_PATH;
            yield return UPM_PANEL_PREFAB_PATH;
        }

        private static int GetPathPriority(string path)
        {
            if (IsDevelopmentSourceMode)
            {
                if (IsSameOrChildPath(path, DEV_ROOT))
                    return 0;

                if (IsSameOrChildPath(path, PROJECT_SETTINGS_PATH))
                    return 1;

                if (path.StartsWith("Assets/"))
                    return 2;

                if (IsSameOrChildPath(path, UPM_ROOT))
                    return 3;

                return 4;
            }

            if (IsSameOrChildPath(path, PROJECT_SETTINGS_PATH))
                return 0;

            if (path.StartsWith("Assets/"))
                return 1;

            if (IsSameOrChildPath(path, UPM_ROOT))
                return 2;

            return 3;
        }

        private static bool IsSameOrChildPath(string path, string root)
        {
            return path == root || path.StartsWith(root + "/");
        }

        private static void EnsureAssetFolder(string assetPath)
        {
            string[] parts = assetPath.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);

                current = next;
            }
        }
    }
}
