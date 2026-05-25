using UnityEngine;

namespace FlexReality.BodyTracking
{
    // Static helpers that spawn code-only VFX — no prefabs required.
    public static class HitEffects
    {
        // ── Lightning bolt from shooter to target ──────────────────────────────
        public static void SpawnLightning(Vector3 from, Vector3 to)
        {
            var go = new GameObject("Lightning");
            var anim = go.AddComponent<LightningAnim>();
            anim.from = from;
            anim.to   = to;
            Object.Destroy(go, 0.25f);
        }

        // ── Shockwave ring expanding at impact point ───────────────────────────
        public static void SpawnShockwave(Vector3 pos, Color color)
        {
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Shockwave";
            Object.Destroy(ring.GetComponent<Collider>());
            ring.transform.position   = pos;
            ring.transform.localScale = new Vector3(0.1f, 0.02f, 0.1f);
            var mr = ring.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = color;
                mr.sharedMaterial = mat;
            }
            ring.AddComponent<ShockwaveAnim>();
            Object.Destroy(ring, 0.35f);
        }

        // ── Camera screen flash ────────────────────────────────────────────────
        public static void SpawnScreenFlash(Color color)
        {
            var cam = Camera.main;
            if (cam == null) return;
            var go = new GameObject("ScreenFlash");
            go.transform.SetParent(cam.transform, false);
            var flash = go.AddComponent<ScreenFlashAnim>();
            flash.flashColor    = color;
            flash.originalColor = cam.backgroundColor;
        }
    }

    // ── Lightning: zigzag LineRenderer that jitters each frame ────────────────
    public class LightningAnim : MonoBehaviour
    {
        public Vector3 from;
        public Vector3 to;

        private LineRenderer lr;
        private const int Segments = 8;

        private void Awake()
        {
            lr = gameObject.AddComponent<LineRenderer>();
            lr.positionCount = Segments + 1;
            lr.startWidth    = 0.06f;
            lr.endWidth      = 0.02f;
            lr.numCapVertices = 3;
            var mat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color"));
            mat.color = new Color(0.7f, 0.7f, 1f, 1f);
            lr.sharedMaterial = mat;
            lr.startColor = new Color(0.8f, 0.8f, 1f, 1f);
            lr.endColor   = new Color(0.4f, 0.4f, 1f, 0.4f);
        }

        private void Update()
        {
            // Redraw every frame with fresh random offsets — gives the jitter look.
            for (int i = 0; i <= Segments; i++)
            {
                float t = (float)i / Segments;
                Vector3 straight = Vector3.Lerp(from, to, t);
                float jitter = (1f - Mathf.Abs(t * 2f - 1f)) * 0.35f; // 0 at ends, max in middle
                Vector3 offset = Random.insideUnitSphere * jitter;
                lr.SetPosition(i, straight + offset);
            }
        }
    }

    // ── Shockwave: flat cylinder expands and fades ─────────────────────────────
    public class ShockwaveAnim : MonoBehaviour
    {
        private float t;
        private MeshRenderer mr;
        private const float Duration = 0.3f;

        private void Awake() => mr = GetComponent<MeshRenderer>();

        private void Update()
        {
            t += Time.deltaTime;
            float k = t / Duration;
            float radius = Mathf.Lerp(0.1f, 3.5f, k);
            transform.localScale = new Vector3(radius, 0.02f, radius);

            if (mr != null)
            {
                var c = mr.material.color;
                c.a = Mathf.Lerp(0.9f, 0f, k);
                mr.material.color = c;
            }
        }
    }

    // ── Screen flash: briefly tint camera background then restore ──────────────
    public class ScreenFlashAnim : MonoBehaviour
    {
        public Color flashColor;
        public Color originalColor;

        private float t;
        private Camera cam;
        private const float Duration = 0.18f;

        private void Awake()
        {
            cam = Camera.main;
        }

        private void Update()
        {
            t += Time.deltaTime;
            float k = t / Duration;
            if (cam != null)
                cam.backgroundColor = Color.Lerp(flashColor, originalColor, k);
            if (t >= Duration)
            {
                if (cam != null) cam.backgroundColor = originalColor;
                Destroy(gameObject);
            }
        }
    }
}
