// This is an Ink Version of Aesops original fable "The Fox and the Goat"
// This fable is famous for it's moral "Look before you leap"
//
// This and other Aesops Fables can be retrieved, as Public Domain files in the USA
// See http://www.gutenberg.org/ebooks/21
//
// Here is the original text from Project Gutenberg
//
// The Fox and the Goat

// A FOX one day fell into a deep well and could find no means of escape. A Goat, overcome with thirst, came to the same well, and seeing the Fox, inquired if the water was good. Concealing his sad plight under a merry guise, the Fox indulged in a lavish praise of the water, saying it was excellent beyond measure, and encouraging him to descend. The Goat, mindful only of his thirst, thoughtlessly jumped down, but just as he drank, the Fox informed him of the difficulty they were both in and suggested a scheme for their common escape. “If,” said he, “you will place your forefeet upon the wall and bend your head, I will run up your back and escape, and will help you out afterwards.” The Goat readily assented and the Fox leaped upon his back. Steadying himself with the Goat’s horns, he safely reached the mouth of the well and made off as fast as he could. When the Goat upbraided him for breaking his promise, he turned around and cried out, “You foolish old fellow! If you had as many brains in your head as you have hairs in your beard, you would never have gone down before you had inspected the way up, nor have exposed yourself to dangers from which you had no means of escape.”

// Look before you leap.

-> Top_Knot

== Top_Knot

    The Fox and the Goat

    >>> MoveTo: Fox, Well_Edge_Mark

    * [Continue] -> Fox_Fell_In_Well

= Fox_Fell_In_Well

    A Fox one day fell into a deep well and could find no means of escape. 

    >>> MoveTo: Fox, Fox_In_Well_Mark
    >>> SetEmotion: Fox, Pleasure, 0
    >>> SetEmotion: Fox, Sadness, 1
    >>> Camera: LookInTheWellVirtualCamera
    >>> AnimationParam: Fox, Emote
    
    * [Continue] -> A_Thirsty_Goat

= A_Thirsty_Goat

    >>> MoveTo: Goat, Well_Edge_Mark
    
    A Goat, overcome with thirst, came to the same well, 
    
    >>> Camera: MainVirtualCamera
    >>> WaitFor: Goat, ReachedTarget
    
    >>> Camera: LookInTheWellVirtualCamera
    >>> TurnToFace: Goat, Fox
    >>> TurnToFace: Fox, Goat
    
    and seeing the Fox, inquired if the water was good. 
    
    
    * [Continue] -> The_Sly_Fox

= The_Sly_Fox

    >>> SetEmotion: Fox, Pleasure, 1
    >>> SetEmotion: Fox, Sadness, 0
    >>> SetEmotion: Fox, Interest, 1
    >>> AnimationParam: Fox, Emote
    
    Concealing his sad plight under a merry guise, the Fox indulged in a lavish praise of the water, saying it was excellent beyond measure, and encouraging him to descend.
    
    * [Continue] -> The_Foolish_Goat

= The_Foolish_Goat

    >>> MoveTo: Goat, Goat_In_Well_Mark

    The Goat, mindful only of his thirst, thoughtlessly jumped down, but just as he drank, the Fox informed him of the difficulty they were both in and suggested a scheme for their common escape. 
    
    * [Continue] -> The_Plan
    
= The_Plan

    >>> SetEmotion: Goat, Pleasure, 0
    >>> SetEmotion: Goat, Sadness, 0.8

    “If,” said he, “you will place your forefeet upon the wall and bend your head, I will run up your back and escape, and will help you out afterwards.” 

    * [Continue] -> The_Fox_Escape

= The_Fox_Escape

    >>> Camera: FoxExitCamera
    >>> TurnToFace: Goat, Nothing
    >>> TurnToFace: Fox, Nothing
    >>> SetEmotion: Fox, Pleasure, 1
    >>> MoveTo: Fox, Well_Edge_Mark
    
    The Goat readily assented and the Fox leaped upon his back. Steadying himself with the Goat’s horns, he safely reached the mouth of the well and made off as fast as he could. 
    
    >>> AnimationParam: Fox, Emote
    
    * [Continue] -> The_Mean_Fox
    
= The_Mean_Fox

    >>> MoveTo: Fox, Fox_Gloat_Mark
    >>> WaitFor: Fox, ReachedTarget
    >>> TurnToFace: Fox, Goat

    When the Goat upbraided him for breaking his promise, he turned around and cried out, “You foolish old fellow! If you had as many brains in your head as you have hairs in your beard, you would never have gone down before you had inspected the way up, nor have exposed yourself to dangers from which you had no means of escape.”
    
    >>> MoveTo: Fox, Fox_Gloat_Mark
    >>> WaitFor: Fox, ReachedTarget
    
    >>> Camera: LookInTheWellVirtualCamera
    >>> SetEmotion: Goat, Pleasure, 0
    >>> SetEmotion: Goat, Sadness, 1
    >>> SetEmotion: Goat, Interest, 0
    >>> AnimationParam: Goat, Emote

    * [Continue] -> The_Moral
    
= The_Moral

    >>> MoveTo: Fox, Fox_Exit_Mark

    Look before you leap.

-> END