using UnityEngine;

namespace FlexReality.BodyTracking
{
    public enum BodyKeypoint
    {
        Head,
        LeftShoulder, RightShoulder,
        LeftElbow, RightElbow,
        LeftWrist, RightWrist,
        LeftHip, RightHip,
        LeftKnee, RightKnee,
        LeftAnkle, RightAnkle,
        Count
    }

    public struct BodyKeypointData
    {
        public Vector3 Position;
        public float Confidence;
    }

    // Pose described in normalized image space:
    //   x: 0 (left) .. 1 (right)
    //   y: 0 (top)  .. 1 (bottom)   -- screen-style, "up" means smaller y
    //   z: depth, negative = closer to camera (mirrors MediaPipe convention)
    public class BodyPoseData
    {
        public BodyKeypointData[] Keypoints = new BodyKeypointData[(int)BodyKeypoint.Count];
        public bool IsTracking;
        public float Timestamp;

        public BodyKeypointData Get(BodyKeypoint kp) => Keypoints[(int)kp];

        public void Set(BodyKeypoint kp, Vector3 pos, float confidence = 1f)
        {
            Keypoints[(int)kp] = new BodyKeypointData { Position = pos, Confidence = confidence };
        }
    }
}
