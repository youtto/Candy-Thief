using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] float moveSpeed = 3.5f;
    [SerializeField] bool followCamera = true;

    Rigidbody2D body;
    InputAction moveAction;
    Vector2 moveInput;
    int facing;

    public Vector2 MoveInput => moveInput;
    public int FacingDirection => facing;
    public bool IsMoving => moveInput.sqrMagnitude > 0.0001f;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    void OnEnable()
    {
        if (InputSystem.actions == null)
            return;

        InputSystem.actions.FindActionMap("Player", false)?.Enable();
        moveAction = InputSystem.actions.FindAction("Player/Move", false);
    }

    void Update()
    {
        moveInput = ReadMove();
        UpdateFacing(moveInput);
    }

    void FixedUpdate()
    {
        if (body == null)
            return;

        Vector2 velocity = moveInput;
        if (velocity.sqrMagnitude > 1f)
            velocity.Normalize();

        body.linearVelocity = velocity * moveSpeed;
    }

    void LateUpdate()
    {
        if (!followCamera)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        Vector3 cameraPosition = cam.transform.position;
        Vector3 playerPosition = transform.position;
        cam.transform.position = new Vector3(playerPosition.x, playerPosition.y, cameraPosition.z);
    }

    Vector2 ReadMove()
    {
        if (moveAction != null)
            return moveAction.ReadValue<Vector2>();

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return Vector2.zero;

        float x = 0f;
        float y = 0f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            x -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            x += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            y -= 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            y += 1f;
        return new Vector2(x, y);
    }

    void UpdateFacing(Vector2 input)
    {
        float ax = Mathf.Abs(input.x);
        float ay = Mathf.Abs(input.y);
        const float deadZone = 0.01f;
        if (ax < deadZone && ay < deadZone)
            return;

        const float tie = 0.001f;
        if (ax > ay + tie)
            facing = input.x > 0f ? 3 : 1;
        else if (ay > ax + tie)
            facing = input.y > 0f ? 2 : 0;
    }

}
