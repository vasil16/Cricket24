using System.Collections.Generic;
using UnityEngine;

public enum DeliveryLength { Yorker, Full, GoodLength, BackOfLength, Short, FullToss }

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

        chosenPoint = Gameplay.instance.pitchPoint;

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
            else chosenLength = DeliveryLength.Full;
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
        float groundY = 0.001f;

        // YORKERS (unchanged)
        AddPoint("Toe-Crusher Straight", new Vector3(0.00f, groundY, 41.5f), DeliveryLength.Yorker);
        AddPoint("Off-Stump Blockhole", new Vector3(-0.52f, groundY, 40.0f), DeliveryLength.Yorker);
        AddPoint("Wide Yorker (Legal)", new Vector3(-3.80f, groundY, 39.0f), DeliveryLength.Yorker);

        // FULL / SLOT (stronger off correction)
        AddPoint("Full at Stumps", new Vector3(-0.70f, groundY, 30.0f), DeliveryLength.Full);
        AddPoint("Full Outside Off (4th stump)", new Vector3(-1.55f, groundY, 26.0f), DeliveryLength.Full);
        AddPoint("Driving Full Length (5th stump)", new Vector3(-2.10f, groundY, 20.0f), DeliveryLength.Full);

        // GOOD / LENGTH (much stronger correction)
        AddPoint("Top of Off", new Vector3(-1.40f, groundY, 13.5f), DeliveryLength.GoodLength);
        AddPoint("Fourth Stump Probe", new Vector3(-2.10f, groundY, 11.5f), DeliveryLength.GoodLength);
        AddPoint("Corridor Control", new Vector3(-2.75f, groundY, 10.0f), DeliveryLength.GoodLength);
        AddPoint("Into Pads Length", new Vector3(0.20f, groundY, 12.5f), DeliveryLength.GoodLength);

        // BACK OF A LENGTH (aggressive correction)
        AddPoint("Heavy Back Length", new Vector3(-1.90f, groundY, 8.0f), DeliveryLength.BackOfLength);
        AddPoint("Rising Corridor Length", new Vector3(-2.85f, groundY, 6.5f), DeliveryLength.BackOfLength);
        AddPoint("Cramping Length", new Vector3(0.60f, groundY, 5.5f), DeliveryLength.BackOfLength);

        // SHORT / BOUNCERS (already correct)
        AddPoint("Head-High Bouncer", new Vector3(-2.10f, groundY, -6.0f), DeliveryLength.Short);
        AddPoint("Rib-Cage Bouncer", new Vector3(-1.70f, groundY, -10.0f), DeliveryLength.Short);
        AddPoint("Wide Surprise Bouncer", new Vector3(-4.20f, groundY, -15.5f), DeliveryLength.Short);
        AddPoint("Nasty Throat Ball", new Vector3(-1.95f, groundY, -12.0f), DeliveryLength.Short);
    }

    private void AddPoint(string n, Vector3 pos, DeliveryLength l)
    {
        deliveryPoints.Add(new BowlingPoint
        {
            name = n,
            point = pos,
            length = l
        });
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