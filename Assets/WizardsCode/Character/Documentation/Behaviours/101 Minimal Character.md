The 101 scene shows the absolute minimal setup for a Wizards Code character. also known as an Actor.  This actor will not exhibit any behaviours. They are a blank slare for your creation of an AI.


# Required Components

## Actor Controller

This is resonsible for controlling the movement and animation of the character. It requires a NavMeshAgent component be attached.

## Brain

The brain tracks all the characters stats and makes decisions about what the charcter will do.

## Animator

This is a standard Unity Animtor component. You can use any animation controller you want as long as it uses the parameters setup in the Actor Controller. If you want to get going quickly you can use the Basic Humanoid Controller that is part of the Wizards Code Animation pack (also open source).

# NavMeshAgent

A standard Unity NavMeshAgent used by the Actor Controller to move the actor.

# Capsule Collider

Required by NavMeshAgent

# Rigidbody

Required to ensure colliders and triggers are effective in the scene.

# Audio Source

A standard unity Audio Source allowing the Actor to make sound.

# Optional Components

## DebugInfo

This is a useful component when in development. If this component is attached to your character an gizmo containing debug info will be shown.

