using System.Collections.Generic;
using UnityEngine;

public class WordChainManager : MonoBehaviour
{
    [Header("References")]
    public WordValidator wordValidator;
    public ObjectSpawner objectSpawner;

    [Header("Squeeze Detection")]
    public float squeezeThreshold = 0.7f; // Scale ratio threshold to trigger transformation
    public bool requireValidWordForSqueeze = true; // Only spawn objects from valid words

    [Header("Visual Feedback")]
    public Color validWordColor = new Color(0.0f, 0.8f, 0.2f, 1.0f); // Default highlight color
    public bool highlightValidWords = true; // Set to false in the inspector to disable color changes completely

    private List<LetterChainBehavior> chainHeads = new List<LetterChainBehavior>();
    private Dictionary<LetterChainBehavior, Vector3> chainOriginalDistances = new Dictionary<LetterChainBehavior, Vector3>();

    private void Start()
    {
        if (wordValidator == null)
        {
            wordValidator = FindObjectOfType<WordValidator>();
        }

        if (objectSpawner == null)
        {
            objectSpawner = FindObjectOfType<ObjectSpawner>();
        }
    }

    private void Update()
    {
        // Update all chains and check for squeezing
        UpdateWordChain();
        CheckForSqueeze();
    }

    public void UpdateWordChain()
    {
        // Find all chain heads (letters with no left neighbor)
        FindChainHeads();

        // Process each chain
        foreach (var head in chainHeads)
        {
            ProcessChain(head);
        }
    }

    private void FindChainHeads()
    {
        chainHeads.Clear();

        // Find all letters in the scene
        LetterChainBehavior[] allLetters = FindObjectsOfType<LetterChainBehavior>();

        // Find those without left neighbors
        foreach (var letter in allLetters)
        {
            if (letter.leftNeighbor == null)
            {
                chainHeads.Add(letter);
            }
        }
    }

    private void ProcessChain(LetterChainBehavior head)
    {
        // Build the word by following the chain
        string word = "";

        LetterChainBehavior current = head;
        LetterChainBehavior tail = head; // Will be the last letter in the chain

        // First pass: build the word and identify the tail
        List<LetterChainBehavior> letterChain = new List<LetterChainBehavior>();
        Vector3 startPos = head.transform.position;
        Vector3 endPos = head.transform.position;

        while (current != null)
        {
            word += current.letter;
            letterChain.Add(current);

            if (current.rightNeighbor == null)
            {
                // This is the tail
                tail = current;
                endPos = current.transform.position;
            }

            current = current.rightNeighbor;
        }

        // Calculate chain dimensions
        Vector3 chainDimensions = new Vector3(
            Mathf.Abs(endPos.x - startPos.x),
            Mathf.Abs(endPos.y - startPos.y),
            Mathf.Abs(endPos.z - startPos.z)
        );

        // Store original dimensions if not already stored
        if (!chainOriginalDistances.ContainsKey(head))
        {
            chainOriginalDistances[head] = chainDimensions;
        }

        // Validate the word
        bool valid = wordValidator.IsValidWord(word);
        Debug.Log($"Word: '{word}' is {(valid ? "valid" : "invalid")}");

        // Update visuals based on validity - ONLY if highlighting is enabled
        if (highlightValidWords)
        {
            // Apply appropriate color to each letter in the chain
            foreach (var letter in letterChain)
            {
                // Set valid state on the letter behavior
                letter.SetValidWord(valid);

                // Apply appropriate color
                if (valid)
                {
                    letter.SetMaterialColor(validWordColor);
                }
                else
                {
                    letter.ResetMaterialColor();
                }
            }
        }

        // Store word validity on the head letter for reference
        head.gameObject.name = $"Letter_{head.letter}_Chain_{word}_Valid:{valid}";
    }

    private void CheckForSqueeze()
    {
        foreach (var head in chainHeads)
        {
            string word = "";

            // Calculate current chain dimensions and extract word
            LetterChainBehavior current = head;
            LetterChainBehavior tail = null;
            Vector3 startPos = head.transform.position;
            Vector3 endPos = head.transform.position;

            while (current != null)
            {
                word += current.letter;

                if (current.rightNeighbor == null)
                {
                    // This is the tail
                    tail = current;
                    endPos = current.transform.position;
                }

                current = current.rightNeighbor;
            }

            // Calculate current dimensions
            Vector3 currentDimensions = new Vector3(
                Mathf.Abs(endPos.x - startPos.x),
                Mathf.Abs(endPos.y - startPos.y),
                Mathf.Abs(endPos.z - startPos.z)
            );

            // Skip single letters or invalid words if required
            if (word.Length < 2) continue;

            bool isValidWord = wordValidator.IsValidWord(word);
            if (requireValidWordForSqueeze && !isValidWord) continue;

            // Get original dimensions
            if (!chainOriginalDistances.TryGetValue(head, out Vector3 originalDimensions))
            {
                continue;
            }

            // Check if the chain is being squeezed primarily in X direction (horizontal)
            float xScaleRatio = currentDimensions.x / originalDimensions.x;

            if (xScaleRatio <= squeezeThreshold)
            {
                // Calculate center position for spawn
                Vector3 centerPosition = (startPos + endPos) / 2f;

                TransformToObject(head, word, centerPosition);
                break; // Only transform one chain per frame
            }
        }
    }

    private void TransformToObject(LetterChainBehavior head, string word, Vector3 position)
    {
        // Gather all letters in the chain to destroy
        List<GameObject> toDestroy = new List<GameObject>();
        LetterChainBehavior current = head;

        while (current != null)
        {
            toDestroy.Add(current.gameObject);
            current = current.rightNeighbor;
        }

        // Spawn new object at the calculated position
        objectSpawner.SpawnObjectForWord(word, position);

        // Remove original chain
        foreach (var obj in toDestroy)
        {
            Destroy(obj);
        }

        // Clean up dictionary
        chainOriginalDistances.Remove(head);
    }
}