using UnityEngine;

namespace FlexReality.BodyTracking
{
    // Plays background music on loop and triggers a "correct"/"wrong" SFX
    // whenever GameSession registers a hit or a miss. Audio clips are loaded
    // from Assets/Resources/ by name, so dropping in new files (same filename)
    // hot-swaps them.
    public class AudioManager : MonoBehaviour
    {
        [Header("Volumes (0..1) — kept low so music sits behind gameplay")]
        [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.25f;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume   = 0.55f;

        [Header("Resource Names (no extension; files live in Assets/Resources/)")]
        [SerializeField] private string backgroundClipName = "background_sound";
        [SerializeField] private string hitClipName        = "ok_sound";
        [SerializeField] private string missClipName       = "error_sound";

        private AudioSource musicSource;
        private AudioSource sfxSource;
        private AudioClip hitClip;
        private AudioClip missClip;
        private GameSession session;

        private void Awake()
        {
            // Music source — looped, low volume, 2D.
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;
            musicSource.volume = musicVolume;

            // One-shot SFX source.
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
            sfxSource.volume = sfxVolume;

            var bg = Resources.Load<AudioClip>(backgroundClipName);
            if (bg != null)
            {
                musicSource.clip = bg;
                musicSource.Play();
            }
            else
            {
                Debug.LogWarning($"[AudioManager] Resources/{backgroundClipName} not found — background music disabled.");
            }

            hitClip  = Resources.Load<AudioClip>(hitClipName);
            missClip = Resources.Load<AudioClip>(missClipName);
            if (hitClip == null)  Debug.LogWarning($"[AudioManager] Resources/{hitClipName} not found — hit SFX disabled.");
            if (missClip == null) Debug.LogWarning($"[AudioManager] Resources/{missClipName} not found — miss SFX disabled.");
        }

        private void Start()
        {
            session = GameSession.Instance ?? FindAnyObjectByType<GameSession>();
            if (session != null)
            {
                session.OnScoreChanged += HandleScoreChanged;
                session.OnLivesChanged += HandleLivesChanged;
            }
        }

        private void OnDestroy()
        {
            if (session != null)
            {
                session.OnScoreChanged -= HandleScoreChanged;
                session.OnLivesChanged -= HandleLivesChanged;
            }
        }

        // OnScoreChanged also fires on Restart with score=0; suppress that.
        private int lastScore;
        private int lastLives = -1;

        private void HandleScoreChanged()
        {
            if (session == null) return;
            if (session.Score > lastScore && hitClip != null)
                sfxSource.PlayOneShot(hitClip, sfxVolume);
            lastScore = session.Score;
        }

        private void HandleLivesChanged()
        {
            if (session == null) return;
            // Only play "error" when lives DROPPED (not on Restart, which sets them up).
            if (lastLives >= 0 && session.Lives < lastLives && missClip != null)
                sfxSource.PlayOneShot(missClip, sfxVolume);
            lastLives = session.Lives;
        }

        private void OnValidate()
        {
            // Live-update volumes when tweaked in inspector during Play.
            if (musicSource != null) musicSource.volume = musicVolume;
            if (sfxSource   != null) sfxSource.volume   = sfxVolume;
        }
    }
}
