using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;
using System.Collections;

public class StretchDetector : MonoBehaviour
{
    public float xScaleThreshold = 1.5f;
    public LetterPrefabManager letterManager;
    private bool isStretching = false;
    private bool isReadyToDestroy = false;
    private GameObject[] spawnedLetters;

    [SerializeField] private HandGrabInteractable grabInteractable;
    private bool wasGrabbed = false;

    // References to our ML components
    [SerializeField] private MLObjectClassifier mlClassifier;
    [SerializeField] private ObjectScreenshotUtility screenshotUtility;

    // The word to decompose into (will be determined by ML classification)
    public string word;

    // Cache for the screenshot
    private Texture2D objectScreenshot;

    // Flag to track if classification has been done
    private bool hasClassified = false;

    private void Awake()
    {
        grabInteractable = GetComponentInChildren<HandGrabInteractable>();
        if (grabInteractable == null)
        {
            Debug.LogError("No HandGrabInteractable found in children of this object.");
        }

        // Find the ML classifier and screenshot utility
        if (mlClassifier == null)
        {
            mlClassifier = FindObjectOfType<MLObjectClassifier>();
        }

        if (screenshotUtility == null)
        {
            screenshotUtility = FindObjectOfType<ObjectScreenshotUtility>();
        }

        if (mlClassifier == null || screenshotUtility == null)
        {
            Debug.LogError("ML components not found! Make sure MLObjectClassifier and ObjectScreenshotUtility are in the scene.");
        }
    }

    private void Start()
    {
        letterManager = FindObjectOfType<LetterPrefabManager>();
        if (letterManager == null)
        {
            Debug.LogError("LetterPrefabManager not found in the scene!");
        }

        // Classify the object to determine its decomposition word
        // We'll do this in Start to ensure everything is initialized
        StartCoroutine(ClassifyObjectAsync());
    }

    private IEnumerator ClassifyObjectAsync()
    {
        yield return new WaitForEndOfFrame();

        if (mlClassifier != null && screenshotUtility != null)
        {
            // Take a screenshot of this object
            objectScreenshot = screenshotUtility.CaptureObject(gameObject);

            // Classify the object
            string objectName = mlClassifier.ClassifyObject(objectScreenshot);

            // Get the corresponding decomposition word
            if (!string.IsNullOrEmpty(objectName))
            {
                word = mlClassifier.GetDecompositionWord(objectName);
                Debug.Log($"Object '{objectName}' will decompose into word: {word}");

                // Set flag to indicate we've classified
                hasClassified = true;
            }
            else
            {
                Debug.LogWarning("Object classification failed! Using object name as fallback.");
                // Fallback to the object's name
                word = gameObject.name.ToLower();
                hasClassified = true;
            }
        }
        else
        {
            // Fallback for testing
            word = gameObject.name.ToLower();
            Debug.LogWarning("ML components not available, using object name as fallback: " + word);
            hasClassified = true;
        }
    }

    private void Update()
    {
        // Only process stretching if we have classified the object
        if (!hasClassified)
            return;

        // Check for stretching
        if (!isStretching)
        {
            Vector3 localScale = transform.localScale;
            if (localScale.x >= xScaleThreshold)
            {
                isStretching = true;
                TriggerDecompositionSetup();
            }
        }

        // Check if we're ready to destroy and monitor grab state
        if (isReadyToDestroy && grabInteractable != null)
        {
            bool isCurrentlyGrabbed = grabInteractable.State == InteractableState.Select;

            // Detect when grab is released
            if (wasGrabbed && !isCurrentlyGrabbed)
            {
                Debug.Log("Grab release detected!");
                CompleteDecomposition();
            }

            wasGrabbed = isCurrentlyGrabbed;
        }
    }

    private void TriggerDecompositionSetup()
    {
        Debug.Log("Stretch threshold reached! Setting up decomposition");

        // Verify we have a valid word to decompose into
        if (string.IsNullOrEmpty(word))
        {
            Debug.LogError("No decomposition word available!");
            return;
        }

        // Store all necessary values
        Vector3 startPos = transform.position;
        float spacing = 0.05f; // Spacing between letters

        // Get camera transform (HMD or main camera)
        Transform cameraTransform = Camera.main.transform;

        // Use a consistent horizontal direction based on camera view
        Vector3 forward = cameraTransform.forward;
        forward.y = 0; // Project onto horizontal plane
        forward.Normalize();

        // Define consistent right direction for letter alignment (perpendicular to camera view)
        Vector3 lineDirection = Vector3.Cross(Vector3.up, forward).normalized;

        // Calculate center offset so letters are centered around the object
        float totalWidth = (word.Length - 1) * spacing;
        Vector3 centerOffset = -lineDirection * (totalWidth / 2f);

        // Create array to store spawned letters
        spawnedLetters = new GameObject[word.Length];
        LetterChainBehavior previousLetter = null;

        // Spawn all letters first
        for (int i = 0; i < word.Length; i++)
        {
            char letter = word[i];
            // Position letter based on index
            Vector3 letterOffset = lineDirection * (i * spacing);

            // Position letters on a consistent horizontal plane at the same height as the original object
            Vector3 pos = startPos + centerOffset + letterOffset;

            GameObject prefab = letterManager.GetPrefabForLetter(letter);
            if (prefab != null)
            {
                // Make all letters face TOWARD the player by using negative of camera forward
                Quaternion rotation = Quaternion.LookRotation(-forward);

                GameObject letterObj = Instantiate(prefab, pos, rotation);
                spawnedLetters[i] = letterObj;

                // Get the letter chain behavior
                LetterChainBehavior letterChain = letterObj.GetComponent<LetterChainBehavior>();

                // Connect to previous letter if it exists
                if (previousLetter != null && letterChain != null)
                {
                    previousLetter.ConnectToRight(letterChain);
                }

                // Update previous letter for next iteration
                previousLetter = letterChain;

                // Initially hide the letters
                letterObj.SetActive(false);
                Debug.Log($"Spawned letter {letter} at position {pos} (hidden)");
            }
            else
            {
                Debug.LogWarning($"No prefab found for letter: {letter}");
            }
        }

        // Mark for destruction when grab is released
        isReadyToDestroy = true;
        wasGrabbed = true; // Assume it's grabbed at this point since we're stretching
    }

    private void CompleteDecomposition()
    {
        Debug.Log("Completing decomposition");

        // Show all the letters
        foreach (GameObject letter in spawnedLetters)
        {
            if (letter != null)
            {
                letter.SetActive(true);
            }
        }

        // Find WordChainManager to update word validation after spawning
        WordChainManager chainManager = FindObjectOfType<WordChainManager>();
        if (chainManager != null)
        {
            chainManager.UpdateWordChain();
        }

        // Clean up the screenshot if we still have it
        if (objectScreenshot != null)
        {
            Destroy(objectScreenshot);
            objectScreenshot = null;
        }

        // Destroy the original object after a tiny delay to ensure we don't interfere with events
        StartCoroutine(DestroyAfterDelay(0.1f));
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // Clean up any resources
        if (objectScreenshot != null)
        {
            Destroy(objectScreenshot);
        }
    }
}