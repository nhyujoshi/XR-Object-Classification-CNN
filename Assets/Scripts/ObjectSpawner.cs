using System.Collections.Generic;
using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    [System.Serializable]
    public class ObjectMapping
    {
        public string word;
        public GameObject prefab;
    }

    public List<ObjectMapping> objectMappings = new List<ObjectMapping>();
    private Dictionary<string, GameObject> objectDictionary = new Dictionary<string, GameObject>();

    [Header("Fallback Objects")]
    public GameObject defaultObjectPrefab;
    public Material defaultMaterial;

    private void Awake()
    {
        // Build dictionary for quick lookup
        foreach (var mapping in objectMappings)
        {
            objectDictionary[mapping.word.ToUpper()] = mapping.prefab;
        }
    }

    public void SpawnObjectForWord(string word, Vector3 position)
    {
        word = word.ToUpper();
        GameObject prefabToSpawn = null;

        // Try to find exact match
        if (objectDictionary.TryGetValue(word, out prefabToSpawn))
        {
            GameObject spawnedObject = Instantiate(prefabToSpawn, position, Quaternion.identity);
            SetupSpawnedObject(spawnedObject);
            return;
        }

        // If no exact match, use default object with text label
        GameObject defaultObject = Instantiate(defaultObjectPrefab, position, Quaternion.identity);

        // Add text label component if needed
        TextMesh textMesh = defaultObject.GetComponentInChildren<TextMesh>();
        if (textMesh != null)
        {
            textMesh.text = word;
        }
        else
        {
            // Create a new text object as child
            GameObject textObject = new GameObject("Label");
            textObject.transform.SetParent(defaultObject.transform);
            textObject.transform.localPosition = Vector3.up * 0.5f;
            textObject.transform.localRotation = Quaternion.identity;

            textMesh = textObject.AddComponent<TextMesh>();
            textMesh.text = word;
            textMesh.fontSize = 48;
            textMesh.alignment = TextAlignment.Center;
            textMesh.anchor = TextAnchor.MiddleCenter;
        }

        SetupSpawnedObject(defaultObject);
    }

    private void SetupSpawnedObject(GameObject obj)
    {
        // Add components needed for interaction
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = obj.AddComponent<Rigidbody>();
        }

        // Make it grabbable
        Oculus.Interaction.HandGrab.HandGrabInteractable grabInteractable =
            obj.GetComponent<Oculus.Interaction.HandGrab.HandGrabInteractable>();
        if (grabInteractable == null)
        {
            // You might need custom logic here to properly set up the grab interactable
            // This is simplified and might not work directly
            obj.AddComponent<Oculus.Interaction.HandGrab.HandGrabInteractable>();
        }

        // Make it stretchable again
        StretchDetector stretchDetector = obj.AddComponent<StretchDetector>();
        stretchDetector.xScaleThreshold = 1f; // Adjust based on your needs
        stretchDetector.word = obj.name.Replace("(Clone)", "").Trim();
    }
}