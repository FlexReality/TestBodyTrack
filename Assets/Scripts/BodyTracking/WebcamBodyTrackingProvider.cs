using System.Collections;
using Unity.InferenceEngine;
using UnityEngine;

namespace FlexReality.BodyTracking
{
    // Real body tracking provider: webcam → BlazePose Landmarker (Unity AI
    // Inference Engine) → BodyPoseData with 13 normalized keypoints.
    //
    // Model: Unity's pre-converted BlazePose Landmarker (Lite/Full/Heavy).
    //   Source: https://huggingface.co/unity/inference-engine-blaze-pose
    //   File  : pose_landmarks_detector_lite.onnx (recommended for MVP — fast).
    //   Input : [1, 256, 256, 3] NHWC float [0..1].
    //   Output: 33 landmarks × 5 floats (x, y, z, visibility, presence).
    //           x,y are in PIXEL space of the input (0..256), z is depth in the
    //           same units. We normalize x,y to [0..1] before storing.
    //
    // MVP shortcut: we skip the BlazePose **detector** stage. The detector
    // exists to find a person bounding box in arbitrary images, but for a
    // single user facing a webcam centered in frame, feeding the center-cropped
    // square straight to the landmarker is good enough. If quality is poor at
    // unusual body positions we'll add the detector later.
    public class WebcamBodyTrackingProvider : MonoBehaviour, IBodyTrackingProvider
    {
        [Header("Model")]
        [Tooltip("BlazePose landmarker .onnx from unity/inference-engine-blaze-pose (lite is recommended for MVP).")]
        [SerializeField] private ModelAsset modelAsset;
        [Tooltip("CPU is the safest cross-platform default. Switch to GPUCompute if your GPU is fast and the model supports it.")]
        [SerializeField] private BackendType backend = BackendType.CPU;

        [Header("Webcam")]
        [SerializeField] private int requestedWidth = 640;
        [SerializeField] private int requestedHeight = 480;
        [SerializeField] private int requestedFps = 30;
        [Tooltip("Mirror webcam horizontally so 'right hand' on screen matches the player's right hand.")]
        [SerializeField] private bool mirror = true;

        [Header("Inference Rate")]
        [Tooltip("Run inference this many times per second. 30 is overkill on CPU — 15–20 is a fine sweet spot.")]
        [SerializeField] private float inferenceHz = 20f;

        [Header("Confidence")]
        [Tooltip("Mean visibility across mapped keypoints below this value flags the pose as 'not tracking'.")]
        [SerializeField] private float minConfidence = 0.2f;

        // BlazePose landmark order (MediaPipe Pose):
        //   0  Nose
        //   11 LeftShoulder    12 RightShoulder
        //   13 LeftElbow       14 RightElbow
        //   15 LeftWrist       16 RightWrist
        //   23 LeftHip         24 RightHip
        //   25 LeftKnee        26 RightKnee
        //   27 LeftAnkle       28 RightAnkle
        // The remaining 20 points are face/finger/foot details we don't need.
        private const int BlazePoseLandmarks = 33;
        private const int FloatsPerLandmark = 5; // x, y, z, visibility, presence
        private const int ModelInputSize = 256;

        private BodyPoseData pose;
        private WebCamTexture webcam;
        private Worker worker;
        private Model model;
        private RenderTexture squareRT;
        private float lastInferenceTime;
        private bool initialized;
        private bool warnedAboutShape;

        public BodyPoseData CurrentPose => pose;
        public bool IsAvailable => webcam != null && webcam.isPlaying && worker != null;
        public WebCamTexture WebcamTexture => webcam;

        public void Initialize()
        {
            if (initialized) return;
            initialized = true;

            // In WebGL the Inference Engine runs through WebAssembly — much
            // slower than native. Halve the inference rate so we keep the
            // gameplay framerate playable.
            if (Application.platform == RuntimePlatform.WebGLPlayer && inferenceHz > 10f)
            {
                inferenceHz = 10f;
                Debug.Log("[WebcamBodyTracking] WebGL detected — inferenceHz dropped to 10 for performance.");
            }

            pose = new BodyPoseData();

            if (modelAsset == null)
            {
                Debug.LogError("[WebcamBodyTracking] ModelAsset is not assigned. Drop pose_landmarks_detector_lite.onnx from unity/inference-engine-blaze-pose into Assets/Models/ and reference it here.", this);
                return;
            }

            model = ModelLoader.Load(modelAsset);
            worker = new Worker(model, backend);

            if (!StartWebcam())
            {
                Debug.LogError("[WebcamBodyTracking] No webcam available.", this);
                return;
            }

            squareRT = new RenderTexture(ModelInputSize, ModelInputSize, 0, RenderTextureFormat.ARGB32);
            squareRT.Create();
        }

        public void Shutdown()
        {
            if (webcam != null && webcam.isPlaying) webcam.Stop();
            webcam = null;
            worker?.Dispose();
            worker = null;
            if (squareRT != null) { squareRT.Release(); Destroy(squareRT); squareRT = null; }
            initialized = false;
        }

        private void Awake() => Initialize();
        private void OnDestroy() => Shutdown();

        private bool StartWebcam()
        {
            if (WebCamTexture.devices.Length == 0) return false;
            webcam = new WebCamTexture(WebCamTexture.devices[0].name, requestedWidth, requestedHeight, requestedFps);
            webcam.Play();
            StartCoroutine(WaitForWebcam());
            return true;
        }

