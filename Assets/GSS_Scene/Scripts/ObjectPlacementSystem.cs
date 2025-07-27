using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class EnhancedObjectPlacementSystem : MonoBehaviour
{

    [Header("BCI Settings")]
    public BCIController bciController;

    [Header("UI References")]
    public Button button_struct;
    public Button button_blob;
    public Button button_wireframe;

    //color

    public Button c_button_struct;
    public Button c_button_blob;
    public Button c_button_wireframe;


    //special
    public Button button_guy;
    public Button button_particles;




    [Header("Prefab References")]
    public GameObject structure;
    public GameObject blob;
    public GameObject wireframe;

    //color
    public GameObject c_structure;
    public GameObject c_blob;
    public GameObject c_wireframe;

    //special

    public GameObject guy;
    public GameObject particles;


    [Header("Transform Control UI")]
    public GameObject transformMenuPanel;
    public Button scaleButton;
    public Button rotationButton;
    public Button colorButton;
    public Button confirmButton;

    [Header("Bar Controller UI")]
    public GameObject barControlPanel;
    public BarController barController;
    public TextMeshProUGUI barLabel;

    [Header("Instruction UI")]
    public TextMeshProUGUI instructionText;



    [Header("Placement Settings")]
    public LayerMask surfaceLayerMask = -1;
    public float raycastDistance = 100f;
    public Material previewMaterial;
    public MaterialBlinker blinker;

    [Header("Transform Settings")]
    public float minScale = 0.1f;
    public float maxScale = 2.5f;

    [Header("Camera Reference")]
    public Camera playerCamera;

    [Header("Camera Control")]
    public MonoBehaviour cameraController;

    // State variables
    private GameObject currentSelectedPrefab;
    private GameObject previewObject;
    private bool isPlacementMode = false;
    private bool isPaused = false;
    private float originalTimeScale;
    private bool isUIMode = false;
    private CursorLockMode originalCursorLockState;
    private bool originalCursorVisible;

    // Transform control states
    private bool isTransformMenuActive = false;
    private bool isTransformControlActive = false;
    private bool isObjectFrozen = false;
    private TransformControlType currentControlType = TransformControlType.None;

    // Frozen object properties
    private Vector3 frozenPosition;
    private Quaternion frozenRotation;

    // Transform values
    private float currentScale = 1f;
    private float currentRotationY = 0f;
    private float currentHue = 0f;

    // UI button colors
    private Color normalColor = Color.white;
    private Color selectedColor = Color.black;

    // Original material properties
    private Material originalMaterial;
    private Color originalColor;

    // Camera control enforcement
    private Coroutine cameraControlEnforcer;
    private Coroutine uiModeEnforcer;
    private bool forceCameraDisabled = false;
    private bool forceUIMode = false;

    public enum TransformControlType
    {
        None,
        Scale,
        Rotation,
        Color
    }

    void Start()
    {
        originalTimeScale = Time.timeScale;
        originalCursorLockState = Cursor.lockState;
        originalCursorVisible = Cursor.visible;

        if (playerCamera == null)
            playerCamera = Camera.main;

        // Setup button listeners
        if (button_struct != null)
            button_struct.onClick.AddListener(() => SelectObject(structure, button_struct));
        if (button_blob != null)
            button_blob.onClick.AddListener(() => SelectObject(blob, button_blob));
        if (button_wireframe != null)
            button_wireframe.onClick.AddListener(() => SelectObject(wireframe, button_wireframe));

        if (c_button_struct != null)
            c_button_struct.onClick.AddListener(() => SelectObject(c_structure, c_button_struct));
        if (c_button_blob != null)
            c_button_blob.onClick.AddListener(() => SelectObject(c_blob, c_button_blob));
        if (c_button_wireframe != null)
            c_button_wireframe.onClick.AddListener(() => SelectObject(c_wireframe, c_button_wireframe));

        if (button_guy != null)
            button_guy.onClick.AddListener(() => SelectObject(guy, button_guy));
        if (button_particles != null)
            button_particles.onClick.AddListener(() => SelectObject(particles, button_particles));





        // Setup transform control button listeners
        if (scaleButton != null)
            scaleButton.onClick.AddListener(() => StartTransformControl(TransformControlType.Scale));
        if (rotationButton != null)
            rotationButton.onClick.AddListener(() => StartTransformControl(TransformControlType.Rotation));
        if (colorButton != null)
            colorButton.onClick.AddListener(() => StartTransformControl(TransformControlType.Color));
        if (confirmButton != null)
            confirmButton.onClick.AddListener(ConfirmPlacement);

        ResetButtonColors();
        HideTransformMenu();
        HideBarControl();
        UpdateInstructionText();
    }

    void Update()
    {
        HandleInput();

        if (isPlacementMode && !isPaused && !isTransformMenuActive && !isTransformControlActive)
        {
            HandlePreview();
            HandleMouseClicks();
        }

        // Handle bar controller input and apply transformations
        if (isTransformControlActive && barController != null)
        {
            HandleBarControllerTransform();
        }
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !isTransformMenuActive && !isTransformControlActive)
        {
            TogglePause();
        }

        if (Input.GetKeyDown(KeyCode.Tab) && !isTransformMenuActive && !isTransformControlActive)
        {
            ToggleUIMode();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleEscapeKey();
            if(bciController != null)
                bciController.OnBtnAppOffClicked();
        }
    }

    void HandleEscapeKey()
    {
        if (isTransformControlActive)
        {
            HideBarControl();
            ShowTransformMenu();
            isTransformControlActive = false;
            currentControlType = TransformControlType.None;

            if (previewObject != null)
            {
                MaterialBlinker[] blinkers = previewObject.GetComponentsInChildren<MaterialBlinker>();
                foreach (var b in blinkers)
                {
                    Destroy(b);
                }
                SetMaterial(previewMaterial);
            }
            UpdateInstructionText();
        }
        else if (isTransformMenuActive)
        {
            HideTransformMenu();
            isTransformMenuActive = false;
            isObjectFrozen = false;

            if (previewObject != null)
            {
                SetMaterial(previewMaterial);
            }

            UpdateInstructionText();
        }
        else if (isPlacementMode)
        {
            ExitPlacementMode();
        }
        else if (isUIMode)
        {
            ExitUIMode();
        }
    }

    void HandlePreview()
    {
        if (currentSelectedPrefab == null || playerCamera == null || isObjectFrozen)
            return;

        if (previewObject == null)
        {
            CreatePreviewObject();
        }

        if (previewObject == null)
            return;

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, raycastDistance, surfaceLayerMask))
        {
            previewObject.transform.position = hit.point;

            Quaternion surfaceRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            Quaternion yRotation = Quaternion.Euler(0, currentRotationY, 0);
            previewObject.transform.rotation = surfaceRotation * yRotation;

            previewObject.transform.localScale = Vector3.one * currentScale;
            previewObject.SetActive(true);
        }
        else
        {
            previewObject.SetActive(false);
        }
    }

    void HandleMouseClicks()
    {
        if (Input.GetMouseButtonDown(0) && previewObject != null && previewObject.activeInHierarchy)
        {
            FreezeObjectForTransform();
            ShowTransformMenu();
            isTransformMenuActive = true;
        }
    }

    void HandleBarControllerTransform()
    {
        if (!isTransformControlActive || previewObject == null || !isObjectFrozen || barController == null)
            return;

        float barValue = barController.CurrentPower; // This returns a value between 0 and 1

        switch (currentControlType)
        {
            case TransformControlType.Scale:
                currentScale = Mathf.Lerp(minScale, maxScale, barValue);
                previewObject.transform.localScale = Vector3.one * currentScale;
                break;

            case TransformControlType.Rotation:
                currentRotationY = barValue * 360f;
                Vector3 up = frozenRotation * Vector3.up;
                Quaternion surfaceRotation = Quaternion.FromToRotation(Vector3.up, up);
                Quaternion yRotation = Quaternion.Euler(0, currentRotationY, 0);
                previewObject.transform.rotation = surfaceRotation * yRotation;
                break;

            case TransformControlType.Color:
                currentHue = barValue;
                ApplyColorChangeToPreviewObject();
                break;
        }
    }

    void FreezeObjectForTransform()
    {
        if (previewObject == null) return;

        frozenPosition = previewObject.transform.position;
        frozenRotation = previewObject.transform.rotation;
        isObjectFrozen = true;

        UpdateInstructionText();
        Debug.Log("Object frozen for transformation");
    }

    void CreatePreviewObject()
    {
        if (currentSelectedPrefab == null)
            return;

        previewObject = Instantiate(currentSelectedPrefab);
        previewObject.name = "Preview_" + currentSelectedPrefab.name;

        Collider[] colliders = previewObject.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        Rigidbody rb = previewObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        Renderer renderer = previewObject.GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            originalMaterial = new Material(renderer.material);
            originalColor = originalMaterial.color;
        }

        if (previewMaterial != null)
        {
            SetMaterial(previewMaterial);
        }

        currentScale = 1f;
        currentRotationY = 0f;
        currentHue = 0f;

        previewObject.transform.position = new Vector3(0, -1000, 0);
        previewObject.SetActive(true);

        Debug.Log($"Created preview object: {previewObject.name}");
    }

    void SetMaterial(Material material)
    {
        if (previewObject == null || material == null)
            return;

        Renderer[] renderers = previewObject.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            Material[] materials = new Material[renderer.materials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = material;
            }
            renderer.materials = materials;
        }
    }

    void ShowTransformMenu()
    {
        if (transformMenuPanel != null)
        {
            transformMenuPanel.SetActive(true);
        }

        if (!isUIMode)
        {
            EnterUIMode();
        }
        else
        {
            StartUIControlEnforcement();
        }

        StartCoroutine(EnsureCursorFreedom());
        UpdateInstructionText();
    }

    void HideTransformMenu()
    {
        if (transformMenuPanel != null)
        {
            transformMenuPanel.SetActive(false);
        }
        isTransformMenuActive = false;

        if (!isPlacementMode && !isPaused && !isTransformControlActive)
        {
            ExitUIMode();
        }
    }

    void ShowBarControl()
    {
        if (barControlPanel != null)
        {
            barControlPanel.SetActive(true);
        }

        FreezeCameraControlForBarControl();
        StartCoroutine(EnsureCursorFreedom());
        UpdateInstructionText();
    }

    void HideBarControl()
    {
        
        isTransformControlActive = false;

        StopCameraControlEnforcement();
        UpdateInstructionText();
    }

    void FreezeCameraControlForBarControl()
    {
        forceCameraDisabled = true;

        if (cameraController != null)
        {
            cameraController.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        StartCameraControlEnforcement();
        Debug.Log("Camera control frozen for bar control");
    }

    void StartCameraControlEnforcement()
    {
        if (cameraControlEnforcer != null)
        {
            StopCoroutine(cameraControlEnforcer);
        }
        cameraControlEnforcer = StartCoroutine(EnforceCameraControlDisabled());
    }

    void StopCameraControlEnforcement()
    {
        forceCameraDisabled = false;
        if (cameraControlEnforcer != null)
        {
            StopCoroutine(cameraControlEnforcer);
            cameraControlEnforcer = null;
        }
    }

    IEnumerator EnforceCameraControlDisabled()
    {
        while (forceCameraDisabled)
        {
            if (cameraController != null && cameraController.enabled)
            {
                cameraController.enabled = false;
            }

            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
            }
            if (!Cursor.visible)
            {
                Cursor.visible = true;
            }

            yield return new WaitForEndOfFrame();
        }
    }

    void StartUIControlEnforcement()
    {
        if (uiModeEnforcer != null)
        {
            StopCoroutine(uiModeEnforcer);
        }
        uiModeEnforcer = StartCoroutine(EnforceUIControlDisabled());
    }

    void StopUIControlEnforcement()
    {
        forceUIMode = false;
        if (uiModeEnforcer != null)
        {
            StopCoroutine(uiModeEnforcer);
            uiModeEnforcer = null;
        }
    }

    IEnumerator EnforceUIControlDisabled()
    {
        while (forceUIMode || isUIMode || isPaused || isTransformMenuActive)
        {
            if (cameraController != null && cameraController.enabled)
            {
                cameraController.enabled = false;
            }

            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
            }
            if (!Cursor.visible)
            {
                Cursor.visible = true;
            }

            yield return new WaitForEndOfFrame();
        }
    }


    void StartTransformControl(TransformControlType controlType)
    {
        currentControlType = controlType;
        isTransformControlActive = true;

        HideTransformMenu();

        if (cameraController != null)
        {
            cameraController.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ShowBarControl();

        // Add blinking effect to preview object
        if (blinker != null && previewObject != null)
        {
            Renderer[] renderers = previewObject.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                GameObject target = renderer.gameObject;

                if (target.GetComponent<MaterialBlinker>() == null)
                {
                    var blinkerInstance = target.AddComponent<MaterialBlinker>();
                    blinkerInstance.material1 = blinker.material1;
                    blinkerInstance.material2 = blinker.material2;
                    blinkerInstance.blinkSpeed = blinker.blinkSpeed;
                }
            }
        }

        // Set bar label based on control type
        if (barLabel != null)
        {
            switch (controlType)
            {
                case TransformControlType.Scale:
                    barLabel.text = "Scale Control";
                    break;
                case TransformControlType.Rotation:
                    barLabel.text = "Rotation Control";
                    break;
                case TransformControlType.Color:
                    barLabel.text = "Color Control";
                    break;
            }
        }

        //Debug.Log($"Started transform control: {controlType} - Use 'T' key to increase value");
        UpdateInstructionText();
    }

    void ApplyColorChangeToPreviewObject()
    {
        if (previewObject == null || originalMaterial == null)
            return;

        Renderer[] renderers = previewObject.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            Material[] newMaterials = new Material[renderer.materials.Length];

            for (int i = 0; i < renderer.materials.Length; i++)
            {
                Material baseMaterial = blinker != null && blinker.material1 != null ? blinker.material1 : originalMaterial;
                Material newMaterial = new Material(baseMaterial);

                Color newColor = Color.HSVToRGB(currentHue, 1f, 1f);
                newColor.a = originalColor.a;
                newMaterial.color = newColor;

                newMaterials[i] = newMaterial;
            }

            renderer.materials = newMaterials;
        }
    }

    void ConfirmPlacement()
    {
        if (previewObject == null) return;

        GameObject finalObject = Instantiate(currentSelectedPrefab, previewObject.transform.position, previewObject.transform.rotation);
        finalObject.transform.localScale = previewObject.transform.localScale;
        finalObject.name = currentSelectedPrefab.name + "_Placed";

        // Remove blinker components from final object
        MaterialBlinker[] blinkersToRemove = finalObject.GetComponentsInChildren<MaterialBlinker>();
        foreach (var b in blinkersToRemove)
        {
            Destroy(b);
        }

        // Apply color if changed
        if (currentControlType == TransformControlType.Color && currentHue > 0)
        {
            Renderer renderer = finalObject.GetComponent<Renderer>();
            if (renderer != null && originalMaterial != null)
            {
                Material newMaterial = new Material(originalMaterial);
                Color newColor = Color.HSVToRGB(currentHue, 1f, 1f);
                newColor.a = originalColor.a;
                newMaterial.color = newColor;
                renderer.material = newMaterial;
            }
        }

        HideBarControl();
        HideTransformMenu();

        if (previewObject != null)
        {
            DestroyImmediate(previewObject);
            previewObject = null;
        }

        isTransformControlActive = false;
        isTransformMenuActive = false;
        isObjectFrozen = false;
        currentControlType = TransformControlType.None;
        isPlacementMode = false;
        currentSelectedPrefab = null;

        ResetButtonColors();
        ExitUIMode();

        currentScale = 1f;
        currentRotationY = 0f;
        currentHue = 0f;

        UpdateInstructionText();
        Debug.Log($"Object placed successfully: {finalObject.name}");
    }

    public void SelectObject(GameObject prefab, Button button)
    {
        if (prefab == null)
        {
            Debug.LogWarning("Prefab is null!");
            return;
        }

        EnterUIMode();

        currentSelectedPrefab = prefab;
        isPlacementMode = true;

        currentScale = 1f;
        currentRotationY = 0f;
        currentHue = 0f;
        isTransformMenuActive = false;
        isTransformControlActive = false;
        isObjectFrozen = false;
        currentControlType = TransformControlType.None;

        HideTransformMenu();
        HideBarControl();

        if (previewObject != null)
        {
            DestroyImmediate(previewObject);
            previewObject = null;
        }

        CreatePreviewObject();

        ResetButtonColors();
        if (button != null)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = selectedColor;
            colors.selectedColor = selectedColor;
            button.colors = colors;
        }

        UpdateInstructionText();
        Debug.Log("Selected prefab: " + prefab.name + " - Preview object created");
    }

    void ResetButtonColors()
    {
        Button[] buttons = { button_struct, button_blob, button_wireframe, c_button_struct, c_button_blob, c_button_wireframe, button_guy, button_particles };

        foreach (Button btn in buttons)
        {
            if (btn != null)
            {
                ColorBlock colors = btn.colors;
                colors.normalColor = normalColor;
                colors.selectedColor = normalColor;
                btn.colors = colors;
            }
        }
    }

    void ExitPlacementMode()
    {
        isPlacementMode = false;
        currentSelectedPrefab = null;

        currentScale = 1f;
        currentRotationY = 0f;
        currentHue = 0f;

        isTransformMenuActive = false;
        isTransformControlActive = false;
        isObjectFrozen = false;
        currentControlType = TransformControlType.None;

        HideTransformMenu();
        HideBarControl();

        if (previewObject != null)
        {
            DestroyImmediate(previewObject);
            previewObject = null;
        }

        ResetButtonColors();
        ExitUIMode();

        UpdateInstructionText();
        Debug.Log("Exited placement mode");
    }

    void EnterUIMode()
    {
        if (isUIMode) return;

        isUIMode = true;
        forceUIMode = true;

        if (cameraController != null)
        {
            cameraController.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        StartUIControlEnforcement();
        StartCoroutine(EnsureCursorFreedom());

        UpdateInstructionText();
        Debug.Log("Entered UI Mode");
    }

    IEnumerator EnsureCursorFreedom()
    {
        yield return null;

        for (int i = 0; i < 3; i++)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            yield return null;
        }

        Debug.Log("Cursor freedom ensured for UI interaction");
    }

    void ExitUIMode()
    {
        if (!isUIMode) return;

        isUIMode = false;
        forceUIMode = false;

        StopUIControlEnforcement();

        if (!isTransformMenuActive && !isTransformControlActive && !isPaused)
        {
            Cursor.lockState = originalCursorLockState;
            Cursor.visible = originalCursorVisible;

            if (cameraController != null)
            {
                cameraController.enabled = true;
            }
        }

        UpdateInstructionText();
        Debug.Log("Exited UI Mode");
    }

    void ToggleUIMode()
    {
        if (isTransformMenuActive || isTransformControlActive) return;

        if (isUIMode)
        {
            ExitUIMode();
        }
        else
        {
            EnterUIMode();
        }
    }

    void TogglePause()
    {
        if (isTransformMenuActive || isTransformControlActive) return;

        isPaused = !isPaused;

        if (isPaused)
        {
            Time.timeScale = 0f;
            forceUIMode = true;

            if (!isUIMode)
            {
                EnterUIMode();
            }
            else
            {
                StartUIControlEnforcement();
            }

            Debug.Log("Game Paused");
        }
        else
        {
            Time.timeScale = originalTimeScale;
            forceUIMode = false;

            if (!isPlacementMode && !isTransformMenuActive && !isTransformControlActive)
            {
                ExitUIMode();
            }
            else
            {
                StopUIControlEnforcement();
            }

            Debug.Log("Game Resumed");
        }

        UpdateInstructionText();
    }

    void OnDestroy()
    {
        if (previewObject != null)
        {
            DestroyImmediate(previewObject);
        }

        StopCameraControlEnforcement();
        StopUIControlEnforcement();

        forceUIMode = false;
        forceCameraDisabled = false;

        Time.timeScale = originalTimeScale;
        Cursor.lockState = originalCursorLockState;
        Cursor.visible = originalCursorVisible;

        if (cameraController != null)
        {
            cameraController.enabled = true;
        }
    }

    void UpdateInstructionText()
    {
        if (instructionText == null) return;

        string currentMode = "";
        string instructions = "";

        if (isPaused)
        {
            currentMode = "PAUSED";
            instructions = "Press SPACE to resume";
        }
        else if (isTransformControlActive)
        {
            switch (currentControlType)
            {
                case TransformControlType.Scale:
                    currentMode = "SCALE CONTROL";
                    instructions = "Hold 'T' to increase scale | Release to decrease | ESC: Back to menu";
                    break;
                case TransformControlType.Rotation:
                    currentMode = "ROTATION CONTROL";
                    instructions = "Hold 'T' to increase rotation | Release to decrease | ESC: Back to menu";
                    break;
                case TransformControlType.Color:
                    currentMode = "COLOR CONTROL";
                    instructions = "Hold 'T' to change color hue | Release to decrease | ESC: Back to menu";
                    break;
                default:
                    currentMode = "TRANSFORM CONTROL";
                    instructions = "ESC: Back to menu";
                    break;
            }
        }
        else if (isTransformMenuActive)
        {
            currentMode = "TRANSFORM MENU";
            instructions = "Select Scale, Rotation, or Color | Confirm to place | ESC: Cancel";
        }
        else if (isPlacementMode)
        {
            if (isObjectFrozen)
            {
                currentMode = "OBJECT FROZEN";
                instructions = "Object ready for transformation";
            }
            else
            {
                currentMode = "PLACEMENT MODE";
                instructions = "Move mouse to position object | Left Click: Freeze for transform | ESC: Cancel";
            }
        }
        else if (isUIMode)
        {
            currentMode = "UI MODE";
            instructions = "Select object to place | TAB: Exit UI | SPACE: Pause";
        }
        else
        {
            currentMode = "GAME MODE";
            instructions = "TAB: Enter UI mode | SPACE: Pause";
        }

        instructionText.text = $"<b>[{currentMode}]</b>\n{instructions}";
    }

    // Public methods for external access
    public bool IsInPlacementMode() => isPlacementMode;
    public bool IsPaused() => isPaused;
    public bool IsInUIMode() => isUIMode;
    public bool IsTransformMenuActive() => isTransformMenuActive;
    public bool IsTransformControlActive() => isTransformControlActive;
    public bool IsObjectFrozen() => isObjectFrozen;
    public TransformControlType GetCurrentControlType() => currentControlType;
    public float GetCurrentScale() => currentScale;
    public float GetCurrentRotation() => currentRotationY;
    public float GetCurrentHue() => currentHue;
}