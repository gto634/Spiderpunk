using UnityEngine;
using NaughtyAttributes;

public class MovementPlayer : MonoBehaviour
{
    [SerializeField] MoveSystem moveSystem;
    [SerializeField] float walkSpeed;
    [SerializeField] float runSpeed;
    [SerializeField, ReadOnly] bool isMoving;
    [SerializeField, ReadOnly] bool isRunning = false;
    
    public Vector3 Direction
    {
        get => moveSystem.Direction;
        set
        {
            moveSystem.Direction = value;  
            
            if (value ==  Vector3.zero) IsMoving = false;
            else                        IsMoving = true;
        } 
            
    }

    public bool IsMoving
    {
        get => isMoving;
        set => isMoving = value;
    }

    public float WalkSpeed => walkSpeed;
    public float RunSpeed => runSpeed;
    public bool IsRunning => isRunning;

    public void SetSpeed(float speed, bool sprinting)
    {
        moveSystem.Speed = speed;
        isRunning = sprinting || speed > walkSpeed;
    }

    void Reset()
    {
        moveSystem = GetComponentInChildren<MoveSystem>();
        walkSpeed = 2f;
        runSpeed = 5f;
        isRunning = false;
    }
}
