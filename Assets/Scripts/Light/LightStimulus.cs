using UnityEngine;

public class LightStimulus : MonoBehaviour
{
    public enum StimulusType { FlashlightCone, LightSpot, Decoy, ArcFlash }
    public StimulusType type;

    public Vector3 sourcePosition;
    public int priority = 1;
}