        private IEnumerator WaitForWebcam()
        {
            while (webcam != null && webcam.width <= 16) yield return null;
            Debug.Log($"[WebcamBodyTracking] Camera ready: {webcam.deviceName} {webcam.width}x{webcam.height}");
        }

        private void Update()
        {
            if (!IsAvailable || squareRT == null) return;
            if (webcam.width <= 16) return;

            float interval = 1f / Mathf.Max(1f, inferenceHz);
            if (Time.unscaledTime - lastInferenceTime < interval) return;
            lastInferenceTime = Time.unscaledTime;

            RunInference();
        }

        private void RunInference()
        {
            // 1) Center-crop webcam → 256×256 RGB.
            BlitToSquare(webcam, squareRT, mirror);

            // 2) Tensor (NHWC, channels=3) in [0..1] float — what BlazePose expects.
            var transform = new TextureTransform()
                .SetDimensions(ModelInputSize, ModelInputSize, 3)
                .SetTensorLayout(TensorLayout.NHWC);
            using var input = TextureConverter.ToTensor(squareRT, transform);

            // 3) Schedule and peek primary output.
            worker.Schedule(input);
            var output = worker.PeekOutput() as Tensor<float>;
            if (output == null)
            {
                if (!warnedAboutShape)
                {
                    Debug.LogWarning("[WebcamBodyTracking] Primary output is null or not Tensor<float>.");
                    warnedAboutShape = true;
                }
                return;
            }

            var data = output.DownloadToArray();
            int needed = BlazePoseLandmarks * FloatsPerLandmark;
            if (data == null || data.Length < needed)
            {
                if (!warnedAboutShape)
                {
                    Debug.LogWarning($"[WebcamBodyTracking] Primary output has {data?.Length} floats, expected at least {needed}. " +
                                     "If your model's first output isn't the landmark tensor, this provider needs to PeekOutput by name.");
                    warnedAboutShape = true;
                }
                return;
            }

            ApplyKeypoints(data);
        }

        private void ApplyKeypoints(float[] data)
        {
            float visSum = 0f;
            int mapped = 0;

            void SetFrom(int blazeIdx, BodyKeypoint kp)
            {
                int o = blazeIdx * FloatsPerLandmark;
                // BlazePose returns coordinates in pixels of the input square (256).
                float xPx = data[o + 0];
                float yPx = data[o + 1];
                // data[o+2] is z (depth) — we operate in 2D, ignore.
                float vis = Sigmoid(data[o + 3]); // raw logit → 0..1
                // data[o+4] is presence — also a logit; we use visibility as primary.

                float x = Mathf.Clamp01(xPx / ModelInputSize);
                float y = Mathf.Clamp01(yPx / ModelInputSize);
                pose.Set(kp, new Vector3(x, y, 0f), vis);
                visSum += vis;
                mapped++;
            }

            // BlazePose labels by anatomy. When we mirror the input (which we
            // do so the player sees themselves like in a real mirror), the
            // anatomy flips relative to screen position, so BlazePose's "left"
            // ends up being the user's actual right side on screen. We swap
            // labels here so the gameplay sees coordinates that match what the
            // player perceives on screen.
            bool swap = mirror;
            SetFrom(0,  BodyKeypoint.Head);
            SetFrom(swap ? 12 : 11, BodyKeypoint.LeftShoulder);
            SetFrom(swap ? 11 : 12, BodyKeypoint.RightShoulder);
            SetFrom(swap ? 14 : 13, BodyKeypoint.LeftElbow);
            SetFrom(swap ? 13 : 14, BodyKeypoint.RightElbow);
            SetFrom(swap ? 16 : 15, BodyKeypoint.LeftWrist);
            SetFrom(swap ? 15 : 16, BodyKeypoint.RightWrist);
            SetFrom(swap ? 24 : 23, BodyKeypoint.LeftHip);
            SetFrom(swap ? 23 : 24, BodyKeypoint.RightHip);
            SetFrom(swap ? 26 : 25, BodyKeypoint.LeftKnee);
            SetFrom(swap ? 25 : 26, BodyKeypoint.RightKnee);
            SetFrom(swap ? 28 : 27, BodyKeypoint.LeftAnkle);
            SetFrom(swap ? 27 : 28, BodyKeypoint.RightAnkle);

            pose.Timestamp = Time.time;
            pose.IsTracking = (mapped > 0) && (visSum / mapped) > minConfidence;
        }

        private static float Sigmoid(float x) => 1f / (1f + Mathf.Exp(-x));

        // Center-crop and resize the webcam into the square target. Blit handles
        // the GPU resize for us; we rebuild a uv-rect crop matrix manually to
        // get a 1:1 square out of the typically wide camera frame.
        private static void BlitToSquare(WebCamTexture src, RenderTexture dst, bool mirror)
        {
            int w = src.width;
            int h = src.height;
            float aspect = (float)w / h;

            float uvW, uvH, uvX, uvY;
            if (aspect >= 1f) // landscape
            {
                uvH = 1f;
                uvW = (float)h / w;
                uvX = (1f - uvW) * 0.5f;
                uvY = 0f;
            }
            else // portrait
            {
                uvW = 1f;
                uvH = (float)w / h;
                uvX = 0f;
                uvY = (1f - uvH) * 0.5f;
            }

            var scale  = new Vector2(mirror ? -uvW : uvW, uvH);
            var offset = new Vector2(mirror ? uvX + uvW : uvX, uvY);
            Graphics.Blit(src, dst, scale, offset);
        }
    }
}
