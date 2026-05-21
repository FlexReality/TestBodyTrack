using System;
using UnityEngine;

namespace FlexReality.BodyTracking
{
    // Single source of truth for run state: score, lives, ramp difficulty,
    // game-over / restart. Other systems subscribe to events rather than
    // reaching into this directly, so the scene wiring stays loose.
    public class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        [Header("Run rules")]
        [SerializeField] private int startingLives = 5;
        [SerializeField] private float startingSpawnInterval = 2.2f;
        [SerializeField] private float minSpawnInterval = 0.9f;
        [SerializeField] private float intervalDecayPerSecond = 0.005f;  // shaves this many sec/sec
        [SerializeField] private float startingObstacleSpeed = 4.5f;
        [SerializeField] private float maxObstacleSpeed = 9f;
        [SerializeField] private float speedRampPerSecond = 0.03f;

        public int Score { get; private set; }
        public int Lives { get; private set; }
        public bool IsAlive { get; private set; }
        public float CurrentSpawnInterval { get; private set; }
        public float CurrentObstacleSpeed { get; private set; }
        public float Elapsed { get; private set; }

        public event Action OnScoreChanged;
        public event Action OnLivesChanged;
        public event Action OnGameOver;
        public event Action OnRestart;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            // WebGL runs ML slower → give the player more time to react and
            // ramp difficulty more gently. Desktop keeps the original tuning.
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                startingLives           = 7;
                startingSpawnInterval   = 3.2f;
                minSpawnInterval        = 1.4f;
                intervalDecayPerSecond  = 0.003f;
                startingObstacleSpeed   = 3.2f;
                maxObstacleSpeed        = 6.5f;
                speedRampPerSecond      = 0.02f;
            }

            Restart();
        }

        private void Update()
        {
            if (!IsAlive) return;
            Elapsed += Time.deltaTime;
            CurrentSpawnInterval = Mathf.Max(minSpawnInterval, startingSpawnInterval - Elapsed * intervalDecayPerSecond);
            CurrentObstacleSpeed = Mathf.Min(maxObstacleSpeed, startingObstacleSpeed + Elapsed * speedRampPerSecond);
        }

        public void RegisterHit()
        {
            if (!IsAlive) return;
            Score++;
            OnScoreChanged?.Invoke();
        }

        public void RegisterMiss()
        {
            if (!IsAlive) return;
            Lives--;
            OnLivesChanged?.Invoke();
            if (Lives <= 0)
            {
                IsAlive = false;
                OnGameOver?.Invoke();
            }
        }

        public void Restart()
        {
            Score = 0;
            Lives = startingLives;
            Elapsed = 0f;
            CurrentSpawnInterval = startingSpawnInterval;
            CurrentObstacleSpeed = startingObstacleSpeed;
            IsAlive = true;
            OnScoreChanged?.Invoke();
            OnLivesChanged?.Invoke();
            OnRestart?.Invoke();
        }
    }
}
