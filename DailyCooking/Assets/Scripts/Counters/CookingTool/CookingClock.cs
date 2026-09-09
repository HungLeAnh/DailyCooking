using UnityEngine;

// Local clock for smooth UI + throttled net sync. Plain C#, testable without Unity.
// Server clock is authoritative; clients estimate locally and snap on drift.
public sealed class CookingClock
{
    public const float NetSyncInterval = 0.15f;
    public const float SnapThreshold = 0.3f;

    public float LocalCook { get; private set; }
    public float LocalBurn { get; private set; }
    public float SyncAccum { get; private set; }

    public void Reset(float netCook, float netBurn)
    {
        LocalCook = netCook;
        LocalBurn = netBurn;
        SyncAccum = 0f;
    }

    public void Clear()
    {
        LocalCook = 0f;
        LocalBurn = 0f;
        SyncAccum = 0f;
    }

    public void Tick(CookingTool.State state, float dt, bool isServer, float netCook, float netBurn)
    {
        if (state == CookingTool.State.Cooking)
        {
            LocalCook += dt;
            if (!isServer && Mathf.Abs(LocalCook - netCook) > SnapThreshold)
                LocalCook = netCook;
        }
        else if (state == CookingTool.State.Cooked)
        {
            LocalBurn += dt;
            if (!isServer && Mathf.Abs(LocalBurn - netBurn) > SnapThreshold)
                LocalBurn = netBurn;
        }
    }

    public void AddSync(float dt)
    {
        SyncAccum += dt;
    }

    public bool ShouldSync()
    {
        return SyncAccum >= NetSyncInterval;
    }

    public void MarkSynced()
    {
        SyncAccum = 0f;
    }
}
