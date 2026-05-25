using UnityEngine;

namespace FlexReality.BodyTracking
{
    // Scrolls the "WorldDecorations" forest toward the player for an
    // endless-runner illusion. Auto-finds the root by name — no manual wiring.
    public class EnvironmentScroller : MonoBehaviour
    {
        [SerializeField] private float scrollSpeed   = 7f;
        [SerializeField] private float recycleZ      = 12f;   // how far behind player before recycling
        [SerializeField] private float respawnZ      = 60f;   // how far ahead to re-place recycled objects

        private Transform[] _pool;
        private float[]     _origX;   // preserve each object's original X (left/right side)

        private void Start()
        {
            var root = GameObject.Find("WorldDecorations");
            if (root == null)
            {
                Debug.LogWarning("[EnvironmentScroller] 'WorldDecorations' not found — run Tools ▸ Body Tracking ▸ Generate Decorated World first.");
                enabled = false;
                return;
            }

            int n  = root.transform.childCount;
            _pool  = new Transform[n];
            _origX = new float[n];
            for (int i = 0; i < n; i++)
            {
                _pool[i]  = root.transform.GetChild(i);
                _origX[i] = _pool[i].position.x;
            }
        }

        private void Update()
        {
            float delta = scrollSpeed * Time.deltaTime;
            for (int i = 0; i < _pool.Length; i++)
            {
                var t = _pool[i];
                t.position += Vector3.back * delta;

                if (t.position.z < -recycleZ)
                {
                    t.position = new Vector3(
                        _origX[i],
                        t.position.y,
                        respawnZ + Random.Range(0f, 12f));
                }
            }
        }
    }
}
