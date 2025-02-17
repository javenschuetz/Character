﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WizardsCode.Stats;
using WizardsCode.Character.Stats;
using System;
using static WizardsCode.Character.StateSO;
using UnityEngine.Serialization;
using WizardsCode.Character.WorldState;

namespace WizardsCode.Character
{
    /// <summary>
    /// Marks an object as interactable so that a actors can find them. 
    /// Records the effects the interactable can have on an actor and
    /// the object itself when interacting.
    /// 
    /// A stats tracker required in an ancestor.
    /// </summary>
    public class Interactable : MonoBehaviour
    {
        [Header("Overview")]
        [SerializeField, TextArea, Tooltip("A description of this interactable action.")]
        string m_Description;
        [SerializeField, Tooltip("The type of this interactable, this is used for sorting and filtering world state. This should represent the primary purpose of this interactable, there may be other interactables on the same object and there may be additional effects from this interactable. However, the type represents the primary purpose.")]
        InteractableTypeSO m_Type;
        [SerializeField, Tooltip("The name of the interaction from the perspective of the actor interacting with this item.")]
        [FormerlySerializedAs("m_InteractionName")]
        string m_InteractionNameFromActorsPerspective = "";

        [Header("Character Settings")]
        [SerializeField, Tooltip("How many characters can interact using this influencer at any one time.")]
        int m_MaxInteractors = 1;
        [SerializeField, Tooltip("The set of character stats and the influence to apply to them when a character interacts with the object.")]
        internal StatInfluence[] m_CharacterInfluences;

        [SerializeField, Tooltip("If the actor stays within the trigger area can they get a new influencer after the duration + cooldown has expired?")]
        bool m_IsRepeating = false;

        [Header("Object Settings")]
        [SerializeField, Tooltip("When an actor has finished interacting with the object should the object be destroyed?")]
        bool m_DestroyOnUse = false;
        [SerializeField, Tooltip("The set of object stats and the influence to apply to them when a character interacts with the object.")]
        internal StatInfluence[] m_ObjectInfluences;
        [SerializeField, Tooltip("The time, in seconds, over which the influencer will be effective. The total change will occure over this time period. If duration is 0 then the total change is applied instantly")]
        float m_Duration = 3;
        [SerializeField, Tooltip("The cooldown time before a character can be influenced by this influencer again.")]
        float m_Cooldown = 30;

        List<StatsTracker> m_Reservations = new List<StatsTracker>();

        StatsTracker m_StatsTracker;
        private Dictionary<Brain, float> m_TimeOfLastInfluence = new Dictionary<Brain, float>();
        private List<StatsTracker> m_ActiveInteractors = new List<StatsTracker>();

        /// <summary>
        /// Get the StatInfluences that act upon a character interacting with this item.
        /// </summary>
        public StatInfluence[]  CharacterInfluences {
            get { return m_CharacterInfluences; }
        }

        public string DisplayName
        {
            //TODO allow the designer to customize this name
            get { return gameObject.name; }
        }

        /// <summary>
        /// The name of this interaction. Used as an ID for this interaction.
        /// </summary>
        public string InteractionName
        {
            get { return m_InteractionNameFromActorsPerspective; }
            set { m_InteractionNameFromActorsPerspective = value; }
        }

        /// <summary>
        /// Get the StatInfluences that act upon this object when a character interacts with this item.
        /// </summary>
        public StatInfluence[] ObjectInfluences
        {
            get { return m_ObjectInfluences; }
        }

        /// <summary>
        /// The time it takes, under normal circumstances, to interact with this thing.
        /// </summary>
        public float Duration {
            get
            {
                return m_Duration;
            }
        }

        public InteractableTypeSO Type 
        { 
            get { return m_Type; }
        }

        void Awake()
        {
            m_StatsTracker = GetComponentInParent<StatsTracker>();
            InteractableManager.Instance.Register(this);
        }

        /// <summary>
        /// Reserve this interactable for a given actor. This actor should
        /// be on their way to the interactable. Only a limited number of actors can reserve
        /// this interactable. Once a reservation is no longer needed then call to ClearReservation(brain).
        /// </summary>
        /// <param name="statsTracker">The actor who reseved this interactable.</param>
        /// <returns>True if the the reservation was succesful.</returns>
        internal bool ReserveFor(StatsTracker statsTracker)
        {
            if (m_Reservations.Count + m_ActiveInteractors.Count >= m_MaxInteractors)
            {
                return false;
            }

            m_Reservations.Add(statsTracker);
            return true;
        }

        /// <summary>
        /// Clears any reservation for this interactable by an actor using
        /// the ReservedFor(brain) method;
        /// </summary>
        internal void ClearReservation(Brain brain)
        {
            m_Reservations.Remove(brain);
        }

