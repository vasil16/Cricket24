using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldManager : MonoBehaviour
{
    public List<Fielder> fielders;
    public Transform ball;
    public float fieldingRange = 1.5f;

    public List<Fielder> bestFielders = new List<Fielder>();
    public static Action<Vector3> StartCheckField;
    public static Action ResetFielder;

    public static Vector3 hitBallPos, hitVelocity;

    public float score;
    public Transform marker, keeper, stumps;
    [SerializeField] MeshRenderer ignoreBounds;
    public bool ballWasAirBorne;

    private void Start()
    {
        StartCheckField = AssignBestFielders;
        ResetFielder = ResetFielders;
    }

    public void AssignBestFielders(Vector3 ballAt)
    {
        bestFielders.Clear();
        ball = Gameplay.instance.currentBall;
        Debug.Log("fielder");
        StartCoroutine(DelayAndCheck(ballAt));
    }   

    IEnumerator DelayAndCheck(Vector3 ballAt)
    {
        yield return new WaitForSeconds(0.4f);
        Vector2 ballPos2D = new Vector2(ball.position.x, ball.position.z);
        Vector2 ballAt2D = new Vector2(ballAt.x, ballAt.z);
        Vector2 dir2D = (ballPos2D - ballAt2D).normalized;

        Vector3 flatDirection = new Vector3(dir2D.x, 0, dir2D.y);
        Ray ballPath = new Ray(new Vector3(ballAt.x, 0.2f, ballAt.z), flatDirection);

        RaycastHit[] hits = Physics.RaycastAll(ballPath, Mathf.Infinity, ~0, QueryTriggerInteraction.Collide);

        Debug.DrawRay(ballPath.origin, ballPath.direction, Color.green, 10);

        foreach (var hit in hits)
        {
            //if (bestFielders.Count >= 1)
            //{
            //    break;
            //}

            if (hit.collider.CompareTag("rayTest"))
            {
                Debug.Log("Added fielder  " + hit.collider.transform.parent.gameObject.name);
                Fielder fielder = hit.collider.transform.parent.GetComponent<Fielder>();
                Vector3 closestPoint = hit.collider.bounds.ClosestPoint(ball.position);
                fielder.enabled = true;
                //fielder.targetPosition = closestPoint;
                fielder.targetPosition = hit.point;
                bestFielders.Add(fielder);
                Debug.Log("fielders added " + bestFielders.Count);
            }
            else
            {
                Debug.Log("sum else");
            }
        }
        foreach (var fielder in bestFielders)
        {
            if (!fielder.startedRun)
            {                
                fielder.startedRun = true;
                fielder.Initiate(ballAt, ball);
            }
        }
        if (!bestFielders.Contains(fielders[0]))
        {
            StartCoroutine(KeeperRunToRecieve());
        }
        else
        {
            if(bestFielders.Count>=3)
            {
                bestFielders.Remove(fielders[0]);
            }
        }
    }

    IEnumerator KeeperRunToRecieve()
    {
        keeper.GetComponent<Fielder>().enabled = true;
        keeper.GetComponent<Fielder>().ball = ball;
        keeper.GetComponent<FielderIK>().PlayAnimation(keeper.GetComponent<Fielder>().runningClip);
        Vector3 moveDirection;
        Quaternion lookRotation;

        while (Vector2.Distance(new Vector2(keeper.transform.position.x, keeper.transform.position.z),new Vector2(stumps.position.x, stumps.position.z))>2f)
        {
            moveDirection = (ball.position - keeper.transform.position).normalized;
            lookRotation = Quaternion.LookRotation(moveDirection);
            lookRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, lookRotation.eulerAngles.y, lookRotation.eulerAngles.z);
            keeper.transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 100f);
            keeper.transform.position = Vector3.MoveTowards(keeper.transform.position, new Vector3(stumps.position.x, keeper.transform.position.y, stumps.position.z), 28 * Time.deltaTime);
            yield return null;
        }
        moveDirection = (ball.position - keeper.transform.position).normalized;
        lookRotation = Quaternion.LookRotation(moveDirection);
        lookRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, lookRotation.eulerAngles.y, lookRotation.eulerAngles.z);
        keeper.GetComponent<FielderIK>().PlayAnimation(keeper.GetComponent<Fielder>().idleClip);
        keeper.transform.rotation = lookRotation;

    }   


    public void ResetFielders()
    {
        keeper.GetComponent<Fielder>().KeeperReset();
        foreach (var fielder in bestFielders)
        {
            fielder.Reset();
            fielder.startedRun = false;
            fielder.GetComponent<Animator>().enabled = true;
            //fielder.enabled = false;
        }
        bestFielders.Clear();
    }
}
