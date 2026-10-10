using System;
using NUnit.Framework;
using UnityEngine;

public class BotAnimationTests
{
    [Test] public void StateHash_MatchesAnimatorStringHashForEveryState()
    {
        foreach (BotAnimation.State state in Enum.GetValues(typeof(BotAnimation.State)))
        {
            Assert.AreEqual(Animator.StringToHash(state.ToString()), BotAnimation.StateHash(state));
        }
    }

    [Test] public void StateHash_IsStableAcrossRepeatedCalls()
    {
        foreach (BotAnimation.State state in Enum.GetValues(typeof(BotAnimation.State)))
        {
            Assert.AreEqual(BotAnimation.StateHash(state), BotAnimation.StateHash(state));
        }
    }

    [Test] public void StateHash_DiffersBetweenStates()
    {
        Assert.AreNotEqual(
            BotAnimation.StateHash(BotAnimation.State.Idle),
            BotAnimation.StateHash(BotAnimation.State.Walking));
        Assert.AreNotEqual(
            BotAnimation.StateHash(BotAnimation.State.Talk),
            BotAnimation.StateHash(BotAnimation.State.Eat));
    }
}
