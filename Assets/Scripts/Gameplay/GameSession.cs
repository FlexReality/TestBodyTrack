using System;
using UnityEngine;

namespace FlexReality.BodyTracking
{
    // Per-question lives model:
    //   - Each new question starts with livesPerQuestion hearts.
    //   - Wrong hit or missed correct answer → lose a life for this question.
    //   - Lives reach 0 → question skipped, next question, hearts reset.
    //   - Correct hit → score++, next question, hearts reset.
    //   - Game never ends — designed for demo/classroom use.
    public class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        [Header("Per-question lives")]
        [SerializeField] private int livesPerQuestion = 3;

        [Header("Difficulty ramp")]
        [SerializeField] private float startingSpawnInterval = 2.2f;
        [SerializeField] private float minSpawnInterval = 0.9f;
        [SerializeField] private float intervalDecayPerSecond = 0.004f;
        [SerializeField] private float startingObstacleSpeed = 4.5f;
        [SerializeField] private float maxObstacleSpeed = 9f;
        [SerializeField] private float speedRampPerSecond = 0.025f;

        public int Score { get; private set; }
        public int Lives { get; private set; }
        public bool IsAlive => true; // game never ends
        public float CurrentSpawnInterval { get; private set; }
        public float CurrentObstacleSpeed { get; private set; }
        public float Elapsed { get; private set; }
        public MathQuestion CurrentQuestion { get; private set; }

        public event Action OnScoreChanged;
        public event Action OnLivesChanged;
        public event Action OnGameOver;   // kept for compatibility, never fires
        public event Action OnRestart;
        public event Action OnQuestionChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                startingSpawnInterval  = 3.2f;
                minSpawnInterval       = 1.4f;
                intervalDecayPerSecond = 0.003f;
                startingObstacleSpeed  = 3.2f;
                maxObstacleSpeed       = 6.5f;
                speedRampPerSecond     = 0.015f;
            }

            Restart();
        }

        private void Start()
        {
            if (FindAnyObjectByType<MathQuestionUI>() == null)
                new GameObject("MathQuestionUI").AddComponent<MathQuestionUI>();
            if (FindAnyObjectByType<ScoreLivesUI>() == null)
                new GameObject("ScoreLivesUI").AddComponent<ScoreLivesUI>();
            if (FindAnyObjectByType<RestartButtonUI>() == null)
                new GameObject("RestartButtonUI").AddComponent<RestartButtonUI>();
            if (FindAnyObjectByType<EnvironmentScroller>() == null)
                new GameObject("EnvironmentScroller").AddComponent<EnvironmentScroller>();
        }

        private void Update()
        {
            Elapsed += Time.deltaTime;
            CurrentSpawnInterval = Mathf.Max(minSpawnInterval,
                startingSpawnInterval - Elapsed * intervalDecayPerSecond);
            CurrentObstacleSpeed = Mathf.Min(maxObstacleSpeed,
                startingObstacleSpeed + Elapsed * speedRampPerSecond);
        }

        // Jump-dodge success (non-math path).
        public void RegisterHit()
        {
            Score++;
            OnScoreChanged?.Invoke();
        }

        // Player hit the correct math answer.
        public void RegisterCorrectHit()
        {
            Score++;
            OnScoreChanged?.Invoke();
            NextQuestion();
        }

        // Player hit a wrong math answer, or the correct answer flew past.
        public void RegisterWrongHit()
        {
            Lives--;
            OnLivesChanged?.Invoke();
            if (Lives <= 0) NextQuestion();
        }

        public void RegisterCorrectAnswerMissed() => RegisterWrongHit();

        // Kept for backward compat with Bottom lane jump-miss.
        public void RegisterMiss() => RegisterWrongHit();

        public void NextQuestion()
        {
            CurrentQuestion = MathQuestion.GenerateRandom();
            Lives = livesPerQuestion;
            OnLivesChanged?.Invoke();
            OnQuestionChanged?.Invoke();
        }

        public void Restart()
        {
            Score = 0;
            Elapsed = 0f;
            CurrentSpawnInterval = startingSpawnInterval;
            CurrentObstacleSpeed = startingObstacleSpeed;
            OnScoreChanged?.Invoke();
            NextQuestion();
            OnRestart?.Invoke();
        }
    }
}
