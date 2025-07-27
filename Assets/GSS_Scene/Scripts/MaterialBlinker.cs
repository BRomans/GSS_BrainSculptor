using UnityEngine;

public class MaterialBlinker : MonoBehaviour
{
    [Header("Materials")]
    public Material material1;
    public Material material2;

    [Header("Blink Settings")]
    public float blinkSpeed = 1.0f; // Blinks per second

    private Renderer objectRenderer;
    private bool isUsingMaterial1 = true;
    private float timer = 0f;

    void Start()
    {
        // Get the renderer component
        objectRenderer = GetComponent<Renderer>();

       
        try
        {
            objectRenderer.material = material1;
        } catch (System.Exception e)
        {
            Debug.Log("Failed to set initial material: " + e.Message);
        }

    }

    void Update()
    {
        // Update timer
        timer += Time.deltaTime;

        // Check if it's time to switch materials
        if (timer >= (1f / blinkSpeed))
        {
            SwitchMaterial();
            timer = 0f; // Reset timer
        }
    }

    void SwitchMaterial()
    {
        if (objectRenderer == null) return;

        // Switch between materials
        if (isUsingMaterial1)
        {
            if (material2 != null)
            {
                objectRenderer.material = material2;
                isUsingMaterial1 = false;
            }
        }
        else
        {
            if (material1 != null)
            {
                objectRenderer.material = material1;
                isUsingMaterial1 = true;
            }
        }
    }

    // Optional: Method to start/stop blinking
    public void SetBlinking(bool shouldBlink)
    {
        enabled = shouldBlink;
    }

    // Optional: Method to change blink speed at runtime
    public void SetBlinkSpeed(float newSpeed)
    {
        blinkSpeed = Mathf.Max(0.1f, newSpeed); // Minimum speed to avoid division by zero
    }
}