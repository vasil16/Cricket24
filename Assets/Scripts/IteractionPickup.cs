using UnityEngine;
using RootMotion.FinalIK;
using System.Collections;

public class SmoothInteractionPickup : MonoBehaviour
{
    [Header("Main Components")]
    public InteractionSystem interactionSystem;
    public FullBodyBipedIK fbbik; // Assign this in Inspector
    public InteractionObject pickupObject;

    [Header("Natural Movement Settings")]
    [Tooltip("How fast the head turns to look at the target")]
    public float lookAtSmoothSpeed = 5f;
    [Tooltip("How much the character looks at the object (0-1)")]
    [Range(0f, 1f)] public float lookAtIntensity = 0.8f;
    public float holdDuration = 2.0f;

    [Header("Effectors")]
    public FullBodyBipedEffector rightHandEffector = FullBodyBipedEffector.RightHand;
    public FullBodyBipedEffector leftHandEffector = FullBodyBipedEffector.LeftHand;

    // Internal State
    private bool isHolding = false;
    private bool isTransitioning = false;
    private float currentLookWeight = 0f;
    private Transform currentLookTarget;

    void Start()
    {
        if (interactionSystem == null) interactionSystem = GetComponent<InteractionSystem>();
        if (fbbik == null) fbbik = GetComponent<FullBodyBipedIK>();

        // Safety check to prevent weird initial snaps
        if (fbbik != null)
        {
            //fbbik.solver.lookAt.weight = 0f;
        }

        //Debug.Log("Smooth Pickup Ready. Press E to Interact.");
    }

    //void Update()
    //{
    //    // --- 1. Handle Looking smoothly in Update (The Conductor of the Symphony) ---
    //    if (fbbik != null)
    //    {
    //        // Smoothly blend the weight
    //        //fbbik.solver.lookAt.weight = Mathf.Lerp(fbbik.solver.lookAt.weight, currentLookWeight, Time.deltaTime * lookAtSmoothSpeed);

    //        // Update target if we have one
    //        if (currentLookTarget != null)
    //        {
    //            //fbbik.solver.lookAt.target = currentLookTarget;
    //        }
    //    }

    //    // --- 2. Input ---
    //    if (Input.GetKeyDown(KeyCode.E) && !isTransitioning)
    //    {
    //        //if (!isHolding) StartCoroutine(PickupSequence());
    //        //else StartCoroutine(DropSequence());
    //    }
    //}

    public void StartPickup(bool singleHand)
    {
        StartCoroutine(PickupSequence(singleHand));
    }

    public void StartDrop()
    {
        StartCoroutine(DropSequence());
    }

    IEnumerator PickupSequence(bool singleHand)
    {
        isTransitioning = true;

        // Step 1: Eye Contact (Anticipation)
        // Humans look before they reach. We set the target and weight BEFORE the arm moves.
        currentLookTarget = pickupObject.transform;
        currentLookWeight = lookAtIntensity;

        // Wait briefly so the head turns first
        //yield return new WaitForSeconds(0.3f);

        // Step 2: Reach with Body
        // We start the interaction. The "Curves" in the Inspector will handle the arm timing.
        if(!singleHand)
        {
            interactionSystem.StartInteraction(leftHandEffector, pickupObject, false);
        }
        interactionSystem.StartInteraction(rightHandEffector, pickupObject, false);

        // Step 3: Wait for Grab
        // Wait until the system pauses (meaning the hand has reached the object and is holding it)
        while (interactionSystem.IsPaused(rightHandEffector))
        {
            yield return null;
        }

        isHolding = true;
        isTransitioning = false;
        Debug.Log("Object Grabbbed.");

        // Optional: Auto drop
        if (holdDuration > 0)
        {
            yield return new WaitForSeconds(holdDuration);
            // Only drop if we are still holding it (user might have dropped manually)
            //if (isHolding) StartCoroutine(DropSequence());
        }
        if (isHolding) StartCoroutine(DropSequence());
    }

    IEnumerator DropSequence()
    {
        isTransitioning = true;
        Debug.Log("Dropping...");

        // Step 1: Release Hands
        interactionSystem.StopInteraction(rightHandEffector);
        interactionSystem.StopInteraction(leftHandEffector);

        // Step 2: Lose Focus
        // We stop looking at the object as we let go
        currentLookWeight = 0f;

        // Step 3: Wait for Blend Out
        // The interaction system takes time to blend weights back to 0. 
        // We wait here to ensure the coroutine doesn't finish while the character is still moving.
        //yield return new WaitForSeconds(1.0f);
        yield return null;
        isHolding = false;
        isTransitioning = false;
        currentLookTarget = null; // Clear target to prevent "ghost" looking
    }
}