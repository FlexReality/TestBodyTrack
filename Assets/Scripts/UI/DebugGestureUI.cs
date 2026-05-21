using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlexReality.BodyTracking
{
    // Renders the current gesture mask as text. Supports either TMP_Text or
    // legacy UnityEngine.UI.Text so the user can wire whichever they prefer.
    public class DebugGestureUI : MonoBehaviour
    {
        [SerializeField] private BodyGestureDetector detector;
        [SerializeField] private TMP_Text tmpLabel;
        [SerializeField] private Text legacyLabel;

        private readonly StringBuilder builder = new StringBuilder(64);

        private void OnEnable()
        {
            if (detector != null) detector.OnGesturesUpdated += UpdateLabel;
        }

        private void OnDisable()
        {
            if (detector != null) detector.OnGesturesUpdated -= UpdateLabel;
        }

        private void UpdateLabel(GestureFlags flags)
        {
            builder.Clear();
            builder.Append("Gesture: ");
            if (flags == GestureFlags.None)
            {
                builder.Append("None");
            }
            else
            {
                bool first = true;
                Append(flags, GestureFlags.Jump,         "Jump",         ref first);
                Append(flags, GestureFlags.MoveLeft,     "MoveLeft",     ref first);
                Append(flags, GestureFlags.MoveRight,    "MoveRight",    ref first);
                Append(flags, GestureFlags.HandsForward, "HandsForward", ref first);
                Append(flags, GestureFlags.RightHandUp,  "RightHandUp",  ref first);
                Append(flags, GestureFlags.LeftHandUp,   "LeftHandUp",   ref first);
            }

            var text = builder.ToString();
            if (tmpLabel != null) tmpLabel.text = text;
            if (legacyLabel != null) legacyLabel.text = text;
        }

        private void Append(GestureFlags flags, GestureFlags mask, string name, ref bool first)
        {
            if ((flags & mask) == 0) return;
            if (!first) builder.Append(" | ");
            builder.Append(name);
            first = false;
        }
    }
}
