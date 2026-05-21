using UnityEditor;
using UnityEngine;

namespace FlexReality.BodyTracking.EditorTools
{
    // Editor utility that turns the currently-open MVP_Webcam scene into the
    // mocap version: nukes the primitive Body/Head, drops the user's Humanoid
    // character under Player, hooks MocapBoneDriver to the tracking provider.
    //
    // Usage:
    //   1. Build / open MVP_Webcam scene as usual.
    //   2. Drag your Humanoid character (from Mixamo, already imported and
    //      configured as Humanoid) FROM the Project window INTO the scene as a
    //      CHILD of the "Player" GameObject. Set its local position to (0,0,0).
    //   3. Select that character GameObject in the Hierarchy.
    //   4. Tools → Body Tracking → Setup Mocap on Selected Character.
    public static class MocapSetupTool
    {
        private const string MenuPath = "Tools/Body Tracking/Setup Mocap on Selected Character";

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            var selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("Setup Mocap",
                    "Select your Humanoid character GameObject in the Hierarchy first.",
                    "OK");
                return;
            }
            if (!SetupOnCharacter(selected, interactive: true))
                return;

            EditorUtility.DisplayDialog("Setup Mocap",
                "Done!\n\n" +
                "1. Make sure Tracking → Model Asset is still assigned.\n" +
                "2. Stand in T-pose for ~1s when you press Play to calibrate.\n" +
                "3. Move — the character should follow your arms / head / chest.",
                "OK");
        }

        // Public API for other tools (e.g. WorldGeneratorTool) to wire a
        // freshly-instantiated character without spawning UI dialogs.
        public static bool SetupOnCharacter(GameObject character, bool interactive = false)
        {
            if (character == null) return false;

            var animator = character.GetComponent<Animator>();
            if (animator == null || !animator.isHuman)
            {
                if (interactive)
                    EditorUtility.DisplayDialog("Setup Mocap",
                        $"'{character.name}' has no Humanoid Animator. Set Rig → Humanoid → Apply on the FBX import.",
                        "OK");
                return false;
            }

            var player = GameObject.Find("Player");
            if (player == null)
            {
                if (interactive)
                    EditorUtility.DisplayDialog("Setup Mocap",
                        "No 'Player' GameObject. Build the MVP scene first.", "OK");
                return false;
            }

            var tracking = GameObject.Find("Tracking");
            if (tracking == null)
            {
                if (interactive)
                    EditorUtility.DisplayDialog("Setup Mocap",
                        "No 'Tracking' GameObject in scene.", "OK");
                return false;
            }

            if (character.transform.parent != player.transform)
            {
                Undo.SetTransformParent(character.transform, player.transform, "Mocap: parent character");
            }
            character.transform.localPosition = Vector3.zero;
            character.transform.localRotation = Quaternion.identity;

            DestroyChildIfExists(player, "Body");
            DestroyChildIfExists(player, "Head");

            MonoBehaviour provider = tracking.GetComponent<WebcamBodyTrackingProvider>();
            if (provider == null) provider = tracking.GetComponent<MockBodyTrackingProvider>();
            if (provider == null) return false;

            var driver = character.GetComponent<MocapBoneDriver>();
            if (driver == null) driver = Undo.AddComponent<MocapBoneDriver>(character);
            var so = new SerializedObject(driver);
            var providerProp = so.FindProperty("providerBehaviour");
            if (providerProp != null) providerProp.objectReferenceValue = provider;
            so.ApplyModifiedProperties();

            var avatar = player.GetComponent<PlayerAvatarController>();
            if (avatar != null)
            {
                var soA = new SerializedObject(avatar);
                var rootProp = soA.FindProperty("avatarRoot");
                if (rootProp != null && rootProp.objectReferenceValue == null)
                    rootProp.objectReferenceValue = player.transform;
                soA.ApplyModifiedProperties();
            }

            EditorUtility.SetDirty(character);
            Debug.Log($"[MocapSetup] Wired MocapBoneDriver on '{character.name}'.");
            return true;
        }

        private static void DestroyChildIfExists(GameObject parent, string childName)
        {
            var t = parent.transform.Find(childName);
            if (t != null) Undo.DestroyObjectImmediate(t.gameObject);
        }
    }
}
