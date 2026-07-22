using UnityEngine;
using UnityEngine.InputSystem; 

public class FlashlightController : MonoBehaviour
{
    public bool isLightOn = false;

    public GameObject dummyPrefab; 
    public GameObject dummyPrefabPool;

    public float maxDistance = 20f;
    public float nodeInterval = 2f;

    private LightStimulus myStimulus;
    private Collider myTrigger;
    private Light myLight;
    private GameObject[] beamNodes;

    private InputAction toggleAction;

    [Header("Battery")]
    public PlayerBattery battery;
    public float kToggleCost = 2f;
    public float kContinuousDrain = 0.25f;

    private string kBatteryDrainId = "FlashLight";

    void Awake()
    {
        myStimulus = GetComponent<LightStimulus>();
        myTrigger = GetComponent<Collider>();
        myLight = GetComponent<Light>();

        toggleAction = new InputAction("ToggleFlashlight", InputActionType.Button);
        toggleAction.AddBinding("<Keyboard>/f");
        toggleAction.AddBinding("<Mouse>/leftButton");

        battery = GetComponent<PlayerBattery>();
    }

    void OnEnable()
    {
        toggleAction.Enable();
        toggleAction.performed += OnTogglePerformed;

        if (battery != null)
        {
            battery.OnLowPower += OnBatteryLowPower;
            battery.OnBatteryExtracted += OnBatteryExtracted;
        }
    }

    void OnDisable()
    {
        toggleAction.Disable();
        toggleAction.performed -= OnTogglePerformed;

        if (battery != null)
        {
            battery.OnLowPower -= OnBatteryLowPower;
            battery.OnBatteryExtracted -= OnBatteryExtracted;
        }
    }

    void Start()
    {
        int maxNodes = Mathf.CeilToInt(maxDistance / nodeInterval) + 1;
        beamNodes = new GameObject[maxNodes];
        for (int i = 0; i < maxNodes; i++)
        {
            beamNodes[i] = Instantiate(dummyPrefab, dummyPrefabPool.transform);
            beamNodes[i].SetActive(false);
        }
    }

    void Update()
    {
        if (isLightOn)
        {
            myStimulus.sourcePosition = transform.position;
            UpdateBeamNodes();
        }
    }

    private void OnTogglePerformed(InputAction.CallbackContext ctx)
    {
        ToggleLight();
    }

    private void ToggleLight()
    {
        if (isLightOn)
        {
            // 关灯
            isLightOn = false;
            battery?.RemoveDrain(kBatteryDrainId);
            if (myTrigger != null) myTrigger.enabled = false;
            if (myLight != null) myLight.enabled = false;
            HideAllNodes();
        }
        else
        {
            // 开灯：只消耗低电量阈值以上的部分，电量到阈值封底
            if (battery != null)
            {
                float availableAboveThreshold = battery.CurrentCharge - battery.LowPowerThreshold;
                if (availableAboveThreshold <= 0f)
                    return; // 已在省电模式，禁止开灯

                // 实际消耗 = min(开灯代价, 阈值以上可用电量)
                float actualCost = Mathf.Min(kToggleCost, availableAboveThreshold);
                battery.Consume(actualCost);

                // Consume 可能触发了 OnLowPower → 省电模式强制关灯，不再继续
                if (battery.IsLowPower)
                    return;
            }
            else
            {
                // 无电池组件，退化行为
                battery?.Consume(kToggleCost);
            }

            // 注册持续消耗 + 开灯
            battery?.AddDrain(kBatteryDrainId, kContinuousDrain);
            isLightOn = true;
            if (myTrigger != null) myTrigger.enabled = true;
            if (myLight != null) myLight.enabled = true;
        }
    }

    private void OnBatteryLowPower()
    {
        if (!isLightOn) return;

        isLightOn = false;
        battery.RemoveDrain(kBatteryDrainId);
        if (myTrigger != null) myTrigger.enabled = false;
        if (myLight != null) myLight.enabled = false;
        HideAllNodes();
    }

    private void OnBatteryExtracted()
    {
        // 核心电池被配电盘取出：强制关灯并禁用开关
        if (isLightOn)
        {
            isLightOn = false;
            battery.RemoveDrain(kBatteryDrainId);
            if (myTrigger != null) myTrigger.enabled = false;
            if (myLight != null) myLight.enabled = false;
            HideAllNodes();
        }
        toggleAction.Disable();
    }

    void OnDestroy()
    {
        if (battery != null && isLightOn)
            battery.RemoveDrain(kBatteryDrainId);
    }

    void UpdateBeamNodes()
    {
        HideAllNodes();
        int layerMask = LayerMask.GetMask("Environment");

        Ray ray = new Ray(transform.position, transform.forward);
        float hitDistance = maxDistance;

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, layerMask))
        {
            hitDistance = hit.distance;
            PlaceNode(0, hit.point);
        }

        int nodeIndex = 1;
        for (float d = nodeInterval; d < hitDistance; d += nodeInterval)
        {
            PlaceNode(nodeIndex, transform.position + transform.forward * d);
            nodeIndex++;
        }
    }

    void PlaceNode(int index, Vector3 pos)
    {
        if (index < beamNodes.Length)
        {
            beamNodes[index].transform.position = pos;
            beamNodes[index].GetComponent<LightStimulus>().sourcePosition = transform.position;
            beamNodes[index].SetActive(true);
        }
    }

    void HideAllNodes()
    {
        foreach (var node in beamNodes) if (node != null) node.SetActive(false);
    }





    // 绘制辅助线 
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        // 画出光线最大射程
        Gizmos.DrawRay(transform.position, transform.forward * maxDistance);

        // 画出沿途的光束节点位置预期
        for (float d = nodeInterval; d < maxDistance; d += nodeInterval)
        {
            Gizmos.DrawWireSphere(transform.position + transform.forward * d, 0.2f);
        }
    }
}