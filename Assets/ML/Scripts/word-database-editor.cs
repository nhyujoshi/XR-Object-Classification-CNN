using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

#if UNITY_EDITOR
[CustomEditor(typeof(ObjectWordModel))]
public class ObjectWordDatabaseEditor : Editor
{
    private bool showAddNewPair = false;
    private string newObjectName = "";
    private string newDecompositionWord = "";
    private string newDescription = "";
    
    private Vector2 scrollPosition;
    
    public override void OnInspectorGUI()
    {
        ObjectWordModel database = (ObjectWordModel)target;
        
        EditorGUILayout.LabelField("Object-Word Database", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "This database maps 3D objects to their decomposition words.\n" +
            "When an object is classified, its corresponding word will be used for decomposition.",
            MessageType.Info);
        
        EditorGUILayout.Space(10);
        
        // Show existing pairs
        EditorGUILayout.LabelField("Object-Word Pairs", EditorStyles.boldLabel);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
        
        List<ObjectWordPair> pairsToRemove = new List<ObjectWordPair>();
        
        for (int i = 0; i < database.objectWordPairs.Count; i++)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Pair {i + 1}", EditorStyles.boldLabel);
            if (GUILayout.Button("Remove", GUILayout.Width(70)))
            {
                pairsToRemove.Add(database.objectWordPairs[i]);
            }
            EditorGUILayout.EndHorizontal();
            
            database.objectWordPairs[i].objectName = EditorGUILayout.TextField("Object Name", 
                database.objectWordPairs[i].objectName);
            
            database.objectWordPairs[i].decompositionWord = EditorGUILayout.TextField("Decomposition Word", 
                database.objectWordPairs[i].decompositionWord);
            
            database.objectWordPairs[i].description = EditorGUILayout.TextField("Description", 
                database.objectWordPairs[i].description);
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
        
        EditorGUILayout.EndScrollView();
        
        // Remove pairs marked for removal
        foreach (var pair in pairsToRemove)
        {
            database.objectWordPairs.Remove(pair);
        }
        
        // Add new pair
        EditorGUILayout.Space(10);
        showAddNewPair = EditorGUILayout.Foldout(showAddNewPair, "Add New Pair", true);
        if (showAddNewPair)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            newObjectName = EditorGUILayout.TextField("Object Name", newObjectName);
            newDecompositionWord = EditorGUILayout.TextField("Decomposition Word", newDecompositionWord);
            newDescription = EditorGUILayout.TextField("Description", newDescription);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Add Pair", GUILayout.Width(100)))
            {
                if (!string.IsNullOrEmpty(newObjectName) && !string.IsNullOrEmpty(newDecompositionWord))
                {
                    ObjectWordPair newPair = new ObjectWordPair
                    {
                        objectName = newObjectName,
                        decompositionWord = newDecompositionWord,
                        description = newDescription
                    };
                    
                    database.objectWordPairs.Add(newPair);
                    
                    // Clear fields
                    newObjectName = "";
                    newDecompositionWord = "";
                    newDescription = "";
                }
                else
                {
                    EditorUtility.DisplayDialog("Missing Information", 
                        "Please provide both an object name and a decomposition word.", "OK");
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        // Add a few preset pairs
        EditorGUILayout.Space(10);
        if (GUILayout.Button("Add Common Preset Pairs"))
        {
            AddPresetPairs(database);
        }
        
        // Apply changes
        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }
    }
    
    private void AddPresetPairs(ObjectWordModel database)
    {
        Dictionary<string, string> presetPairs = new Dictionary<string, string>
        {
            { "pineapple", "pineapple" },
            { "apple", "apple" },
            { "banana", "banana" },
            { "orange", "orange" },
            { "book", "book" },
            { "pencil", "pencil" },
            { "chair", "chair" },
            { "table", "table" }
        };
        
        foreach (var pair in presetPairs)
        {
            // Check if this pair already exists
            bool exists = false;
            foreach (var existingPair in database.objectWordPairs)
            {
                if (existingPair.objectName.ToLower() == pair.Key.ToLower())
                {
                    exists = true;
                    break;
                }
            }
            
            // Add if it doesn't exist
            if (!exists)
            {
                database.objectWordPairs.Add(new ObjectWordPair
                {
                    objectName = pair.Key,
                    decompositionWord = pair.Value,
                    description = $"Object: {pair.Key}, Word: {pair.Value}"
                });
            }
        }
        
        EditorUtility.SetDirty(target);
    }
    
    [MenuItem("Assets/Create/ML/Object Word Database")]
    public static void CreateObjectWordDatabase()
    {
        ObjectWordModel asset = ScriptableObject.CreateInstance<ObjectWordModel>();
        
        AssetDatabase.CreateAsset(asset, "Assets/ObjectWordDatabase.asset");
        AssetDatabase.SaveAssets();
        
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
    }
}
#endif
