using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FlexReality.BodyTracking.EditorTools
{
    // Older Quaternius FBX packs (2017-2021) ship without vertex colours OR
    // textures — the colour info lived in Blender material slots and didn't
    // survive the FBX export. So we colour each model ourselves, by inferring
    // a sensible tint from its filename keyword (Apple → red, Tree → green,
    // Burger → brown, …) and remapping every sub-material in the FBX to a
    // single shared coloured material.
    //
    // Trade-off: every sub-mesh of a given FBX gets the SAME colour (a burger
    // is uniformly brown, not bread-brown + lettuce-green). For a fast-paced
    // kid-friendly endless runner that reads fine at the speed cubes fly past.
    public static class QuaterniusMaterialTool
    {
        private const string MenuPath = "Tools/Body Tracking/Apply Quaternius Auto-Colors";
        private const string RootFolder = "Assets/Quaternius";
        private const string MatFolder = "Assets/Quaternius/_AutoMaterials";

        [MenuItem(MenuPath)]
        public static void Apply()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                EditorUtility.DisplayDialog("Quaternius Auto-Colors",
                    "URP/Lit shader not found. Make sure Universal RP is installed.", "OK");
                return;
            }

            if (!AssetDatabase.IsValidFolder(RootFolder))
            {
                EditorUtility.DisplayDialog("Quaternius Auto-Colors",
                    $"Folder '{RootFolder}' not found. Did the Quaternius packs land in the right place?", "OK");
                return;
            }
            if (!AssetDatabase.IsValidFolder(MatFolder))
                AssetDatabase.CreateFolder(RootFolder, "_AutoMaterials");

            var fbxGuids = AssetDatabase.FindAssets("t:Model", new[] { RootFolder });

            // PASS 1: nuke any stale material remaps and force a clean reimport
            // so LoadAllAssetsAtPath returns the *original* FBX material names
            // (otherwise we'd be reading a previous remap's names — exactly the
            // bug that left every model magenta after our first attempt).
            for (int i = 0; i < fbxGuids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(fbxGuids[i]);
                if (EditorUtility.DisplayCancelableProgressBar("Auto-Colors (pass 1/2)",
                        $"Resetting {System.IO.Path.GetFileName(path)}", (float)i / fbxGuids.Length))
                    break;

                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) continue;

                bool changed = false;
                var existing = new Dictionary<AssetImporter.SourceAssetIdentifier, Object>(importer.GetExternalObjectMap());
                foreach (var kvp in existing)
                {
                    if (kvp.Key.type == typeof(Material))
                    {
                        importer.RemoveRemap(kvp.Key);
                        changed = true;
                    }
                }
                if (importer.materialImportMode != ModelImporterMaterialImportMode.ImportViaMaterialDescription)
                {
                    importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                    changed = true;
                }
                if (importer.materialLocation != ModelImporterMaterialLocation.InPrefab)
                {
                    importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
                    changed = true;
                }
                if (changed) importer.SaveAndReimport();
            }
            EditorUtility.ClearProgressBar();

            // PASS 2: now read each FBX's true embedded material names and
            // remap them to the colour material chosen by ResolveColorFromName.
            var materialCache = new Dictionary<string, Material>();
            int totalSlots = 0;
            int totalFbx = 0;

            for (int i = 0; i < fbxGuids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(fbxGuids[i]);
                if (EditorUtility.DisplayCancelableProgressBar("Auto-Colors (pass 2/2)",
                        $"Colouring {System.IO.Path.GetFileName(path)}", (float)i / fbxGuids.Length))
                    break;

                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) continue;

                var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                var color = ResolveColorFromName(fileName);
                var matKey = $"{color.r:F2}_{color.g:F2}_{color.b:F2}";

                if (!materialCache.TryGetValue(matKey, out var mat))
                {
                    var matPath = $"{MatFolder}/AutoColor_{matKey}.mat";
                    mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (mat == null)
                    {
                        mat = new Material(shader);
                        mat.SetColor("_BaseColor", color);
                        AssetDatabase.CreateAsset(mat, matPath);
                    }
                    else
                    {
                        mat.shader = shader;
                        mat.SetColor("_BaseColor", color);
                        EditorUtility.SetDirty(mat);
                    }
                    materialCache[matKey] = mat;
                }

                var subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
                var names = new HashSet<string>();
                foreach (var asset in subAssets)
                    if (asset is Material m) names.Add(m.name);

                foreach (var n in names)
                {
                    var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), n);
                    importer.AddRemap(id, mat);
                    totalSlots++;
                }
                importer.SaveAndReimport();
                totalFbx++;
            }
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[QuaterniusAutoColor] Tinted {totalSlots} material slot(s) across {totalFbx} FBX models using {materialCache.Count} unique colours.");
            EditorUtility.DisplayDialog("Quaternius Auto-Colors",
                $"Done — {totalFbx} models in {materialCache.Count} colours.\n\n" +
                "Drop a Burger / Apple / Tree from Assets/Quaternius/ into the scene to verify they're coloured now.",
                "OK");
        }

        // Map a filename (case-insensitive substring match) to a kid-friendly
        // tint. Order matters — more specific names first (apple_green before
        // apple). Add new mappings here if a model lands as a wrong colour.
        private static Color ResolveColorFromName(string name)
        {
            string n = name.ToLowerInvariant();

            // -------- Food: greens --------
            if (Has(n, "apple_green", "broccoli", "avocado", "lettuce", "lime", "leaf",
                       "cabbage", "pickle", "kiwi")) return Hex("#7CBF3F");

            // -------- Food: reds --------
            if (Has(n, "apple", "strawberry", "tomato", "cherry", "pepper_red",
                       "watermelon", "raspberry", "ketchup")) return Hex("#D8463A");

            // -------- Food: yellows --------
            if (Has(n, "banana", "cheese", "lemon", "corn", "butter", "egg",
                       "fries", "pineapple")) return Hex("#E8C547");

            // -------- Food: oranges --------
            if (Has(n, "carrot", "orange", "pumpkin", "mango", "peach")) return Hex("#E5872F");

            // -------- Food: pinks (donut/cupcake/icecream) --------
            if (Has(n, "donut", "cupcake", "icecream", "milkshake", "cake",
                       "candy", "marshmallow")) return Hex("#F3A6B8");

            // -------- Food: browns (bread/burger/cookie/meat) --------
            if (Has(n, "bread", "bun", "burger", "cookie", "patty", "bacon",
                       "chicken", "chocolate", "hotdog", "pretzel", "wood",
                       "log", "trunk", "stem", "barrel")) return Hex("#9A6B40");

            // -------- Food: darks (soda/coffee/grape) --------
            if (Has(n, "soda", "coffee", "grape", "olive", "blueberry")) return Hex("#553A6F");

            // -------- Food: whites/creams (milk/rice/sugar) --------
            if (Has(n, "milk", "rice", "sugar", "salt", "ricecake", "noodle",
                       "pasta", "tofu", "bone")) return Hex("#EDE3CC");

            // -------- Nature: trees / bushes / grass --------
            if (Has(n, "tree", "bush", "grass", "plant", "flower_stem",
                       "leaves")) return Hex("#5BA64A");

            // -------- Nature: autumn leaves --------
            if (Has(n, "autumn")) return Hex("#D9802B");

            // -------- Nature: dead trees, snow trees --------
            if (Has(n, "dead")) return Hex("#6E5A40");
            if (Has(n, "snow", "ice")) return Hex("#E8F2F8");

            // -------- Nature: rocks / stones --------
            if (Has(n, "rock", "stone", "boulder", "cliff")) return Hex("#7A7A7A");

            // -------- Nature: mushrooms --------
            if (Has(n, "mushroom")) return Hex("#C44848");

            // -------- Nature: flowers --------
            if (Has(n, "flower")) return Hex("#E76FB5");

            // -------- Animals (rough — they have many parts; this just keeps them visible) --------
            if (Has(n, "fox")) return Hex("#D87B3A");
            if (Has(n, "wolf", "husky")) return Hex("#9CA3AB");
            if (Has(n, "bull", "cow")) return Hex("#3B2A1F");
            if (Has(n, "horse")) return Hex("#6B4226");
            if (Has(n, "horse_white")) return Hex("#F4F0E6");
            if (Has(n, "alpaca", "sheep")) return Hex("#F0E6D2");
            if (Has(n, "deer", "stag", "donkey")) return Hex("#A07550");
            if (Has(n, "shibainu", "shiba")) return Hex("#E08F4C");

            // Fallback: warm gray so nothing reads as "broken material".
            return Hex("#B4A892");
        }

        private static bool Has(string source, params string[] keywords)
        {
            for (int i = 0; i < keywords.Length; i++)
                if (source.Contains(keywords[i])) return true;
            return false;
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }
    }
}
