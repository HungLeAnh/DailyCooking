using System.Collections.Generic;
using UnityEngine;

public class BotAnimation
{
    public enum State
    {
        Idle,
        Walking,
        Talk,
        Eat
    }

    private static readonly Dictionary<State, int> stateHashes = new Dictionary<State, int>();

    // Animator.Play(string) allocates a string and re-hashes it every call.
    public static int StateHash(State state)
    {
        if (!stateHashes.TryGetValue(state, out int hash))
        {
            hash = Animator.StringToHash(state.ToString());
            stateHashes[state] = hash;
        }
        return hash;
    }
}
