using UnityEngine;
using UnityEngine.AI;

public class NavMeshExtention
{
    public static Vector3 FindNearestPointSmart(Vector3 target, float maxPossibleRadius = 10f)
    {
        NavMeshHit hit;
        float currentRadius = 1f;

        while (currentRadius <= maxPossibleRadius)
        {
            if (NavMesh.SamplePosition(target, out hit, currentRadius, NavMesh.AllAreas))
            {
                return hit.position;
            }
            currentRadius += .5f; // Double the radius and try again
        }
        return target;
    }
}
