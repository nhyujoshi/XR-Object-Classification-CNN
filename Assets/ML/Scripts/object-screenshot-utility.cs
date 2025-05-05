using UnityEngine;

public class ObjectScreenshotUtility : MonoBehaviour
{
    // Camera used to render the object
    [SerializeField] private Camera objectCamera;
    
    // Target resolution for the screenshot
    [SerializeField] private Vector2Int resolution = new Vector2Int(224, 224);
    
    // Screenshot backdrop color
    [SerializeField] private Color backgroundColor = Color.black;

    private RenderTexture renderTexture;

    private void Awake()
    {
        // Create the render texture if it doesn't exist
        if (renderTexture == null || renderTexture.width != resolution.x || renderTexture.height != resolution.y)
        {
            if (renderTexture != null)
            {
                renderTexture.Release();
            }
            
            renderTexture = new RenderTexture(resolution.x, resolution.y, 24);
        }
        
        // Create a camera if one is not assigned
        if (objectCamera == null)
        {
            GameObject cameraObject = new GameObject("ObjectClassificationCamera");
            objectCamera = cameraObject.AddComponent<Camera>();
            objectCamera.clearFlags = CameraClearFlags.SolidColor;
            objectCamera.backgroundColor = backgroundColor;
            objectCamera.targetTexture = renderTexture;
            objectCamera.enabled = false;  // Only enable when taking screenshots
        }
    }

    // Take a screenshot of the provided object
    public Texture2D CaptureObject(GameObject targetObject)
    {
        // Store the original state
        bool wasActive = targetObject.activeSelf;
        Transform originalParent = targetObject.transform.parent;
        Vector3 originalPosition = targetObject.transform.position;
        Quaternion originalRotation = targetObject.transform.rotation;
        Vector3 originalScale = targetObject.transform.localScale;
        
        try
        {
            // Position the camera to view the object
            PositionCameraForObject(targetObject);
            
            // Render to the texture
            objectCamera.targetTexture = renderTexture;
            objectCamera.Render();
            
            // Create and populate the texture
            Texture2D screenshot = new Texture2D(resolution.x, resolution.y, TextureFormat.RGB24, false);
            RenderTexture.active = renderTexture;
            screenshot.ReadPixels(new Rect(0, 0, resolution.x, resolution.y), 0, 0);
            screenshot.Apply();
            
            return screenshot;
        }
        finally
        {
            // Restore the original state
            targetObject.transform.SetParent(originalParent);
            targetObject.transform.position = originalPosition;
            targetObject.transform.rotation = originalRotation;
            targetObject.transform.localScale = originalScale;
            targetObject.SetActive(wasActive);
            
            // Clean up
            RenderTexture.active = null;
        }
    }

    private void PositionCameraForObject(GameObject targetObject)
    {
        // Temporarily unparent the object
        targetObject.transform.SetParent(null);
        
        // Enable the object if it's disabled
        targetObject.SetActive(true);
        
        // Get the object bounds
        Bounds bounds = CalculateObjectBounds(targetObject);
        
        // Position the camera to see the entire object
        float objectSize = bounds.size.magnitude;
        float distance = objectSize * 1.5f;  // Add some margin
        
        // Position the camera looking at the object center
        objectCamera.transform.position = bounds.center - objectCamera.transform.forward * distance;
        objectCamera.transform.LookAt(bounds.center);
    }

    private Bounds CalculateObjectBounds(GameObject obj)
    {
        // Get all renderers in the object hierarchy
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        
        if (renderers.Length == 0)
        {
            // If no renderers, use a default size around the object's position
            return new Bounds(obj.transform.position, Vector3.one);
        }
        
        // Start with the first renderer's bounds
        Bounds bounds = renderers[0].bounds;
        
        // Expand to include all renderers
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        
        return bounds;
    }

    private void OnDestroy()
    {
        // Clean up resources
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
    }
}
