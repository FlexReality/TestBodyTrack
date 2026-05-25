using UnityEngine;

namespace FlexReality.BodyTracking
{
    // Scrolls existing scene trees toward the player to create an endless-runner
    // illusion. Assign the parent GameObject that holds all the forest trees to
    // ForestParent — the scroller pools and recycles its children automatically.
    public class EnvironmentScroller : MonoBehaviour
    {
        [Header("Source")]
        [Tooltip("Parent whose children are the trees/props to scroll. Drag your forest root here.")]
        [SerializeField] private Transform forestParent;

        [Header("Scroll")]
        [SerializeField] private float scrollSpeed = 7f;

        [Header("Recycle")]
        [Tooltip("Z distance behind the player at which an object is teleported to the front.")]
        [SerializeField] private float recycleZ = 12f;
        [Tooltip("Z distance in front of the player where recycled objects reappear.")]
        [SerializeField] private float respawnZ = 55f;

        private Transform[] _pool;
        private float[]     _poolOrigX; // keep original X so recycled trees stay on their side

        private void Start()
        {
            if (forestParent == null)
            {
                Debug.LogWarning("[EnvironmentScroller] ForestParent not assigned. " +
                                 "Drag the forest root GameObject into the ForestParent field.", this);
                enabled = false;
                return;
            }

            int count = forestParent.childCount;
            _pool      = new Transform[count];
            _poolOrigX = new float[count];

            for (int i = 0; i < count; i++)
            {
                _pool[i]      = forestParent.GetChild(i);
                _poolOrigX[i] = _pool[i].position.x;
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
                        _poolOrigX[i],                          // keep original side (left/right)
                        t.position.y,
                        respawnZ + Random.Range(0f, 10f));      // small random spread so trees don't clump
                }
            }
        }
    }
}
