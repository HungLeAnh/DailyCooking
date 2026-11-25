using System.Collections.Generic;
using UnityEngine;

// During a close-up, hides (on this device only) any avatar standing between the close-up camera
// and its tool, the local player's own avatar included, and shows it again afterwards.
public class CloseUpOccluderHider : MonoBehaviour
{
    private const float AvatarChestHeight = 1f;
    private const float AvatarRadius = 0.6f;

    private readonly Dictionary<PlayerStateMachine, List<Renderer>> hidden = new Dictionary<PlayerStateMachine, List<Renderer>>();
    private readonly List<PlayerStateMachine> toRestore = new List<PlayerStateMachine>();

    private void LateUpdate()
    {
        CameraModeController cameraMode = CameraModeController.Instance;
        bool inCloseUp = cameraMode != null && cameraMode.Mode == CameraMode.CloseUp && cameraMode.CloseUpCameraTransform != null;

        toRestore.Clear();
        foreach (PlayerStateMachine avatar in hidden.Keys)
        {
            if (avatar == null || !inCloseUp || !Blocks(cameraMode, avatar))
                toRestore.Add(avatar);
        }
        foreach (PlayerStateMachine avatar in toRestore)
            Restore(avatar);

        if (!inCloseUp)
            return;
        foreach (PlayerStateMachine avatar in PlayerStateMachine.All)
        {
            if (avatar != null && !hidden.ContainsKey(avatar) && Blocks(cameraMode, avatar))
                Hide(avatar);
        }
    }

    private void OnDisable()
    {
        foreach (PlayerStateMachine avatar in new List<PlayerStateMachine>(hidden.Keys))
            Restore(avatar);
    }

    private static bool Blocks(CameraModeController cameraMode, PlayerStateMachine avatar)
    {
        return SegmentProximity.IsNear(cameraMode.CloseUpCameraTransform.position, cameraMode.CloseUpLookAt,
            avatar.transform.position + Vector3.up * AvatarChestHeight, AvatarRadius);
    }

    private void Hide(PlayerStateMachine avatar)
    {
        var turnedOff = new List<Renderer>();
        foreach (Renderer renderer in avatar.GetComponentsInChildren<Renderer>())
        {
            if (!renderer.enabled)
                continue;
            renderer.enabled = false;
            turnedOff.Add(renderer);
        }
        hidden[avatar] = turnedOff;
    }

    // Re-enables exactly the renderers this component turned off, nothing else.
    private void Restore(PlayerStateMachine avatar)
    {
        if (hidden.TryGetValue(avatar, out List<Renderer> renderers))
        {
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null)
                    renderer.enabled = true;
            }
        }
        hidden.Remove(avatar);
    }
}
