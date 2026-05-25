using UnityEngine;

namespace FlexReality.BodyTracking
{
    // Scrolls trees from in front of the player toward the camera to create
    // an endless-runner illusion. Trees are spawned procedurally on the left
    // and right sides; the centre road stays clear.
    //
    // Setup: add this component to any empty GameObject in the scene.
    // No manual wiring needed — it creates everything automatically.
    public class EnvironmentScroller : MonoBehaviour
    {
        [Header("Scroll")]
        [SerializeField] private float scrollSpeed = 7f;

        [Header("Layout")]
        [Tooltip("Half-width of the clear centre road (no trees inside this range).")]
        [SerializeField] private float roadHalfWidth = 3.2f;
        [Tooltip("How far to the side trees can spawn.")]
        [SerializeField] private float sideSpread = 9f;
        [Tooltip("How far ahead trees are placed when spawned/recycled.")]
        [SerializeField] private float spawnDepth = 55f;
        [Tooltip("How far behind the player a tree must travel before being recycled.")]
        [SerializeField] private float recycleZ = 10f;
        [Tooltip("Total number of trees in the pool (split evenly left/right).")]
        [SerializeField] private int treeCount = 36;

        private Transform[] trees;

        private void Start()
        {
            trees = new Transform[treeCount];
            for (int i = 0; i < treeCount; i++)
            {
                int side = (i % 2 == 0) ? -1 : 1; // alternate left/right
                float startZ = (float)i / treeCount * spawnDepth;
                trees[i] = SpawnTree(side, startZ);
            }
        }

        private void Update()
        {
            float delta = scrollSpeed * Time.deltaTime;
            foreach (var t in trees)
            {
                t.position += Vector3.back * delta;
                if (t.position.z < -recycleZ)
                {
                    int side = t.position.x < 0 ? -1 : 1;
                    t.position = new Vector3(
                        t.position.x,
                        t.position.y,
                        spawnDepth + Random.Range(0f, 8f));
                }
            }
        }

        private Transform SpawnTree(int side, float startZ)
        {
            var root = new GameObject("Tree");
            root.transform.SetParent(transform, false);

            float xRand = Random.Range(roadHalfWidth + 0.5f, roadHalfWidth + sideSpread);
            float x = side * xRand;
            float scale = Random.Range(0.7f, 1.4f);
            root.transform.position = new Vector3(x, 0f, startZ);
            root.transform.localScale = Vector3.one * scale;

            BuildTreeMesh(root.transform);
            return root.transform;
        }

        private static void BuildTreeMesh(Transform root)
        {
            // Trunk — brown cylinder
            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(root, false);
            trunk.transform.localPosition = new Vector3(0f, 1f, 0f);
            trunk.transform.localScale    = new Vector3(0.28f, 1f, 0.28f);
            SetColor(trunk, new Color(0.42f, 0.27f, 0.12f));
            RemoveCollider(trunk);

            // Foliage — slightly squashed sphere, random shade of green
            var top = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            top.name = "Foliage";
            top.transform.SetParent(root, false);
            top.transform.localPosition = new Vector3(0f, 2.9f, 0f);
            top.transform.localScale    = new Vector3(1.1f, 1.35f, 1.1f);
            float g = Random.Range(0.45f, 0.75f);
            SetColor(top, new Color(0.1f, g, 0.15f));
            RemoveCollider(top);
        }

        private static void SetColor(GameObject go, Color c)
        {
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.material.color = c;
        }

        private static void RemoveCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
        }
    }
}
