using UnityEngine;

namespace FlexReality.BodyTracking
{
    // Top-level orchestrator. For the MVP its only job is to initialize the
    // provider on start and shut it down on exit so we don't leak camera
    // handles when we eventually switch to a real webcam provider.
    public class GameController : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour providerBehaviour;
        [SerializeField] private BodyGestureDetector gestureDetector;
        [SerializeField] private ObstacleSpawner spawner;

        private IBodyTrackingProvider provider;

        private void Awake()
        {
            provider = providerBehaviour as IBodyTrackingProvider;
            if (provider == null)
            {
                Debug.LogError("[GameController] Provider must implement IBodyTrackingProvider.", this);
                return;
            }
            provider.Initialize();
        }

        private void OnDestroy()
        {
            provider?.Shutdown();
        }
    }
}
