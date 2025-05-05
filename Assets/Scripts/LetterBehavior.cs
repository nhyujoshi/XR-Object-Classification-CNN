using UnityEngine;
using Oculus.Interaction.HandGrab;

public class LetterBehavior : MonoBehaviour
{
    public char Letter { get; private set; }

    [SerializeField] private HandGrabInteractable grabInteractable;

    private void Awake()
    {
        // Extract letter from prefab name (assuming prefab is named after its letter)
        string name = gameObject.name.ToUpper();
        if (name.Length > 0)
        {
            Letter = name[0];
            if (name.Contains("(CLONE)"))
            {
                Letter = name.Split('(')[0][0];
            }
        }

        // Get grab interactable
        if (grabInteractable == null)
            grabInteractable = GetComponentInChildren<HandGrabInteractable>();
    }
}