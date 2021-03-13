using System;
using UnityEngine;
using UnityEngine.AI;
using WizardsCode.Stats;

namespace WizardsCode.Character
{
    /// <summary>
    /// A character actor performs for the camera and takes cues from a director.
    /// Converts NavMesh movement to animation controller parameters.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class ActorController : MonoBehaviour
    {
        [Header("Animation")]
        [SerializeField, Tooltip("The name of the parameter in the animator that sets the forward speed of the character.")]
        private string SpeedParameterName = "Forward";
        [SerializeField, Tooltip("The name of the parameter in the animator that sets the turn angle of the character.")]
        private string TurnParameterName = "Turn";
        [SerializeField, Tooltip("The speed of this character when at a run. It will usually be going slower than this, and for short periods, can go faster (at a spring).")]
        private float m_RunningSpeed = 8;

        [Header("IK")]
        [SerializeField, Tooltip("Should the actor use IK to look at a given target.")]
        bool m_EnableIKLook = true;
        [SerializeField, Tooltip("A transform at the point in space that the actor should look towards.")]
        Transform m_LookAtTarget;
        [SerializeField, Tooltip("The head bone, used for Look IK. If this is blank there will be an attempt to automatically find the head upon startup.")]
        public Transform head = null;
        [SerializeField, Tooltip("The time it takes for the head to start moving when it needs to turn to look at something.")]
        float m_LookAtHeatTime = 0.2f;
        [SerializeField, Tooltip("The time it takes for the look IK rig to cool after reaching the correct look angle.")]
        float m_LookAtCoolTime = 0.2f;

        private Animator m_Animator;
        private NavMeshAgent m_Agent;
        private Brain m_Brain;

        private Vector3 m_CurrentLookAtPosition;
        private float lookAtWeight = 0.0f;

        internal Transform LookAtTarget
        {
            get { return m_LookAtTarget; }
            set
            {
                m_LookAtTarget.transform.SetParent(value);
                m_LookAtTarget.localPosition = Vector3.zero;
                m_LookAtTarget.localRotation = Quaternion.identity;
            }
        }

        internal Animator Animator
        {
            get { return m_Animator; }
        }

        internal Vector3 MoveTargetPosition
        {
            get { return m_Agent.destination; }
            set
            {
                m_Agent.SetDestination(value);
            }
        }

        internal void MoveTo(Transform destination)
        {
            MoveTargetPosition = destination.position;
        }

        /// <summary>
        /// Stop the actor from moving. Clearing the current path if there is one.
        /// </summary>
        internal void StopMoving()
        {
            m_Agent.ResetPath();
        }

        System.Collections.IEnumerator cueCoroutine;
        /// <summary>
        /// Prompt the actor to enact a cue. A cue describes
        /// a position and actions that an actor should take.
        /// </summary>
        /// <param name="cue">The cue to enact.</param>
        public void Prompt(ActorCue cue)
        {
            cueCoroutine = cue.Prompt(this);
            if (cueCoroutine != null)
            {
                StartCoroutine(cueCoroutine);
            }
        }

        protected virtual void Awake()
        {
            m_Animator = GetComponentInChildren<Animator>();
            m_Agent = GetComponent<NavMeshAgent>();
            m_Brain = GetComponent<Brain>();
            MoveTargetPosition = transform.position;

            // Look IK Setup
            if (!head)
            {
                head = transform.Find("Head");
            }
            if (!head)
            {
                Debug.LogError("No head transform set on " + gameObject.name + " and one could not be found automatically - LookAt disabled");
                m_EnableIKLook = false;
            }
        }

        [Obsolete("Use an ActorCue that plays the chosen Emote.")] // v0.0.9
        public void PlayEmote(string name)
        {
            Animator animator = GetComponent<Animator>();
            animator.Play(name);
        }

        protected virtual void Update()
        {
            float sqrMagToLookAtTarget = Vector3.SqrMagnitude(LookAtTarget.position - transform.position);
            if (sqrMagToLookAtTarget > 100)
            {
                ResetLookAt();
            }

            if (m_Animator != null && m_Agent != null)
            {
                float speed = m_Agent.desiredVelocity.magnitude / m_RunningSpeed;
                if (speed < 0.05 || speed > 0.05)
                {
                    m_Animator.SetFloat(SpeedParameterName, speed);
                }
                else
                {
                    m_Animator.SetFloat(SpeedParameterName, 0);
                }

                Vector3 s = m_Agent.transform.InverseTransformDirection(m_Agent.velocity).normalized;
                float turn = s.x;
                m_Animator.SetFloat(TurnParameterName, turn);
            }
        }

        internal bool IsMoving
        {
            get
            {
                if (m_Agent.hasPath && !m_Agent.pathPending)
                {
                    if (m_Agent.remainingDistance <= m_Agent.stoppingDistance)
                    {
                        return false;
                    } else
                    {
                        return true;
                    }
                } 
                
                if (m_Agent.hasPath && m_Agent.pathPending)
                {
                    return true;
                }
                
                if (!m_Agent.hasPath && !m_Agent.pathPending)
                {
                    return true;
                }

                return false;
            }
        }

        [Obsolete("Use IsMoving instead")] // v0.0.11
        internal bool HasReachedTarget
        {
            get { return IsMoving; }
        }

        /// <summary>
        /// A measure of how noticable this character is from 0 to 1. 
        /// 0 is as good as invisible, 1 is can't miss them.
        /// How noticable an actor is depends on what they are doing
        /// at any given time as well as their emations. For example, 
        /// a fearful character who is resting is less noticeable
        /// than an interested character. Anger will increase noticability,
        /// but sadness will reduce it. Similarly a character who is attacking
        /// is more noticable than one who is idle.
        /// </summary>
        public float Noticability { 
            get
            {
                float result = 0;

                EmotionalState emotion = GetComponent<EmotionalState>();
                if (emotion)
                {
                    result = emotion.Noticability;
                }

                //TODO currently active behaviour should impact noticability. Add a noticability factor to behaviours.
                return Mathf.Clamp01(result);
            }
        }

        void OnAnimatorIK()
        {
            if (!m_EnableIKLook)
            {
                return;
            }

            Vector3 pos = LookAtTarget.position;
            //pos.y = head.position.y;

            float lookAtTargetWeight = m_EnableIKLook ? 1.0f : 0.0f;

            Vector3 curDir = m_CurrentLookAtPosition - head.position;
            Vector3 futDir = pos - head.position;

            curDir = Vector3.RotateTowards(curDir, futDir, 6.28f * Time.deltaTime, float.PositiveInfinity);
            m_CurrentLookAtPosition = head.position + curDir;

            float blendTime = lookAtTargetWeight > lookAtWeight ? m_LookAtHeatTime : m_LookAtCoolTime;
            lookAtWeight = Mathf.MoveTowards(lookAtWeight, lookAtTargetWeight, Time.deltaTime / blendTime);
            m_Animator.SetLookAtWeight(lookAtWeight, 0.2f, 0.5f, 0.7f, 0.5f);
            m_Animator.SetLookAtPosition(m_CurrentLookAtPosition);
        }

        /// <summary>
        /// Move the look at target to its default position and parent it to the actor.
        /// </summary>
        internal void ResetLookAt()
        {
            LookAtTarget.transform.SetParent(transform);
            LookAtTarget.transform.localPosition = head.position + new Vector3(0, 0, 1);
        }
    }
}
