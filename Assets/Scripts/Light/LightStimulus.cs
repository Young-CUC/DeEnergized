using UnityEngine;

public class LightStimulus : MonoBehaviour
{
    public enum StimulusType { FlashlightCone, LightSpot, Decoy }
    public StimulusType type;

    // 真实光源的绝对坐标（玩家坐标）
    public Vector3 sourcePosition;
    public int priority = 1;
}