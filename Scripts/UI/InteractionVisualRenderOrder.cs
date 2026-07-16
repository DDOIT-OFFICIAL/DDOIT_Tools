using System;
using System.Collections.Generic;
using UnityEngine;

namespace DDOIT.Tools.UI
{
    /// <summary>
    /// Raises Meta interaction affordance visuals above the DDOIT in-scene UI mesh.
    /// </summary>
    public static class InteractionVisualRenderOrder
    {
        #region Constants

        private const int RESCAN_INTERVAL_FRAMES = 30;

        #endregion

        #region Static Fields

        private static readonly HashSet<int> ProcessedRenderers = new HashSet<int>();
        private static readonly string[] VisualComponentTypeNames =
        {
            "Oculus.Interaction.RayInteractorRayVisual",
            "Oculus.Interaction.ControllerRayVisual",
            "Oculus.Interaction.HandVisual",
            "Oculus.Interaction.Input.Visuals.ControllerVisual",
            "Oculus.Interaction.Input.Visuals.OVRControllerVisual"
        };

        private static int _lastScanFrame = -RESCAN_INTERVAL_FRAMES;

        #endregion

        #region Public Methods

        /// <summary>
        /// Applies a late render queue to known hand, controller, and ray visuals.
        /// </summary>
        public static void ApplyAboveOverlay(int renderQueue)
        {
            if (Time.frameCount - _lastScanFrame < RESCAN_INTERVAL_FRAMES)
                return;

            _lastScanFrame = Time.frameCount;

            ApplyToKnownVisualComponents(renderQueue);
            ApplyToRayShaderRenderers(renderQueue);
        }

        #endregion

        #region Private Methods

        private static void ApplyToKnownVisualComponents(int renderQueue)
        {
            for (int i = 0; i < VisualComponentTypeNames.Length; i++)
            {
                Type type = FindType(VisualComponentTypeNames[i]);
                if (type == null)
                    continue;

                UnityEngine.Object[] components = UnityEngine.Object.FindObjectsByType(
                    type,
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);

                for (int j = 0; j < components.Length; j++)
                {
                    if (components[j] is Component component)
                        ApplyToRenderers(component.GetComponentsInChildren<Renderer>(true), renderQueue);
                }
            }
        }

        private static void ApplyToRayShaderRenderers(int renderQueue)
        {
            Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                Material[] sharedMaterials = renderer.sharedMaterials;
                for (int j = 0; j < sharedMaterials.Length; j++)
                {
                    Material material = sharedMaterials[j];
                    if (material == null || material.shader == null)
                        continue;

                    if (material.shader.name == "Interaction/OculusRayVisual")
                    {
                        ApplyToRenderer(renderer, renderQueue);
                        break;
                    }
                }
            }
        }

        private static void ApplyToRenderers(Renderer[] renderers, int renderQueue)
        {
            for (int i = 0; i < renderers.Length; i++)
                ApplyToRenderer(renderers[i], renderQueue);
        }

        private static void ApplyToRenderer(Renderer renderer, int renderQueue)
        {
            if (renderer == null)
                return;

            int id = renderer.GetInstanceID();
            if (ProcessedRenderers.Contains(id))
                return;

            Material[] materials = renderer.materials;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] != null)
                    materials[i].renderQueue = renderQueue;
            }

            ProcessedRenderers.Add(id);
        }

        private static Type FindType(string fullName)
        {
            AppDomain domain = AppDomain.CurrentDomain;
            System.Reflection.Assembly[] assemblies = domain.GetAssemblies();

            for (int i = 0; i < assemblies.Length; i++)
            {
                Type type = assemblies[i].GetType(fullName);
                if (type != null)
                    return type;
            }

            return null;
        }

        #endregion
    }
}
