using System.IO;
using System.Diagnostics;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

#if UNITY_EDITOR
[CustomEditor(typeof(MLObjectClassifier))]
public class MLManagerEditor : Editor
{
    // Paths
    private string pythonPath = @"venv\Scripts\python.exe";
    private string trainingScriptPath = "Assets/ML/Scripts/train_model.py";
    private string onnxModelPath = "Assets/ML/Model/object_classifier.onnx";
    private string sentisModelPath = "Assets/ML/Model/object_classifier.sentis";
    
    // UI state
    private bool showPythonSettings = false;
    private bool showDataCollection = false;
    private bool showTraining = false;
    private bool showModel = false;
    private Vector2 scrollPosition;
    
    // Training objects
    private List<GameObject> trainingObjects = new List<GameObject>();
    
    // Training data folder
    private string trainingDataFolder = "Assets/ML/Training";
    
    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("ML Training Pipeline", EditorStyles.boldLabel);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // Python Settings
        showPythonSettings = EditorGUILayout.Foldout(showPythonSettings, "Python Settings", true);
        if (showPythonSettings)
        {
            EditorGUI.indentLevel++;
            pythonPath = EditorGUILayout.TextField("Python Path", pythonPath);
            trainingScriptPath = EditorGUILayout.TextField("Training Script Path", trainingScriptPath);
            
            if (GUILayout.Button("Test Python Environment"))
            {
                TestPythonEnvironment();
            }
            EditorGUI.indentLevel--;
        }
        
