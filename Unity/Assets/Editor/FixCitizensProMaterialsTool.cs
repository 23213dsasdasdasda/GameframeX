using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameFrameX.EditorTools
{
    public static class FixCitizensProMaterialsTool
    {
        private const string SearchRoot = "Assets/Citizens PRO/Models";
        private const string UrpLitShaderName = "Universal Render Pipeline/Lit";

        [MenuItem("Tools/Citizens PRO/Fix People Materials For URP")]
        public static void FixPeopleMaterialsForUrp()
        {
            var urpShader = Shader.Find(UrpLitShaderName);
            if (urpShader == null)
            {
                Debug.LogError($"Shader not found: {UrpLitShaderName}");
                return;
            }

            var guids = AssetDatabase.FindAssets("t:Material", new[] { SearchRoot });
            var updated = 0;

            AssetDatabase.StartAssetEditing();
            try
            {
                for (var i = 0; i < guids.Length; i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (!IsCitizenMaterial(path))
                    {
                        continue;
                    }

                    var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (material == null)
                    {
                        continue;
                    }

                    if (!NeedsUpgrade(material))
                    {
                        continue;
                    }

                    UpgradeMaterial(material, urpShader, path);
                    EditorUtility.SetDirty(material);
                    updated++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Citizens PRO URP material fix complete. Updated: {updated}");
        }

        private static bool IsCitizenMaterial(string path)
        {
            return path.Contains("/People 1.0/", StringComparison.OrdinalIgnoreCase)
                || path.Contains("/People 2.0/", StringComparison.OrdinalIgnoreCase)
                || path.Contains("/Models/Materials/", StringComparison.OrdinalIgnoreCase);
        }

        private static bool NeedsUpgrade(Material material)
        {
            return material.shader == null
                || material.shader.name == "Standard"
                || material.shader.name == "Legacy Shaders/Diffuse"
                || material.shader.name == "Legacy Shaders/Transparent/Cutout/Diffuse"
                || material.shader.name == "Legacy Shaders/Transparent/Diffuse";
        }

        private static void UpgradeMaterial(Material material, Shader urpShader, string path)
        {
            var mainTex = GetTexture(material, "_MainTex");
            var bumpMap = GetTexture(material, "_BumpMap");
            var metallicGlossMap = GetTexture(material, "_MetallicGlossMap");
            var color = GetColor(material, "_Color", Color.white);
            var emissionColor = GetColor(material, "_EmissionColor", Color.black);
            var bumpScale = GetFloat(material, "_BumpScale", 1f);
            var metallic = GetFloat(material, "_Metallic", 0f);
            var cutoff = GetFloat(material, "_Cutoff", 0.5f);
            var mode = GetFloat(material, "_Mode", 0f);
            var srcBlend = GetFloat(material, "_SrcBlend", 1f);
            var dstBlend = GetFloat(material, "_DstBlend", 0f);
            var zWrite = GetFloat(material, "_ZWrite", 1f);

            material.shader = urpShader;

            if (mainTex != null)
            {
                material.SetTexture("_BaseMap", mainTex);
                material.SetTexture("_MainTex", mainTex);
            }

            material.SetColor("_BaseColor", color);
            material.SetColor("_Color", color);

            if (bumpMap != null)
            {
                material.SetTexture("_BumpMap", bumpMap);
                material.SetFloat("_BumpScale", bumpScale);
                material.EnableKeyword("_NORMALMAP");
            }
            else
            {
                material.DisableKeyword("_NORMALMAP");
            }

            if (metallicGlossMap != null)
            {
                material.SetTexture("_MetallicGlossMap", metallicGlossMap);
            }

            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", 0.35f);
            material.SetColor("_EmissionColor", emissionColor);
            material.SetFloat("_ReceiveShadows", 1f);
            material.SetFloat("_SpecularHighlights", 1f);
            material.SetFloat("_EnvironmentReflections", 1f);

            var transparent = ShouldUseTransparent(path, material, mode, srcBlend, dstBlend, zWrite);
            if (transparent)
            {
                ApplyTransparent(material, cutoff);
            }
            else
            {
                ApplyOpaque(material, cutoff);
            }
        }

        private static bool ShouldUseTransparent(string path, Material material, float mode, float srcBlend, float dstBlend, float zWrite)
        {
            if (path.Contains("hairalpha", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (material.shaderKeywords.Any(keyword => keyword.Contains("ALPHABLEND", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            return mode >= 2f || srcBlend > 1f || dstBlend >= 10f || zWrite <= 0f;
        }

        private static void ApplyOpaque(Material material, float cutoff)
        {
            material.SetOverrideTag("RenderType", string.Empty);
            material.renderQueue = -1;
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_AlphaClip", 0f);
            material.SetFloat("_Cutoff", cutoff);
            material.SetFloat("_SrcBlend", 1f);
            material.SetFloat("_DstBlend", 0f);
            material.SetFloat("_ZWrite", 1f);
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.DisableKeyword("_ALPHATEST_ON");
        }

        private static void ApplyTransparent(Material material, float cutoff)
        {
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_AlphaClip", 0f);
            material.SetFloat("_Cutoff", cutoff);
            material.SetFloat("_SrcBlend", 1f);
            material.SetFloat("_DstBlend", 10f);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            material.DisableKeyword("_ALPHATEST_ON");
        }

        private static Texture GetTexture(Material material, string propertyName)
        {
            return material.HasProperty(propertyName) ? material.GetTexture(propertyName) : null;
        }

        private static Color GetColor(Material material, string propertyName, Color fallback)
        {
            return material.HasProperty(propertyName) ? material.GetColor(propertyName) : fallback;
        }

        private static float GetFloat(Material material, string propertyName, float fallback)
        {
            return material.HasProperty(propertyName) ? material.GetFloat(propertyName) : fallback;
        }
    }
}
