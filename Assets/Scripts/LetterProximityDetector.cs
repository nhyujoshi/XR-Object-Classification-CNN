using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class LetterProximityDetector : MonoBehaviour
{
    [Header("Detection Settings")]
    public float proximityThreshold = 0.1f;
    public float validationInterval = 0.5f;

    [Header("References")]
    public WordValidator wordValidator;
    public ObjectSpawner objectSpawner;

    private float validationTimer;
    private List<LetterBehavior> allLetters = new List<LetterBehavior>();

    private void Start()
    {
        // Find the word validator and object spawner
        if (wordValidator == null)
            wordValidator = FindObjectOfType<WordValidator>();

        if (objectSpawner == null)
            objectSpawner = FindObjectOfType<ObjectSpawner>();

        // Invoke with delay to ensure all letters have spawned
        Invoke("FindAllLetters", 0.5f);
    }

    private void FindAllLetters()
    {
        allLetters.Clear();
        allLetters.AddRange(FindObjectsOfType<LetterBehavior>());
    }

    private void Update()
    {
        validationTimer += Time.deltaTime;

        // Check for word formation periodically to avoid performance issues
        if (validationTimer >= validationInterval)
        {
            validationTimer = 0;
            CheckForWordFormation();
        }
    }

    private void CheckForWordFormation()
    {
        // Ensure we have letters to work with
        if (allLetters.Count == 0) return;

        // Build clusters of letters that are close to each other
        List<List<LetterBehavior>> letterClusters = BuildLetterClusters();

        // Process each cluster to form words
        foreach (var cluster in letterClusters)
        {
            if (cluster.Count < 2) continue; // Skip single letters

            // Sort letters by X position (left to right)
            cluster.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));

            // Form the word
            string word = new string(cluster.Select(l => l.Letter).ToArray());

            // Validate the word
            if (wordValidator.IsValidWord(word))
            {
                Debug.Log($"Valid word formed: {word}");

                // Spawn the corresponding object
                objectSpawner.SpawnObjectForWord(word, GetClusterCenterPosition(cluster));

                // Disable the used letters
                foreach (var letter in cluster)
                {
                    letter.gameObject.SetActive(false);
                }

                // Refresh our letter list
                FindAllLetters();
                break;
            }
        }
    }

    private List<List<LetterBehavior>> BuildLetterClusters()
    {
        List<List<LetterBehavior>> clusters = new List<List<LetterBehavior>>();
        List<LetterBehavior> remainingLetters = new List<LetterBehavior>(allLetters);

        while (remainingLetters.Count > 0)
        {
            // Start a new cluster with the first remaining letter
            List<LetterBehavior> currentCluster = new List<LetterBehavior>();
            Queue<LetterBehavior> toProcess = new Queue<LetterBehavior>();

            LetterBehavior firstLetter = remainingLetters[0];
            toProcess.Enqueue(firstLetter);
            currentCluster.Add(firstLetter);
            remainingLetters.RemoveAt(0);

            // Process queue to find all connected letters
            while (toProcess.Count > 0)
            {
                LetterBehavior current = toProcess.Dequeue();

                // Find all letters close to this one
                for (int i = remainingLetters.Count - 1; i >= 0; i--)
                {
                    LetterBehavior other = remainingLetters[i];
                    if (Vector3.Distance(current.transform.position, other.transform.position) <= proximityThreshold)
                    {
                        toProcess.Enqueue(other);
                        currentCluster.Add(other);
                        remainingLetters.RemoveAt(i);
                    }
                }
            }

            // Add completed cluster
            clusters.Add(currentCluster);
        }

        return clusters;
    }

    private Vector3 GetClusterCenterPosition(List<LetterBehavior> cluster)
    {
        Vector3 sum = Vector3.zero;
        foreach (var letter in cluster)
        {
            sum += letter.transform.position;
        }
        return sum / cluster.Count;
    }
}