        /// <summary>
        /// Test to see if this interactable will affect a state in a way that
        /// is desired.
        /// </summary>
        /// <param name="stateImpact">The desired state impact</param>
        /// <returns>True if the desired impact will result from interaction, otherwise false.</returns>
        public bool HasInfluenceOn(DesiredStatImpact stateImpact) {
            for (int i = 0; i < CharacterInfluences.Length; i++)
            {
                if (CharacterInfluences[i].statTemplate.name == stateImpact.statTemplate.name)
                {
                    switch (stateImpact.objective)
                    {
                        case Objective.LessThan:
                            return CharacterInfluences[i].maxChange < 0;
                        case Objective.Approximately:
                            return Mathf.Approximately(CharacterInfluences[i].maxChange, 0);
                        case Objective.GreaterThan:
                            return CharacterInfluences[i].maxChange > 0;
                        default:
                            Debug.LogError("Don't know how to handle objective " + stateImpact.objective);
                            break;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Test to see if this influencer trigger is on cooldown for a given actor.
        /// </summary>
        /// <param name="brain">The brain of the actor we are testing against</param>
        /// <returns>True if this influencer is on cooldown, meaning the actor cannot use it yet.</returns>
        internal bool IsOnCooldownFor(Brain brain)
        {
            float lastTime;
            if (m_TimeOfLastInfluence.TryGetValue(brain, out lastTime))
            {
                return Time.timeSinceLevelLoad < lastTime + m_Cooldown;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Tests to see if the interactable has space in the reservation queue 
        /// for this actor.
        /// </summary>
        /// <param name="brain"></param>
        /// <returns></returns>
        public bool HasSpaceFor(Brain brain)
        {
            return m_MaxInteractors > m_ActiveInteractors.Count + m_Reservations.Count; 
        }

        internal bool HasRequiredObjectStats()
        {
            bool isValid = true;
            for (int i = 0; i < ObjectInfluences.Length; i++)
            {
                if (ObjectInfluences[i].maxChange < 0 
                    && m_StatsTracker.GetOrCreateStat(ObjectInfluences[i].statTemplate).Value < Math.Abs(ObjectInfluences[i].maxChange))
                {
                    isValid = false;
                    break;
                }
            }
            return isValid;
        }
        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject == this.gameObject) return;

            Brain brain = other.transform.root.GetComponentInChildren<Brain>();
            if (brain == null || !brain.ShouldInteractWith(this)) return;

            if (!HasSpaceFor(brain))
            {
                brain.TargetInteractable = null;
                return;
            }

            StartCharacterInteraction(brain);
            AddObjectInfluence();
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.gameObject == this.gameObject)
            {
                return;
            }

            Brain brain = other.transform.root.GetComponentInChildren<Brain>();
            if (brain == null
                || (!m_IsRepeating
                && m_ActiveInteractors.Contains(brain)))
            {
                return;
            }

            if (!brain.ShouldInteractWith(this))
            {
                return;
            }

            if (!HasSpaceFor(brain))
            {
                brain.TargetInteractable = null;
                return;
            }

            if (!IsOnCooldownFor(brain))
            {
                StartCharacterInteraction(brain);
                AddObjectInfluence();
            }
        }

        private void StartCharacterInteraction(Brain brain)
        {
            GenericInteractionAIBehaviour behaviour = (GenericInteractionAIBehaviour)brain.ActiveBlockingBehaviour;
            behaviour.StartBehaviour(this);

            for (int i = 0; i < CharacterInfluences.Length; i++)
            {
                StatInfluencerSO influencer = ScriptableObject.CreateInstance<StatInfluencerSO>();
                influencer.InteractionName = CharacterInfluences[i].statTemplate.name + " influencer from " + InteractionName + " (" + GetInstanceID() + ")";
                influencer.Trigger = this;
                influencer.stat = CharacterInfluences[i].statTemplate;
                influencer.maxChange = CharacterInfluences[i].maxChange;
                influencer.duration = m_Duration;
                influencer.CooldownDuration = m_Cooldown;

                if (brain.TryAddInfluencer(influencer))
                {
                    m_Reservations.Remove(brain);
                    m_ActiveInteractors.Add(brain);
                    m_TimeOfLastInfluence.Remove(brain);
                    m_TimeOfLastInfluence.Add(brain, Time.timeSinceLevelLoad);
                }
            }
        }

        internal void StopCharacterInteraction(StatsTracker statsTracker)
        {
            m_ActiveInteractors.Remove(statsTracker);
            if (m_DestroyOnUse)
            {
                Destroy(gameObject, 0.05f);
            }
        }

        private void AddObjectInfluence()
        {
            for (int i = 0; i < ObjectInfluences.Length; i++)
            {
                StatInfluencerSO influencer = ScriptableObject.CreateInstance<StatInfluencerSO>();
                influencer.InteractionName = ObjectInfluences[i].statTemplate.name + " influencer from " + InteractionName + " (" + GetInstanceID() + ")";
                influencer.Trigger = this;
                influencer.stat = ObjectInfluences[i].statTemplate;
                influencer.maxChange = ObjectInfluences[i].maxChange;
                influencer.duration = m_Duration;
                influencer.CooldownDuration = m_Cooldown;

                m_StatsTracker.TryAddInfluencer(influencer);
            }
        }
    }
}
