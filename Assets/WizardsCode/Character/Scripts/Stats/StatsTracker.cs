﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using WizardsCode.Character;
using WizardsCode.Character.Stats;
using static WizardsCode.Character.StateSO;

namespace WizardsCode.Stats {
    /// <summary>
    /// The Brain is responsible for tracking the stats and goal states of the character and
    /// making decisions and plans to reach those goal stats.
    /// </summary>
    public class StatsTracker : MonoBehaviour
#if UNITY_EDITOR
        , IDebug
#endif
    {
        [SerializeField, Tooltip("Name of the owner of this stats tracker. If null a name will be generated.")]
        string m_DisplayName;
        [SerializeField, Tooltip("Desired States are the states that the actor would like to satisfy. These are, essentially, the things that drive the actor.")]
        StateSO[] m_DesiredStates = default;

        [Header("Optimization")]
        [SerializeField, Tooltip("How often stats should be processed for changes.")]
        float m_TimeBetweenUpdates = 0.5f;

        [HideInInspector, SerializeField]
        internal List<StatSO> m_Stats = new List<StatSO>();
        [HideInInspector, SerializeField]
        internal List<StatInfluencerSO> m_StatsInfluencers = new List<StatInfluencerSO>();

        float m_TimeOfLastUpdate = 0;
        internal float m_TimeOfNextUpdate = 0;

        internal List<StatInfluencerSO> StatsInfluencers
        {
            get { return m_StatsInfluencers; }
        }

        public string DisplayName
        {
            get
            {
                if (string.IsNullOrEmpty(m_DisplayName))
                {
                    m_DisplayName = GenerateName();
                }
                return m_DisplayName;
            }
        }

        /// <summary>
        /// Desired States are the states that the actor would like to satisfy.
        /// These are, essentially, the things that drive the actor.
        /// </summary>
        public StateSO[] DesiredStates { get { return m_DesiredStates; } }

        public StateSO[] UnsatisfiedDesiredStates { get; internal set; }
        
        internal virtual void Update()
        {
            if (Time.timeSinceLevelLoad < m_TimeOfNextUpdate) return;

            UpdateAllStats();
            ApplyStatInfluencerEffects();
            UpdateUnsatisfiedStates();

            m_TimeOfLastUpdate = Time.timeSinceLevelLoad;
            m_TimeOfNextUpdate = Time.timeSinceLevelLoad + m_TimeBetweenUpdates;
        }

        internal void ApplyStatInfluencerEffects()
        {
            for (int i = StatsInfluencers.Count - 1; i >= 0; i--)
            {
                if (StatsInfluencers[i] != null)
                {
                    StatsInfluencers[i].ChangeStat(this);

                    if (Mathf.Abs(StatsInfluencers[i].influenceApplied) >= Mathf.Abs(StatsInfluencers[i].maxChange))
                    {
                        if (StatsInfluencers[i].Trigger)
                        {
                            StatsInfluencers[i].Trigger.StopCharacterInteraction(this);
                        }
                        StatsInfluencers.RemoveAt(i);
                    }
                }
                else
                {
                    StatsInfluencers.RemoveAt(i);
                }
            }
        }

        internal void UpdateAllStats()
        {
            for (int i = 0; i < m_Stats.Count; i++)
            {
                m_Stats[i].OnUpdate();
            }
        }


        /// <summary>
        /// Iterates over all the desired stated and checks to see if they are currently satsified.
        /// Unsatisfied states are caches in `UnsatisfiedStates`.
        /// </summary>
        private void UpdateUnsatisfiedStates()
        {
            List<StateSO> states = new List<StateSO>();
            for (int i = 0; i < DesiredStates.Length; i++)
            {
                if (!DesiredStates[i].IsSatisfiedFor(this))
                {
                    states.Add(DesiredStates[i]);
                }
            }
            UnsatisfiedDesiredStates = states.ToArray();
        }

        /// <summary>
        /// Get a list of stats that are currently outside the desired state for that stat.
        /// This can be used, for example. by AI deciding what action to take next.
        /// </summary>
        /// <returns>A list of stats that are not in a desired state.</returns>
        [Obsolete("This method needs to be replaced with one that identifies whether the stat is satisfied or needs to increase or decrease. Or perhaps it is not needed at all since it is currently only used in WanderWithIntent. Maybe that behaviour should look for places the brain has identified for it.")]
        public StatSO[] GetStatsNotInDesiredState()
        {
            List<StatSO> stats = new List<StatSO>();
            for (int i = 0; i < UnsatisfiedDesiredStates.Length; i++)
            {
                stats.AddRange(GetStatsDesiredForState(UnsatisfiedDesiredStates[i]));
            }
            return stats.ToArray();
        }

        private List<StatSO> GetStatsDesiredForState(StateSO state)
        {
            List<StatSO> stats = new List<StatSO>();
            if (state.statTemplate != null)
            {
                StatSO stat = GetOrCreateStat(state.statTemplate);
                if (GetGoalFor(state.statTemplate) != StateSO.Goal.NoAction)
                {
                    stats.Add(stat);
                }
            }

            for (int idx = 1; idx < state.SubStates.Length; idx++)
            {
                stats.AddRange(GetStatsDesiredForState(state.SubStates[idx]));
            }
            return stats;
        }

        /// <summary>
        /// Get the Stat of a given type.
        /// </summary>
        /// <param name="name">The name of the stat we want to retrieve</param>
        /// <returns>The stat, if it exists, or null.</returns>
        public StatSO GetStat(StatSO template)
        {
            for (int i = 0; i < m_Stats.Count; i++)
            {
                if (template != null)
                {
                    if (m_Stats[i].name == template.name)
                    {
                        return m_Stats[i];
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Get the stat object representing a named stat. If it does not already
        /// exist it will be created with a base value.
        /// </summary>
        /// <param name="name">Tha name of the stat to Get or Create for this controller</param>
        /// <returns>A StatSO representing the named stat</returns>
        public StatSO GetOrCreateStat(StatSO template, float? value = null)
        {
            StatSO stat = GetStat(template);
            if (stat != null) return stat;

            stat = Instantiate(template);
            stat.name = template.name;
            if (value != null)
            {
                stat.NormalizedValue = (float)value;
            }

            m_Stats.Add(stat);
            return stat;
        }

        /// <summary>
        /// Add an influencer to this controller. If this controller is not managing the required stat then 
        /// do nothing.
        /// </summary>
        /// <param name="influencer">The influencer to add.</param>
        /// <returns>True if the influencer was added, otherwise false.</returns>
        public virtual bool TryAddInfluencer(StatInfluencerSO influencer)
        {
            StatsInfluencers.Add(influencer);

            return true;
        }

        internal List<StateSO> GetDesiredStatesFor(StatSO stat)
        {
            List<StateSO> states = new List<StateSO>();

            for (int i = 0; i < DesiredStates.Length; i++)
            {
                if (DesiredStates[i].statTemplate != null && stat.name == DesiredStates[i].statTemplate.name)
                {
                    states.Add(DesiredStates[i]);
                    break;
                }
            }

            return states;
        }

        /// <summary>
        /// Get the current goal for a given stat. That is do we currently want to 
        /// increase, decrease or maintaint his stat.
        /// If there are multiple desired states then an attempt is made to create 
        /// a meaningful goal. For example, if there are multiple greater than goals
        /// then the target will be the highest goal. 
        /// 
        /// If there are conflicting goals,
        /// such as a greater than and a less than then lessThan will take preference
        /// over greaterThan, but approximately will always be given prefernce.
        /// </summary>
        /// <returns>The current goal for the stat.</returns>
        public Goal GetGoalFor(StatSO stat)
        {
            float lessThan = float.MaxValue;
            float greaterThan = float.MinValue;

            List<StateSO> states = GetDesiredStatesFor(stat);
            for (int i = 0; i < states.Count; i++)
            {
                switch (states[i].objective)
                {
                    case Objective.LessThan:
                        if (stat.NormalizedValue >= states[i].normalizedTargetValue && states[i].normalizedTargetValue < lessThan)
                        {
                            lessThan = states[i].normalizedTargetValue;
                        }
                        break;

                    case Objective.Approximately:
                        if (Mathf.Approximately(stat.NormalizedValue, states[i].normalizedTargetValue)) {
                            if (stat.NormalizedValue > states[i].normalizedTargetValue)
                            {
                                return StateSO.Goal.Decrease;
                            } else
                            {
                                return StateSO.Goal.Increase;
                            }
                        }
                        break;

                    case Objective.GreaterThan:
                        if (stat.NormalizedValue <= states[i].normalizedTargetValue && states[i].normalizedTargetValue > greaterThan)
                        {
                            greaterThan = states[i].normalizedTargetValue;
                        }
                        break;
                }
            }

            if (lessThan != float.MaxValue) return StateSO.Goal.Decrease;
            if (greaterThan != float.MinValue) return StateSO.Goal.Increase;

            return StateSO.Goal.NoAction;
        }

        /// <summary>
        /// Generate a name for the owner of this stats tracker. This
        /// can be overridden for different kinds of stats trackers.
        /// By default it returns the name of the game object this
        /// component is attached to.
        /// </summary>
        /// <returns>A name for this stats tracker object to be used in the UI.</returns>
        internal virtual string GenerateName()
        {
            return gameObject.name;
        }

#if UNITY_EDITOR
        string IDebug.StatusText()
        {
            string msg = DisplayName;
            msg += "\n\nStats";
            for (int i = 0; i < m_Stats.Count; i++)
            {
                msg += "\n" + m_Stats[i].statusDescription;
            }

            msg += GetActiveInfluencersDescription();

            msg += "\n\nUnsatisfied Desired States";
            if (UnsatisfiedDesiredStates.Length == 0) msg += "\nNone";
            for (int i = 0; i < UnsatisfiedDesiredStates.Length; i++)
            {
                StatSO stat = GetOrCreateStat(UnsatisfiedDesiredStates[i].statTemplate);
                msg += "\nIs not ";
                msg += UnsatisfiedDesiredStates[i].name + " ";
                msg += " (" + stat.name + " should be " + UnsatisfiedDesiredStates[i].objective + " " + UnsatisfiedDesiredStates[i].normalizedTargetValue + ")";
            }

            return msg;
        }

        internal string GetActiveInfluencersDescription()
        {
            StringBuilder msg = new StringBuilder("\n\nActive Influencers\n");
            if (StatsInfluencers.Count == 0) msg.AppendLine("None");
            for (int i = 0; i < StatsInfluencers.Count; i++)
            {
                msg.Append(StatsInfluencers[i].InteractionName);
                msg.Append(" at ");
                msg.Append(StatsInfluencers[i].GeneratorName);
                msg.AppendLine();
                msg.Append("\t - ");
                msg.Append(StatsInfluencers[i].stat.name);
                msg.Append(" changed by ");
                msg.Append(StatsInfluencers[i].maxChange);
                msg.Append(" at ");
                msg.Append(StatsInfluencers[i].changePerSecond);
                msg.Append(" per second for ");
                msg.Append(StatsInfluencers[i].duration);
                msg.Append(" seconds (");
                msg.Append(Mathf.Round((StatsInfluencers[i].influenceApplied / StatsInfluencers[i].maxChange) * 100));
                msg.AppendLine("% applied)");
            }

            return msg.ToString();
        }
#endif
    }
}