        // Data Collection
        showDataCollection = EditorGUILayout.Foldout(showDataCollection, "Data Collection", true);
        if (showDataCollection)
        {
            EditorGUI.indentLevel++;
            
            // Training objects
            EditorGUILayout.LabelField("Training Objects", EditorStyles.boldLabel);
            
            // Display a button to add a new object
            if (GUILayout.Button("Add Training Object"))
            {
                trainingObjects.Add(null);
            }
            
            // Display all objects in the list
            for (int i = 0; i < trainingObjects.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                trainingObjects[i] = (GameObject)EditorGUILayout.ObjectField(
                    trainingObjects[i], typeof(GameObject), false);
                
                if (GUILayout.Button("X", GUILayout.Width(30)))
                {
                    trainingObjects.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.Space(5);
            trainingDataFolder = EditorGUILayout.TextField("Output Folder", trainingDataFolder);
            
            if (GUILayout.Button("Collect Training Data"))
            {
                CollectTrainingData();
            }
            
            EditorGUI.indentLevel--;
        }
        
        // Training
        showTraining = EditorGUILayout.Foldout(showTraining, "Model Training", true);
        if (showTraining)
        {
            EditorGUI.indentLevel++;
            
            EditorGUILayout.HelpBox(
                "Ensure you have collected training data before training the model.\n" +
                "The training process will happen in a Python environment and might take some time.",
                MessageType.Info);
            
            if (GUILayout.Button("Train Model"))
            {
                TrainModel();
            }
            
            EditorGUI.indentLevel--;
        }
        
        // Model Management
        showModel = EditorGUILayout.Foldout(showModel, "Model Management", true);
        if (showModel)
        {
            EditorGUI.indentLevel++;
            
            sentisModelPath = EditorGUILayout.TextField("Sentis Model Path", sentisModelPath);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Import ONNX Model"))
            {
                ImportONNXModel();
            }
            
            if (GUILayout.Button("Select ONNX File"))
            {
                string path = EditorUtility.OpenFilePanel("Select ONNX Model", "", "onnx");
                if (!string.IsNullOrEmpty(path))
                {
                    onnxModelPath = path;
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void TestPythonEnvironment()
    {
        try
        {
            Process process = new Process();
            process.StartInfo.FileName = pythonPath;
            process.StartInfo.Arguments = "--version";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            
            if (!string.IsNullOrEmpty(error))
            {
                EditorUtility.DisplayDialog("Python Error", 
                    $"Error testing Python environment: {error}", "OK");
                return;
            }
            
            EditorUtility.DisplayDialog("Python Environment", 
                $"Python version detected: {output}", "OK");
            
            // Check for required Python libraries
            CheckPythonLibrary("tensorflow");
            CheckPythonLibrary("numpy");
            CheckPythonLibrary("tf2onnx");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("Python Error", 
                $"Error testing Python environment: {e.Message}", "OK");
        }
    }
    private void CheckPythonLibrary(string library)
    {
        try
        {
            Process process = new Process();
            process.StartInfo.FileName = pythonPath;
            process.StartInfo.Arguments = $"-c \"import {library}; print(getattr({library}, '__version__', 'No __version__'))\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            string output = process.StandardOutput.ReadToEnd().Trim();
            string error = process.StandardError.ReadToEnd().Trim();
            process.WaitForExit();

            UnityEngine.Debug.Log($"[CheckPythonLibrary] Output for {library}:\n{output}");
            if (!string.IsNullOrEmpty(error))
            {
                UnityEngine.Debug.LogWarning($"[CheckPythonLibrary] STDERR for {library}:\n{error}");
            }

            if (string.IsNullOrEmpty(output) || output.ToLower().Contains("error") || output.Contains("Traceback"))
            {
                EditorUtility.DisplayDialog("Python Library Issue",
                    $"There was an issue importing '{library}'. Check the Unity Console for details.\n\n" +
                    $"You may need to install it via:\n\npip install {library}", "OK");
            }
            else
            {
                UnityEngine.Debug.Log($"✅ Found {library} version: {output}");
                EditorUtility.DisplayDialog("Python Library Check",
                    $"Successfully imported '{library}' version:\n{output}", "OK");
            }
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("Python Error",
                $"Error checking for library '{library}':\n{e.Message}", "OK");
        }
    }


    private void CollectTrainingData()
    {
        // Ensure we have objects to collect data for
        if (trainingObjects.Count == 0)
        {
            EditorUtility.DisplayDialog("No Training Objects", 
                "Please add at least one training object before collecting data.", "OK");
            return;
        }
        
        // Find or create the screenshot utility
        ObjectScreenshotUtility screenshotUtility = FindObjectOfType<ObjectScreenshotUtility>();
        if (screenshotUtility == null)
        {
            GameObject utilityObject = new GameObject("ObjectScreenshotUtility");
            screenshotUtility = utilityObject.AddComponent<ObjectScreenshotUtility>();
        }
        
        // Create or find the data collector
        TrainingDataCollector dataCollector = FindObjectOfType<TrainingDataCollector>();
        if (dataCollector == null)
        {
            GameObject collectorObject = new GameObject("TrainingDataCollector");
            dataCollector = collectorObject.AddComponent<TrainingDataCollector>();
        }
        
        // Configure the data collector
        dataCollector.trainingObjects = new List<GameObject>(trainingObjects);
        dataCollector.outputFolderPath = trainingDataFolder;
        dataCollector.screenshotUtility = screenshotUtility;
        
        // Start data collection
        dataCollector.CollectTrainingData();
        
        EditorUtility.DisplayDialog("Data Collection Complete", 
            $"Training data has been collected and saved to '{trainingDataFolder}'.", "OK");
    }
    
    private void TrainModel()
    {
        // Check if the training data directory exists
        if (!Directory.Exists(trainingDataFolder))
        {
            EditorUtility.DisplayDialog("Missing Training Data", 
                $"The training data folder '{trainingDataFolder}' does not exist. " +
                "Please collect training data first.", "OK");
            return;
        }
        
        // Check if the training script exists
        if (!File.Exists(trainingScriptPath))
        {
            EditorUtility.DisplayDialog("Missing Training Script", 
                $"The training script '{trainingScriptPath}' does not exist. " +
                "Please make sure the path is correct.", "OK");
            return;
        }
        
        try
        {
            // Start the python process to train the model
            Process process = new Process();
            process.StartInfo.FileName = pythonPath;
            process.StartInfo.Arguments = trainingScriptPath;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            
            process.Start();
            
            // Show a dialog indicating training has started
            EditorUtility.DisplayDialog("Training Started", 
                "Model training has started. This may take some time.\n\n" +
                "You can check the console for progress updates.", "OK");
            
            // Handle the output
            process.OutputDataReceived += (sender, e) => {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    UnityEngine.Debug.Log($"Training: {e.Data}");
                }
            };
            
            process.ErrorDataReceived += (sender, e) => {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    UnityEngine.Debug.LogError($"Training Error: {e.Data}");
                }
            };
            
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            // Wait for the process to complete
            process.WaitForExit();
            
            if (process.ExitCode == 0)
            {
                EditorUtility.DisplayDialog("Training Complete", 
                    "Model training completed successfully! You can now import the ONNX model.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Training Error", 
                    "An error occurred during model training. Check the console for details.", "OK");
            }
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("Training Error", 
                $"Error starting training process: {e.Message}", "OK");
        }
    }
    
    private void ImportONNXModel()
    {
        try
        {
            // Check if the ONNX model exists
            if (!File.Exists(onnxModelPath))
            {
                EditorUtility.DisplayDialog("Missing ONNX Model", 
                    $"The ONNX model '{onnxModelPath}' does not exist. " +
                    "Please make sure the path is correct.", "OK");
                return;
            }
            
            // Make sure the directory for the Sentis model exists
            string sentisDir = Path.GetDirectoryName(sentisModelPath);
            if (!Directory.Exists(sentisDir))
            {
                Directory.CreateDirectory(sentisDir);
            }
            
            // Import the ONNX model for Sentis
            // Note: This assumes the model can be imported directly
            // In a real implementation, you might need to use the Unity Sentis API
            File.Copy(onnxModelPath, sentisModelPath, true);
            
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Import Complete", 
                "ONNX model has been imported for use with Unity Sentis!", "OK");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("Import Error", 
                $"Error importing ONNX model: {e.Message}", "OK");
        }
    }
}
#endif
