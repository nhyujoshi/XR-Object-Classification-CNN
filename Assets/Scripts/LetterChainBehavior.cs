using UnityEngine;
using Oculus.Interaction.HandGrab;
using System.Collections.Generic;

public class LetterChainBehavior : MonoBehaviour
{
    [Header("Connection Points")]
    public SphereCollider leftConnector;
    public SphereCollider rightConnector;

    [Header("Letter Info")]
    public char letter;

    [Header("Chain References")]
    public LetterChainBehavior leftNeighbor;
    public LetterChainBehavior rightNeighbor;

    [Header("Connection Settings")]
    public float maxConnectionDistance = 0.1f; // Maximum distance before connection breaks

    [Header("Chain Movement")]
    public bool moveEntireChainWhenValid = true;
    public bool requireBothEndsGrabbed = true; // New setting to control chain movement behavior

    // Connection tracking
    private List<LetterChainBehavior> potentialLeftConnections = new List<LetterChainBehavior>();
    private List<LetterChainBehavior> potentialRightConnections = new List<LetterChainBehavior>();

    // Chain management
    private static WordChainManager chainManager;

    // Material handling
    private Color originalColor;
    private Renderer letterRenderer;
    private bool isPartOfValidWord = false;

    // Movement tracking
    private Vector3 lastPosition;
    private bool isBeingMoved = false;
    private float movementThreshold = 0.001f; // Minimum movement to trigger chain movement

    // Grab status tracking
    private bool isGrabbed = false;

    // Interaction component
    private HandGrabInteractable handGrabInteractable;

    private void Start()
    {
        // Ensure colliders are set as triggers
        if (leftConnector != null) leftConnector.isTrigger = true;
        if (rightConnector != null) rightConnector.isTrigger = true;

        // Find the chain manager if not already found
        if (chainManager == null)
        {
            chainManager = FindObjectOfType<WordChainManager>();
        }

        // Store original material color reference and current position
        letterRenderer = GetComponent<Renderer>();
        if (letterRenderer != null && letterRenderer.sharedMaterials.Length > 0)
        {
            originalColor = letterRenderer.sharedMaterials[0].color;
            Debug.Log($"Original color for letter {letter}: {originalColor}");
        }

        lastPosition = transform.position;

        // Set up grab detection
        handGrabInteractable = GetComponent<HandGrabInteractable>();
    }

    private void Update()
    {
        // Check for grab state from the HandGrabInteractable component
        if (handGrabInteractable != null)
        {
            isGrabbed = handGrabInteractable.State == Oculus.Interaction.InteractableState.Select;
        }

        // Check if connections need to be broken due to distance
        CheckConnectionDistance();

        // Check if this letter is being moved and should move the chain
        CheckChainMovement();
    }

    private void CheckChainMovement()
    {
        // Only propagate movement for valid words and if we're at the start or end of a chain
        if (!moveEntireChainWhenValid || !isPartOfValidWord) return;
        if (!(leftNeighbor == null || rightNeighbor == null)) return;

        // Calculate movement delta
        Vector3 movement = transform.position - lastPosition;

        // If movement is significant, check if we should propagate it
        if (movement.magnitude > movementThreshold)
        {
            if (requireBothEndsGrabbed)
            {
                // Only move the chain if both ends are grabbed
                if (AreBothEndsGrabbed())
                {
                    isBeingMoved = true;
                    PropagateMovement(movement);
                }
            }
            else
            {
                // Original behavior - move chain if any end is grabbed
                isBeingMoved = true;
                PropagateMovement(movement);
            }
        }
        else if (isBeingMoved)
        {
            // We were moving but stopped, so notify the chain
            isBeingMoved = false;
        }

        // Update last position
        lastPosition = transform.position;
    }

    private bool AreBothEndsGrabbed()
    {
        // Find the start and end of the chain
        LetterChainBehavior start = FindChainStart();
        LetterChainBehavior end = FindChainEnd();

        // Check if both ends are grabbed
        return start.isGrabbed && end.isGrabbed;
    }

    private LetterChainBehavior FindChainStart()
    {
        LetterChainBehavior current = this;
        while (current.leftNeighbor != null)
        {
            current = current.leftNeighbor;
        }
        return current;
    }

    private LetterChainBehavior FindChainEnd()
    {
        LetterChainBehavior current = this;
        while (current.rightNeighbor != null)
        {
            current = current.rightNeighbor;
        }
        return current;
    }

