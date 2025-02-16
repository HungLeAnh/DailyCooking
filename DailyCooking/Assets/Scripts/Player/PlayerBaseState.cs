using UnityEngine;

public abstract class PlayerBaseState : BaseState<PlayerStateMachine.EPlayerState>
{
    protected PlayerStateContext Context;
    public PlayerBaseState(PlayerStateContext context,PlayerStateMachine.EPlayerState stateKey) : base(stateKey)
    {
        Context = context;
    }    

    public virtual void ChangeAnimationState(string animationName)
    {
        Context.CharacterAnimator.CrossFade(animationName,0.1f,0,0.1f,0.1f);
    }
    public override void UpdateState()
    {
        if(!Context.IsDisableInput)
        {
            Update();
            HandleMovement();
            HandleInteractions();
        }
    }
    private void HandleMovement()
    {
        Vector2 inputVector = Context.PlayerGameInput.GetMovementVectorNormalized();

        //Debug.Log("input Vector: " + inputVector);
        Vector3 moveDir = new Vector3(inputVector.x, 0, inputVector.y);

        float moveDistance = Context.MoveSpeed * Time.deltaTime;
        float playerRadius = 0.7f;
        float playerHeight = 2f;
        bool canMove = !Physics.CapsuleCast(Context.PlayerTransform.position, Context.PlayerTransform.position + Vector3.up * playerHeight, playerRadius, moveDir, moveDistance);

        if (!canMove)
        {
            //try to move on X
            Vector3 moveDirX = new Vector3(moveDir.x, 0, 0).normalized;
            canMove = (moveDir.x < -.5f || moveDir.x > .5f) && !Physics.CapsuleCast(Context.PlayerTransform.position, Context.PlayerTransform.position + Vector3.up * playerHeight, playerRadius, moveDirX, moveDistance);
            if (canMove)
            {
                moveDir = moveDirX;

            }
            else
            {
                //can't move on X
                //try to move on Z
                Vector3 moveDirZ = new Vector3(0, 0, moveDir.z).normalized;
                canMove = (moveDir.z < -.5f || moveDir.z > .5f) && !Physics.CapsuleCast(Context.PlayerTransform.position, Context.PlayerTransform.position + Vector3.up * playerHeight, playerRadius, moveDirZ, moveDistance);
                if (canMove)
                {
                    //can move on Z
                    moveDir = moveDirZ;
                }
                else
                {
                    //can't move at all
                }
            }


        }

        if (canMove)
        {
            Context.PlayerTransform.position += moveDir * moveDistance;
        }


        Context.IsWalking = moveDir != Vector3.zero;
        
        float rotateSpeed = 10f;

        if (Context.IsWalking)
        {
            Context.PlayerTransform.forward = Vector3.Slerp(Context.PlayerTransform.forward, moveDir, rotateSpeed * Time.deltaTime);
            //SoundManager.Instance.PlayFootStepSound(Context.PlayerTransform.position, 1);
        }
    }
    private void HandleInteractions()
    {
        Vector2 inputVector = Context.PlayerGameInput.GetMovementVectorNormalized();


        Vector3 moveDir = new Vector3(inputVector.x, 0, inputVector.y);

        if (moveDir != Vector3.zero)
        {
            Context.LastInteractDir = moveDir;
        }

        float interactDistance = 2f;
        if (Physics.Raycast(Context.PlayerTransform.position, Context.LastInteractDir, out RaycastHit raycastHit, interactDistance, Context.CounterLayerMask))
        {
            if (raycastHit.transform.TryGetComponent(out BaseCounterView baseCounter))
            {
                if (baseCounter != Context.SelectedCounter)
                {
                    SetSelectedCounter(baseCounter);
                }
            }
            else
            {
                SetSelectedCounter(null);
            }
        }
        else
        {
            SetSelectedCounter(null);
        }

    }
    private void SetSelectedCounter(BaseCounterView selectedCounter)
    {
        Context.SelectedCounter = selectedCounter;
        PlayerStateMachine.Instance.FireOnSelectedCounterChanged(new PlayerStateMachine.OnSelectedCounterChangedEventArgs
        {

            selectedCounterView = selectedCounter != null ? selectedCounter : null

        });
    }

}
