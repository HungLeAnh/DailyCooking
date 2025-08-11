using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
public class PlayerStateContext 
{
    public float defaultDistanceTarget = 0.5f;
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
    private Vector3 endPosition;
    private List<int2> _pathList;
    private int _wayPointIndex;
    private bool isTouching;
    private bool isReachedDestination;
    public PlayerStateContext(Animator animator, 
        float moveSpeed,
        Transform playerTransform, LayerMask counterLayerMask, Transform kitchenObjectHoldPoint)
    {
        _characterAnimator = animator;
        _gameInput = GameInput.Instance;
        _movespeed = moveSpeed;
        _playerTransform = playerTransform;
        _counterLayermask = counterLayerMask;
        _kitchenObjectHoldPoint = kitchenObjectHoldPoint;
        _idDisableInput = false;
        _pathList = new List<int2>();
        _isWalking = false;
        _wayPointIndex = 0;
    }

    //Read only
    public Animator CharacterAnimator => _characterAnimator;
    public GameInput PlayerGameInput => _gameInput; 
    public float MoveSpeed { get => _movespeed; set => _movespeed = value; }
    public Transform PlayerTransform => _playerTransform;
    public LayerMask CounterLayerMask => _counterLayermask;
    //Read and Write
    public bool IsDisableInput { get => _idDisableInput; set => _idDisableInput = value; }
    public bool IsWalking { get => _isWalking; set => _isWalking = value; }
    public Vector3 LastInteractDir { get => _lastInteractDir; set => _lastInteractDir = value; }
    public BaseCounterView SelectedCounter { get => _selectedCounter; set => _selectedCounter = value; }
    public KitchenObject KitchenObject { get => _kitchenObject; set => _kitchenObject = value; }
    public Vector3 EndPosition { get => endPosition; set => endPosition = value; }
    public List<int2> PathList { get => _pathList; set => _pathList = value; }
    public int WayPointIndex { get => _wayPointIndex; set => _wayPointIndex = value; }
    public bool IsTouching { get => isTouching; set => isTouching = value; }
    public bool IsReachedDestination { get => isReachedDestination; set => isReachedDestination = value; }
}
