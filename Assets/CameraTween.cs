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
        public Quaternion m_RotVel = Quaternion.identity;

        public float m_TweenStart;
        public float m_TweenEnd;

        public CamState( CamState target, Transform trans )
        {
            m_Target = target;

            m_Pos = trans.position;
            m_Rot = trans.rotation;
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

        if (Input.GetMouseButtonDown(2))
        {
            // teleport and tween at the same time
            m_State[1].m_TweenEnd = Time.time + 1.0f;
            m_State[1].m_TweenStart = Time.time;
            m_Target++;
            m_State[0].m_Pos = m_Targets[m_Target % m_Targets.Count].transform.position;
        }

        if ( Input.GetMouseButtonDown(1) ) 
        {
            // tween
            m_State[1].m_TweenEnd = Time.time + 1.0f;
            m_State[1].m_TweenStart = Time.time;
        }

        if (Input.GetMouseButtonDown(0))
        {
            // teleport
            m_Target++;
            m_State[0].m_Pos = m_Targets[m_Target % m_Targets.Count].transform.position;
        }

        // Update the transform to match the tween state always
        transform.position = m_State[1].m_Pos;
        transform.rotation = m_State[1].m_Rot;
    }

    void UpdatePlayer(CamState state)
    {
        Vector3 posVelocity = Vector3.zero;
        Quaternion rotVelocity= Quaternion.identity;

        float rotSpeed = 60.0f;
        rotVelocity = Quaternion.Euler(Input.mouseScrollDelta.y * rotSpeed * Time.deltaTime, Input.mouseScrollDelta.x * rotSpeed * Time.deltaTime, 0);

        if (Input.GetKey(KeyCode.W))
			posVelocity = posVelocity + Vector3.forward;
		if (Input.GetKey(KeyCode.D))
			posVelocity = posVelocity + Vector3.right;
		if (Input.GetKey(KeyCode.S))
			posVelocity = posVelocity - Vector3.forward;
		if (Input.GetKey(KeyCode.A))
			posVelocity = posVelocity - Vector3.right;

		posVelocity = posVelocity * 5.0f;

        state.m_Pos = state.m_Pos + posVelocity * Time.deltaTime;
		state.m_Rot = rotVelocity * state.m_Rot;
	}

	void UpdateTween(CamState state)
	{
        if ( state.m_TweenEnd <= Time.time )
        {
            state.m_PosVel = (state.m_Target.m_Pos - state.m_Pos) / Time.deltaTime;
			state.m_RotVel = state.m_Target.m_Rot * Quaternion.Inverse( state.m_Rot );

			state.m_Pos = state.m_Target.m_Pos;
            state.m_Rot = state.m_Target.m_Rot;
        }
        else if (state.m_TweenStart > 0.0f)
        {
#if false
            float dt = Time.deltaTime;
            float time = state.m_TweenEnd - Time.time;
            float elapsedTime = Time.time - state.m_TweenStart;
            float duration = state.m_TweenEnd - state.m_TweenStart;

			Vector3 distance = state.m_Target.m_Pos - state.m_Pos;

            // Calculate the damping factor based on the elapsed time and a sin wave starting at full, going to zero, and coming back to full
            //float damping = Mathf.Clamp01(Mathf.Sin((Mathf.PI * 0.5f) + (Mathf.PI * 2.0f) * (elapsedTime / duration)));

            Vector3 acceleration = 2.0f * (distance - state.m_PosVel * time) / (time * time);// * (1.0f - damping);

			Vector3 newPos = state.m_Pos + state.m_PosVel * dt + 0.5f * acceleration * dt * dt;
			state.m_PosVel = (newPos - state.m_Pos) / dt;
            state.m_Pos = newPos;
#elif false
            float dt = Time.deltaTime;
            float timeLeft = state.m_TweenEnd - Time.time;

            Vector3 distance = state.m_Target.m_Pos - state.m_Pos;

            Vector3 targetVel = distance / dt;

            float coef = Mathf.Min( 1.0f, dt / timeLeft );

            state.m_PosVel = Vector3.Lerp(state.m_PosVel, targetVel, coef * coef);

            Vector3 newPos = state.m_Pos + state.m_PosVel * dt;

            state.m_Pos = newPos;
#elif false
            float dt = Time.deltaTime;
            float timeRemaining = state.m_TweenEnd - Time.time;

            Vector3 currentPos = state.m_Pos;
            Vector3 targetPos = state.m_Target.m_Pos;

            Vector3 distance = targetPos - currentPos;

            float timeRatio = 1.0f - Mathf.Clamp01((timeRemaining - dt) / timeRemaining);
            float easingFactor = SmoothStep(timeRatio);
            currentPos = Vector3.Lerp(currentPos, targetPos, easingFactor);

            state.m_Pos = currentPos;
#else
            float timeLeft = state.m_TweenEnd - Time.time;
            Vector3 newPos = SmoothDampSimple(state.m_Pos, state.m_Target.m_Pos, ref state.m_PosVel, timeLeft, Time.deltaTime);
            state.m_Pos = newPos;
#endif
        }
    }

    float SmoothStep(float x)
    {
        return x * x * (3.0f - 2.0f * x);
    }

    public static Vector3 SmoothDamp(Vector3 currentPosition, Vector3 targetPosition, ref Vector3 currentVelocity, float smoothTime, float deltaTime)
    {
        float epsilon = 0.0001f;
        float smoothTimeClamped = Mathf.Max(epsilon, smoothTime);

        float omega = 2f / smoothTimeClamped;
        float x = omega * deltaTime;

        float gain = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);

        float distanceX = currentPosition.x - targetPosition.x;
        float distanceY = currentPosition.y - targetPosition.y;
        float distanceZ = currentPosition.z - targetPosition.z;

        Vector3 target = targetPosition;

        target.x = currentPosition.x - distanceX;
        target.y = currentPosition.y - distanceY;
        target.z = currentPosition.z - distanceZ;

        float velocityX = currentVelocity.x + omega * distanceX * deltaTime;
        float velocityY = currentVelocity.y + omega * distanceY * deltaTime;
        float velocityZ = currentVelocity.z + omega * distanceZ * deltaTime;

        currentVelocity.x = (currentVelocity.x - omega * velocityX * deltaTime) * gain;
        currentVelocity.y = (currentVelocity.y - omega * velocityY * deltaTime) * gain;
        currentVelocity.z = (currentVelocity.z - omega * velocityZ * deltaTime) * gain;

        float nextPositionX = target.x + (distanceX + velocityX) * gain;
        float nextPositionY = target.y + (distanceY + velocityY) * gain;
        float nextPositionZ = target.z + (distanceZ + velocityZ) * gain;

        float currentPositionToTargetX = targetPosition.x - currentPosition.x;
        float currentPositionToTargetY = targetPosition.y - currentPosition.y;
        float currentPositionToTargetZ = targetPosition.z - currentPosition.z;

        float nextPositionToTargetX = nextPositionX - targetPosition.x;
        float nextPositionToTargetY = nextPositionY - targetPosition.y;
        float nextPositionToTargetZ = nextPositionZ - targetPosition.z;

        float dot = currentPositionToTargetX * nextPositionToTargetX + currentPositionToTargetY * nextPositionToTargetY + currentPositionToTargetZ * nextPositionToTargetZ;

        if (dot > 0f)
        {
            nextPositionX = targetPosition.x;
            nextPositionY = targetPosition.y;
            nextPositionZ = targetPosition.z;

            currentVelocity.x = currentPositionToTargetX / deltaTime;
            currentVelocity.y = currentPositionToTargetY / deltaTime;
            currentVelocity.z = currentPositionToTargetZ / deltaTime;
        }

        return new Vector3(nextPositionX, nextPositionY, nextPositionZ);
    }

    public static Vector3 SmoothDampSimple(Vector3 currentPosition, Vector3 targetPosition, ref Vector3 currentVelocity, float smoothTime, float deltaTime)
    {
        // Calculate the smoothed time.
        float smoothTimeClamped = Mathf.Max(0.0001f, smoothTime);

        // Calculate the angular frequency.
        float omega = 2f / smoothTimeClamped;

        // Calculate the gain.
        float x = omega * deltaTime;
        float gain = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);

        // Calculate the target and velocity.
        Vector3 distance = currentPosition - targetPosition;
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

}

