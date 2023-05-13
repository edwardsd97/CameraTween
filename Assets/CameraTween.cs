using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CameraTween : MonoBehaviour
{
    public class CamState
    {
        public CamState m_Target;

        public Vector3 m_Pos;
        public Quaternion m_Rot;

        public Vector3 m_PosVel;
        public Quaternion m_RotVel;
        public Vector3 m_AngVel;

        public float m_TweenEnd;

        public CamState(CamState target, Transform trans)
        {
            m_Target = target;

            m_Pos = trans.position;
            m_Rot = trans.rotation;
        }

        public void StartTween(float duration)
        {
            m_TweenEnd = Time.time + duration;
        }

        public void Teleport(Transform transform)
        {
            m_Pos = transform.position;
            m_Rot = transform.rotation;
        }

        /// Get the rotation that would be applied to 'start' to end up at 'end'.
        public static Quaternion FromToRotation(Quaternion start, Quaternion end)
        {
            return Quaternion.Inverse(start) * end;
        }

        public Vector3 AngularVelocity(Quaternion from, Quaternion to, float overTime)
        {
            Quaternion delta = FromToRotation( from, to );
            return AngularVelocity(delta, overTime);
        }

        public Vector3 AngularVelocity(Quaternion rotationDelta, float overTime)
        {
            Vector3 axis;
            float angle;
            QuaternionUtil.GetAngleAxis(rotationDelta, out axis, out angle);
            Vector3 result = Quaternion.AngleAxis(angle / overTime, axis ).eulerAngles;
            return result;
        }
    }

    public CamState[] m_State = new CamState[2];
    public List<GameObject> m_Targets = new List<GameObject>();
    protected int m_Target = 0;

    // Start is called before the first frame update
    void Start()
    {
        m_State[0] = new CamState(null, transform);
        m_State[1] = new CamState(m_State[0], transform);
    }

    // Update is called once per frame
    void Update()
    {
        UpdatePlayer( m_State[0] );

        UpdateTween( m_State[1] );

        if (Input.GetMouseButtonDown(0))
        {
            // Left Mouse
            m_Target++;
            m_State[0].Teleport(m_Targets[m_Target % m_Targets.Count].transform);
        }

        if ( Input.GetMouseButtonDown(1) ) 
        {
            // Right mouse
            m_State[1].StartTween(1.0f);
        }

        if (Input.GetMouseButtonDown(2))
        {
            // Middle Mouse

            // teleport and tween at the same time
            m_State[1].StartTween(1.0f);

            m_Target++;
            m_State[0].Teleport(m_Targets[m_Target % m_Targets.Count].transform);
        }

        // Update the transform to match the tween state always
        transform.position = m_State[1].m_Pos;
        transform.rotation = m_State[1].m_Rot;
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

        Quaternion newRot = state.m_Rot * rotVelocityTick;
        state.m_AngVel = state.AngularVelocity(state.m_Rot, newRot, Time.deltaTime);
        state.m_RotVel = Quaternion.Euler( state.m_AngVel );

        state.m_Rot = newRot;

        posVelocity = state.m_Rot * posVelocity; // align local movement to current rotation space

        state.m_Pos = state.m_Pos + posVelocity * Time.deltaTime;
        state.m_PosVel = posVelocity;
    }

    void UpdateTween(CamState state)
	{
        if ( state.m_TweenEnd <= Time.time )
        {
            state.m_PosVel = (state.m_Target.m_Pos - state.m_Pos) / Time.deltaTime;
            state.m_AngVel = state.AngularVelocity(state.m_Rot, state.m_Target.m_Rot, Time.deltaTime);
            state.m_RotVel = Quaternion.Euler(state.m_AngVel);

			state.m_Pos = state.m_Target.m_Pos;
            state.m_Rot = state.m_Target.m_Rot;
        }
        else
        {
            float timeLeft = state.m_TweenEnd - Time.time;

            // Update tween state based on current input coming from the player
            Vector3 newPos = state.m_Pos + state.m_Target.m_PosVel * Time.deltaTime;
            Quaternion newRot = state.m_Rot;

            // Smoothdamp toward target
            newPos = SmoothDamp(newPos, state.m_Target.m_Pos, ref state.m_PosVel, timeLeft, Time.deltaTime);
            newRot = SmoothDamp(newRot, state.m_Target.m_Rot, ref state.m_RotVel, timeLeft, Time.deltaTime);

            // Dont need this but just keeping it up to date
            state.m_AngVel = state.AngularVelocity(state.m_Rot, newRot, Time.deltaTime);

            // update current pos and rotation
            state.m_Pos = newPos;
            state.m_Rot = newRot;
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

