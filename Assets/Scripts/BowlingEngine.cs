using System.Collections.Generic;
using UnityEngine;

public enum DeliveryLength { Yorker, Slot, GoodLength, BackOfLength, Short, FullToss }

[System.Serializable]
public struct BowlingPoint
{
    public string name;
    public Vector3 point;
    public DeliveryLength length;
}

[ExecuteInEditMode]
public class BowlingEngine : MonoBehaviour
{
    [Header("References")]
    public Rigidbody ballRigidbody;
    public Transform handReleasePoint, marker;

    [Header("Speed Settings")]
    [Tooltip("Target speed in km/h (e.g., 145 for Fast, 120 for Medium)")]
    public float deliverySpeedKmh = 145f;

    [Header("Delivery Points Data")]
    public List<BowlingPoint> deliveryPoints = new List<BowlingPoint>();

    public static BowlingEngine instance;
    [HideInInspector] public Vector3 chosenPoint;

    private void Awake()
    {
        instance = this;
        if (deliveryPoints == null || deliveryPoints.Count == 0)
        {
            InitializePoints();
        }
    }

    public Vector3 DecidePoint(bool isDeathOvers)
    {
        BowlingPoint targetZone = SelectTargetZoneRealistic(isDeathOvers);

        // Add minor variation (Spray)
        chosenPoint = targetZone.point + new Vector3(Random.Range(-0.1f, 0.1f), 0, Random.Range(-0.1f, 0.1f));

        Debug.Log($"<color=cyan>CHOSEN:</color> <b>{targetZone.name}</b> | Target Pos: {targetZone.point} | Speed: {deliverySpeedKmh}km/h");

        if (marker != null) marker.position = chosenPoint;
        return chosenPoint;
    }

    public void Release()
    {
        if (!Application.isPlaying) return;

        // Convert km/h to m/s for Unity Physics
        float speedMs = deliverySpeedKmh / 3.6f;

        Vector3 launchVelocity = CalculateVelocityForSpeed(chosenPoint, speedMs);

        ballRigidbody.isKinematic = false;
        ballRigidbody.velocity = Vector3.zero;
        ballRigidbody.angularVelocity = Vector3.zero;
        ballRigidbody.transform.position = handReleasePoint.position;
        ballRigidbody.velocity = launchVelocity;

        Debug.Log($"<color=green>RELEASED:</color> Speed {deliverySpeedKmh} km/h toward {chosenPoint}");
    }

    private Vector3 CalculateVelocityForSpeed(Vector3 target, float speed)
    {
        Vector3 origin = handReleasePoint.position;
        Vector3 toTarget = target - origin;

        // Calculate time based on 3D distance and speed
        float distance = toTarget.magnitude;
        float time = distance / speed;

        // X and Z components (Constant velocity)
        float vx = toTarget.x / time;
        float vz = toTarget.z / time;

        // Y component (Accounting for gravity)
        // Formula: y = v0y*t + 0.5*g*t^2  => v0y = (y - 0.5*g*t^2) / t
        float vy = (toTarget.y - (0.5f * Physics.gravity.y * time * time)) / time;

        return new Vector3(vx, vy, vz);
    }

    private BowlingPoint SelectTargetZoneRealistic(bool isDeathOvers)
    {
        List<BowlingPoint> pool = new List<BowlingPoint>();
        float r = Random.value;
        DeliveryLength chosenLength;

        if (isDeathOvers)
        {
            if (r < 0.65f) chosenLength = DeliveryLength.Yorker;
            else if (r < 0.85f) chosenLength = DeliveryLength.FullToss;
            else chosenLength = DeliveryLength.Slot;
        }
        else
        {
            if (r < 0.75f) chosenLength = DeliveryLength.GoodLength;
            else if (r < 0.90f) chosenLength = DeliveryLength.BackOfLength;
            else chosenLength = DeliveryLength.Short;
        }

        pool = deliveryPoints.FindAll(p => p.length == chosenLength);
        if (pool.Count == 0) pool = deliveryPoints;

        return pool[Random.Range(0, pool.Count)];
    }

    [ContextMenu("Reset to Default Points")]
    public void InitializePoints()
    {
        deliveryPoints.Clear();
        float groundY = -4.42f;
        float centerZ = -0.36f;

        // --- CALIBRATED FOR 90-UNIT PITCH ---
        AddPoint("Top of Off", new Vector3(2.0f, groundY, -0.8f), DeliveryLength.GoodLength);
        AddPoint("Corridor Off", new Vector3(5.0f, groundY, -1.8f), DeliveryLength.GoodLength);
        AddPoint("Good Length Middle", new Vector3(0.0f, groundY, centerZ), DeliveryLength.GoodLength);
        AddPoint("Tight into Pads", new Vector3(-2.5f, groundY, 0.45f), DeliveryLength.GoodLength);

        AddPoint("Yorker Middle", new Vector3(-30.5f, groundY, centerZ), DeliveryLength.Yorker);
        AddPoint("Yorker Off", new Vector3(-30.2f, groundY, -0.8f), DeliveryLength.Yorker);
        AddPoint("Yorker Wide", new Vector3(-29.5f, groundY, -4.0f), DeliveryLength.Yorker);

        AddPoint("Heavy Ball Ribs", new Vector3(18.0f, groundY, 0.1f), DeliveryLength.BackOfLength);
        AddPoint("Defensive Back", new Vector3(25.0f, groundY, -2.0f), DeliveryLength.BackOfLength);

        // Bouncers (Pitched closer to bowler for higher trajectory)
        AddPoint("Standard Bouncer", new Vector3(38.0f, groundY, centerZ), DeliveryLength.Short);
        AddPoint("Wide Bouncer", new Vector3(35.0f, groundY, -3.5f), DeliveryLength.Short);

        AddPoint("Full Toss", new Vector3(-31.08f, -3.8f, centerZ), DeliveryLength.FullToss);
    }

    private void AddPoint(string n, Vector3 pos, DeliveryLength l)
    {
        deliveryPoints.Add(new BowlingPoint { name = n, point = pos, length = l });
    }

    public void UpdatePoint(int index, Vector3 newPos)
    {
        if (index >= 0 && index < deliveryPoints.Count)
        {
            BowlingPoint p = deliveryPoints[index];
            p.point = newPos;
            deliveryPoints[index] = p;
        }
    }

    [ContextMenu("Log Points for Script")]
    public void LogPointsForScript()
    {
        string code = "public void InitializePoints()\n{\n    deliveryPoints.Clear();\n    float groundY = -4.42f;\n";
        foreach (var dp in deliveryPoints)
        {
            code += $"    AddPoint(\"{dp.name}\", new Vector3({dp.point.x:F2}f, {dp.point.y:F2}f, {dp.point.z:F2}f), DeliveryLength.{dp.length});\n";
        }
        code += "}";
        Debug.Log(code);
    }
}