using System;
using UnityEngine;
public class StoveCounterView : BaseCounterView
{
    [SerializeField] private CookingTool _cookingTool;
    [SerializeField] private AudioSource _audioSource;

    public CookingTool CookingTool { get => _cookingTool; set => _cookingTool = value; }
    public AudioSource AudioSource { get => _audioSource; set => _audioSource = value; }
}