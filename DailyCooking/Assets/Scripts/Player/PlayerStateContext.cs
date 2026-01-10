using System.Collections.Generic;
using UnityEngine;
public class PlayerStateContext 
{
    public float defaultDistanceTarget = 0.5f;
    private bool idDisableInput;
    private Animator characterAnimator;
    private bool isWalking;
    private GameInput gameInput;
    private Transform playerTransform;
    private LayerMask counterLayermask;
    private KitchenObject kitchenObject;
    private Transform _kitchenObjectHoldPoint;
    private IInteractable selectedInteractableObject;
    private Vector3 lastInteractDir;
    private List<IHighlightable> highlightable;
    public PlayerStateContext(Animator animator, 
        Transform playerTransform, LayerMask counterLayerMask, 
        Transform kitchenObjectHoldPoint)
    {
        characterAnimator = animator;
        gameInput = GameInput.Instance;
        this.playerTransform = playerTransform;
        counterLayermask = counterLayerMask;
        _kitchenObjectHoldPoint = kitchenObjectHoldPoint;
        idDisableInput = false;
        isWalking = false;
        highlightable = new List<IHighlightable>();
    }

    //Read only
    public Animator CharacterAnimator => characterAnimator;
    public GameInput PlayerGameInput => gameInput; 
    public Transform PlayerTransform => playerTransform;
    public LayerMask CounterLayerMask => counterLayermask;
    //Read and Write
    public bool IsDisableInput { get => idDisableInput; set => idDisableInput = value; }
    public bool IsWalking { get => isWalking; set => isWalking = value; }
    public KitchenObject KitchenObject { get => kitchenObject; set => kitchenObject = value; }
    public IInteractable SelectedInteractableObject { get => selectedInteractableObject; set => selectedInteractableObject = value; }
    public Vector3 LastInteractDir { get => lastInteractDir;  set=> lastInteractDir = value; }
    public List<IHighlightable> Highlightable { get=>highlightable; set=> highlightable = value; }
}
