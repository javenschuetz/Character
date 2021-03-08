using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Ink.Runtime;
using TMPro;
using UnityEngine.UI;
using WizardsCode.Character;
using WizardsCode.Utility;
using System.Text;
using System;
using static WizardsCode.Character.EmotionalState;

namespace WizardsCode.Ink
{
    public class InkManager : AbstractSingleton<InkManager>
    {
        enum Direction { Cue, TurnToFace, PlayerControl, MoveTo, SetEmotion }

        [Header("Script")]
        [SerializeField, Tooltip("The Ink file to work with.")]
        TextAsset m_InkJSON;
        [SerializeField, Tooltip("The actors that are available in this scene.")]
        ActorController[] m_Actors;
        [SerializeField, Tooltip("The cues that will be used in this scene.")]
        ActorCue[] m_Cues;
        [SerializeField, Tooltip("Should the story start as soon as the game starts. If this is set to false the story will not start until a trigger or similar is set.")]
        bool m_PlayOnAwake = true;

        [Header("UI")]
        [SerializeField, Tooltip("The panel on which to display the text in the story.")]
        RectTransform textPanel;
        [SerializeField, Tooltip("The panel on which to display the choice buttons in the story.")]
        RectTransform choicesPanel;
        [SerializeField, Tooltip("Story chunk prefab for creation when we want to display a story chunk.")]
        TextMeshProUGUI m_StoryChunkPrefab;
        [SerializeField, Tooltip("Story choice button")]
        Button m_ChoiceButtonPrefab;

        Story m_Story;
        bool m_IsUIDirty = false;
        StringBuilder m_NewStoryText = new StringBuilder();

        private bool m_IsDisplayingUI = false;
        internal bool IsDisplayingUI
        {
            get { return m_IsDisplayingUI; } 
            set { 
                m_IsDisplayingUI = value;
                m_IsUIDirty = value;
            }
        }

        private void Awake()
        {
            m_Story = new Story(m_InkJSON.text);
            IsDisplayingUI = m_PlayOnAwake;
        }

        /// <summary>
        /// Jump to a specific point in the story.
        /// </summary>
        /// <param name="knot">The name of the knot to jump to.</param>
        /// <param name="stitch">The name of the stitch within the named not.</param>
        internal void JumpToPath(string knot, string stitch = "")
        {
            if (!string.IsNullOrEmpty(stitch))
            {
                m_Story.ChoosePathString(knot + "." + stitch);
            }
            else
            {
                m_Story.ChoosePathString(knot);
            }
        }

        public void ChoosePath(string knotName, string stitchName = null)
        {
            string path = knotName;
            if (!string.IsNullOrEmpty(stitchName))
            {
                path = string.IsNullOrEmpty(path) ? stitchName : "." + stitchName;
            }

            if (!string.IsNullOrEmpty(path))
            {
                m_Story.ChoosePathString(path);
            }
        }

        public void Update()
        {
            if (IsDisplayingUI)
            {
                if (m_IsUIDirty)
                {
                    ProcessStoryChunk();
                    UpdateGUI();
                }
            } else
            {
                textPanel.gameObject.SetActive(false);
                choicesPanel.gameObject.SetActive(false);
            }
        }

        private void EraseUI()
        {
            for (int i = 0; i < textPanel.transform.childCount; i++)
            {
                Destroy(textPanel.transform.GetChild(i).gameObject);
            }

            for (int i = 0; i < choicesPanel.transform.childCount; i++) {
                Destroy(choicesPanel.transform.GetChild(i).gameObject);
            }
        }

        private void UpdateGUI()
        {
            EraseUI();

            textPanel.gameObject.SetActive(true);
            TextMeshProUGUI chunkText = Instantiate(m_StoryChunkPrefab) as TextMeshProUGUI;
            chunkText.text = m_NewStoryText.ToString();
            chunkText.transform.SetParent(textPanel.transform, false);

            for (int i = 0; i < m_Story.currentChoices.Count; i++)
            {
                choicesPanel.gameObject.SetActive(true);
                Choice choice = m_Story.currentChoices[i];
                Button choiceButton = Instantiate(m_ChoiceButtonPrefab) as Button;
                TextMeshProUGUI choiceText = choiceButton.GetComponentInChildren<TextMeshProUGUI>();
                choiceText.text = m_Story.currentChoices[i].text;
                choiceButton.transform.SetParent(choicesPanel.transform, false);

                choiceButton.onClick.AddListener(delegate
                {
                    ChooseStoryChoice(choice);
                });
            }

            m_IsUIDirty = false;
        }

        /// <summary>
        /// Called whenever the story needs to progress.
        /// </summary>
        /// <param name="choice">The choice made to progress the story.</param>
        void ChooseStoryChoice(Choice choice)
        {
            m_Story.ChooseChoiceIndex(choice.index);
            m_IsUIDirty = true;
        }

        void PromptCue(string[]args)
        {
            if (!ValidateArgumentCount(args, 2))
            {
                return;
            }

            ActorController actor = FindActor(args[0].Trim());
            ActorCue cue = FindCue(args[1].Trim());

            if (actor != null)
            {
                actor.Prompt(cue);
            }
        }

        /// <summary>
        /// The MoveTo direction instructs an actor to move to a specific location. It is up to the ActorController
        /// to decide how they should move.
        /// </summary>
        /// <param name="args"></param>
        void MoveTo(string[] args)
        {
            if (!ValidateArgumentCount(args, 2))
            {
                return;
            }

            ActorController actor = FindActor(args[0].Trim());
            Transform target = FindTarget(args[1].Trim());

            if (actor != null)
            {
                actor.MoveTo(target);
            }
        }

