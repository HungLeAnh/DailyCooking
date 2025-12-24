using UnityEngine;
public class PlayerStateContext 
{
    public float defaultDistanceTarget = 0.5f;
    private bool _idDisableInput;
    private Animator _characterAnimator;
    private bool _isWalking;
    private GameInput _gameInput;
    private Transform _playerTransform;
    private LayerMask _counterLayermask;
    private KitchenObject _kitchenObject;
    private Transform _kitchenObjectHoldPoint;
    private IInteractable _selectedInteractableObject;
    private Vector3 _lastInteractDir;
    private IHighlightable _highlightable;
    public PlayerStateContext(Animator animator, 
        Transform playerTransform, LayerMask counterLayerMask, 
        Transform kitchenObjectHoldPoint)
    {
        _characterAnimator = animator;
        _gameInput = GameInput.Instance;
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
    public KitchenObject KitchenObject { get => _kitchenObject; set => _kitchenObject = value; }
    public IInteractable SelectedInteractableObject { get => _selectedInteractableObject; set => _selectedInteractableObject = value; }
    public Vector3 LastInteractDir { get => _lastInteractDir;  set=> _lastInteractDir = value; }
    public IHighlightable Highlightable { get=>_highlightable; set=> _highlightable = value; }
}
