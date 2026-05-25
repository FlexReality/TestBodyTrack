using UnityEngine;

namespace FlexReality.BodyTracking
{
    // Yellow sphere fired by player gestures. Flies forward and calls
    // HitByPlayer() on the first ObstacleController it touches.
    public class AttackProjectile : MonoBehaviour
    {
        public float speed = 20f;
        public float maxDistance = 25f;
        public float colliderRadius = 0.55f;

        private Vector3 startPos;
        private bool consumed;

        private void Awake()
        {
            startPos = transform.position;

            // Kinematic Rigidbody is required for trigger-vs-trigger detection.
            var rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var sc = gameObject.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = colliderRadius;
        }

        private void Update()
        {
            if (consumed) return;
            transform.position += transform.forward * speed * Time.deltaTime;
            if (Vector3.Distance(transform.position, startPos) >= maxDistance)
                Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (consumed) return;
            var oc = other.GetComponentInParent<ObstacleController>();
            if (oc == null) return;
            consumed = true;
            oc.HitByPlayer();

            // Brief white flash then disappear.
            var mr = GetComponent<MeshRenderer>();
            if (mr != null) mr.material.color = Color.white;
            Destroy(gameObject, 0.06f);
        }
    }
}