        /// <summary>
        /// The SetEmotion direction looks for a defined emotion on an character and sets it if found.
        /// 
        /// </summary>
        /// <param name="args">[ActorName], [EmotionName], [Float]</param>
        void SetEmotion(string[] args)
        {
            if (!ValidateArgumentCount(args, 3))
            {
                return;
            }

            ActorController actor = FindActor(args[0].Trim(), false);
            if (actor)
            {
                EmotionalState emotions = FindEmotionalState(actor);
                EmotionType emotion = (EmotionType)Enum.Parse(typeof(EmotionType), args[1].Trim());
                float value = float.Parse(args[2].Trim());

                if (actor != null)
                {
                    emotions.SetEmotionValue(emotion, value);
                }
            } else
            {
                Debug.LogError("There is a direction to set the value of the emotion " + args[1].Trim() + "  " + actor + " but there is no EmotionalState component on that actor.");
            }
        }

        void TurnToFace(string[] args)
        {
            if (!ValidateArgumentCount(args, 2))
            {
                return;
            }

            ActorController actor = FindActor(args[0].Trim());
            Transform target = FindTarget(args[1].Trim());

            if (target != null)
            {
                actor.LookAtTarget = target;
            }
        }

        EmotionalState FindEmotionalState(ActorController actor)
        {
            EmotionalState emotions = actor.GetComponent<EmotionalState>();
            if (!emotions)
            {
                Debug.LogError("There is a direction to set an emotion value on " + actor + " but there is no EmotionalState component on that actor.");
            }
            return emotions;
        }

        Transform FindTarget(string objectName)
        {
            ActorController actor = FindActor(objectName, false);
            if (actor != null)
            {
                return actor.transform.root;
            }

            //TODO Don't use Find at runtime. When initiating the InkManager we should pre-emptively parse all directions and cache the results - or perhaps (since the story may be larger or dynamic) we should do it in a CoRoutine just ahead of execution of the story chunk
            GameObject go = GameObject.Find(objectName);
            if (go)
            {
                return go.transform;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Look through the known actors to see if we have one with the given name.
        /// </summary>
        /// <param name="actorName">The name of the actor we want.</param>
        /// <param name="logError">If true (the default) an error will be logged to the console if the actor is not found.</param>
        /// <returns>The actor with the given name or null if they cannot be found.</returns>
        private ActorController FindActor(string actorName, bool logError = true)
        {
            ActorController actor = null;
            for (int i = 0; i < m_Actors.Length; i++)
            {
                if (m_Actors[i].name == actorName.Trim())
                {
                    actor = m_Actors[i];
                    break;
                }
            }

            if (logError && actor == null)
            {
                Debug.LogError("Script contains a direction for " + actorName + ". However, the actor cannot be found.");
            }

            return actor;
        }

        private ActorCue FindCue(string cueName)
        {
            ActorCue cue = null;
            for (int i = 0; i < m_Cues.Length; i++)
            {
                if (m_Cues[i].name == cueName)
                {
                    cue = m_Cues[i];
                    break;
                }
            }

            if (cue == null)
            {
                Debug.LogError("Script contains a Cue direction but the cue called `" + cueName + "` cannot be found.");
            }

            return cue;
        }

        /// <summary>
        /// Grab the current story chunk and parse it for processing.
        /// </summary>
        void ProcessStoryChunk()
        {
            string line;

            while (m_Story.canContinue)
            {
                m_NewStoryText.Clear();
                line = m_Story.Continue();

                // Process Directions;
                int cmdIdx = line.IndexOf(">>>");
                if (cmdIdx >= 0)
                {
                    int startIdx = line.IndexOf(' ', cmdIdx);
                    int endIdx = line.IndexOf(':') - startIdx;
                    Enum.TryParse(line.Substring(startIdx, endIdx).Trim(), out Direction cmd);
                    string[] args = line.Substring(endIdx + startIdx + 1).Split(',');

                    switch (cmd) {
                        case Direction.Cue:
                            PromptCue(args);
                            break;
                        case Direction.TurnToFace:
                            TurnToFace(args);
                            break;
                        case Direction.PlayerControl:
                            SetPlayerControl(args);
                            string resp = m_Story.ContinueMaximally();
                            break;
                        case Direction.MoveTo:
                            MoveTo(args);
                            break;
                        case Direction.SetEmotion:
                            SetEmotion(args);
                            break;
                        default:
                            Debug.LogError("Unknown Direction: " + line);
                            break;
                    }
                } else
                {
                    m_NewStoryText.AppendLine(line);
                }

                // Process Tags
                List<string> tags = m_Story.currentTags;
                for (int i = 0; i < tags.Count; i++)
                {
                }
            }

            m_IsUIDirty = true;
        }

        void SetPlayerControl(string[] args)
        {
            ValidateArgumentCount(args, 1);

            if (args[0].Trim().ToLower() == "on")
            {
                SetPlayerControl(true);
                //TODO At present there we need to set a DONE divert in the story which is less than ideal since it means the writers can't use the Inky test tools: asked for guidance at https://discordapp.com/channels/329929050866843648/329929390358265857/818370835177275392
                
            }
            else
            {
                SetPlayerControl(false);
            }
        }

        internal void SetPlayerControl(bool value)
        {
            IsDisplayingUI = !value;
        }

        bool ValidateArgumentCount(string[] args, int requiredCount)
        {
            if (args.Length < requiredCount)
            {
                Debug.LogError("Direction has too few arguments. There should be " + requiredCount + ". Ignoring direction.");
                return false;
            }
            else if (args.Length > requiredCount)
            {
                Debug.LogWarning("Direction has too few arguments. There should be " + requiredCount + ". Ignoring the additional arguments.");
                return true;
            }

            return true;
        }
    }
}
