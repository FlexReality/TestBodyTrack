using System;
using UnityEngine;

namespace FlexReality.BodyTracking
{
    public class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        [Header("Run rules")]
        [SerializeField] private int startingLives = 10;
        [SerializeField] private float startingSpawnInterval = 2.2f;
        [SerializeField] private float minSpawnInterval = 0.9f;
        [SerializeField] private float intervalDecayPerSecond = 0.004f;
        [SerializeField] private float startingObstacleSpeed = 4.5f;
        [SerializeField] private float maxObstacleSpeed = 9f;
        [SerializeField] private float speedRampPerSecond = 0.025f;

        public int Score { get; private set; }
        public int Lives { get; private set; }
        public bool IsAlive { get; private set; }
        public float CurrentSpawnInterval { get; private set; }
        public float CurrentObstacleSpeed { get; private set; }
        public float Elapsed { get; private set; }
        public MathQuestion CurrentQuestion { get; private set; }

        public event Action OnScoreChanged;
        public event Action OnLivesChanged;
        public event Action OnGameOver;
        public event Action OnRestart;
        public event Action OnQuestionChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            // WebGL runs ML slower → give the player more time to react.
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                startingLives           = 10;
                startingSpawnInterval   = 3.2f;
                minSpawnInterval        = 1.4f;
                intervalDecayPerSecond  = 0.003f;
                startingObstacleSpeed   = 3.2f;
                maxObstacleSpeed        = 6.5f;
                speedRampPerSecond      = 0.015f;
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

        // Jump-dodge success (non-math path).
        public void RegisterHit()
        {
            if (!IsAlive) return;
            Score++;
            OnScoreChanged?.Invoke();
        }

        // Player hit the correct math answer.
        public void RegisterCorrectHit()
        {
            if (!IsAlive) return;
            Score++;
            OnScoreChanged?.Invoke();
            NextQuestion();
        }

        // Player hit a wrong math answer.
        public void RegisterWrongHit()
        {
            if (!IsAlive) return;
            Lives--;
            OnLivesChanged?.Invoke();
            if (Lives <= 0) { IsAlive = false; OnGameOver?.Invoke(); }
        }

        // Correct answer reached the player without being hit.
        public void RegisterCorrectAnswerMissed()
        {
            if (!IsAlive) return;
            Lives--;
            OnLivesChanged?.Invoke();
            if (Lives <= 0) { IsAlive = false; OnGameOver?.Invoke(); return; }
            NextQuestion();
        }

        // Non-math miss (obstacle hit player, jump not made).
        public void RegisterMiss()
        {
            if (!IsAlive) return;
            Lives--;
            OnLivesChanged?.Invoke();
            if (Lives <= 0) { IsAlive = false; OnGameOver?.Invoke(); }
        }

        public void NextQuestion()
        {
            CurrentQuestion = MathQuestion.GenerateRandom();
            OnQuestionChanged?.Invoke();
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
            NextQuestion();  // generates first question and fires OnQuestionChanged
            OnRestart?.Invoke();
        }
    }
}
