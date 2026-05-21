using System;

namespace FlexReality.BodyTracking
{
    public enum GestureType
    {
        None,
        Jump,
        MoveLeft,
        MoveRight,
        HandsForward,
        RightHandUp,
        LeftHandUp
    }

    [Flags]
    public enum GestureFlags
    {
        None         = 0,
        Jump         = 1 << 0,
        MoveLeft     = 1 << 1,
        MoveRight    = 1 << 2,
        HandsForward = 1 << 3,
        RightHandUp  = 1 << 4,
        LeftHandUp   = 1 << 5
    }
}
