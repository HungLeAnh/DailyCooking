using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerIKHandler : MonoBehaviour
{
    [SerializeField] private TwoBoneIKConstraint handRig;
    [SerializeField] private float lerpSpeed = 10f;
    
    private float targetWeight = 0f;

    private void Update()
    {
        if (handRig != null)
        {
            handRig.weight = Mathf.Lerp(handRig.weight, targetWeight, Time.deltaTime * lerpSpeed);
        }
    }

    public void SetIKWeight(float weight)
    {
        targetWeight = weight;
    }
}
