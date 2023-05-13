using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CameraTween : MonoBehaviour
{
    public class CamState
    {
        public Vector3 m_Pos;
        public Quaternion m_Rot;

        public Vector3 m_PosVel;
        public Quaternion m_RotVel;
        public Vector3 m_AngVel;

        public float m_TweenStart;
        public float m_TweenEnd;

        public CamState(Transform trans)
        {
            m_Pos = trans.position;
            m_Rot = trans.rotation;
        }

        public void StartTween(float duration)
        {
            m_TweenStart = Time.time;
            m_TweenEnd = Time.time + duration;
        }

        public void Teleport(Transform toTransform)
        {
            m_Pos = toTransform.position;
            m_Rot = toTransform.rotation;
        }
    }

    public CamState m_PlayState;    // Controlled by game play
    public CamState m_TweenState;   // Tween simulation state

    public List<GameObject> m_Targets = new List<GameObject>();
    protected int m_Target = 0;

    // Start is called before the first frame update
    void Start()
    {
        m_PlayState = new CamState(transform);
        m_TweenState = new CamState(transform);
    }

    // Update is called once per frame
    void Update()
    {
        UpdatePlayer(m_PlayState);

        UpdateTween(m_TweenState, m_PlayState);

        if (Input.GetMouseButtonDown(0))
        {
            // Left Mouse
            m_PlayState.Teleport(m_Targets[m_Target++ % m_Targets.Count].transform);
        }

        if ( Input.GetMouseButtonDown(1) ) 
        {
            // Right mouse
            m_TweenState.StartTween(1.0f);
        }

        if (Input.GetMouseButtonDown(2))
        {
            // Middle Mouse
            m_TweenState.StartTween(1.0f);
            m_PlayState.Teleport(m_Targets[m_Target++ % m_Targets.Count].transform);
        }

        // Update the transform to match the tween state always
        transform.position = m_TweenState.m_Pos;
        transform.rotation = m_TweenState.m_Rot;
    }

    void UpdatePlayer(CamState state)
    {
        Vector3 posVelocity = Vector3.zero;
        Quaternion rotVelocityTick = Quaternion.identity;

        float rotSpeed = 360.0f;
        Vector3 mouseDelta = new Vector3( Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"), 0 );
        rotVelocityTick = Quaternion.Euler(-mouseDelta.y * rotSpeed * Time.deltaTime, mouseDelta.x * rotSpeed * Time.deltaTime, 0);

        if (Input.GetKey(KeyCode.W))
			posVelocity = posVelocity + Vector3.forward;
		if (Input.GetKey(KeyCode.D))
			posVelocity = posVelocity + Vector3.right;
		if (Input.GetKey(KeyCode.S))
			posVelocity = posVelocity - Vector3.forward;
		if (Input.GetKey(KeyCode.A))
			posVelocity = posVelocity - Vector3.right;

		posVelocity = posVelocity * 5.0f;

        state.m_Rot = state.m_Rot * rotVelocityTick;

        posVelocity = state.m_Rot * posVelocity; // align local movement to current rotation space

        state.m_Pos = state.m_Pos + posVelocity * Time.deltaTime;
    }

    void UpdateTween(CamState tweenState, CamState targetState )
    {
        if (tweenState.m_TweenEnd <= Time.time )
        {
            // Not Tweening - keep calculating the inferred velocities and position
            // ---------------------------------------------------------------------
            tweenState.m_PosVel = ( targetState.m_Pos - tweenState.m_Pos) / Time.deltaTime;
            tweenState.m_AngVel = AngularVelocity(tweenState.m_Rot, targetState.m_Rot, Time.deltaTime);
            tweenState.m_RotVel = Quaternion.Euler(tweenState.m_AngVel);

            tweenState.m_Pos = targetState.m_Pos;
            tweenState.m_Rot = targetState.m_Rot;
        }
        else
        {
            // Tweening
            // ---------------------------------------------------------------------
            float timeLeft = tweenState.m_TweenEnd - Time.time;
            float timeTotal = tweenState.m_TweenEnd - tweenState.m_TweenStart;

            // Smooth timeLeft with a sin lerp
            float sinLerpTimeLeft = Mathf.Clamp01( timeLeft * (timeLeft / timeTotal) );
            sinLerpTimeLeft = (Mathf.Sin((sinLerpTimeLeft * Mathf.PI) - (Mathf.PI * 0.5f)) * 0.5f) + 0.5f;

            // Smoothdamp toward target
            Vector3 newPos      = SmoothDamp(tweenState.m_Pos, targetState.m_Pos, ref tweenState.m_PosVel, sinLerpTimeLeft, Time.deltaTime);
            Quaternion newRot   = SmoothDamp(tweenState.m_Rot, targetState.m_Rot, ref tweenState.m_RotVel, sinLerpTimeLeft, Time.deltaTime);

            // [Dont need this but just keeping it up to date]
            tweenState.m_AngVel = AngularVelocity(tweenState.m_Rot, newRot, Time.deltaTime);

            // update current pos and rotation
            tweenState.m_Pos = newPos;
            tweenState.m_Rot = newRot;
        }
    }

    public static Vector3 AngularVelocity(Quaternion from, Quaternion to, float overTime)
    {
        Vector3 axis;
        float angle;
        Quaternion delta = Quaternion.Inverse(from) * to;
        GetAngleAxis(delta, out axis, out angle);
        Vector3 result = Quaternion.AngleAxis(angle / overTime, axis).eulerAngles;
        return result;
    }

    public static void GetAngleAxis(Quaternion q, out Vector3 axis, out float angle)
    {
        q.Normalize();

        //get as doubles for precision
        var qw = (double)q.w;
        var qx = (double)q.x;
        var qy = (double)q.y;
        var qz = (double)q.z;
        var ratio = System.Math.Sqrt(1.0d - qw * qw);

        angle = (float)(2.0d * System.Math.Acos(qw)) * Mathf.Rad2Deg;
        if (ratio < 0.001d)
        {
            axis = new Vector3(1f, 0f, 0f);
        }
        else
        {
            axis = new Vector3(
                (float)(qx / ratio),
                (float)(qy / ratio),
                (float)(qz / ratio));
            axis.Normalize();
        }
    }

    public static float SmoothDamp(float current, float target, ref float currentVelocity, float smoothTime, float deltaTime)
    {
        // Based on Game Programming Gems 4 Chapter 1.10
        // https://archive.org/details/game-programming-gems-4/page/95/mode/2up

        smoothTime = Mathf.Max(Mathf.Epsilon, smoothTime);
        float omega = 2f / smoothTime;

        float x = omega * deltaTime;
        float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);
        float change = current - target;
        if (Mathf.Abs(change) <= Mathf.Epsilon)
        {
            currentVelocity = 0;
            return current;
        }

        float originalTo = target;

        target = current - change;

        float temp = (currentVelocity + omega * change) * deltaTime;
        currentVelocity = (currentVelocity - omega * temp) * exp;
        float output = target + (change + temp) * exp;

        // Prevent overshooting
        if (originalTo - current > 0.0f == output > originalTo)
        {
            output = originalTo;
            currentVelocity = (output - originalTo) / deltaTime;
        }

        return output;
    }

    public static Vector3 SmoothDamp(Vector3 currentPosition, Vector3 targetPosition, ref Vector3 currentVelocity, float smoothTime, float deltaTime)
    {
        // Based on Game Programming Gems 4 Chapter 1.10
        // https://archive.org/details/game-programming-gems-4/page/95/mode/2up

        // Calculate the smoothed time.
        float smoothTimeClamped = Mathf.Max(Mathf.Epsilon, smoothTime);

        // Calculate the angular frequency.
        float omega = 2f / smoothTimeClamped;

        // Calculate the gain.
        float x = omega * deltaTime;
        float gain = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);

        // Calculate the target and velocity.
        Vector3 distance = currentPosition - targetPosition;
        if (distance.sqrMagnitude < Mathf.Epsilon)
        {
            currentVelocity = Vector3.zero;
            return currentPosition;
        }

        Vector3 target = currentPosition - distance;
        Vector3 velocity = currentVelocity + omega * distance * deltaTime;

        // Update the current velocity.
        currentVelocity = (currentVelocity - omega * velocity * deltaTime) * gain;

        // Calculate the next position.
        Vector3 nextPosition = target + (distance + velocity) * gain;

        // Check if we overshot the target and adjust accordingly.
        Vector3 currentPositionToTarget = targetPosition - currentPosition;
        Vector3 nextPositionToTarget = nextPosition - targetPosition;
        float dot = Vector3.Dot(currentPositionToTarget, nextPositionToTarget);
        if (dot > 0f)
        {
            nextPosition = targetPosition;
            currentVelocity = currentPositionToTarget / deltaTime;
        }

        // Return the next position.
        return nextPosition;
    }

    public static Quaternion SmoothDamp(Quaternion rot, Quaternion target, ref Quaternion deriv, float time, float deltaTime)
    {
        if (Time.deltaTime < Mathf.Epsilon)
            return rot;

        if ( rot.normalized == target.normalized )
        {
            deriv = Quaternion.identity;
            return target;
        }

        // account for double-cover
        var Dot = Quaternion.Dot(rot, target);
        var Multi = Dot > 0f ? 1f : -1f;
        target.x *= Multi;
        target.y *= Multi;
        target.z *= Multi;
        target.w *= Multi;

        // SmoothDamp each value individually
        var Result = new Vector4(
            SmoothDamp(rot.x, target.x, ref deriv.x, time, deltaTime),
            SmoothDamp(rot.y, target.y, ref deriv.y, time, deltaTime),
            SmoothDamp(rot.z, target.z, ref deriv.z, time, deltaTime),
            SmoothDamp(rot.w, target.w, ref deriv.w, time, deltaTime)
        ).normalized;

        // ensure deriv is tangent
        var derivError = Vector4.Project(new Vector4(deriv.x, deriv.y, deriv.z, deriv.w), Result);
        deriv.x -= derivError.x;
        deriv.y -= derivError.y;
        deriv.z -= derivError.z;
        deriv.w -= derivError.w;

        return new Quaternion(Result.x, Result.y, Result.z, Result.w);
    }
}

