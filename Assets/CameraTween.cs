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

        if ( Input.GetMouseButtonDown(1) ) 
        {
            m_State[1].m_TweenEnd = Time.time + 1.0f;
            m_State[1].m_TweenStart = Time.time;
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

        if (Input.GetMouseButtonDown(0))
        {
            m_Target++;
            state.m_Pos = m_Targets[m_Target % m_Targets.Count].transform.position;
        }
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
			float time = state.m_TweenEnd - Time.time;
            float elapsedTime = Time.time - state.m_TweenStart;
            float duration = state.m_TweenEnd - state.m_TweenStart;
            float dt = Time.deltaTime;

			Vector3 distance = state.m_Target.m_Pos - state.m_Pos;

            // Calculate the damping factor based on the elapsed time and a sin wave starting at full, going to zero, and coming back to full
            //float damping = Mathf.Clamp01(Mathf.Sin((Mathf.PI * 0.5f) + (Mathf.PI * 2.0f) * (elapsedTime / duration)));

            Vector3 acceleration = 2.0f * (distance - state.m_PosVel * time) / (time * time);// * (1.0f - damping);

			Vector3 newPos = state.m_Pos + state.m_PosVel * dt + 0.5f * acceleration * dt * dt;
			state.m_PosVel = (newPos - state.m_Pos) / dt;
            state.m_Pos = newPos;
		}
	}
}

