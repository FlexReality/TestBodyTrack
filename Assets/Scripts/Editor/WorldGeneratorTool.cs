using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FlexReality.BodyTracking.EditorTools
{
    // One-click world dresser. Builds a kid-friendly cartoon meadow around
    // the existing playable scene:
    //   1. Wires the ObstacleSpawner with food prefabs so flying obstacles
    //      become apples / bananas / broccoli / etc.
    //   2. Replaces the small grey floor with a big green grass field.
    //   3. Plants two tree-lined "borders" along the runway so the camera
    //      always sees a forested edge.
    //   4. Sprinkles bushes / mushrooms / flowers between the trees, plus a
    //      few background animals as set dressing.
    //   5. Tints sky / sun for a warm midday cartoon palette.
    //
    // Re-runnable. Re-clears its previous output each run.
    public static class WorldGeneratorTool
    {
        private const string MenuPath = "Tools/Body Tracking/Generate Decorated World";
        private const string DecoRootName = "WorldDecorations";

        // -------- Food per lane --------
        private static readonly string[] RedFood = {
            "Assets/Quaternius/Food/Apple.fbx",
            "Assets/Quaternius/Food/Tomato.fbx",
            "Assets/Quaternius/Food/Popsicle_Strawberry.fbx",
            "Assets/Quaternius/Food/Pepper_Red.fbx",
            "Assets/Quaternius/JunkFood/Cake.fbx",
        };
        private static readonly string[] BlueFood = {
            // Quaternius has no truly-blue food; we use dark/purple items so
            // the blue lane is varied like the others. Lane identity is
            // signalled by the floor pad colour, not by the obstacle colour.
            "Assets/Quaternius/Food/Eggplant.fbx",
            "Assets/Quaternius/Food/Donut3.fbx",
            "Assets/Quaternius/Food/Donut4.fbx",
            "Assets/Quaternius/Food/Soda.fbx",
            "Assets/Quaternius/JunkFood/SodaCan.fbx",
            "Assets/Quaternius/Food/Popsicle_Multiple.fbx",
        };
        private static readonly string[] YellowFood = {
            "Assets/Quaternius/Food/Banana.fbx",
            "Assets/Quaternius/Food/Cheese_Singles.fbx",
            "Assets/Quaternius/JunkFood/Donut.fbx",
            "Assets/Quaternius/JunkFood/Pizza.fbx",
        };
        private static readonly string[] GreenFood = {
            "Assets/Quaternius/Food/Apple_Green.fbx",
            "Assets/Quaternius/Food/Broccoli.fbx",
            "Assets/Quaternius/Food/Avocado.fbx",
        };

        // -------- Decoration pools --------
        private static readonly string[] Trees = {
            "Assets/Quaternius/Nature/CommonTree_1.fbx",
            "Assets/Quaternius/Nature/CommonTree_2.fbx",
            "Assets/Quaternius/Nature/CommonTree_3.fbx",
            "Assets/Quaternius/Nature/CommonTree_4.fbx",
            "Assets/Quaternius/Nature/CommonTree_5.fbx",
            "Assets/Quaternius/Nature/BirchTree_1.fbx",
            "Assets/Quaternius/Nature/BirchTree_2.fbx",
            "Assets/Quaternius/Nature/BirchTree_3.fbx",
            "Assets/Quaternius/Nature/BirchTree_4.fbx",
            "Assets/Quaternius/Nature/BirchTree_5.fbx",
            "Assets/Quaternius/Nature/PineTree_1.fbx",
            "Assets/Quaternius/Nature/PineTree_2.fbx",
            "Assets/Quaternius/Nature/PineTree_3.fbx",
        };
        private static readonly string[] SmallProps = {
            "Assets/Quaternius/Nature/Bush_1.fbx",
            "Assets/Quaternius/Nature/Bush_2.fbx",
            "Assets/Quaternius/Nature/BushBerries_1.fbx",
            "Assets/Quaternius/Nature/BushBerries_2.fbx",
            "Assets/Quaternius/Nature/Flowers.fbx",
            "Assets/Quaternius/Nature/Grass.fbx",
            "Assets/Quaternius/Nature/Grass_2.fbx",
            "Assets/Quaternius/Nature/Grass_Short.fbx",
            "Assets/Quaternius/Nature/Plant_1.fbx",
            "Assets/Quaternius/Nature/Plant_2.fbx",
            "Assets/Quaternius/Nature/Plant_3.fbx",
        };
        private static readonly string[] Rocks = {
            "Assets/Quaternius/Nature/Rock_1.fbx",
            "Assets/Quaternius/Nature/Rock_2.fbx",
            "Assets/Quaternius/Nature/Rock_3.fbx",
            "Assets/Quaternius/Nature/Rock_Moss_1.fbx",
            "Assets/Quaternius/Nature/Rock_Moss_2.fbx",
        };
        private static readonly string[] Animals = {
            "Assets/Quaternius/Animals/Fox.fbx",
            "Assets/Quaternius/Animals/Deer.fbx",
            "Assets/Quaternius/Animals/Stag.fbx",
            "Assets/Quaternius/Animals/Cow.fbx",
            "Assets/Quaternius/Animals/ShibaInu.fbx",
            "Assets/Quaternius/Animals/Alpaca.fbx",
        };

        [MenuItem(MenuPath)]
        public static void Generate()
        {
            // 0. Auto-attach the default tracking model + Mixamo character if
            //    they're sitting in Assets/Models/ but not yet wired in scene.
            AutoAttachDefaults();

            // 1. Wire spawner.
            int foodSlots = WireSpawnerFood();

            // 2. Big green grass floor (resize + recolor existing).
            ReshapeFloor();

            // 3. Decorations (clear old, scatter new).
            var oldRoot = GameObject.Find(DecoRootName);
            if (oldRoot != null) Object.DestroyImmediate(oldRoot);
            var root = new GameObject(DecoRootName);

            int treeCount    = PlantTreeBorders(root.transform);
            // Quaternius Nature pack ships at ~1/30 of expected scale relative to
            // their animal pack — so foliage needs much bigger multipliers than
            // the animals do, or it appears as grass-blade-sized specks.
            int innerProps   = ScatterBetween(root.transform, SmallProps, 80, scaleMin: 15f, scaleMax: 30f);
            int rockProps    = ScatterBetween(root.transform, Rocks,      30, scaleMin: 10f, scaleMax: 20f);
            int animalProps  = ScatterAnimals(root.transform, 6);

            // 5. Road with perspective lines + scrolling dashes.
            BuildRoad();
            BuildScrollingDashes(root.transform);

            // 4. Sky/sun palette.
            TintCameraAndSun();

            EditorUtility.DisplayDialog("World Generator",
                $"Decorated!\n\n" +
                $"  • {treeCount} trees lining the runway\n" +
                $"  • {innerProps} bushes/grass/flowers\n" +
                $"  • {rockProps} rocks/mushrooms\n" +
                $"  • {animalProps} background animals\n" +
                $"  • {foodSlots} food prefab slots wired into the spawner\n\n" +
                "Re-run any time for a new layout.",
                "OK");

            Debug.Log($"[WorldGenerator] Placed {treeCount + innerProps + rockProps + animalProps} props.");
        }

        // -------- 0. Auto-attach BlazePose model + Mixamo character --------
        private static void AutoAttachDefaults()
        {
            // (a) BlazePose ONNX into Tracking → WebcamBodyTrackingProvider.modelAsset
            var tracking = GameObject.Find("Tracking");
            if (tracking != null)
            {
                var webcam = tracking.GetComponent<WebcamBodyTrackingProvider>();
                if (webcam != null)
                {
                    var so = new SerializedObject(webcam);
                    var prop = so.FindProperty("modelAsset");
                    if (prop != null && prop.objectReferenceValue == null)
                    {
                        var modelAsset = AssetDatabase.LoadAssetAtPath<Object>("Assets/Models/pose_landmarks_detector_lite.onnx");
                        if (modelAsset != null)
                        {
                            prop.objectReferenceValue = modelAsset;
                            so.ApplyModifiedProperties();
                            Debug.Log("[WorldGenerator] Auto-attached pose_landmarks_detector_lite.onnx to Tracking.");
                        }
                    }
                }
            }

            // (b) Mocap character under Player — only if Player has no humanoid
            //     child yet (so re-running doesn't duplicate). We prefer Klav
            //     over X Bot when available; X Bot is the safe fallback.
            var player = GameObject.Find("Player");
            if (player != null)
            {
                bool hasHumanoid = false;
                foreach (var anim in player.GetComponentsInChildren<Animator>())
                {
                    if (anim.isHuman) { hasHumanoid = true; break; }
                }
                if (!hasHumanoid)
                {
                    string[] candidatePaths = {
                        "Assets/Models/Ch46_nonPBR.fbx",
                        "Assets/Models/Klav/Klav/Models/Klav_mesh.fbx",
                        "Assets/Models/X Bot.fbx",
                    };

                    foreach (var path in candidatePaths)
                    {
                        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (asset == null) continue;

                        // Verify the asset's Animator is Humanoid before spawning;
                        // otherwise MocapBoneDriver can't drive it.
                        var assetAnim = asset.GetComponent<Animator>();
                        if (assetAnim == null || !assetAnim.isHuman)
                        {
                            Debug.LogWarning($"[WorldGenerator] '{path}' is not Humanoid (animator.isHuman = false). " +
                                "Switch the FBX's Rig → Animation Type to Humanoid in the Inspector, then re-run.");
                            continue;
                        }

                        var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
                        if (instance == null) instance = Object.Instantiate(asset);
                        instance.name = System.IO.Path.GetFileNameWithoutExtension(path);
                        Undo.RegisterCreatedObjectUndo(instance, "World: spawn mocap character");
                        bool ok = MocapSetupTool.SetupOnCharacter(instance, interactive: false);
                        if (ok)
                        {
                            Debug.Log($"[WorldGenerator] Auto-spawned '{instance.name}' under Player and wired MocapBoneDriver.");
                            break;
                        }
                        else
                        {
                            // Cleanup failed instance and try next candidate.
                            Object.DestroyImmediate(instance);
                            Debug.LogWarning($"[WorldGenerator] '{path}' loaded but MocapSetup failed. Trying next candidate.");
                        }
                    }
                }
            }
        }

        // -------- 1. Wire spawner --------
        private static int WireSpawnerFood()
        {
            var spawner = Object.FindFirstObjectByType<ObstacleSpawner>();
            if (spawner == null)
            {
                Debug.LogWarning("[WorldGenerator] No ObstacleSpawner in scene — food assignment skipped.");
                return 0;
            }
            var so = new SerializedObject(spawner);
            int count = 0;
            count += AssignPrefabArray(so, "frontPrefabs",  RedFood);
            count += AssignPrefabArray(so, "leftPrefabs",   BlueFood);
            count += AssignPrefabArray(so, "rightPrefabs",  YellowFood);
            count += AssignPrefabArray(so, "bottomPrefabs", GreenFood);
            // Quaternius food is microscopic at base scale — force a sane size
            // so the spawned obstacles are actually visible.
            var scaleProp = so.FindProperty("obstacleScale");
            if (scaleProp != null) scaleProp.floatValue = 80f;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(spawner);
            return count;
        }

        // -------- 2. Floor --------
        private static void ReshapeFloor()
        {
            var floor = GameObject.Find("Floor");
            if (floor == null)
            {
                // Make one if the scene doesn't have it.
                floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "Floor";
            }
            // 60m wide, 80m deep — well outside the runway in every direction.
            floor.transform.localScale = new Vector3(6f, 1f, 8f);
            floor.transform.position   = new Vector3(0f, 0f, 10f);
            var mr = floor.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                // Use sharedMaterial to avoid leaking instances in the editor.
                if (mr.sharedMaterial == null || mr.sharedMaterial.name == "Default-Material")
                {
                    var shader = Shader.Find("Universal Render Pipeline/Lit");
                    if (shader != null) mr.sharedMaterial = new Material(shader) { name = "FloorGrass" };
                }
                if (mr.sharedMaterial != null) mr.sharedMaterial.color = new Color(0.40f, 0.68f, 0.32f);
            }
        }

        // -------- 3a. Trees in two lines along the runway --------
        private static int PlantTreeBorders(Transform parent)
        {
            var loaded = LoadAll(Trees);
            if (loaded.Count == 0) return 0;

            int placed = 0;
            // Inner row — min x=±12 so large tree crowns stay off the road.
            for (float z = -6f; z <= 35f; z += Random.Range(3f, 4.5f))
            {
                placed += PlantOne(parent, loaded, new Vector3(-12f - Random.value * 2f, 0f, z + Random.Range(-0.6f, 0.6f)), 100f, 150f);
                placed += PlantOne(parent, loaded, new Vector3( 12f + Random.value * 2f, 0f, z + Random.Range(-0.6f, 0.6f)), 100f, 150f);
            }
            // Outer second row — parallax depth.
            for (float z = -8f; z <= 38f; z += Random.Range(4f, 6f))
            {
                placed += PlantOne(parent, loaded, new Vector3(-18f - Random.value * 4f, 0f, z), 130f, 180f);
                placed += PlantOne(parent, loaded, new Vector3( 18f + Random.value * 4f, 0f, z), 130f, 180f);
            }
            // Horizon cluster — |x| > 10 so recycled trees never land on road.
            for (int i = 0; i < 12; i++)
            {
                float hx = Random.value < 0.5f ? Random.Range(-25f, -10f) : Random.Range(10f, 25f);
                placed += PlantOne(parent, loaded,
                    new Vector3(hx, 0f, Random.Range(40f, 60f)),
                    150f, 220f);
            }

            return placed;
        }

        private static int PlantOne(Transform parent, List<GameObject> pool, Vector3 pos, float sMin, float sMax, bool uprightFix = true)
        {
            var prefab = pool[Random.Range(0, pool.Count)];
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            if (inst == null)
            {
                inst = Object.Instantiate(prefab, parent);
            }
            inst.transform.position = pos;
            // Quaternius Nature pack ships Z-up (Blender). Unity reads it as
            // Y-up, so trees lie on their sides at import. -90° around X stands
            // them up. Animals pack ships Y-up correctly, so we skip the fix.
            float xRot = uprightFix ? -90f : 0f;
            inst.transform.rotation = Quaternion.Euler(xRot, Random.Range(0f, 360f), 0f);
            inst.transform.localScale = Vector3.one * Random.Range(sMin, sMax);
            return 1;
        }

        // -------- 3b. Smaller props sprinkled between the trees and runway --------
        private static int ScatterBetween(Transform parent, string[] pool, int count, float scaleMin, float scaleMax)
        {
            var loaded = LoadAll(pool);
            if (loaded.Count == 0) return 0;

            int placed = 0;
            int attempts = 0;
            while (placed < count && attempts < count * 6)
            {
                attempts++;
                // Keep props outside x=±9 — tree crowns at scale 15-30 still need margin.
                float x = (Random.value < 0.5f
                    ? Random.Range(-18f, -9.0f)
                    : Random.Range(  9.0f, 18f));
                float z = Random.Range(-6f, 35f);
                placed += PlantOne(parent, loaded, new Vector3(x, 0f, z), scaleMin, scaleMax);
            }
            return placed;
        }

        // -------- 3c. Animals further out so they feel like background life --------
        private static int ScatterAnimals(Transform parent, int count)
        {
            var loaded = LoadAll(Animals);
            if (loaded.Count == 0) return 0;
            int placed = 0;
            for (int i = 0; i < count; i++)
            {
                float x = (Random.value < 0.5f ? Random.Range(-14f, -5f) : Random.Range(5f, 14f));
                float z = Random.Range(2f, 30f);
                // Animals pack ships Y-up correctly — skip the upright fix.
                placed += PlantOne(parent, loaded, new Vector3(x, 0f, z), 0.3f, 0.5f, uprightFix: false);
            }
            return placed;
        }

        // -------- 5. Road surface + converging lane lines --------
        private static void BuildRoad()
        {
            var oldRoad = GameObject.Find("Road");
            if (oldRoad != null) Object.DestroyImmediate(oldRoad);

            var roadRoot = new GameObject("Road");
            Undo.RegisterCreatedObjectUndo(roadRoot, "World: build road");

            var urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) urpLit = Shader.Find("Standard");

            // Asphalt surface — 4.5 units wide, 90 deep.
            var asphalt = GameObject.CreatePrimitive(PrimitiveType.Plane);
            asphalt.name = "Asphalt";
            asphalt.transform.SetParent(roadRoot.transform);
            asphalt.transform.position   = new Vector3(0f, 0.01f, 35f);
            asphalt.transform.localScale = new Vector3(0.45f, 1f, 9f);
            ApplyColor(asphalt, new Color(0.18f, 0.18f, 0.18f), urpLit);
            Object.DestroyImmediate(asphalt.GetComponent<Collider>());

            // White edge lines.
            CreateRoadLine(roadRoot.transform, -2.2f, urpLit, "EdgeLine_L");
            CreateRoadLine(roadRoot.transform,  2.2f, urpLit, "EdgeLine_R");
        }

        private static void CreateRoadLine(Transform parent, float x, Shader shader, string goName)
        {
            var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = goName;
            line.transform.SetParent(parent);
            line.transform.position   = new Vector3(x, 0.03f, 40f);
            line.transform.localScale = new Vector3(0.12f, 0.02f, 90f);
            ApplyColor(line, Color.white, shader);
            Object.DestroyImmediate(line.GetComponent<Collider>());
        }

        // Scrolling center dashes — placed in WorldDecorations so EnvironmentScroller
        // moves them toward the player, creating the endless-runner road flash.
        private static void BuildScrollingDashes(Transform decoParent)
        {
            var urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) urpLit = Shader.Find("Standard");

            float dashLen = 2.5f, step = 5f;
            for (float z = 0f; z < 75f; z += step)
            {
                var dash = GameObject.CreatePrimitive(PrimitiveType.Cube);
                dash.name = "RoadDash";
                dash.transform.SetParent(decoParent);
                dash.transform.position   = new Vector3(0f, 0.04f, z + dashLen * 0.5f);
                dash.transform.localScale = new Vector3(0.12f, 0.02f, dashLen);
                ApplyColor(dash, Color.white, urpLit);
                Object.DestroyImmediate(dash.GetComponent<Collider>());
            }
        }

        private static void ApplyColor(GameObject go, Color color, Shader shader)
        {
            var mr = go.GetComponent<MeshRenderer>();
            if (mr == null) return;
            var mat = new Material(shader != null ? shader : Shader.Find("Standard")) { color = color };
            mat.SetFloat("_Smoothness", 0.05f);
            mr.sharedMaterial = mat;
        }

        // -------- 4. Lighting + sky --------
        private static void TintCameraAndSun()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.62f, 0.82f, 0.95f); // soft sky blue
            }
            var sun = Object.FindFirstObjectByType<Light>();
            if (sun != null && sun.type == LightType.Directional)
            {
                sun.color = new Color(1f, 0.97f, 0.88f);
                sun.intensity = Mathf.Max(sun.intensity, 1.1f);
                sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }
            RenderSettings.ambientMode  = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.62f, 0.7f);
        }

        // -------- Helpers --------
        private static List<GameObject> LoadAll(string[] paths)
        {
            var list = new List<GameObject>(paths.Length);
            foreach (var p in paths)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (go != null) list.Add(go);
                else Debug.LogWarning($"[WorldGenerator] Asset not found: {p}");
            }
            return list;
        }

        private static int AssignPrefabArray(SerializedObject so, string fieldName, string[] paths)
        {
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[WorldGenerator] Field '{fieldName}' not found on spawner.");
                return 0;
            }
            var loaded = LoadAll(paths);
            prop.arraySize = loaded.Count;
            for (int i = 0; i < loaded.Count; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = loaded[i];
            return loaded.Count;
        }
    }
}
