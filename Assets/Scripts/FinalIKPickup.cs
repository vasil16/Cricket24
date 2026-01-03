using UnityEngine;
using RootMotion.FinalIK;

public class TestFinalIK : MonoBehaviour
{
    public FullBodyBipedIK ik;
    public Transform testObject;

    void Start()
    {
        if (ik == null) ik = GetComponent<FullBodyBipedIK>();

        // Make sure IK is properly set up
        if (ik.solver == null)
        {
            Debug.LogError("IK solver is null! Click 'Create References' on FullBodyBipedIK component");
            return;
        }

        Debug.Log("TestFinalIK Ready! Press T to test hand reach");
    }

    void Update()
    {
        // Test with T key
        if (Input.GetKeyDown(KeyCode.T) && testObject != null)
        {
            TestHandReach();
        }

        // Reset with R key
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetIK();
        }

        // Pickup with E key
        if (Input.GetKeyDown(KeyCode.E) && testObject != null)
        {
            SimplePickup();
        }
    }

    void TestHandReach()
    {
        Debug.Log("Testing Hand Reach...");

        // Enable IK
        ik.enabled = true;

        // Set hand target to object
        ik.solver.rightHandEffector.target = testObject;
        ik.solver.rightHandEffector.positionWeight = 1f;
        ik.solver.rightHandEffector.rotationWeight = 0f;

        // Make body bend a little
        ik.solver.bodyEffector.positionWeight = 0.3f;
        ik.solver.bodyEffector.position = testObject.position;

        // Look at object
        //ik.solver.lookAt.target = testObject;
        //ik.solver.lookAt.weight = 0.5f;

        Debug.Log($"Hand reaching for: {testObject.position}");
    }

    void SimplePickup()
    {
        Debug.Log("Simple Pickup...");
        StartCoroutine(PickupAnimation());
    }

    System.Collections.IEnumerator PickupAnimation()
    {
        ik.enabled = true;

        // Phase 1: Look at object
        //ik.solver.lookAt.target = testObject;
        //ik.solver.lookAt.weight = 0f;

        float timer = 0f;
        while (timer < 0.3f)
        {
            timer += Time.deltaTime;
            //ik.solver.lookAt.weight = timer / 0.3f;
            yield return null;
        }

        // Phase 2: Bend and reach
        ik.solver.rightHandEffector.target = testObject;
        ik.solver.bodyEffector.positionWeight = 0f;

        timer = 0f;
        while (timer < 0.5f)
        {
            timer += Time.deltaTime;
            float t = timer / 0.5f;

            ik.solver.rightHandEffector.positionWeight = t;
            ik.solver.bodyEffector.positionWeight = t * 0.4f;

            // Calculate body bend position (slightly forward and down)
            Vector3 bodyBendPos = transform.position +
                                 Vector3.down * 0.2f +
                                 transform.forward * 0.3f;
            ik.solver.bodyEffector.position = bodyBendPos;

            yield return null;
        }

        // Hold for 0.2 seconds
        yield return new WaitForSeconds(0.2f);

        // Phase 3: Return
        timer = 0f;
        while (timer < 0.4f)
        {
            timer += Time.deltaTime;
            float t = timer / 0.4f;
            float returnT = 1f - t;

            ik.solver.rightHandEffector.positionWeight *= returnT;
            ik.solver.bodyEffector.positionWeight *= returnT;
            //ik.solver.lookAt.weight *= returnT;

            yield return null;
        }

        ResetIK();
        Debug.Log("Pickup complete!");
    }

    void ResetIK()
    {
        ik.solver.rightHandEffector.positionWeight = 0f;
        ik.solver.rightHandEffector.rotationWeight = 0f;
        ik.solver.bodyEffector.positionWeight = 0f;
        //ik.solver.lookAt.weight = 0f;
        ik.enabled = false;
        Debug.Log("IK Reset");
    }
}