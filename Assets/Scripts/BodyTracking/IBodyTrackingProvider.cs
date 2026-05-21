namespace FlexReality.BodyTracking
{
    public interface IBodyTrackingProvider
    {
        BodyPoseData CurrentPose { get; }
        bool IsAvailable { get; }
        void Initialize();
        void Shutdown();
    }
}
