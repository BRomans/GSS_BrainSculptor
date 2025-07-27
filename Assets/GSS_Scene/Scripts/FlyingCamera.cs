using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;

public class FlyingCamera : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float fastMoveSpeed = 10f;

    [Header("Mouse Look Settings")]
    public float mouseSensitivity = 2f;

    [Header("UI Settings")]
    public GameObject cursorUI;

    private float rotationX = 0f;
    private float rotationY = 0f;

    void Start()
    {
        // Lock cursor to center of screen
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Initialize rotation based on current transform
        rotationY = transform.eulerAngles.y;
        rotationX = transform.eulerAngles.x;

        // Create UI cursor if not assigned
        if (cursorUI == null)
        {
            CreateUICursor();
        }

        // Show UI cursor
        if (cursorUI != null)
        {
            cursorUI.SetActive(true);
        }
    }

    void Update()
    {
        HandleMouseLook();
        HandleMovement();
        UpdateCursorPosition();

        // Press Escape to unlock cursor
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (cursorUI != null) cursorUI.SetActive(false);
        }

        // Click to lock cursor again
        if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (cursorUI != null) cursorUI.SetActive(true);
        }
    }

    void HandleMouseLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Update rotation
        rotationY += mouseX;
        rotationX -= mouseY;

        // Clamp vertical rotation to prevent flipping
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);

        // Apply rotation
        transform.rotation = Quaternion.Euler(rotationX, rotationY, 0f);
    }

    void HandleMovement()
    {
        // Determine current move speed (hold Shift for fast movement)
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? fastMoveSpeed : moveSpeed;

        // Get input
        float horizontal = Input.GetAxis("Horizontal"); // A/D keys
        float vertical = Input.GetAxis("Vertical");     // W/S keys
        float upDown = 0f;

        // Q/E for up/down movement
        if (Input.GetKey(KeyCode.Q)) upDown = -1f;
        if (Input.GetKey(KeyCode.E)) upDown = 1f;

        // Calculate movement direction relative to camera
        Vector3 direction = transform.right * horizontal +
                           transform.forward * vertical +
                           Vector3.up * upDown;

        // Apply movement
        transform.position += direction * currentSpeed * Time.deltaTime;
    }

    void UpdateCursorPosition()
    {
        if (cursorUI == null) return;

        // Get camera reference
        Camera cam = GetComponent<Camera>();
        if (cam == null) return;

        // Always keep cursor at center of camera view
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);

        // Convert to UI coordinates
        RectTransform rectTransform = cursorUI.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            Canvas canvas = cursorUI.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                Vector2 uiPosition;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvas.transform as RectTransform,
                    screenCenter,
                    canvas.worldCamera,
                    out uiPosition
                );
                rectTransform.localPosition = uiPosition;
            }
        }
    }

    void CreateUICursor()
    {
        // Find or create Canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Create cursor UI
        GameObject cursor = new GameObject("UICursor");
        cursor.transform.SetParent(canvas.transform, false);

        // Add Image component
        Image cursorImage = cursor.AddComponent<Image>();
        cursorImage.color = Color.white;

        // Create a simple crosshair texture
        Texture2D crosshairTexture = new Texture2D(32, 32);
        Color[] pixels = new Color[32 * 32];

        // Fill with transparent
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }

        // Draw crosshair lines
        for (int i = 0; i < 32; i++)
        {
            // Horizontal line
            pixels[15 * 32 + i] = Color.white;
            pixels[16 * 32 + i] = Color.white;

            // Vertical line
            pixels[i * 32 + 15] = Color.white;
            pixels[i * 32 + 16] = Color.white;
        }

        crosshairTexture.SetPixels(pixels);
        crosshairTexture.Apply();

        // Create sprite and assign to image
        Sprite crosshairSprite = Sprite.Create(crosshairTexture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
        cursorImage.sprite = crosshairSprite;

        // Position at screen center initially
        RectTransform rectTransform = cursor.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(32, 32);

        cursorUI = cursor;
    }
}