using System.Collections.Generic;
using UnityEngine;

public class LetterPrefabManager : MonoBehaviour
{
    public List<LetterEntry> letterPrefabs;
    private Dictionary<char, GameObject> letterMap;

    [System.Serializable]
    public class LetterEntry
    {
        public char letter;
        public GameObject prefab;
    }

    void Awake()
    {
        letterMap = new Dictionary<char, GameObject>();
        foreach (var entry in letterPrefabs)
        {
            letterMap[char.ToUpper(entry.letter)] = entry.prefab;
        }
    }

    public GameObject GetPrefabForLetter(char c)
    {
        letterMap.TryGetValue(char.ToUpper(c), out var prefab);
        return prefab;
    }
}
