using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ObjectWordDatabase", menuName = "ML/Object Word Database")]
public class ObjectWordModel : ScriptableObject
{
    public List<ObjectWordPair> objectWordPairs = new List<ObjectWordPair>();

    // Dictionary for quick lookups
    private Dictionary<string, string> objectToWordMap;

    // Initialize the dictionary when needed
    public void InitializeDictionary()
    {
        objectToWordMap = new Dictionary<string, string>();
        foreach (var pair in objectWordPairs)
        {
            if (!string.IsNullOrEmpty(pair.objectName))
            {
                objectToWordMap[pair.objectName.ToLower()] = pair.decompositionWord;
            }
        }
    }

    // Get word for an object name
    public string GetWordForObject(string objectName)
    {
        if (objectToWordMap == null)
        {
            InitializeDictionary();
        }

        objectName = objectName.ToLower();
        if (objectToWordMap.ContainsKey(objectName))
        {
            return objectToWordMap[objectName];
        }
        return null;
    }
}