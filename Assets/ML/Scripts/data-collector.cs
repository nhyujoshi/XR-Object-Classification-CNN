using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class TrainingDataCollector : MonoBehaviour
{
    // List of objects to collect training data for
    public List<GameObject> trainingObjects = new List<GameObject>();
    
    // Number of images to generate per object
    public int imagesPerObject = 100;
    
    // Output folder for training data
    public string outputFolderPath = "TrainingData";
    
    // Reference to the screenshot utility
    public ObjectScreenshotUtility screenshotUtility;
    
    // Camera positions for varied viewpoints
    private Vector3[] cameraPositions;

    public void CollectTrainingData()
    {
        if (screenshotUtility == null)
        {
            Debug.LogError("Screenshot utility not assigned!");
            return;
        }

        // Generate camera positions for varied viewpoints
        GenerateCameraPositions();
        
        // Create the output directory if it doesn't exist
        if (!Directory.Exists(outputFolderPath))
        {
            Directory.CreateDirectory(outputFolderPath);
        }

        // Process each training object
        foreach (GameObject obj in trainingObjects)
        {
            if (obj == null) continue;
            
            string objectName = obj.name.ToLower();
            Debug.Log($"Collecting training data for: {objectName}");
            
            // Create directory for this object
            string objectDir = Path.Combine(outputFolderPath, objectName);
            if (!Directory.Exists(objectDir))
            {
                Directory.CreateDirectory(objectDir);
            }
            
            // Create a temporary instance of the object for capturing
            GameObject tempObject = Instantiate(obj);
            
            // Capture from different angles
            for (int i = 0; i < imagesPerObject; i++)
            {
                // Apply random rotation to the object
                tempObject.transform.rotation = Random.rotation;
                
                // Take a screenshot
                Texture2D screenshot = screenshotUtility.CaptureObject(tempObject);
                
                // Save the screenshot
                string filename = $"{objectName}_{i:D4}.png";
                string filePath = Path.Combine(objectDir, filename);
                SaveTextureToPNG(screenshot, filePath);
                
                // Clean up
                Destroy(screenshot);
                
                // Report progress
                if (i % 10 == 0)
                {
                    Debug.Log($"Progress: {i}/{imagesPerObject} images for {objectName}");
                }
            }
            
            // Clean up the temporary object
            Destroy(tempObject);
            
            Debug.Log($"Completed data collection for: {objectName}");
        }
        
        Debug.Log("Training data collection complete!");
    }

    private void GenerateCameraPositions()
    {
        // Generate camera positions around a unit sphere for varied viewpoints
        int positionCount = 8;
        cameraPositions = new Vector3[positionCount];
        
        for (int i = 0; i < positionCount; i++)
        {
            float angle = i * (360f / positionCount);
            float radian = angle * Mathf.Deg2Rad;
            cameraPositions[i] = new Vector3(Mathf.Cos(radian), 0.2f, Mathf.Sin(radian)).normalized * 5f;
        }
    }

    private void SaveTextureToPNG(Texture2D texture, string filePath)
    {
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(filePath, bytes);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(TrainingDataCollector))]
public class TrainingDataCollectorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        TrainingDataCollector collector = (TrainingDataCollector)target;
        
        if (GUILayout.Button("Collect Training Data"))
        {
            collector.CollectTrainingData();
        }
    }
}
#endif
