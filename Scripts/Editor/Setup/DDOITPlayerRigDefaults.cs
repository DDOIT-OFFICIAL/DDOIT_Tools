using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DDOIT.Tools.Setup
{
    /// <summary>
    /// DDOIT 씬의 Player rig를 컨트롤러 중심 표준값으로 보정하는 Editor 전용 유틸리티입니다.
    /// Meta Interaction SDK 타입은 패키지 컴파일 순서 영향을 줄이기 위해 문자열/SerializedObject로 접근합니다.
    /// </summary>
    internal static class DDOITPlayerRigDefaults
    {
        #region Constants

        private const string OVR_CAMERA_RIG_NAME = "OVRCameraRig";
        private const string PLAYER_RIG_TYPE_NAME = "DDOIT.Tools.Player.PlayerRig";
        private const string TURNER_EVENT_BROADCASTER_TYPE_NAME = "Oculus.Interaction.Locomotion.TurnerEventBroadcaster";
        private const string LOCOMOTION_EVENTS_CONNECTION_TYPE_NAME = "Oculus.Interaction.Locomotion.LocomotionEventsConnection";
        private const string FIRST_PERSON_LOCOMOTOR_TYPE_NAME = "Oculus.Interaction.Locomotion.FirstPersonLocomotor";
        private const string LOCOMOTION_EVENT_SELECTOR_TYPE_NAME = "Oculus.Interaction.Locomotion.LocomotionEventSelector";

        private const int DEFAULT_CHARACTER_LAYER_MASK = 7;

        private const int SNAP_TURN_METHOD_ENUM_INDEX = 0;
        private const float SNAP_TURN_DEGREES = 45f;
        private const bool FIRE_SNAP_ON_UNSELECT = false;
        private const bool COMFORT_TUNNELING_ENABLED = false;

        private const string CENTER_EYE_ANCHOR_PATH = "TrackingSpace/CenterEyeAnchor";
        private const string LOCOMOTOR_PATH = "OVRInteractionComprehensive/Locomotor";
        private const string PLAYER_CONTROLLER_PATH = "OVRInteractionComprehensive/Locomotor/PlayerController";
        private const string BODY_TELEPORT_INTERACTOR_PATH = "OVRInteractionComprehensive/Locomotor/BodyTeleportInteractor";
        private const string WALKING_STICK_GROUP_PATH = "OVRInteractionComprehensive/Locomotor/WalkingStickGroup";
        private const string SMOOTH_MOVEMENT_TUNNELING_PATH = "OVRInteractionComprehensive/Locomotor/SmoothMovementTunneling";
        private const string WALL_PENETRATION_TUNNELING_PATH = "OVRInteractionComprehensive/Locomotor/WallPenetrationTunneling";

        private const string LEFT_CONTROLLER_GROUP_PATH =
            "OVRInteractionComprehensive/LeftInteractions/Interactors/Controller/LocomotionControllerInteractorGroup";

        private const string RIGHT_CONTROLLER_GROUP_PATH =
            "OVRInteractionComprehensive/RightInteractions/Interactors/Controller/LocomotionControllerInteractorGroup";

        private const string LEFT_LOCOMOTION_OUTPUT_PATH =
            "OVRInteractionComprehensive/LeftInteractions/LocomotionOutput";

        private const string RIGHT_LOCOMOTION_OUTPUT_PATH =
            "OVRInteractionComprehensive/RightInteractions/LocomotionOutput";

        private const string TELEPORT_CONTROLLER_INTERACTOR_PATH = "TeleportControllerInteractor";
        private const string CONTROLLER_TURNER_INTERACTOR_PATH = "ControllerTurnerInteractor";
        private const string CONTROLLER_STEP_INTERACTOR_PATH = "ControllerStepInteractor";
        private const string CONTROLLER_SLIDE_INTERACTOR_PATH = "ControllerSlideInteractor";
        private const string CONTROLLER_LOCOMOTION_SLIDE_ACTIONS_PATH = "ControllerLocomotionSlideActions";

        #endregion

        #region Public API

        internal static void AppendPreflight(string scenePath, IList<string> changes, IList<string> warnings)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                warnings.Add("DDOIT Player defaults: skipped while Editor is entering or running Play Mode.");
                return;
            }

            if (!SceneFileExists(scenePath))
            {
                warnings.Add($"{scenePath}: scene not found. Run Init Project before player rig migration.");
                return;
            }

            WithScene(scenePath, false, warnings, scene =>
            {
                Transform rigRoot = FindRoot(scene, OVR_CAMERA_RIG_NAME);
                if (rigRoot == null)
                {
                    warnings.Add($"{scene.path}: OVRCameraRig not found.");
                    return;
                }

                if (NeedsCharacterControllerLayerMask(rigRoot))
                    changes.Add("DDOIT Player: CharacterController layer mask will be reset to Default/TransparentFX/Ignore Raycast");

                bool activeStatesNeedChange =
                    NeedsActiveState(rigRoot, Combine(LEFT_CONTROLLER_GROUP_PATH, CONTROLLER_SLIDE_INTERACTOR_PATH), true)
                    || NeedsActiveState(rigRoot, Combine(LEFT_CONTROLLER_GROUP_PATH, CONTROLLER_TURNER_INTERACTOR_PATH), false)
                    || NeedsActiveState(rigRoot, Combine(LEFT_CONTROLLER_GROUP_PATH, CONTROLLER_STEP_INTERACTOR_PATH), false)
                    || NeedsActiveState(rigRoot, Combine(LEFT_CONTROLLER_GROUP_PATH, TELEPORT_CONTROLLER_INTERACTOR_PATH), false)
                    || NeedsActiveState(rigRoot, Combine(RIGHT_CONTROLLER_GROUP_PATH, CONTROLLER_SLIDE_INTERACTOR_PATH), false)
                    || NeedsActiveState(rigRoot, Combine(RIGHT_CONTROLLER_GROUP_PATH, CONTROLLER_TURNER_INTERACTOR_PATH), true)
                    || NeedsActiveState(rigRoot, Combine(RIGHT_CONTROLLER_GROUP_PATH, CONTROLLER_STEP_INTERACTOR_PATH), false)
                    || NeedsActiveState(rigRoot, Combine(RIGHT_CONTROLLER_GROUP_PATH, TELEPORT_CONTROLLER_INTERACTOR_PATH), false)
                    || NeedsActiveState(rigRoot, SMOOTH_MOVEMENT_TUNNELING_PATH, COMFORT_TUNNELING_ENABLED)
                    || NeedsActiveState(rigRoot, WALL_PENETRATION_TUNNELING_PATH, COMFORT_TUNNELING_ENABLED)
                    || NeedsActiveState(rigRoot, WALKING_STICK_GROUP_PATH, false);

                if (activeStatesNeedChange)
                    changes.Add("DDOIT Player: controller locomotion active states will be normalized");

                if (NeedsSnapTurnDefaults(rigRoot, Combine(RIGHT_CONTROLLER_GROUP_PATH, CONTROLLER_TURNER_INTERACTOR_PATH))
                    || NeedsSnapTurnDefaults(rigRoot, Combine(LEFT_CONTROLLER_GROUP_PATH, CONTROLLER_TURNER_INTERACTOR_PATH)))
                    changes.Add("DDOIT Player: controller turning will be configured as 45 degree snap turn");

                if (NeedsFirstPersonLocomotorDefaults(rigRoot))
                    changes.Add("DDOIT Player: FirstPersonLocomotor velocity movement will be enabled");

                if (NeedsPlayerRigSerializedDefaults(rigRoot))
                    changes.Add("DDOIT PlayerRig: serialized controller profile references will be stored");

                if (NeedsLocomotionConnectionDefaults(rigRoot))
                    changes.Add("DDOIT Player: Meta Locomotion event chain will be repaired");
            });
        }

        internal static int ApplyToScene(Scene scene, List<string> warnings)
        {
            Transform rigRoot = FindRoot(scene, OVR_CAMERA_RIG_NAME);
            if (rigRoot == null)
            {
                warnings.Add($"{scene.path}: OVRCameraRig not found.");
                return 0;
            }

            int changed = 0;
            changed += ApplyCharacterControllerLayerMask(rigRoot, warnings);
            changed += ApplyActiveStates(rigRoot, warnings);
            changed += ApplySnapTurnDefaults(rigRoot, warnings);
            changed += ApplyFirstPersonLocomotorDefaults(rigRoot, warnings);
            changed += ApplyPlayerRigSerializedDefaults(rigRoot, warnings);
            changed += ApplyLocomotionConnectionDefaults(rigRoot, warnings);
            return changed;
        }

        #endregion

        #region Active States

        private static int ApplyActiveStates(Transform rigRoot, List<string> warnings)
        {
            int changed = 0;

            changed += SetActive(rigRoot, Combine(LEFT_CONTROLLER_GROUP_PATH, CONTROLLER_SLIDE_INTERACTOR_PATH), true, warnings);
            changed += SetActive(rigRoot, Combine(LEFT_CONTROLLER_GROUP_PATH, CONTROLLER_TURNER_INTERACTOR_PATH), false, warnings);
            changed += SetActive(rigRoot, Combine(LEFT_CONTROLLER_GROUP_PATH, CONTROLLER_STEP_INTERACTOR_PATH), false, warnings);
            changed += SetActive(rigRoot, Combine(LEFT_CONTROLLER_GROUP_PATH, TELEPORT_CONTROLLER_INTERACTOR_PATH), false, warnings);

            changed += SetActive(rigRoot, Combine(RIGHT_CONTROLLER_GROUP_PATH, CONTROLLER_SLIDE_INTERACTOR_PATH), false, warnings);
            changed += SetActive(rigRoot, Combine(RIGHT_CONTROLLER_GROUP_PATH, CONTROLLER_TURNER_INTERACTOR_PATH), true, warnings);
            changed += SetActive(rigRoot, Combine(RIGHT_CONTROLLER_GROUP_PATH, CONTROLLER_STEP_INTERACTOR_PATH), false, warnings);
            changed += SetActive(rigRoot, Combine(RIGHT_CONTROLLER_GROUP_PATH, TELEPORT_CONTROLLER_INTERACTOR_PATH), false, warnings);

            changed += SetActive(rigRoot, SMOOTH_MOVEMENT_TUNNELING_PATH, COMFORT_TUNNELING_ENABLED, warnings);
            changed += SetActive(rigRoot, WALL_PENETRATION_TUNNELING_PATH, COMFORT_TUNNELING_ENABLED, warnings);
            changed += SetActive(rigRoot, WALKING_STICK_GROUP_PATH, false, warnings);

            return changed;
        }

        private static bool NeedsActiveState(Transform rigRoot, string path, bool active)
        {
            Transform target = rigRoot.Find(path);
            return target != null && target.gameObject.activeSelf != active;
        }

        private static int SetActive(Transform rigRoot, string path, bool active, List<string> warnings)
        {
            Transform target = rigRoot.Find(path);
            if (target == null)
            {
                warnings.Add($"{rigRoot.name}/{path}: object not found.");
                return 0;
            }

            if (target.gameObject.activeSelf == active)
                return 0;

            target.gameObject.SetActive(active);
            EditorUtility.SetDirty(target.gameObject);
            return 1;
        }

        #endregion

        private static bool NeedsCharacterControllerLayerMask(Transform rigRoot)
        {
            Component characterController = GetComponentAtPath(
                rigRoot,
                PLAYER_CONTROLLER_PATH,
                "Oculus.Interaction.Locomotion.CharacterController");
            if (characterController == null)
                return false;

            var so = new SerializedObject(characterController);
            var prop = so.FindProperty("_layerMask");
            return prop != null && prop.intValue != DEFAULT_CHARACTER_LAYER_MASK;
        }

        private static int ApplyCharacterControllerLayerMask(Transform rigRoot, List<string> warnings)
        {
            Component characterController = GetComponentAtPath(
                rigRoot,
                PLAYER_CONTROLLER_PATH,
                "Oculus.Interaction.Locomotion.CharacterController");
            if (characterController == null)
            {
                warnings.Add($"{rigRoot.name}/{PLAYER_CONTROLLER_PATH}: CharacterController not found.");
                return 0;
            }

            var so = new SerializedObject(characterController);
            var prop = so.FindProperty("_layerMask");
            if (prop == null)
            {
                warnings.Add($"{rigRoot.name}/{PLAYER_CONTROLLER_PATH}: CharacterController _layerMask not found.");
                return 0;
            }

            if (prop.intValue == DEFAULT_CHARACTER_LAYER_MASK)
                return 0;

            prop.intValue = DEFAULT_CHARACTER_LAYER_MASK;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(characterController);
            return 1;
        }

        #region Component Defaults

        private static int ApplySnapTurnDefaults(Transform rigRoot, List<string> warnings)
        {
            int changed = 0;
            changed += ApplySnapTurnDefaults(
                rigRoot,
                Combine(LEFT_CONTROLLER_GROUP_PATH, CONTROLLER_TURNER_INTERACTOR_PATH),
                warnings);
            changed += ApplySnapTurnDefaults(
                rigRoot,
                Combine(RIGHT_CONTROLLER_GROUP_PATH, CONTROLLER_TURNER_INTERACTOR_PATH),
                warnings);
            return changed;
        }

        private static bool NeedsSnapTurnDefaults(Transform rigRoot, string path)
        {
            Component turner = GetComponentAtPath(rigRoot, path, TURNER_EVENT_BROADCASTER_TYPE_NAME);
            if (turner == null)
                return false;

            var so = new SerializedObject(turner);
            return NeedsEnum(so, "_turnMethod", SNAP_TURN_METHOD_ENUM_INDEX)
                || NeedsFloat(so, "_snapTurnDegrees", SNAP_TURN_DEGREES)
                || NeedsBool(so, "_fireSnapOnUnselect", FIRE_SNAP_ON_UNSELECT);
        }

        private static int ApplySnapTurnDefaults(Transform rigRoot, string path, List<string> warnings)
        {
            Component turner = GetComponentAtPath(rigRoot, path, TURNER_EVENT_BROADCASTER_TYPE_NAME);
            if (turner == null)
            {
                warnings.Add($"{rigRoot.name}/{path}: TurnerEventBroadcaster not found.");
                return 0;
            }

            var so = new SerializedObject(turner);
            int changed = 0;
            changed += SetEnum(so, "_turnMethod", SNAP_TURN_METHOD_ENUM_INDEX);
            changed += SetFloat(so, "_snapTurnDegrees", SNAP_TURN_DEGREES);
            changed += SetBool(so, "_fireSnapOnUnselect", FIRE_SNAP_ON_UNSELECT);
            if (changed > 0)
                EditorUtility.SetDirty(turner);

            return changed;
        }

        private static bool NeedsFirstPersonLocomotorDefaults(Transform rigRoot)
        {
            Component locomotor = GetComponentAtPath(rigRoot, PLAYER_CONTROLLER_PATH, FIRST_PERSON_LOCOMOTOR_TYPE_NAME);
            if (locomotor == null)
                return false;

            var so = new SerializedObject(locomotor);
            return NeedsBool(so, "_velocityDisabled", false);
        }

        private static int ApplyFirstPersonLocomotorDefaults(Transform rigRoot, List<string> warnings)
        {
            Component locomotor = GetComponentAtPath(rigRoot, PLAYER_CONTROLLER_PATH, FIRST_PERSON_LOCOMOTOR_TYPE_NAME);
            if (locomotor == null)
            {
                warnings.Add($"{rigRoot.name}/{PLAYER_CONTROLLER_PATH}: FirstPersonLocomotor not found.");
                return 0;
            }

            var so = new SerializedObject(locomotor);
            int changed = SetBool(so, "_velocityDisabled", false);
            if (changed > 0)
                EditorUtility.SetDirty(locomotor);

            return changed;
        }

        private static bool NeedsPlayerRigSerializedDefaults(Transform rigRoot)
        {
            Component playerRig = GetComponentByTypeName(rigRoot.gameObject, PLAYER_RIG_TYPE_NAME);
            if (playerRig == null)
                return false;

            var so = new SerializedObject(playerRig);
            return NeedsBool(so, "_applyDefaultControllerProfileOnAwake", true)
                || NeedsFloat(so, "_snapTurnDegrees", SNAP_TURN_DEGREES)
                || NeedsBool(so, "_fireSnapOnUnselect", FIRE_SNAP_ON_UNSELECT)
                || NeedsBool(so, "_comfortTunnelingEnabled", COMFORT_TUNNELING_ENABLED)
                || NeedsObject(so, "_headTransform", FindTransform(rigRoot, CENTER_EYE_ANCHOR_PATH))
                || NeedsObject(so, "_playerOrigin", rigRoot)
                || NeedsObject(so, "_walkingStickRoot", FindObject(rigRoot, WALKING_STICK_GROUP_PATH))
                || NeedsObject(so, "_leftControllerSlideInteractor", FindObject(rigRoot, Combine(LEFT_CONTROLLER_GROUP_PATH, CONTROLLER_SLIDE_INTERACTOR_PATH)))
                || NeedsObject(so, "_leftControllerTurnerInteractor", FindObject(rigRoot, Combine(LEFT_CONTROLLER_GROUP_PATH, CONTROLLER_TURNER_INTERACTOR_PATH)))
                || NeedsObject(so, "_leftControllerStepInteractor", FindObject(rigRoot, Combine(LEFT_CONTROLLER_GROUP_PATH, CONTROLLER_STEP_INTERACTOR_PATH)))
                || NeedsObject(so, "_leftTeleportControllerInteractor", FindObject(rigRoot, Combine(LEFT_CONTROLLER_GROUP_PATH, TELEPORT_CONTROLLER_INTERACTOR_PATH)))
                || NeedsObject(so, "_rightControllerSlideInteractor", FindObject(rigRoot, Combine(RIGHT_CONTROLLER_GROUP_PATH, CONTROLLER_SLIDE_INTERACTOR_PATH)))
                || NeedsObject(so, "_rightControllerTurnerInteractor", FindObject(rigRoot, Combine(RIGHT_CONTROLLER_GROUP_PATH, CONTROLLER_TURNER_INTERACTOR_PATH)))
                || NeedsObject(so, "_rightControllerStepInteractor", FindObject(rigRoot, Combine(RIGHT_CONTROLLER_GROUP_PATH, CONTROLLER_STEP_INTERACTOR_PATH)))
                || NeedsObject(so, "_rightTeleportControllerInteractor", FindObject(rigRoot, Combine(RIGHT_CONTROLLER_GROUP_PATH, TELEPORT_CONTROLLER_INTERACTOR_PATH)))
                || NeedsObject(so, "_smoothMovementTunneling", FindObject(rigRoot, SMOOTH_MOVEMENT_TUNNELING_PATH))
                || NeedsObject(so, "_wallPenetrationTunneling", FindObject(rigRoot, WALL_PENETRATION_TUNNELING_PATH));
        }

        private static int ApplyPlayerRigSerializedDefaults(Transform rigRoot, List<string> warnings)
        {
            Component playerRig = GetComponentByTypeName(rigRoot.gameObject, PLAYER_RIG_TYPE_NAME);
            if (playerRig == null)
            {
                warnings.Add($"{rigRoot.name}: PlayerRig component not found.");
                return 0;
            }

            var so = new SerializedObject(playerRig);
            int changed = 0;

            changed += SetBool(so, "_applyDefaultControllerProfileOnAwake", true);
            changed += SetFloat(so, "_snapTurnDegrees", SNAP_TURN_DEGREES);
            changed += SetBool(so, "_fireSnapOnUnselect", FIRE_SNAP_ON_UNSELECT);
            changed += SetBool(so, "_comfortTunnelingEnabled", COMFORT_TUNNELING_ENABLED);
            changed += SetObject(so, "_headTransform", FindTransform(rigRoot, CENTER_EYE_ANCHOR_PATH));
            changed += SetObject(so, "_playerOrigin", rigRoot);
            changed += SetObject(so, "_walkingStickRoot", FindObject(rigRoot, WALKING_STICK_GROUP_PATH));
            changed += SetObject(so, "_leftControllerSlideInteractor", FindObject(rigRoot, Combine(LEFT_CONTROLLER_GROUP_PATH, CONTROLLER_SLIDE_INTERACTOR_PATH)));
            changed += SetObject(so, "_leftControllerTurnerInteractor", FindObject(rigRoot, Combine(LEFT_CONTROLLER_GROUP_PATH, CONTROLLER_TURNER_INTERACTOR_PATH)));
            changed += SetObject(so, "_leftControllerStepInteractor", FindObject(rigRoot, Combine(LEFT_CONTROLLER_GROUP_PATH, CONTROLLER_STEP_INTERACTOR_PATH)));
            changed += SetObject(so, "_leftTeleportControllerInteractor", FindObject(rigRoot, Combine(LEFT_CONTROLLER_GROUP_PATH, TELEPORT_CONTROLLER_INTERACTOR_PATH)));
            changed += SetObject(so, "_rightControllerSlideInteractor", FindObject(rigRoot, Combine(RIGHT_CONTROLLER_GROUP_PATH, CONTROLLER_SLIDE_INTERACTOR_PATH)));
            changed += SetObject(so, "_rightControllerTurnerInteractor", FindObject(rigRoot, Combine(RIGHT_CONTROLLER_GROUP_PATH, CONTROLLER_TURNER_INTERACTOR_PATH)));
            changed += SetObject(so, "_rightControllerStepInteractor", FindObject(rigRoot, Combine(RIGHT_CONTROLLER_GROUP_PATH, CONTROLLER_STEP_INTERACTOR_PATH)));
            changed += SetObject(so, "_rightTeleportControllerInteractor", FindObject(rigRoot, Combine(RIGHT_CONTROLLER_GROUP_PATH, TELEPORT_CONTROLLER_INTERACTOR_PATH)));
            changed += SetObject(so, "_smoothMovementTunneling", FindObject(rigRoot, SMOOTH_MOVEMENT_TUNNELING_PATH));
            changed += SetObject(so, "_wallPenetrationTunneling", FindObject(rigRoot, WALL_PENETRATION_TUNNELING_PATH));

            if (changed > 0)
                EditorUtility.SetDirty(playerRig);

            return changed;
        }

        #endregion

        #region Locomotion Connections

        private static bool NeedsLocomotionConnectionDefaults(Transform rigRoot)
        {
            Component locomotorConnection = GetComponentAtPath(rigRoot, LOCOMOTOR_PATH, LOCOMOTION_EVENTS_CONNECTION_TYPE_NAME);
            Component leftGroupConnection = GetComponentAtPath(rigRoot, LEFT_CONTROLLER_GROUP_PATH, LOCOMOTION_EVENTS_CONNECTION_TYPE_NAME);
            Component rightGroupConnection = GetComponentAtPath(rigRoot, RIGHT_CONTROLLER_GROUP_PATH, LOCOMOTION_EVENTS_CONNECTION_TYPE_NAME);
            Component leftOutputConnection = GetComponentAtPath(rigRoot, LEFT_LOCOMOTION_OUTPUT_PATH, LOCOMOTION_EVENTS_CONNECTION_TYPE_NAME);
            Component rightOutputConnection = GetComponentAtPath(rigRoot, RIGHT_LOCOMOTION_OUTPUT_PATH, LOCOMOTION_EVENTS_CONNECTION_TYPE_NAME);

            if (locomotorConnection == null || leftGroupConnection == null || rightGroupConnection == null
                || leftOutputConnection == null || rightOutputConnection == null)
                return false;

            return NeedsArray(
                    new SerializedObject(leftGroupConnection),
                    "_handlers",
                    new UnityEngine.Object[] { leftOutputConnection })
                || NeedsArray(
                    new SerializedObject(rightGroupConnection),
                    "_handlers",
                    new UnityEngine.Object[] { rightOutputConnection })
                || NeedsArray(
                    new SerializedObject(leftOutputConnection),
                    "_handlers",
                    new UnityEngine.Object[] { locomotorConnection })
                || NeedsArray(
                    new SerializedObject(rightOutputConnection),
                    "_handlers",
                    new UnityEngine.Object[] { locomotorConnection })
                || NeedsArray(
                    new SerializedObject(locomotorConnection),
                    "_handlers",
                    GetLocomotorHandlers(rigRoot).ToArray());
        }

        private static int ApplyLocomotionConnectionDefaults(Transform rigRoot, List<string> warnings)
        {
            int changed = 0;

            Component locomotorConnection = RequireComponentAtPath(
                rigRoot,
                LOCOMOTOR_PATH,
                LOCOMOTION_EVENTS_CONNECTION_TYPE_NAME,
                warnings);
            Component leftGroupConnection = RequireComponentAtPath(
                rigRoot,
                LEFT_CONTROLLER_GROUP_PATH,
                LOCOMOTION_EVENTS_CONNECTION_TYPE_NAME,
                warnings);
            Component rightGroupConnection = RequireComponentAtPath(
                rigRoot,
                RIGHT_CONTROLLER_GROUP_PATH,
                LOCOMOTION_EVENTS_CONNECTION_TYPE_NAME,
                warnings);
            Component leftOutputConnection = RequireComponentAtPath(
                rigRoot,
                LEFT_LOCOMOTION_OUTPUT_PATH,
                LOCOMOTION_EVENTS_CONNECTION_TYPE_NAME,
                warnings);
            Component rightOutputConnection = RequireComponentAtPath(
                rigRoot,
                RIGHT_LOCOMOTION_OUTPUT_PATH,
                LOCOMOTION_EVENTS_CONNECTION_TYPE_NAME,
                warnings);

            if (locomotorConnection == null || leftGroupConnection == null || rightGroupConnection == null
                || leftOutputConnection == null || rightOutputConnection == null)
                return changed;

            changed += SetArray(new SerializedObject(leftGroupConnection), "_handlers", new UnityEngine.Object[] { leftOutputConnection });
            changed += SetArray(new SerializedObject(rightGroupConnection), "_handlers", new UnityEngine.Object[] { rightOutputConnection });
            changed += SetArray(new SerializedObject(leftOutputConnection), "_handlers", new UnityEngine.Object[] { locomotorConnection });
            changed += SetArray(new SerializedObject(rightOutputConnection), "_handlers", new UnityEngine.Object[] { locomotorConnection });
            changed += SetArray(new SerializedObject(locomotorConnection), "_handlers", GetLocomotorHandlers(rigRoot).ToArray());

            return changed;
        }

        private static List<UnityEngine.Object> GetLocomotorHandlers(Transform rigRoot)
        {
            var handlers = new List<UnityEngine.Object>();

            Component firstPersonLocomotor = GetComponentAtPath(
                rigRoot,
                PLAYER_CONTROLLER_PATH,
                FIRST_PERSON_LOCOMOTOR_TYPE_NAME);
            if (firstPersonLocomotor != null)
                handlers.Add(firstPersonLocomotor);

            Component bodyTeleportSelector = GetComponentAtPath(
                rigRoot,
                BODY_TELEPORT_INTERACTOR_PATH,
                LOCOMOTION_EVENT_SELECTOR_TYPE_NAME);
            if (bodyTeleportSelector != null)
                handlers.Add(bodyTeleportSelector);

            return handlers;
        }

        #endregion

        #region Scene Helpers

        private static void WithScene(string scenePath, bool save, IList<string> warnings, Action<Scene> action)
        {
            var activeScene = SceneManager.GetActiveScene();
            var scene = FindLoadedScene(scenePath);
            bool openedForMigration = !scene.IsValid();

            if (openedForMigration)
            {
                scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                    scenePath,
                    UnityEditor.SceneManagement.OpenSceneMode.Additive);
            }

            if (!scene.IsValid() || !scene.isLoaded)
            {
                warnings.Add($"{scenePath}: could not be opened for player rig migration.");
                return;
            }

            action(scene);

            if (save && scene.isDirty)
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

            if (openedForMigration)
            {
                if (activeScene.IsValid() && activeScene.isLoaded)
                    UnityEditor.SceneManagement.EditorSceneManager.SetActiveScene(activeScene);

                UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static Scene FindLoadedScene(string scenePath)
        {
            string normalizedPath = scenePath.Replace("\\", "/");
            int sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.path.Replace("\\", "/") == normalizedPath)
                    return scene;
            }

            return default;
        }

        private static bool SceneFileExists(string scenePath)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrEmpty(projectRoot))
                return false;

            string fullPath = Path.Combine(projectRoot, scenePath);
            return File.Exists(fullPath);
        }

        private static Transform FindRoot(Scene scene, string rootName)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == rootName)
                    return root.transform;
            }

            return null;
        }

        private static string Combine(string parent, string child)
        {
            return $"{parent}/{child}";
        }

        #endregion

        #region SerializedObject Helpers

        private static UnityEngine.Object FindObject(Transform rigRoot, string path)
        {
            Transform target = rigRoot.Find(path);
            return target == null ? null : target.gameObject;
        }

        private static Transform FindTransform(Transform rigRoot, string path)
        {
            return rigRoot.Find(path);
        }

        private static Component RequireComponentAtPath(
            Transform rigRoot,
            string path,
            string typeName,
            List<string> warnings)
        {
            Component component = GetComponentAtPath(rigRoot, path, typeName);
            if (component == null)
                warnings.Add($"{rigRoot.name}/{path}: {typeName} not found.");

            return component;
        }

        private static Component GetComponentAtPath(Transform rigRoot, string path, string typeName)
        {
            Transform target = rigRoot.Find(path);
            return target == null ? null : GetComponentByTypeName(target.gameObject, typeName);
        }

        private static Component GetComponentByTypeName(GameObject target, string typeName)
        {
            foreach (var component in target.GetComponents<Component>())
            {
                if (component != null && component.GetType().FullName == typeName)
                    return component;
            }

            return null;
        }

        private static bool NeedsBool(SerializedObject so, string propertyName, bool value)
        {
            var prop = so.FindProperty(propertyName);
            return prop != null && prop.boolValue != value;
        }

        private static bool NeedsEnum(SerializedObject so, string propertyName, int value)
        {
            var prop = so.FindProperty(propertyName);
            return prop != null && prop.enumValueIndex != value;
        }

        private static bool NeedsFloat(SerializedObject so, string propertyName, float value)
        {
            var prop = so.FindProperty(propertyName);
            return prop != null && !Mathf.Approximately(prop.floatValue, value);
        }

        private static bool NeedsObject(SerializedObject so, string propertyName, UnityEngine.Object value)
        {
            var prop = so.FindProperty(propertyName);
            return prop != null && prop.objectReferenceValue != value;
        }

        private static bool NeedsArray(SerializedObject so, string propertyName, IReadOnlyList<UnityEngine.Object> values)
        {
            var prop = so.FindProperty(propertyName);
            if (prop == null || !prop.isArray)
                return false;

            if (prop.arraySize != values.Count)
                return true;

            for (int i = 0; i < values.Count; i++)
            {
                if (prop.GetArrayElementAtIndex(i).objectReferenceValue != values[i])
                    return true;
            }

            return false;
        }

        private static int SetBool(SerializedObject so, string propertyName, bool value)
        {
            var prop = so.FindProperty(propertyName);
            if (prop == null || prop.boolValue == value)
                return 0;

            prop.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            return 1;
        }

        private static int SetEnum(SerializedObject so, string propertyName, int value)
        {
            var prop = so.FindProperty(propertyName);
            if (prop == null || prop.enumValueIndex == value)
                return 0;

            prop.enumValueIndex = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            return 1;
        }

        private static int SetFloat(SerializedObject so, string propertyName, float value)
        {
            var prop = so.FindProperty(propertyName);
            if (prop == null || Mathf.Approximately(prop.floatValue, value))
                return 0;

            prop.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            return 1;
        }

        private static int SetObject(SerializedObject so, string propertyName, UnityEngine.Object value)
        {
            var prop = so.FindProperty(propertyName);
            if (prop == null || prop.objectReferenceValue == value)
                return 0;

            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            return 1;
        }

        private static int SetArray(SerializedObject so, string propertyName, IReadOnlyList<UnityEngine.Object> values)
        {
            var prop = so.FindProperty(propertyName);
            if (prop == null || !prop.isArray || !NeedsArray(so, propertyName, values))
                return 0;

            prop.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(so.targetObject);
            return 1;
        }

        #endregion
    }
}
