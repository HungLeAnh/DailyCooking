using UnityEngine;
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
    private BaseCounterController _selectedCounterController;
    private KitchenObject _kitchenObject;
    private Transform _kitchenObjectHoldPoint;
    private IInteractable _selectedInteactableObject;
    public PlayerStateContext(Animator animator, 
        float moveSpeed,
        Transform playerTransform, LayerMask counterLayerMask, 
        Transform kitchenObjectHoldPoint)
    {
        _characterAnimator = animator;
        _gameInput = GameInput.Instance;
        _movespeed = moveSpeed;
        _playerTransform = playerTransform;
        _counterLayermask = counterLayerMask;
        _kitchenObjectHoldPoint = kitchenObjectHoldPoint;
        _idDisableInput = false;
        _isWalking = false;
    }

    //Read only
    public Animator CharacterAnimator => _characterAnimator;
    public GameInput PlayerGameInput => _gameInput; 
    public Transform PlayerTransform => _playerTransform;
    public LayerMask CounterLayerMask => _counterLayermask;
    //Read and Write
    public bool IsDisableInput { get => _idDisableInput; set => _idDisableInput = value; }
    public bool IsWalking { get => _isWalking; set => _isWalking = value; }
    public BaseCounterController SelectedCounterController { get => _selectedCounterController; set => _selectedCounterController = value; }
    public KitchenObject KitchenObject { get => _kitchenObject; set => _kitchenObject = value; }
    public Vector3 LastInteractDir { get => _lastInteractDir; set => _lastInteractDir = value; }
    public IInteractable SelectedInteactableObject { get => _selectedInteactableObject; set => _selectedInteactableObject = value; }
}
