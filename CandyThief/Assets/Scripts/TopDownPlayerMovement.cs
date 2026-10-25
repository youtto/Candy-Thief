using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]
public class TopDownPlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D body;
    private InputAction moveAction;
    private Vector2 moveInput;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        PlayerInput playerInput = GetComponent<PlayerInput>();
        if (playerInput == null || playerInput.actions == null)
        {
            Debug.LogError("TopDownPlayerMovement requires PlayerInput with the project Input Actions asset.", this);
            return;
        }

        moveAction = playerInput.actions.FindActionMap("Player", true).FindAction("Move", true);
    }

    private void Update()
    {
        if (moveAction == null)
        {
            return;
        }

        moveInput = moveAction.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        Vector2 input = moveInput;
        if (input.sqrMagnitude > 1f)
        {
            input.Normalize();
        }

        body.linearVelocity = input * moveSpeed;
    }
}
