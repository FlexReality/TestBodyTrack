using UnityEngine;

namespace FlexReality.BodyTracking
{
    // Spawns cube obstacles around the player from 4 directions, aiming at the
    // player. Spawn rate and obstacle speed are driven by GameSession so the
    // difficulty ramps up over time. Pauses when the game is over.
    public class ObstacleSpawner : MonoBehaviour
    {
        [Header("Spawning")]
        [SerializeField] private GameObject obstaclePrefab;     // fallback; cube generated if null
        [SerializeField] private Transform playerTarget;
        [SerializeField] private float spawnDistance = 14f;
        [Tooltip("Uniform scale applied to instantiated obstacles. Quaternius Food ships microscopic (~0.03 units), so we need ~30–50× to get a fruit-sized item. Bump down if huge, up if tiny.")]
        [SerializeField] private float obstacleScale = 40f;

        [Header("Prefab Pools (drag FBXs per lane; empty array falls back to coloured cube)")]
        [SerializeField] private GameObject[] frontPrefabs;
        [SerializeField] private GameObject[] leftPrefabs;
        [SerializeField] private GameObject[] rightPrefabs;
        [SerializeField] private GameObject[] bottomPrefabs;

        [Header("Directions")]
        [SerializeField] private bool enableFront = true;
        [SerializeField] private bool enableLeft = true;
        [SerializeField] private bool enableRight = true;
        [SerializeField] private bool enableBottom = true;

        [Header("Heights")]
        [SerializeField] private float chestHeight = 1.2f;
        [SerializeField] private float lowHeight = 0.3f;

        [Header("Lane Layout")]
        [Tooltip("How far left/right the side obstacles land from the player. Must match PlayerAvatarController.sideAttackOffset.")]
        [SerializeField] private float sideTargetOffset = 1.8f;

        [Header("Fallback (used if no GameSession in scene)")]
        [SerializeField] private float fallbackInterval = 1.4f;
        [SerializeField] private float fallbackSpeed = 7f;

        private float timer;
        private GameObject defaultPrefab;

        private void Awake()
        {
            if (obstaclePrefab == null) obstaclePrefab = BuildDefaultObstaclePrefab();
        }

        private void Update()
        {
            if (playerTarget == null) return;
            var session = GameSession.Instance;
            if (session != null && !session.IsAlive) return;

            timer -= Time.deltaTime;
            if (timer > 0f) return;

            float interval = session != null ? session.CurrentSpawnInterval : fallbackInterval;
            timer = interval;

            if (!TryPickDirection(out var dir)) return;
            float speed = session != null ? session.CurrentObstacleSpeed : fallbackSpeed;
            Spawn(dir, speed);
        }

        private bool TryPickDirection(out ObstacleDirection dir)
        {
            System.Span<ObstacleDirection> options = stackalloc ObstacleDirection[4];
            int count = 0;
            if (enableFront)  options[count++] = ObstacleDirection.Front;
            if (enableLeft)   options[count++] = ObstacleDirection.Left;
            if (enableRight)  options[count++] = ObstacleDirection.Right;
            if (enableBottom) options[count++] = ObstacleDirection.Bottom;

            if (count == 0) { dir = ObstacleDirection.Front; return false; }
            dir = options[Random.Range(0, count)];
            return true;
        }

        private void Spawn(ObstacleDirection dir, float speed)
        {
            Vector3 fwd = playerTarget.forward;
            Vector3 right = playerTarget.right;
            Vector3 origin = playerTarget.position;
            Vector3 spawnPos, targetPos;
            Vector3 velocity;

            switch (dir)
            {
                case ObstacleDirection.Front:
                    targetPos = origin + Vector3.up * chestHeight;
                    spawnPos  = targetPos + fwd * spawnDistance;
                    break;
                case ObstacleDirection.Left:
                    targetPos = origin - right * sideTargetOffset + Vector3.up * chestHeight;
                    spawnPos  = targetPos + fwd * spawnDistance;
                    break;
                case ObstacleDirection.Right:
                    targetPos = origin + right * sideTargetOffset + Vector3.up * chestHeight;
                    spawnPos  = targetPos + fwd * spawnDistance;
                    break;
                default: // Bottom
                    targetPos = origin + Vector3.up * lowHeight;
                    spawnPos  = targetPos + fwd * spawnDistance;
                    break;
            }
            velocity = (targetPos - spawnPos).normalized * speed;

            var prefab = PickPrefabForDirection(dir);
            bool usingFallback = prefab == obstaclePrefab;

            var go = Instantiate(prefab, spawnPos, Quaternion.identity);
            go.name = $"Obstacle_{dir}";
            go.SetActive(true);
            if (!usingFallback) go.transform.localScale = Vector3.one * obstacleScale;

            // Real FBX food models rarely have a collider — add a trigger box so
            // the player's attack sphere can register hits via OverlapSphere.
            if (go.GetComponent<Collider>() == null)
            {
                var bc = go.AddComponent<BoxCollider>();
                bc.isTrigger = true;
            }

            var oc = go.GetComponent<ObstacleController>();
            if (oc == null) oc = go.AddComponent<ObstacleController>();
            oc.Launch(dir, velocity, targetPos, playerTarget);

            // Only the fallback cube gets tinted to the lane colour. Real food
            // already has the right colour from the auto-colour material remap.
            if (usingFallback) ColorizeByDirection(go, dir);
        }

        private GameObject PickPrefabForDirection(ObstacleDirection dir)
        {
            var pool = dir switch
            {
                ObstacleDirection.Front  => frontPrefabs,
                ObstacleDirection.Left   => leftPrefabs,
                ObstacleDirection.Right  => rightPrefabs,
                _                        => bottomPrefabs
            };
            if (pool != null && pool.Length > 0)
            {
                var pick = pool[Random.Range(0, pool.Length)];
                if (pick != null) return pick;
            }
            return obstaclePrefab;
        }

        private static void ColorizeByDirection(GameObject go, ObstacleDirection dir)
        {
            var mr = go.GetComponentInChildren<MeshRenderer>();
            if (mr == null) return;
            Color c = dir switch
            {
                ObstacleDirection.Front  => new Color(0.9f, 0.3f, 0.3f),
                ObstacleDirection.Left   => new Color(0.3f, 0.6f, 0.9f),
                ObstacleDirection.Right  => new Color(0.9f, 0.7f, 0.3f),
                ObstacleDirection.Bottom => new Color(0.4f, 0.9f, 0.4f),
                _ => Color.white
            };
            mr.material.color = c;
        }

        private GameObject BuildDefaultObstaclePrefab()
        {
            if (defaultPrefab != null) return defaultPrefab;
            defaultPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            defaultPrefab.name = "DefaultObstacleTemplate";
            defaultPrefab.transform.SetParent(transform, false);
            defaultPrefab.SetActive(false);
            var col = defaultPrefab.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
            defaultPrefab.AddComponent<ObstacleController>();
            return defaultPrefab;
        }
    }
}
