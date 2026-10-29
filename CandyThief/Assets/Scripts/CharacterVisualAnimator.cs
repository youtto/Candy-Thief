using UnityEngine;

[RequireComponent(typeof(Animator))]
public class CharacterVisualAnimator : MonoBehaviour
{
    [SerializeField] PlayerController movementSource;
    [SerializeField, Range(0, 3)] int defaultDirection;

    Animator animator;
    string currentState;

    void Awake()
    {
        animator = GetComponent<Animator>();
        if (movementSource == null)
            movementSource = GetComponentInParent<PlayerController>();
    }

    void OnEnable()
    {
        PlayState(false, defaultDirection);
    }

    void Update()
    {
        if (movementSource == null)
            return;

        PlayState(movementSource.IsMoving, movementSource.FacingDirection);
    }

    void PlayState(bool moving, int direction)
    {
        string stateName = (moving ? "Walk_" : "Idle_") + DirectionName(direction);
        if (stateName == currentState)
            return;

        currentState = stateName;
        animator.Play(stateName, 0, 0f);
    }

    static string DirectionName(int direction)
    {
        switch (direction)
        {
            case 1: return "Left";
            case 2: return "Up";
            case 3: return "Right";
            default: return "Down";
        }
    }
}
