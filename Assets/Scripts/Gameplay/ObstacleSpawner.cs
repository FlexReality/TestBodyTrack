using UnityEngine;

namespace FlexReality.BodyTracking
{
    // Spawns answer-carrying obstacles for the math game (Front/Left/Right lanes)
    // and classic jump-dodge cubes for the Bottom lane.
    // Spawn pattern: correct, wrong, wrong, correct, wrong, wrong, ...
    // so a correct answer is always reachable within 3 spawns.
    public class ObstacleSpawner : MonoBehaviour
    {
        [Header("Spawning")]
        [SerializeField] private GameObject obstaclePrefab;
        [SerializeField] private Transform playerTarget;
        [SerializeField] private float spawnDistance = 14f;
        [Tooltip("Uniform scale applied to FBX food models (~0.03 units). Unused for math-number cubes.")]
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

        // Pattern: every 3rd spawn (index 0, 3, 6, ...) is the correct answer.
        // Starts high so the very first obstacle spawned is always correct.
        private int _spawnsSinceCorrect = 99;

        private void Awake()
        {
            if (obstaclePrefab == null) obstaclePrefab = BuildDefaultObstaclePrefab();
        }

        private void Start()
        {
            if (GameSession.Instance != null)
                GameSession.Instance.OnRestart += OnSessionRestart;
        }

        private void OnDestroy()
        {
            if (GameSession.Instance != null)
                GameSession.Instance.OnRestart -= OnSessionRestart;
        }

        private void OnSessionRestart() => _spawnsSinceCorrect = 99;

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
            Vector3 fwd   = playerTarget.forward;
            Vector3 right = playerTarget.right;
            // Use fixed world-centre X so lanes stay on the road regardless
            // of which lane the avatar is currently standing in.
            Vector3 origin = new Vector3(0f, playerTarget.position.y, playerTarget.position.z);
            Vector3 spawnPos, targetPos;

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
            Vector3 velocity = (targetPos - spawnPos).normalized * speed;

            // Math lanes (Front/Left/Right) always use a plain cube — food prefabs
            // are only for the Bottom jump-dodge lane where the mesh stays visible.
            bool isMathLaneSpawn = (dir != ObstacleDirection.Bottom);
            var prefab = isMathLaneSpawn ? obstaclePrefab : PickPrefabForDirection(dir);
            bool usingFallback = (prefab == obstaclePrefab);

            var go = Instantiate(prefab, spawnPos, Quaternion.identity);
            go.name = $"Obstacle_{dir}";
            go.SetActive(true);
            if (!usingFallback) go.transform.localScale = Vector3.one * obstacleScale;

            if (go.GetComponent<Collider>() == null)
            {
                var bc = go.AddComponent<BoxCollider>();
                bc.isTrigger = true;
            }

            var oc = go.GetComponent<ObstacleController>();
            if (oc == null) oc = go.AddComponent<ObstacleController>();
            oc.Launch(dir, velocity, targetPos, playerTarget);

            var session = GameSession.Instance;
            if (isMathLaneSpawn && session != null)
            {
                bool spawnCorrect = (_spawnsSinceCorrect >= 2);
                if (spawnCorrect) _spawnsSinceCorrect = 0;
                else _spawnsSinceCorrect++;

                int value;
                if (spawnCorrect)
                {
                    value = session.CurrentQuestion.Answer;
                }
                else
                {
                    var wrongs = session.CurrentQuestion.WrongAnswers(3);
                    value = wrongs[Random.Range(0, wrongs.Length)];
                }
                oc.SetAsMathAnswer(value, spawnCorrect);
            }
            else if (usingFallback)
            {
                ColorizeByDirection(go, dir);
            }
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
