using UnityEditor;
using UnityEngine;

namespace FlexReality.BodyTracking.EditorTools
{
    // Klav ships with Built-in Render Pipeline materials (Standard shader). In a
    // URP project those render as bright magenta (Unity's "this shader doesn't
    // belong here" signal). This menu swaps every Klav material's shader to
    // URP/Lit while preserving the albedo texture and tint.
    //
    // Idempotent: re-running on already-URP materials is a no-op.
    public static class KlavMaterialUrpConverter
    {
        private const string MenuPath = "Tools/Body Tracking/Convert Klav Materials to URP";
        private const string KlavRoot = "Assets/Models/Klav";

        [MenuItem(MenuPath)]
        public static void Convert()
        {
            var urpLit = Shader.Find("Universal Render Pipeline/Lit");
            var urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (urpLit == null)
            {
                EditorUtility.DisplayDialog("URP Converter",
                    "URP/Lit shader not found. Is the Universal Render Pipeline package installed?", "OK");
                return;
            }

            if (!AssetDatabase.IsValidFolder(KlavRoot))
            {
                EditorUtility.DisplayDialog("URP Converter",
                    $"Folder '{KlavRoot}' not found.", "OK");
                return;
            }

            var guids = AssetDatabase.FindAssets("t:Material", new[] { KlavRoot });
            int converted = 0;
            int skipped = 0;

            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (mat == null) continue;

                    string shaderName = mat.shader != null ? mat.shader.name : "";
                    if (shaderName.StartsWith("Universal Render Pipeline/"))
                    {
                        skipped++;
                        continue;
                    }

                    // Capture interesting bits from the old material before swap.
                    Texture albedoTex = null;
                    Texture normalTex = null;
                    Color color = Color.white;
                    float smoothness = 0.5f;
                    float metallic = 0f;
                    Texture emissionTex = null;
                    Color emissionColor = Color.black;

                    if (mat.HasProperty("_MainTex")) albedoTex = mat.GetTexture("_MainTex");
                    if (mat.HasProperty("_BaseMap") && albedoTex == null) albedoTex = mat.GetTexture("_BaseMap");
                    if (mat.HasProperty("_BumpMap")) normalTex = mat.GetTexture("_BumpMap");
                    if (mat.HasProperty("_NormalMap") && normalTex == null) normalTex = mat.GetTexture("_NormalMap");
                    if (mat.HasProperty("_Color")) color = mat.GetColor("_Color");
                    else if (mat.HasProperty("_BaseColor")) color = mat.GetColor("_BaseColor");
                    if (mat.HasProperty("_Glossiness")) smoothness = mat.GetFloat("_Glossiness");
                    else if (mat.HasProperty("_Smoothness")) smoothness = mat.GetFloat("_Smoothness");
                    if (mat.HasProperty("_Metallic")) metallic = mat.GetFloat("_Metallic");
                    if (mat.HasProperty("_EmissionMap")) emissionTex = mat.GetTexture("_EmissionMap");
                    if (mat.HasProperty("_EmissionColor")) emissionColor = mat.GetColor("_EmissionColor");

                    // Pick URP shader — Lit unless the source was Unlit-style.
                    bool wasUnlit = shaderName.Contains("Unlit");
                    mat.shader = wasUnlit && urpUnlit != null ? urpUnlit : urpLit;

                    // Re-apply captured values onto URP properties.
                    if (mat.HasProperty("_BaseMap"))      mat.SetTexture("_BaseMap", albedoTex);
                    if (mat.HasProperty("_BaseColor"))    mat.SetColor("_BaseColor", color);
                    if (mat.HasProperty("_BumpMap"))      mat.SetTexture("_BumpMap", normalTex);
                    if (mat.HasProperty("_Smoothness"))   mat.SetFloat("_Smoothness", smoothness);
                    if (mat.HasProperty("_Metallic"))     mat.SetFloat("_Metallic", metallic);
                    if (mat.HasProperty("_EmissionMap"))  mat.SetTexture("_EmissionMap", emissionTex);
                    if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emissionColor);
                    if (emissionTex != null || emissionColor.maxColorComponent > 0.01f)
                        mat.EnableKeyword("_EMISSION");

                    EditorUtility.SetDirty(mat);
                    converted++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log($"[KlavURP] Converted {converted} material(s); {skipped} already URP.");
            EditorUtility.DisplayDialog("URP Converter",
                $"Done — {converted} materials converted to URP/Lit, {skipped} were already URP.\n\n" +
                "If Klav still looks magenta in scene, drag him out and back in to refresh the renderer references.",
                "OK");
        }
    }
}
