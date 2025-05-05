using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class WordValidator : MonoBehaviour
{
    [SerializeField] private TextAsset dictionaryFile;
    private HashSet<string> validWords = new HashSet<string>();

    [SerializeField] private int minWordLength = 3;

    private void Awake()
    {
        LoadDictionary();
    }

    private void LoadDictionary()
    {
        if (dictionaryFile != null)
        {
            string[] lines = dictionaryFile.text.Split('\n');
            foreach (string line in lines)
            {
                string word = line.Trim().ToUpper();
                if (!string.IsNullOrEmpty(word))
                {
                    validWords.Add(word);
                }
            }
            Debug.Log($"Loaded {validWords.Count} words into dictionary");
        }
        else
        {
            Debug.LogError("Dictionary file not assigned!");
        }
    }

    public bool IsValidWord(string word)
    {
        if (word.Length < minWordLength)
            return false;

        return validWords.Contains(word.ToUpper());
    }

    // For compound words, you might add logic like:
    public bool IsValidCompoundWord(string word)
    {
        if (word.Length < minWordLength)
            return false;

        if (validWords.Contains(word.ToUpper()))
            return true;

        // Check if it's a valid compound word
        for (int i = minWordLength; i <= word.Length - minWordLength; i++)
        {
            string firstPart = word.Substring(0, i).ToUpper();
            string secondPart = word.Substring(i).ToUpper();

            if (validWords.Contains(firstPart) && validWords.Contains(secondPart))
                return true;
        }

        return false;
    }
}