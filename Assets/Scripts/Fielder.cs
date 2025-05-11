using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class Fielder : MonoBehaviour
{
    public float runSpeed, score, angleDiff, timeToReachLanding;
    private Vector3 targetPosition, actualPos, actualRot, idleRightHand, idleLeftHand;
    BallHit ballComp;
    Rigidbody ballRb;
    public Transform ball;
    public bool canReachInTime, startedRun;
    [SerializeField] FieldManager fm;
    [SerializeField] AnimationClip runClip;
    [SerializeField] Transform rightHand, leftHand;
    [SerializeField] TwoBoneIKConstraint leftHandIk, rightHandIk;
    [SerializeField] MultiAimConstraint headAim, neckAim;

    string[] allTriggers = { "StopField", "Throw", "Pick", "DiveLeft", "DiveRight", "PickLeft", "PickRight" };

    public Animator animator;
    public float weight = 1.0f;

    private void OnEnable()
    {
        actualPos = transform.position;
        actualRot = transform.rotation.eulerAngles;
        idleRightHand = rightHand.localPosition;
        idleLeftHand = leftHand.localPosition;
        animator = GetComponent<Animator>();
        if (runClip != null)
        {
            // Get all properties of the Transform component (or other components if needed)
            //AddKeyframesFromComponent(transform, runClip);
        }
    }

    #region AnimatioHelper

    void ResetToDefaultPose()
    {
        //if (animator && gameObject.name == "keeper")
        //{
        //    // Reset IK for all body parts to stop influencing the character
        //    animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0);
        //    animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0);
        //    animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0);
        //    animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0);
        //    animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0);
        //    animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 0);
        //    animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0);
        //    animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 0);

        //    // Reset LookAt weight
        //    animator.SetLookAtWeight(0);

        //    // Optionally reset any other body parts
        //    // For example, reset spine and chest rotations if needed
        //    Transform chestBone = animator.GetBoneTransform(HumanBodyBones.Chest);
        //    if (chestBone != null)
        //    {
        //        chestBone.rotation = Quaternion.identity; // Reset to neutral pose
        //    }

        //    // Reset other bones if needed (e.g., head, arms)
        //    Transform headBone = animator.GetBoneTransform(HumanBodyBones.Head);
        //    if (headBone != null)
        //    {
        //        headBone.rotation = Quaternion.identity; // Reset head rotation
        //    }

        //    // You can add similar logic for other body parts if required
        //}
    }

    //void OnAnimatorIK(int layerIndex)
    //{
    //    if (animator && gameObject.name == "keeper")
    //    {
    //        // Set the overall weight for IK
    //        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, weight);
    //        animator.SetIKRotationWeight(AvatarIKGoal.RightHand, weight);
    //        animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, weight);
    //        animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, weight);

    //        if (ball)
    //        {
    //            // **1. Adjust Upper Body Rotation Toward Ball**
    //            animator.SetLookAtWeight(1.0f, 0.6f, 0.6f, 1.0f, 0.5f); // Customize weights as needed
    //            animator.SetLookAtPosition(ball.position);

    //            // **2. IK for Right Hand**
    //            animator.SetIKPosition(AvatarIKGoal.RightHand, ball.position);
    //            Vector3 rightHandToBallDirection = ball.position - animator.GetIKPosition(AvatarIKGoal.RightHand);
    //            Quaternion rightHandRotation = Quaternion.LookRotation(rightHandToBallDirection, Vector3.up);
    //            animator.SetIKRotation(AvatarIKGoal.RightHand, rightHandRotation);

    //            // **3. IK for Left Hand (Offset for Two-Handed Catch)**
    //            Vector3 leftHandPosition = ball.position + new Vector3(-0.15f, 0, 0); // Slightly offset to the left
    //            animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandPosition);
    //            Vector3 leftHandToBallDirection = ball.position - leftHandPosition;
    //            Quaternion leftHandRotation = Quaternion.LookRotation(leftHandToBallDirection, Vector3.up);
    //            animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandRotation);

    //            // **4. Optional: Adjust Spine Rotation**
    //            Transform chestBone = animator.GetBoneTransform(HumanBodyBones.Chest); // Use UpperChest if needed
    //            if (chestBone != null)
    //            {
    //                Vector3 chestToBallDirection = ball.position - chestBone.position;
    //                Quaternion chestRotation = Quaternion.LookRotation(chestToBallDirection, Vector3.up);
    //                chestBone.rotation = Quaternion.Lerp(chestBone.rotation, chestRotation, 0.5f); // Blend for smoothness
    //            }
    //        }
    //        else
    //        {
    //            // Reset IK weights if no ball
    //            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0);
    //            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0);
    //            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0);
    //            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0);
    //            animator.SetLookAtWeight(0);
    //        }
    //    }
    //}

    void AddKeyframesFromComponent(Component component, AnimationClip clip)
    {
        // Get all public properties of the component
        PropertyInfo[] properties = component.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Loop through all properties
        foreach (PropertyInfo property in properties)
        {
            // Check if it's a property that can be animated (i.e., numeric types like float, Vector3, etc.)
            if (property.PropertyType == typeof(Vector3))
            {
                Vector3 value = (Vector3)property.GetValue(component);
                AddKeyframeForVector3(property.Name, value, clip);
            }
            else if (property.PropertyType == typeof(Quaternion))
            {
                Quaternion value = (Quaternion)property.GetValue(component);
                AddKeyframeForQuaternion(property.Name, value, clip);
            }
            else if (property.PropertyType == typeof(float))
            {
                float value = (float)property.GetValue(component);
                AddKeyframeForFloat(property.Name, value, clip);
            }
            // Add more property types as needed (e.g., Color, etc.)
        }
    }

    void AddKeyframeForVector3(string propertyName, Vector3 value, AnimationClip clip)
    {
        clip.SetCurve("", typeof(Transform), propertyName + ".x", new AnimationCurve(new Keyframe(0f, value.x)));
        clip.SetCurve("", typeof(Transform), propertyName + ".y", new AnimationCurve(new Keyframe(0f, value.y)));
        clip.SetCurve("", typeof(Transform), propertyName + ".z", new AnimationCurve(new Keyframe(0f, value.z)));
    }

    void AddKeyframeForQuaternion(string propertyName, Quaternion value, AnimationClip clip)
    {
        // Quaternion can be split into four components: x, y, z, w
        clip.SetCurve("", typeof(Transform), propertyName + ".x", new AnimationCurve(new Keyframe(0f, value.x)));
        clip.SetCurve("", typeof(Transform), propertyName + ".y", new AnimationCurve(new Keyframe(0f, value.y)));
        clip.SetCurve("", typeof(Transform), propertyName + ".z", new AnimationCurve(new Keyframe(0f, value.z)));
        clip.SetCurve("", typeof(Transform), propertyName + ".w", new AnimationCurve(new Keyframe(0f, value.w)));
    }

    void AddKeyframeForFloat(string propertyName, float value, AnimationClip clip)
    {
        clip.SetCurve("", typeof(Transform), propertyName, new AnimationCurve(new Keyframe(0f, value)));
    }

    #endregion

    IEnumerator StartField()
    {
        yield return new WaitForSeconds(0.1f);
        if (IsBallComingAtFielder() && ballComp.groundShot)
        {
            Debug.Log("Coming to fielder");
            StartCoroutine(WaitForBall());
        }

        else
        {
            Debug.Log("away from fielder");
            animator.Play("running");
            StartCoroutine(RunToBall());
        }
        yield break;
    }

    private bool IsBallComingAtFielder()
    {
        Vector3 toBall = (ball.position - transform.position).normalized;

        float angleToBall = Vector3.Angle(transform.forward, toBall);

        float thresholdAngle = 30f;
        return Mathf.Abs(angleToBall - 180f) < thresholdAngle;
    }

    IEnumerator RunToBall()
    {
        Vector3 moveDirection = (targetPosition - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(moveDirection);
        lookRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, lookRotation.eulerAngles.y, lookRotation.eulerAngles.z);
        lookRotation.x = actualRot.x;
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 30f);

        while (!ballComp.fielderReached)
        {
            if (ballComp.keeperReceive)
            {
                Gameplay.instance.deliveryDead = true;
                break;
            }
            if(ShouldChase(ball.position,ballRb.velocity,transform.position)&&ballComp.groundShot)
            {
                Debug.Log("chasee");
                StartCoroutine(CollectBall());
                yield break;
            }           
            
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, runSpeed * Time.deltaTime);
            
            if (Gameplay.instance.deliveryDead)
            {
                animator.SetTrigger("StopField");
                StopAllCoroutines();
                yield break;
            }

            if (!ballComp.groundShot)
            {
                if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(targetPosition.x, targetPosition.z)) < 1f)
                {
                    if(ballComp.fielderReached)
                    {
                        ReachedBall();
                        StopAllCoroutines();
                        yield break;
                    }
                    Debug.Log(gameObject.name + " reached target go for");
                    StartCoroutine(TrackAndCatchBall());
                    yield break;
                }
            }

            else
            {
                if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(targetPosition.x, targetPosition.z)) < 3f)
                {
                    Debug.Log(gameObject.name + " reached target go for collection");
                    if(ballComp.fielderReached)
                    {
                        ReachedBall();
                        StopAllCoroutines();
                        yield break;
                    }

                    if (IsBallComingAtFielder())
                    {
                        animator.SetTrigger("StopField");
                        StartCoroutine(WaitForBall());
                    }
                    else
                    {
                        StartCoroutine(CollectBall());
                    }
                    yield break;
                }
            }  
            yield return null;
        }
        Debug.Log("runningtoball");

        ReachedBall();
    }

    IEnumerator WaitForBall()
    {
        Debug.Log(gameObject.name + "  waiting for ball");
        while (!ballComp.fielderReached)
        {
            if (ballComp.keeperReceive)
            {
                Gameplay.instance.deliveryDead = true;
                break;
            }
            Vector3 moveDirection = (ball.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(moveDirection);
            lookRotation.x = actualRot.x;
            lookRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, lookRotation.eulerAngles.y, lookRotation.eulerAngles.z);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 30f);

            if (Gameplay.instance.deliveryDead)
            {
                yield break;
            }
            if (ballRb.velocity.magnitude < 5)
            {
                Debug.Log("slowed beyound thrshold");
                transform.position = Vector3.MoveTowards(transform.position, new Vector3(ball.position.x, transform.position.y, ball.position.z), runSpeed * Time.deltaTime);
            }
            yield return null;
        }
        ReachedBall();
    }

    IEnumerator CollectBall()
    {        
        while (!ballComp.fielderReached)
        {
            if (Gameplay.instance.deliveryDead)
            {
                animator.SetTrigger("StopField");
                yield break;
            }
            Vector3 moveDirection = (ball.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(moveDirection);
            lookRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, lookRotation.eulerAngles.y, lookRotation.eulerAngles.z);
            lookRotation.x = actualRot.x;
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 30f);
            transform.position = Vector3.MoveTowards(transform.position, new Vector3(ball.position.x, transform.position.y, ball.position.z), runSpeed * Time.deltaTime);
            yield return null;
        }

        Debug.Log("reached collection");
        ReachedBall();
        yield return null;
    }

    IEnumerator TrackAndCatchBall()
    {
        animator.SetTrigger("Slow");
        if (!ball) yield break;
        while (!ballComp.fielderReached)
        {
            if (Gameplay.instance.deliveryDead)
            {
                animator.SetTrigger("StopField");
                yield break;
            }
            Vector3 moveDirection = (ball.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(moveDirection);
            lookRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, lookRotation.eulerAngles.y, lookRotation.eulerAngles.z);
            lookRotation.x = actualRot.x;
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 30f);

            transform.position = Vector3.MoveTowards(transform.position, new Vector3(ball.position.x, transform.position.y, ball.position.z), runSpeed * 0.8f * Time.deltaTime);
            yield return null;
        }
        ReachedBall();
    }

    public void KeeperRecieve(Vector3 targetPosition, Transform ball)
    {
        animator.enabled = true;
        this.ball = ball;
        Debug.Log("nam  " + gameObject.name);
        if(targetPosition.y>6f)
        {
            animator.Play("jump");
        }
        if(targetPosition.z > -2.13f)
        {
            //animator.SetIKPosition(AvatarIKGoal.LeftFoot,)
            animator.Play("moveLeft");
        }
        if (targetPosition.z < -6.44f)
        {
            animator.Play("moveRight");
        }
        StartCoroutine(SetTarget());
        rightHand.position = leftHand.position = targetPosition;
        leftHandIk.weight = rightHandIk.weight = 1;
        StartCoroutine(ReleaseTarget(targetPosition.z));
    }

    IEnumerator SetTarget()
    {
        Debug.Log("recive start");
        float time = 0;
        float duration = .4f;
        float lerpValue = 0;
        while (time <= duration)
        {
            time += Time.deltaTime;
            lerpValue = Mathf.Lerp(0, 1, time / duration);
            leftHandIk.weight = rightHandIk.weight = headAim.weight = neckAim.weight = lerpValue;
            yield return null;
        }
    }

    IEnumerator ReleaseTarget(float z)
    {
        while(!ball.GetComponent<BallHit>().stopTriggered)
        {
            yield return null;
        }
        float time = 0;
        float duration = 0.5f;
        float lerpValue=1;
        while (time <= duration)
        {
            time += Time.deltaTime;
            lerpValue = Mathf.Lerp(1, 0, time / duration);
            leftHandIk.weight = rightHandIk.weight = headAim.weight = neckAim.weight = lerpValue;
            yield return null;
        }
        if (z > -2.13f)
        {
            //animator.SetIKPosition(AvatarIKGoal.LeftFoot,)
            animator.SetTrigger("MoveBackLeft");
        }
        if (z < -6.44f)
        {
            animator.SetTrigger("MoveBackRight");
        }
        Debug.Log("recive done");
    }

    public void KeeperReset()
    {
        Debug.Log("reset ");
        rightHand.localPosition = idleRightHand;
        leftHand.localPosition = idleLeftHand;
        ball = null;
        startedRun = false;
        transform.position = actualPos;
        transform.rotation = Quaternion.Euler(actualRot);
        leftHandIk.weight = rightHandIk.weight = headAim.weight = neckAim.weight = 0;
        foreach (string trigger in allTriggers)
        {
            animator.ResetTrigger(trigger);
        }
        animator.enabled = false;
        this.enabled = false;
    }

    void ReachedBall()
    {        
        if (ballComp.fieldedPlayer == this.gameObject)
        {
            rightHand.position = leftHand.position = ball.position;
            leftHandIk.weight = rightHandIk.weight = 1;
            animator.SetIKPosition(AvatarIKGoal.RightHand, ball.position);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, ball.position);
            Vector3 toBall = ball.position - transform.position;
            float distance = toBall.magnitude;
            Vector3 toBallNormalized = toBall.normalized;

            float side = Vector3.Dot(transform.right, toBallNormalized);     // + right, - left
            float forward = Vector3.Dot(transform.forward, toBallNormalized); // + in front, - behind

            // Set some tuning thresholds
            float sideThresholdToDive = 0.5f;
            float diveDistanceThreshold = 2.5f;
            float frontThreshold = 0.6f;

            if (forward > frontThreshold)
            {
                if (Mathf.Abs(side) > sideThresholdToDive && distance > diveDistanceThreshold)
                {
                    // Ball is far to the side → dive
                    if (side > 0)
                        animator.SetTrigger("DiveRight");
                    else
                        animator.SetTrigger("DiveLeft");
                }
                else
                {
                    // Ball is close or centered → pick from front
                    if(ball.position.y>6f)
                    {
                        animator.Play("jump");
                    }
                    else
                    {
                        animator.SetTrigger("Pick");
                    }
                }
            }
            else
            {
                // Ball is on side or behind, close enough to pick
                if (side > 0)
                    animator.SetTrigger("Pick");
                else
                    animator.SetTrigger("Pick");
            }


            if (!ballComp.groundShot)
            {
                ballRb.isKinematic = true;
                Gameplay.instance.deliveryDead = true;
                Gameplay.instance.Out();
                return;
            }
            else
            {
                //ballRb.isKinematic = true;
            }            
            StartCoroutine(FielderPickupThrow());
        }
        else if(ballComp.fieldedPlayer == fm.keeper.gameObject)
        {
            animator.SetTrigger("StopField");
            Gameplay.instance.deliveryDead = true;
        }
        else
        {
            animator.SetTrigger("StopField");
            StopAllCoroutines();
            return;
        }
    }

    bool canThrow;

    public void ReadyToThrow()
    {
        canThrow = true;
    }

    IEnumerator FielderPickupThrow()
    {
        if (this.name == "keeper")
        {
            Debug.Log("fld done");
            Gameplay.instance.deliveryDead = true;
            StopAllCoroutines();
            yield break;
        }

        float timer = 0;

        while (!ballComp.stopTriggered && timer < 2f)
        {
            timer += Time.deltaTime;
            yield return null; // wait for next frame
        }

        Vector3 lookDirection = (fm.stumps.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(lookDirection);
        lookRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, lookRotation.eulerAngles.y, lookRotation.eulerAngles.z);
        transform.rotation = lookRotation;

        animator.SetTrigger("Throw");

        yield return new WaitForSeconds(0.4f);

        //ball.position = new Vector3(ball.position.x, 4f, ball.position.z);

        // Play the throw animation

        //yield return new WaitUntil(()=>ballComp.stopTriggered);

        //yield return new WaitUntil(() => canThrow);

        // Calculate direction and distance
        Vector3 direction = (fm.keeper.position - ball.position).normalized;
        float distance = Vector3.Distance(ball.position, fm.stumps.position);

        // Adjust speed relative to distance (tweak multiplier as needed)
        float baseSpeed = 1f; // Adjust as per your game
        float speed = baseSpeed + distance * 0.1f; // Example scaling

        // Calculate force
        Vector3 force = direction * speed;

        // Apply force to the ball
        ballRb.isKinematic = false;
        ballRb.AddForce(force, ForceMode.Impulse);

        leftHandIk.weight = rightHandIk.weight = 0;

        //fm.keeper.GetComponent<Fielder>().KeeperRecieve(ball.position);

        // Wait until the ball reaches close to the keeper
        while (!ballComp.keeperReceive)
        {
            Debug.Log("wait for ball reach");
            yield return null;
        }
        Debug.Log("fld done");
        // Mark the delivery as complete
        yield return new WaitForSeconds(0.3f);
        Gameplay.instance.deliveryDead = true;

        // Stop any other coroutines related to the fielder
        StopAllCoroutines();
    }

    private Vector3 UpdateTargetPosition()
    {
        Vector3 target;

        if (ballRb.velocity.magnitude < 20f || Vector3.Distance(transform.position, ball.position) < 0.5f)
        {
            target = ball.position;
        }
        else
        {                        
            target = CalculateInterceptPosition(ball.position, ballRb.velocity, transform.position, transform.right);
            target.y = transform.position.y;            
        }

        if (!Gameplay.instance.stadiumBounds.Contains(target))
        {
            Vector3 direction = (target - transform.position).normalized;

            Vector3 boundaryPoint = Gameplay.instance.stadiumBounds.ClosestPoint(target);

            Vector3 offset = -direction * 3f;
            target = boundaryPoint + offset;

        }
        target.y = transform.position.y;

        return target;
    }

    bool FielderCanReachOnTime(Vector3 position)
    {
        float distance = Vector2.Distance(new Vector2(position.x, position.z), new Vector2(transform.position.x, transform.position.z)) + 0.57f;
        float timeReq = distance / runSpeed;
        Vector3 positionAtReqTime = PredictBallPosition(FieldManager.hitBallPos, FieldManager.hitVelocity, timeReq);
        float fielderDistanceToPredictedPos = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(positionAtReqTime.x, positionAtReqTime.z));
        return fielderDistanceToPredictedPos / runSpeed <= timeReq;
    }

    Vector3 PredictBallPosition(Vector3 initialPosition, Vector3 velocity, float time)
    {
        // Gravity effect on ball
        float gravity = Physics.gravity.y; // Usually -9.81
        Vector3 displacement = velocity * time + 0.5f * new Vector3(0, gravity, 0) * time * time;

        Vector3 returnVec = initialPosition + displacement;
        returnVec.y = transform.position.y; // Align height with fielder
        return returnVec;
    }

    bool ShouldChase(Vector3 ballPosition, Vector3 ballVelocity, Vector3 fielderPosition)
    {
        // Convert to 2D for ground movement
        Vector2 fielderToBall = new Vector2(ballPosition.x, ballPosition.z) - new Vector2(fielderPosition.x, fielderPosition.z);
        Vector2 ballDirection = new Vector2(ballVelocity.x, ballVelocity.z).normalized;

        // Check if the ball is moving in the direction of the fielder and has passed the fielder
        float distanceAlongDirection = Vector2.Dot(fielderToBall, ballDirection);

        // The fielder should chase the ball if the ball is moving past the fielder in the direction of travel
        return distanceAlongDirection > 0;
    }

    private Vector3 CalculateInterceptPosition(Vector3 ballPos, Vector3 ballVel, Vector3 fielderPos, Vector3 fielderRight)
    {
        Vector3 boundaryHitPoint = Vector3.zero;
        // Step 1: Check if the ball velocity is high enough to consider boundary interception
        Debug.Log("Speed at calc " + ballVel.magnitude);
        if (ballVel.magnitude > 68f && this.tag is "DeepFielder") // Adjust threshold as needed for gameplay
        {
            Debug.Log("High ball velocity, calculating boundary interception for fielder." + gameObject.name);
            Vector3 rayDirection = ballPos - FieldManager.hitBallPos;
            rayDirection.y = 0; // Flatten the direction to ensure it's straight in the horizontal plane

            Ray ballRay = new Ray(FieldManager.hitBallPos, rayDirection.normalized);

            Debug.DrawRay(FieldManager.hitBallPos, (ballPos - FieldManager.hitBallPos).normalized * 1100f, Color.red, 5f);

            RaycastHit hitInfo;

            if (Physics.Raycast(ballRay, out hitInfo, 1100f))
            {
                if (hitInfo.collider.gameObject.CompareTag("boundary"))
                {
                    boundaryHitPoint = hitInfo.point;
                    boundaryHitPoint.y = fielderPos.y; // Align with the fielder's height
                    Debug.DrawLine(ballPos, boundaryHitPoint, Color.blue, 2f); // For visualization
                    Debug.Log("hitt at " + boundaryHitPoint);
                }
            }
            Debug.Log("no hitt");
        }

        // Step 2: Fallback to normal interception logic for fielders closer to the ball
        float timeToIntercept = Vector3.Distance(fielderPos, ballPos) / (runSpeed - 18 + ballVel.magnitude);
        Vector3 interceptPosition = ballPos + ballVel * timeToIntercept;

        // Adjust intercept point with fielder's lateral movement
        Vector3 lateralOffset = fielderRight * Vector3.Dot((interceptPosition - fielderPos), fielderRight.normalized);
        interceptPosition += lateralOffset * 0.23f; // Fine-tune lateral alignment

        // Move the intercept position slightly behind along the ball's movement direction
        Vector3 ballDirection = ballVel.normalized;
        //interceptPosition -= ballDirection * 20f; // Adjust by 2 units backward

        // Clamp intercept position within stadium bounds
        if (!Gameplay.instance.stadiumBounds.Contains(interceptPosition))
        {
            interceptPosition = Gameplay.instance.stadiumBounds.ClosestPoint(interceptPosition);
        }

        interceptPosition.y = fielderPos.y; // Align with fielder height
        Debug.DrawLine(fielderPos, interceptPosition, Color.green, 2f); // For visualization

        if (FielderCanReachOnTime(interceptPosition) || boundaryHitPoint == Vector3.zero)
        {            
            return interceptPosition;
        }
        else
        {
            return boundaryHitPoint;
        }

    }

    public void Initiate(Vector3 position, Transform ball)
    {
        animator.enabled = true;
        actualPos = transform.position;
        actualRot = transform.rotation.eulerAngles;
        ballComp = ball.GetComponent<BallHit>();
        ballRb = ball.GetComponent<Rigidbody>();
        this.ball = ball;
        if(!ballComp.groundShot && FielderCanReachOnTime(position))
        {
            if (!Gameplay.instance.stadiumBounds.Contains(position))
            {
                Vector3 direction = (position - transform.position).normalized;

                Vector3 boundaryPoint = Gameplay.instance.stadiumBounds.ClosestPoint(position);

                Vector3 offset = -direction * 3f;
                position = boundaryPoint + offset;

            }
            targetPosition = position;
            targetPosition.y = transform.position.y;
            //targetPosition = ballComp.shootMarker.transform.position;
            Debug.Log("ball still air will reach in time");
        }
        else
        {
            targetPosition = UpdateTargetPosition();
        }        
        StartCoroutine(StartField());
    }

    public void Reset()
    {
        //agent.Stop();
        //agent.ResetPath();
        canThrow = false;
        animator.SetTrigger("StopField");
        StopCoroutine(StartField());
        rightHand.localPosition = idleRightHand;
        leftHand.localPosition = idleLeftHand;
        ball = null;
        startedRun = false;
        transform.position = actualPos;
        transform.rotation = Quaternion.Euler(actualRot);
        leftHandIk.weight = rightHandIk.weight = 0;
        animator.ResetTrigger("StopField");
        animator.ResetTrigger("Throw");
        animator.ResetTrigger("Pick");
        foreach (string trigger in allTriggers)
        {
            animator.ResetTrigger(trigger);
        }
        animator.enabled = false;
        this.enabled = false;
    }
}
