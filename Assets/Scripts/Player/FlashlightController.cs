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

    void Awake()
    {
        myStimulus = GetComponent<LightStimulus>();
        myTrigger = GetComponent<Collider>();
        myLight = GetComponent<Light>();

        toggleAction = new InputAction("ToggleFlashlight", InputActionType.Button);
        toggleAction.AddBinding("<Keyboard>/f");
        toggleAction.AddBinding("<Mouse>/leftButton");
        
    }

    void OnEnable()
    {
        toggleAction.Enable();
        toggleAction.performed += OnTogglePerformed;
    }

    void OnDisable()
    {
        toggleAction.Disable();
        toggleAction.performed -= OnTogglePerformed;

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
        isLightOn = !isLightOn;

        if (myTrigger != null) myTrigger.enabled = isLightOn;
        if (myLight != null) myLight.enabled = isLightOn;

        if (!isLightOn) HideAllNodes();
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