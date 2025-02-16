using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
public class PlayerStateContext 
{
    private bool _idDisableInput;
    private Animator _characterAnimator;
    private float _movespeed;
    private bool _isWalking;
    private Vector3 _lastInteractDir;
    private GameInput _gameInput;
    private Transform _playerTransform;
    private LayerMask _counterLayermask;
    private BaseCounterView _selectedCounter;
    private KitchenObject _kitchenObject;
    private Transform _kitchenObjectHoldPoint;

    public PlayerStateContext(Animator animator, GameInput gameInput,
        float moveSpeed,Transform playerTransform, LayerMask counterLayerMask, Transform kitchenObjectHoldPoint)
    {
        _characterAnimator = animator;
        _gameInput = gameInput;
        _movespeed = moveSpeed;
        _playerTransform = playerTransform;
        _counterLayermask = counterLayerMask;
        _kitchenObjectHoldPoint = kitchenObjectHoldPoint;
        _idDisableInput = false;
    }

    //Read only
    public Animator CharacterAnimator => _characterAnimator;
    public GameInput PlayerGameInput => _gameInput; 
    public float MoveSpeed => _movespeed;
    public Transform PlayerTransform => _playerTransform;
    public LayerMask CounterLayerMask => _counterLayermask;
    //Read and Write
    public bool IsDisableInput { get => _idDisableInput; set => _idDisableInput = value; }
    public bool IsWalking { get => _isWalking; set => _isWalking = value; }
    public Vector3 LastInteractDir { get => _lastInteractDir; set => _lastInteractDir = value; }
    public BaseCounterView SelectedCounter { get => _selectedCounter; set => _selectedCounter = value; }
    public KitchenObject KitchenObject { get => _kitchenObject; set => _kitchenObject = value; }
}
