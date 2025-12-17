using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class Main : MonoBehaviour
{
    [Header("Prefab and Materials")]
    public GameObject cubePrefab;
    public Material[] materials = new Material[4]; // 0:青, 1:緑, 2:赤, 3:黄

    [Header("Input Actions")]
    public InputActionAsset inputActions;
    private Dictionary<string, InputAction> keyActions = new Dictionary<string, InputAction>();
    private Dictionary<string, int> keyToMaterial = new Dictionary<string, int>
    {
        { "DropD", 0 },
        { "DropF", 1 },
        { "DropJ", 2 },
        { "DropK", 3 }
    };
    private InputAction retryAction;

    [Header("Settings")]
    public int instanceCount = 10;
    public float forceStrength = 5f;

    [Header("Audio")]
    public AudioClip dropSound;
    public AudioClip invalidKeySound;
    private AudioSource audioSource;

    [Header("UI")]
    public TimerUI timer;

    private class CubeData
    {
        public GameObject cube;
        public Material originalMaterial;

        public CubeData(GameObject cube, Material originalMaterial)
        {
            this.cube = cube;
            this.originalMaterial = originalMaterial;
        }
    }

    private List<CubeData> cubeStack = new List<CubeData>();
    private int currentIndex = 0;

    void Awake()
    {
        if (inputActions == null)
        {
            Debug.LogError("InputActions が設定されていません。");
            return;
        }

        InputActionMap actionMap = inputActions.FindActionMap("GamePlay", throwIfNotFound: false);
        if (actionMap == null)
        {
            Debug.LogError("GamePlay アクションマップが見つかりません。");
            return;
        }

        foreach (var pair in keyToMaterial)
        {
            InputAction action = actionMap.FindAction(pair.Key, throwIfNotFound: false);
            if (action != null)
            {
                keyActions[pair.Key] = action;
            }
            else
            {
                Debug.LogError($"{pair.Key} アクションが見つかりません。");
            }
        }

        retryAction = actionMap.FindAction("Retry", throwIfNotFound: false);
        if (retryAction == null)
        {
            Debug.LogError("Retry アクションが見つかりません。");
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void OnEnable()
    {
        foreach (var pair in keyActions)
        {
            pair.Value.Enable();
            pair.Value.performed += ctx => HandleDrop(keyToMaterial[pair.Key]);
        }

        if (retryAction != null)
        {
            retryAction.Enable();
            retryAction.performed += ctx => HandleRetry();
        }
    }

    void OnDisable()
    {
        foreach (var pair in keyActions)
        {
            pair.Value.performed -= ctx => HandleDrop(keyToMaterial[pair.Key]);
            pair.Value.Disable();
        }

        if (retryAction != null)
        {
            retryAction.performed -= ctx => HandleRetry();
            retryAction.Disable();
        }
    }

    void Start()
    {
        GenerateStack();
    }

    void GenerateStack()
    {
        foreach (CubeData data in cubeStack)
        {
            if (data.cube != null)
            {
                Destroy(data.cube);
            }
        }
        cubeStack.Clear();
        currentIndex = 0;

        float yOffset = (instanceCount - 1) / 2f;

        for (int i = 0; i < instanceCount; i++)
        {
            float y = i - yOffset;
            Vector3 position = new Vector3(0, y, 0);
            Quaternion rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            GameObject cube = Instantiate(cubePrefab, position, rotation);

            Material assignedMaterial = null;

            if (i != 0)
            {
                int matIndex = Random.Range(0, materials.Length);
                assignedMaterial = materials[matIndex];

                Renderer renderer = cube.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material brightened = new Material(assignedMaterial);
                    brightened.color = AdjustBrightness(assignedMaterial.color, 1.5f);
                    renderer.material = brightened;
                }
            }

            Rigidbody rb = cube.GetComponent<Rigidbody>() ?? cube.AddComponent<Rigidbody>();
            if (i == 0 || i != instanceCount - 1)
            {
                rb.constraints = RigidbodyConstraints.FreezeAll;
            }

            cubeStack.Insert(0, new CubeData(cube, assignedMaterial));
        }

        UpdateCameraBackground();
    }

    void UpdateCameraBackground()
    {
        if (currentIndex >= cubeStack.Count) return;

        CubeData top = cubeStack[currentIndex];
        Renderer rend = top.cube?.GetComponent<Renderer>();
        if (rend?.material == null) return;

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            Color darkColor = AdjustBrightness(top.originalMaterial.color, 0.5f);
            cam.backgroundColor = darkColor;
        }
    }

    Color AdjustBrightness(Color color, float brightnessFactor)
    {
        Color.RGBToHSV(color, out float h, out float s, out float v);

        if (brightnessFactor > 1f)
        {
            s = Mathf.Clamp01(s * (1f / brightnessFactor));
        }
        else
        {
            v = Mathf.Clamp01(v * brightnessFactor);
        }

        return Color.HSVToRGB(h, s, v);
    }

    void HandleDrop(int materialIndex)
    {
        if (currentIndex >= cubeStack.Count) return;

        CubeData top = cubeStack[currentIndex];
        if (top.originalMaterial != materials[materialIndex])
        {
            audioSource?.PlayOneShot(invalidKeySound);
            timer?.AdvanceTime(5f);
            return;
        }

        audioSource?.PlayOneShot(dropSound);
        timer?.AddScore(1);

        Rigidbody rb = top.cube.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.None;
            Vector3 randomForce = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(0.5f, 1.5f),
                Random.Range(-1f, 1f)
            ).normalized * forceStrength;

            rb.AddForce(randomForce, ForceMode.Impulse);
        }

        currentIndex++;

        if (currentIndex < cubeStack.Count - 1)
        {
            Rigidbody nextRb = cubeStack[currentIndex].cube.GetComponent<Rigidbody>();
            if (nextRb != null) nextRb.constraints = RigidbodyConstraints.None;
            UpdateCameraBackground();
        }
        else
        {
            Invoke(nameof(GenerateStack), 1.0f);
        }
    }

    void HandleRetry()
    {
        GenerateStack();
    }
}