    private void PropagateMovement(Vector3 movement)
    {
        // Don't propagate zero movement or during first init
        if (movement.magnitude < 0.0001f) return;

        // Function to apply movement recursively through the chain
        void MoveChainRecursively(LetterChainBehavior letter, Vector3 delta, HashSet<LetterChainBehavior> visited)
        {
            if (letter == null || visited.Contains(letter)) return;

            visited.Add(letter);

            // Skip the current letter (it's already moved by the player/physics)
            if (letter != this)
            {
                Rigidbody rb = letter.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    // Use MovePosition for rigidbodies to maintain physics properly
                    rb.MovePosition(rb.position + delta);
                }
                else
                {
                    // Fallback for non-rigidbody objects
                    letter.transform.position += delta;
                }
            }

            // Propagate left
            if (letter.leftNeighbor != null && !visited.Contains(letter.leftNeighbor))
            {
                MoveChainRecursively(letter.leftNeighbor, delta, visited);
            }

            // Propagate right
            if (letter.rightNeighbor != null && !visited.Contains(letter.rightNeighbor))
            {
                MoveChainRecursively(letter.rightNeighbor, delta, visited);
            }
        }

        // Start the recursive chain movement
        HashSet<LetterChainBehavior> visited = new HashSet<LetterChainBehavior>();
        MoveChainRecursively(this, movement, visited);
    }

    // Rest of the methods remain the same as before...
    private void CheckConnectionDistance()
    {
        // Check left connection
        if (leftNeighbor != null)
        {
            float leftDistance = Vector3.Distance(
                leftConnector.transform.position,
                leftNeighbor.rightConnector.transform.position);

            if (leftDistance > maxConnectionDistance)
            {
                // Break connection
                Debug.Log($"Breaking left connection between {letter} and {leftNeighbor.letter} due to distance");
                LetterChainBehavior oldNeighbor = leftNeighbor;
                leftNeighbor.rightNeighbor = null;
                leftNeighbor = null;

                // Notify chain manager
                if (chainManager != null)
                {
                    chainManager.UpdateWordChain();
                }

                // Reset visual feedback
                SetValidWord(false);
                ResetMaterialColor();
                oldNeighbor.SetValidWord(false);
                oldNeighbor.ResetMaterialColor();
            }
        }

        // Check right connection
        if (rightNeighbor != null)
        {
            float rightDistance = Vector3.Distance(
                rightConnector.transform.position,
                rightNeighbor.leftConnector.transform.position);

            if (rightDistance > maxConnectionDistance)
            {
                // Break connection
                Debug.Log($"Breaking right connection between {letter} and {rightNeighbor.letter} due to distance");
                LetterChainBehavior oldNeighbor = rightNeighbor;
                rightNeighbor.leftNeighbor = null;
                rightNeighbor = null;

                // Notify chain manager
                if (chainManager != null)
                {
                    chainManager.UpdateWordChain();
                }

                // Reset visual feedback
                SetValidWord(false);
                ResetMaterialColor();
                oldNeighbor.SetValidWord(false);
                oldNeighbor.ResetMaterialColor();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check for connections with other letters
        if (other.CompareTag("RightConnector"))
        {
            // Their right connector is touching our left connector
            LetterChainBehavior otherLetter = other.transform.parent.GetComponent<LetterChainBehavior>();
            if (otherLetter != null && otherLetter != this)
            {
                potentialLeftConnections.Add(otherLetter);
                TryConnect();
            }
        }
        else if (other.CompareTag("LeftConnector"))
        {
            // Their left connector is touching our right connector
            LetterChainBehavior otherLetter = other.transform.parent.GetComponent<LetterChainBehavior>();
            if (otherLetter != null && otherLetter != this)
            {
                potentialRightConnections.Add(otherLetter);
                TryConnect();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Remove from potential connections when no longer touching
        if (other.CompareTag("RightConnector"))
        {
            LetterChainBehavior otherLetter = other.transform.parent.GetComponent<LetterChainBehavior>();
            if (otherLetter != null)
            {
                potentialLeftConnections.Remove(otherLetter);
            }
        }
        else if (other.CompareTag("LeftConnector"))
        {
            LetterChainBehavior otherLetter = other.transform.parent.GetComponent<LetterChainBehavior>();
            if (otherLetter != null)
            {
                potentialRightConnections.Remove(otherLetter);
            }
        }
    }

    private void TryConnect()
    {
        // Check for left connections
        if (leftNeighbor == null && potentialLeftConnections.Count > 0)
        {
            // Find closest potential connection
            LetterChainBehavior closest = GetClosestLetter(potentialLeftConnections, leftConnector.transform.position);
            if (closest != null && closest.rightNeighbor == null)
            {
                ConnectToLeft(closest);
            }
        }

        // Check for right connections
        if (rightNeighbor == null && potentialRightConnections.Count > 0)
        {
            // Find closest potential connection
            LetterChainBehavior closest = GetClosestLetter(potentialRightConnections, rightConnector.transform.position);
            if (closest != null && closest.leftNeighbor == null)
            {
                ConnectToRight(closest);
            }
        }

        // Notify chain manager of connection changes
        if (chainManager != null)
        {
            chainManager.UpdateWordChain();
        }
    }

    private LetterChainBehavior GetClosestLetter(List<LetterChainBehavior> letters, Vector3 position)
    {
        LetterChainBehavior closest = null;
        float closestDistance = float.MaxValue;

        foreach (var letter in letters)
        {
            float distance = Vector3.Distance(position, letter.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = letter;
            }
        }

        return closest;
    }

    public void ConnectToLeft(LetterChainBehavior leftLetter)
    {
        if (leftLetter == this) return;

        // Set up the connection references
        leftNeighbor = leftLetter;
        leftLetter.rightNeighbor = this;

        // Create a joint to physically connect the letters
        ConfigurableJoint joint = gameObject.AddComponent<ConfigurableJoint>();
        joint.connectedBody = leftLetter.GetComponent<Rigidbody>();

        // Configure joint for a rigid connection but allowing some movement
        joint.xMotion = ConfigurableJointMotion.Limited;
        joint.yMotion = ConfigurableJointMotion.Limited;
        joint.zMotion = ConfigurableJointMotion.Limited;
        joint.angularXMotion = ConfigurableJointMotion.Limited;
        joint.angularYMotion = ConfigurableJointMotion.Limited;
        joint.angularZMotion = ConfigurableJointMotion.Limited;

        // Set relatively high breaking force to keep connected but allow breaking if needed
        joint.breakForce = 2000;
        joint.breakTorque = 2000;

        Debug.Log($"Connected {leftLetter.letter} to the left of {letter}");
    }

    public void ConnectToRight(LetterChainBehavior rightLetter)
    {
        if (rightLetter == this) return;

        // Set up the connection references
        rightNeighbor = rightLetter;
        rightLetter.leftNeighbor = this;

        // Create a joint to physically connect the letters
        ConfigurableJoint joint = gameObject.AddComponent<ConfigurableJoint>();
        joint.connectedBody = rightLetter.GetComponent<Rigidbody>();

        // Configure joint for a rigid connection but allowing some movement
        joint.xMotion = ConfigurableJointMotion.Limited;
        joint.yMotion = ConfigurableJointMotion.Limited;
        joint.zMotion = ConfigurableJointMotion.Limited;
        joint.angularXMotion = ConfigurableJointMotion.Limited;
        joint.angularYMotion = ConfigurableJointMotion.Limited;
        joint.angularZMotion = ConfigurableJointMotion.Limited;

        // Set relatively high breaking force to keep connected but allow breaking if needed
        joint.breakForce = 2000;
        joint.breakTorque = 2000;

        Debug.Log($"Connected {rightLetter.letter} to the right of {letter}");
    }

    public void SetValidWord(bool isValid)
    {
        isPartOfValidWord = isValid;
    }

    public void ResetMaterialColor()
    {
        if (letterRenderer != null)
        {
            // Try to access the material properly
            Material[] materials = letterRenderer.materials;
            if (materials.Length > 0)
            {
                materials[0].color = originalColor;
                letterRenderer.materials = materials;
                Debug.Log($"Reset material color for letter {letter} to {originalColor}");
            }
        }
    }

    public void SetMaterialColor(Color color)
    {
        if (letterRenderer != null)
        {
            // Create a new array of materials to modify
            Material[] materials = letterRenderer.materials;
            if (materials.Length > 0)
            {
                materials[0].color = color;
                letterRenderer.materials = materials;
                Debug.Log($"Set material color for letter {letter} to {color}");
            }
        }
    }

    private void OnJointBreak(float breakForce)
    {
        Debug.Log($"Joint broke on letter {letter} with force {breakForce}");

        // Store references before clearing them
        LetterChainBehavior oldLeftNeighbor = leftNeighbor;
        LetterChainBehavior oldRightNeighbor = rightNeighbor;

        // Clear connection references when joint breaks
        if (leftNeighbor != null)
        {
            leftNeighbor.rightNeighbor = null;
            leftNeighbor = null;
        }

        if (rightNeighbor != null)
        {
            rightNeighbor.leftNeighbor = null;
            rightNeighbor = null;
        }

        // Reset visual state to invalid
        SetValidWord(false);
        ResetMaterialColor();

        // Also reset neighbors' visual state if they were connected
        if (oldLeftNeighbor != null)
        {
            oldLeftNeighbor.SetValidWord(false);
            oldLeftNeighbor.ResetMaterialColor();
        }

        if (oldRightNeighbor != null)
        {
            oldRightNeighbor.SetValidWord(false);
            oldRightNeighbor.ResetMaterialColor();
        }

        // Notify chain manager of the break
        if (chainManager != null)
        {
            chainManager.UpdateWordChain();
        }
    }
}