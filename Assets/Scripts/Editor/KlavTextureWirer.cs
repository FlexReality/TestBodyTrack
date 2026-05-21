using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlexReality.BodyTracking.EditorTools
{
    // After Klav materials are switched to URP/Lit, their textures are usually
    // empty because the original Klav shader used custom property names. This
    // utility walks every Klav material and assigns the best-matching albedo
    // and normal texture from `Assets/Models/Klav/Klav/Textures/` based on
    // filename keywords (M_Klav → T_Klav_Albedo, M_Klav_Body_Army → T_*Army*Albedo).
    //
    // Skip-list: leaves face/eye/emission/pixel-screen materials alone — they
    // rely on Klav's custom shaders that can't be cleanly mapped to URP/Lit.
    public static class KlavTextureWirer
    {
        private const string MenuPath = "Tools/Body Tracking/Wire Klav Textures";
        private const string KlavRoot = "Assets/Models/Klav";
        private const string TexRoot  = "Assets/Models/Klav/Klav/Textures";

        // Materials we explicitly skip (face is animated via PixelScreen).
        private static readonly string[] SkipKeywords = {
            "Face", "Eye", "Mouth", "Nose", "EyeLid", "Pixel",
        };

        [MenuItem(MenuPath)]
        public static void Wire()
        {
            if (!AssetDatabase.IsValidFolder(TexRoot))
            {
                EditorUtility.DisplayDialog("Klav Texture Wirer",
                    $"Textures folder '{TexRoot}' missing.", "OK");
                return;
            }

            // Preload all textures under Klav/Textures so we can name-match cheaply.
            var allTexturePaths = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { TexRoot }))
                allTexturePaths.Add(AssetDatabase.GUIDToAssetPath(guid));

            var matGuids = AssetDatabase.FindAssets("t:Material", new[] { KlavRoot });
            int wiredAlbedo = 0;
            int wiredNormal = 0;
            int skipped = 0;

            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (var matGuid in matGuids)
                {
                    var matPath = AssetDatabase.GUIDToAssetPath(matGuid);
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (mat == null) continue;

                    var matName = mat.name;
                    if (ShouldSkip(matName)) { skipped++; continue; }

                    string albedoPath = FindBestMatch(matName, allTexturePaths, isAlbedo: true);
                    string normalPath = FindBestMatch(matName, allTexturePaths, isAlbedo: false);

                    bool changed = false;
                    if (albedoPath != null && mat.HasProperty("_BaseMap"))
                    {
                        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
                        if (tex != null && mat.GetTexture("_BaseMap") != tex)
                        {
                            mat.SetTexture("_BaseMap", tex);
                            // Reset tint to white so the texture's colours come through.
                            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
                            wiredAlbedo++;
                            changed = true;
                        }
                    }
                    if (normalPath != null && mat.HasProperty("_BumpMap"))
                    {
                        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                        if (tex != null && mat.GetTexture("_BumpMap") != tex)
                        {
                            mat.SetTexture("_BumpMap", tex);
                            mat.EnableKeyword("_NORMALMAP");
                            wiredNormal++;
                            changed = true;
                        }
                    }
                    if (changed) EditorUtility.SetDirty(mat);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log($"[KlavTextureWirer] Wired {wiredAlbedo} albedo + {wiredNormal} normal texture(s). Skipped {skipped} face/pixel materials.");
            EditorUtility.DisplayDialog("Klav Texture Wirer",
                $"Done — {wiredAlbedo} albedo and {wiredNormal} normal maps wired.\n\n" +
                "If Klav still has bare/grey parts, those are face/eye materials using Klav's custom shaders — leave them, or pick textures manually.",
                "OK");
        }

        private static bool ShouldSkip(string matName)
        {
            string lower = matName.ToLowerInvariant();
            foreach (var k in SkipKeywords)
                if (lower.Contains(k.ToLowerInvariant())) return true;
            return false;
        }

        // For "M_Klav_Body_Army" we want the texture whose filename most
        // contains "Klav", "Body", "Army" (in any order). Albedo matching also
        // prefers files with "Albedo", "BaseColor", "BaseMap" in the name.
        private static string FindBestMatch(string matName, List<string> texturePaths, bool isAlbedo)
        {
            // Tokenize material name: strip "M_" prefix, split by underscores.
            string clean = matName.StartsWith("M_") ? matName.Substring(2) : matName;
            var tokens = clean.ToLowerInvariant().Split('_');

            string[] albedoBoosts = { "albedo", "basecolor", "basemap", "diffuse", "color" };
            string[] normalBoosts = { "normal", "bump", "nrm" };
            string[] negativeKeywords = isAlbedo
                ? new[] { "normal", "bump", "nrm", "emission", "mask", "roughness", "metallic" }
                : new[] { "emission", "mask", "roughness", "metallic" };

            string best = null;
            int bestScore = 0;

            foreach (var path in texturePaths)
            {
                string fileName = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                int score = 0;

                // Skip files with negative keywords (so an "albedo" search doesn't grab a normal map).
                bool isNegative = false;
                foreach (var n in negativeKeywords)
                    if (fileName.Contains(n)) { isNegative = true; break; }
                if (isNegative) continue;

                // Boost for albedo/normal markers depending on what we're looking for.
                foreach (var b in (isAlbedo ? albedoBoosts : normalBoosts))
                    if (fileName.Contains(b)) score += 5;

                // Each matching token from the material name adds a point.
                foreach (var t in tokens)
                {
                    if (t.Length < 2) continue;
                    if (fileName.Contains(t)) score += 2;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = path;
                }
            }
            return bestScore >= 2 ? best : null; // require at least some keyword overlap
        }
    }
}
