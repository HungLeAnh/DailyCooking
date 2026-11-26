using System.Collections.Generic;
using UnityEngine;

// Hides avatars (on this device only) that would block the view:
// - in a close-up, any avatar standing between the close-up camera and its tool, the local
//   player's own avatar included;
// - in the walking view, the local avatar when the camera has been pulled in right behind its
//   head (e.g. against a wall) and would fill the screen.
// The food an avatar is holding is hidden with it. Everything comes back when it stops blocking.
public class CloseUpOccluderHider : MonoBehaviour
{
    private static readonly float[] AvatarSampleHeights = { 1.0f, 1.7f }; // chest, head
    private const float AvatarRadius = 0.6f;
    private const float WalkHeadHeight = 1.2f;
    private const float WalkTooCloseDistance = 1.2f;

    private sealed class HiddenAvatar
    {
        public readonly List<Renderer> TurnedOff = new List<Renderer>();
        // The held food's renderers, fetched again only when the avatar holds something else.
        public KitchenObject Held;
        public Renderer[] HeldRenderers;
    }

    private readonly Dictionary<PlayerStateMachine, HiddenAvatar> hidden = new Dictionary<PlayerStateMachine, HiddenAvatar>();
    private readonly List<PlayerStateMachine> toRestore = new List<PlayerStateMachine>();

    private void LateUpdate()
    {
        CameraModeController cameraMode = CameraModeController.Instance;

        toRestore.Clear();
        foreach (PlayerStateMachine avatar in hidden.Keys)
        {
            if (avatar == null || !ShouldHide(cameraMode, avatar))
                toRestore.Add(avatar);
        }
        foreach (PlayerStateMachine avatar in toRestore)
            Restore(avatar);

        if (cameraMode == null)
            return;
        // By index: foreach over the IReadOnlyList interface allocates an enumerator every frame.
        IReadOnlyList<PlayerStateMachine> avatars = PlayerStateMachine.All;
        for (int i = 0; i < avatars.Count; i++)
        {
            PlayerStateMachine avatar = avatars[i];
            if (avatar == null || !ShouldHide(cameraMode, avatar))
                continue;
            if (!hidden.TryGetValue(avatar, out HiddenAvatar entry))
            {
                entry = new HiddenAvatar();
                hidden[avatar] = entry;
                TurnOff(avatar.GetComponentsInChildren<Renderer>(), entry.TurnedOff);
            }
            // Food picked up while hidden is hidden too.
            KitchenObject held = avatar.GetKitchenObject();
            if (held != entry.Held)
            {
                entry.Held = held;
                entry.HeldRenderers = held != null ? held.GetComponentsInChildren<Renderer>() : null;
            }
            if (entry.HeldRenderers != null)
                TurnOff(entry.HeldRenderers, entry.TurnedOff);
        }
    }

    private void OnDisable()
    {
        foreach (PlayerStateMachine avatar in new List<PlayerStateMachine>(hidden.Keys))
            Restore(avatar);
    }

    private static bool ShouldHide(CameraModeController cameraMode, PlayerStateMachine avatar)
    {
        if (cameraMode == null)
            return false;
        if (cameraMode.Mode == CameraMode.CloseUp && cameraMode.CloseUpCameraTransform != null)
        {
            Vector3 cameraPosition = cameraMode.CloseUpCameraTransform.position;
            foreach (float height in AvatarSampleHeights)
            {
                if (SegmentProximity.IsNear(cameraPosition, cameraMode.CloseUpLookAt,
                        avatar.transform.position + Vector3.up * height, AvatarRadius))
                    return true;
            }
            return false;
        }
        if (cameraMode.Mode == CameraMode.Walk && avatar == PlayerStateMachine.LocalInstance)
        {
            Camera camera = Camera.main;
            return camera != null && Vector3.Distance(camera.transform.position,
                avatar.transform.position + Vector3.up * WalkHeadHeight) < WalkTooCloseDistance;
        }
        return false;
    }

    private static void TurnOff(Renderer[] renderers, List<Renderer> turnedOff)
    {
        foreach (Renderer renderer in renderers)
        {
            if (!renderer.enabled)
                continue;
            renderer.enabled = false;
            turnedOff.Add(renderer);
        }
    }

    // Re-enables exactly the renderers this component turned off, nothing else.
    private void Restore(PlayerStateMachine avatar)
    {
        if (hidden.TryGetValue(avatar, out HiddenAvatar entry))
        {
            foreach (Renderer renderer in entry.TurnedOff)
            {
                if (renderer != null)
                    renderer.enabled = true;
            }
        }
        hidden.Remove(avatar);
    }
}
