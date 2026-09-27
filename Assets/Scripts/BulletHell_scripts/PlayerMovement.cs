using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{

    [SerializeField] private float defaultSpeed = 10f;
    [SerializeField] private float focusMultiplier = .3f;
    [SerializeField] private float edgePadding = 0.3f;

    public float CurrentSpeed { get; private set; }
    public float DefaultSpeed => defaultSpeed;

    private Rigidbody2D rigidBody;
    private Vector2 moveInput;
    private bool isFocused;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        rigidBody.bodyType = RigidbodyType2D.Kinematic;
        rigidBody.sleepMode = RigidbodySleepMode2D.NeverSleep;
        CurrentSpeed = defaultSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null)
        {
            moveInput = Vector2.zero;
            return;
        }

        float x = 0f;
        float y = 0f;

        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) y -= 1f;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) y += 1f;

        moveInput = new Vector2(x, y).normalized;
        isFocused = kb.leftShiftKey.isPressed;
    }
    void FixedUpdate()
    {
        float speed = isFocused ? CurrentSpeed * focusMultiplier : CurrentSpeed;
        Vector2 target = rigidBody.position + moveInput * speed * Time.fixedDeltaTime;
        rigidBody.MovePosition(rigidBody.position + moveInput * speed * Time.fixedDeltaTime);
        if (PlayArea.Instance != null)
        {
            target = PlayArea.Instance.Clamp(target, edgePadding);
        }
        rigidBody.MovePosition(target);
    }

    public void SetSpeed(float newSpeed)
    {
        CurrentSpeed = Mathf.Max(0f, newSpeed); // no negative speeds
    }

    public void ResetSpeed()
    {
        CurrentSpeed = defaultSpeed;
    }
}

