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
    public bool isTransitioning = false;
    private float currentLookWeight = 0f;
    private Transform currentLookTarget;

    void Start()
    {
        if (interactionSystem == null) interactionSystem = GetComponent<InteractionSystem>();
        if (fbbik == null) fbbik = GetComponent<FullBodyBipedIK>();

        // Safety check to prevent weird initial snaps
        if (fbbik != null)
        {
            //fbbik.solver..weight = 0f;
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

    // Example: Moving the foot AND shifting the body


    [Header("Strafe Motion")]
    public float stepDistance = 0.5f; // Length of one step
    public float stepHeight = 0.2f;   // Height of the arc
    public float stepSpeed = 180f;    // Speed of the animation

    private float stepCycle = 0f;
    private Vector3 plantedFootPos;   // Remembers where we stepped

    // Call this every frame in your loop.
    // returns TRUE when the character has finished a full step (useful for your logic)
    public bool StrafeStep(float direction)
    {
        // 1. Reset if no input
        if (Mathf.Abs(direction) < 0.1f)
        {
            ResetOffsets();
            return false;
        }

        bool stepComplete = false;

        // 2. Advance the Cycle (0 to PI is one full step)
        stepCycle += Time.deltaTime * stepSpeed;

        // If we finished a step (Cycle > PI), reset for the next one
        if (stepCycle >= Mathf.PI)
        {
            stepCycle = 0;
            stepComplete = true; // Tell the loop we finished a step

            // Finalize the movement (Snap body to exact target to prevent drift)
            // This ensures the loop doesn't lose precision over time
        }

        // 3. Calculate Phases
        // We split the step into two halves based on the Sine Wave
        // 0 to PI/2 (0 to 1.57) = REACH Phase
        // PI/2 to PI (1.57 to 3.14) = PULL Phase

        float cyclePhase = stepCycle;
        float progress = cyclePhase / Mathf.PI; // 0 to 1

        // Sine wave for lifting the foot (Arch)
        float lift = Mathf.Sin(cyclePhase) * stepHeight;

        // 4. Identify Legs
        bool moveRight = direction > 0;
        IKEffector leadLeg = moveRight ? fbbik.solver.rightFootEffector : fbbik.solver.leftFootEffector;
        IKEffector trailLeg = moveRight ? fbbik.solver.leftFootEffector : fbbik.solver.rightFootEffector;

        // ==========================================================
        // THE MAGIC: STOP SLIDING
        // ==========================================================

        if (cyclePhase < Mathf.PI / 2)
        {
            // --- PHASE 1: REACH (Body Stationary, Leg Moves) ---

            // Map 0..PI/2 to 0..1
            float reachProgress = cyclePhase / (Mathf.PI / 2);

            // Move Lead Leg OUT
            Vector3 reachOffset = transform.right * direction * stepDistance * reachProgress;
            reachOffset.y = lift; // Add height

            leadLeg.positionOffset = reachOffset;

            // Keep Body & Trail Leg Still
            trailLeg.positionOffset = Vector3.zero;
            fbbik.solver.bodyEffector.positionOffset = Vector3.zero;
        }
        else
        {
            // --- PHASE 2: PULL (Leg Planted, Body Moves) ---

            // We are moving the transform, but we need the foot to LOOK like it stays still.
            // So we move the Foot Effector BACKWARDS at the same speed the Body moves FORWARDS.

            float pullProgress = (cyclePhase - (Mathf.PI / 2)) / (Mathf.PI / 2); // 0 to 1

            // 1. Actually Move the Character Transform
            float moveSpeedThisFrame = (stepDistance / (Mathf.PI / stepSpeed * 0.5f)) * Time.deltaTime;
            transform.position += transform.right * direction * moveSpeedThisFrame;

            // 2. Counter-Animate the Feet to "Stick" them to the ground

            // The Lead Leg is now "behind" us relative to the movement, effectively planted
            // We fade out its forward offset as the body catches up
            Vector3 leadLegCounter = transform.right * direction * stepDistance * (1 - pullProgress);
            leadLegCounter.y = lift; // Curve back down

            leadLeg.positionOffset = leadLegCounter;

            // The Trail Leg is being dragged along
            // It needs to look like it's leaving the ground slightly or sliding
            trailLeg.positionOffset = Vector3.zero;

            // Shift hips slightly to show weight transfer
            fbbik.solver.bodyEffector.positionOffset = transform.right * direction * 0.1f * pullProgress;
        }

        return stepComplete;
    }

    private void ResetOffsets()
    {
        fbbik.solver.rightFootEffector.positionOffset = Vector3.zero;
        fbbik.solver.leftFootEffector.positionOffset = Vector3.zero;
        fbbik.solver.bodyEffector.positionOffset = Vector3.zero;
        stepCycle = 0;
    }

    public void StartPickup(bool singleHand)
    {
        StartCoroutine(PickupSequence(singleHand));
    }

    IEnumerator PickupSequence(bool singleHand)
    {
        isTransitioning = true;

        // 1. ANTICIPATION: Look at the ball first
        currentLookTarget = pickupObject.transform;
        currentLookWeight = lookAtIntensity;
        interactionSystem.lookAt.lookAtTarget = currentLookTarget;
        // Optional: Small delay so the head moves slightly before the arm
        yield return new WaitForSeconds(0.1f);

        // 2. THE REACH: Start the interaction
        // Ensure the InteractionObject has a Pause event at Time 0.5
        if (!singleHand)
        {
            interactionSystem.StartInteraction(leftHandEffector, pickupObject, false);
        }
        interactionSystem.StartInteraction(rightHandEffector, pickupObject, false);
        interactionSystem.lookAt.lookAtTarget = pickupObject.transform;

        Debug.Log("Fielder reaching for ball..."+Time.time);

        // 3. THE WAIT: Wait for the hand to arrive at the ball
        // This loop runs until the 'Pause' event in your InteractionObject is hit
        while (!interactionSystem.IsPaused(rightHandEffector))
        {
            // Safety check: if the interaction is interrupted, stop the coroutine
            if (!interactionSystem.IsInInteraction(rightHandEffector))
            {
                isTransitioning = false;
                yield break;
            }
            yield return null;
        }

        // 4. THE COLLECTION: The hand is now at the ball
        isHolding = true;

        // Parent the ball immediately so it travels with us
        //pickupObject.transform.SetParent(null);

        // Optional: Small cradle time
        if (holdDuration > 0) yield return new WaitForSeconds(holdDuration);

        // --- THE FIX FOR "HANDS BEHIND" ---
        // We are running. If we let the curve play at normal speed (Speed 1),
        // the hand will get stuck on the ground behind us for 0.5 seconds.
        // We boost speed to 5x or 10x to force the hand to "swish" back to the body immediately.
        //interactionSystem.speed = 10.0f;

        // 5. THE RETURN
        interactionSystem.ResumeInteraction(rightHandEffector);
        if (!singleHand) interactionSystem.ResumeInteraction(leftHandEffector);

        // Wait for the fast return to finish
        while (interactionSystem.IsInInteraction(rightHandEffector))
        {
            yield return null;
        }

        // Reset speed for the next interaction!
        interactionSystem.speed = 1.0f;

        isTransitioning = false;
        Debug.Log("Pickup Sequence Complete.");
    }

    [SerializeField] Transform throwArm;

    public void StartDrop()
    {
        StartCoroutine(DropSequence());
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