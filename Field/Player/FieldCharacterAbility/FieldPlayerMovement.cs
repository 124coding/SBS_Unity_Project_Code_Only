using UnityEngine;
using Unity.Cinemachine;

public class FieldPlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float moveAcceleration = 50f;
    public float moveDeceleration = 50f;
    public bool isInputInverted = false;
    private float currentSpeed = 0f;
    private float currentAcceleration = 0f;
    private float currentDeceleration = 0f;
    private float currentPlayerVelocityX = 0f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    public float coyoteTime = 0.15f;      // 발이 떨어져도 점프 가능한 시간
    public float CoyoteTimeCounter { get; private set; } // 외부에서 읽을 수 있게 프로퍼티화

    private bool isGrounded;

    public bool IsGrounded => isGrounded;

    private Rigidbody2D rb;
    private Vector2 moveInput;

    private MovingPlatform currentPlatform;

    public void SetCurrentSpeed(float speed)
    {
        currentPlayerVelocityX = speed;
    }

    public void SetOverrideSpeed(float newSpeed, float newAcceleration, float newDeceleration)
    {
        currentSpeed = newSpeed;
        currentAcceleration = newAcceleration;
        currentDeceleration = newDeceleration;
    }

    public void ResetSpeed()
    {
        currentSpeed = moveSpeed;
        currentAcceleration = moveAcceleration;
        currentDeceleration = moveDeceleration;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ResetSpeed();
    }

    private void Update()
    {
        Collider2D groundHit = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        isGrounded = groundHit != null;

        if (isGrounded)
        {
            CoyoteTimeCounter = coyoteTime;
            currentPlatform = groundHit.GetComponentInParent<MovingPlatform>();
        }
        else
        {
            CoyoteTimeCounter -= Time.deltaTime;
            currentPlatform = null;
        }

        moveInput = InputManager.Instance.inputActions.Field.Move.ReadValue<Vector2>();

        if(moveInput.y > 0f)
        {
            moveInput.y = 0f;
        }

        if (isInputInverted) moveInput *= -1f;

        if (Mathf.Abs(moveInput.x) < 0.1f) moveInput.x = 0f;

        if (moveInput.x != 0)
        {
            FieldPlayerPushPull pushPull = GetComponent<FieldPlayerPushPull>();
            bool canFlip = (pushPull == null || !pushPull.IsGrabbing);

            if (canFlip)
            {
                float facingDir = moveInput.x > 0 ? 1f : -1f;
                transform.localScale = new Vector3(facingDir, 1f, 1f);
            }
        }
    }

    private void FixedUpdate()
    {
        float targetVelocityX = moveInput.x * currentSpeed;

        if (moveInput.x == 0 && currentDeceleration >= 100f)
        {
            currentPlayerVelocityX = 0f;
        }
        else
        {
            float accelRate = (moveInput.x == 0 || (Mathf.Sign(moveInput.x) != Mathf.Sign(currentPlayerVelocityX) && currentPlayerVelocityX != 0))
                ? currentDeceleration : currentAcceleration;

            currentPlayerVelocityX = Mathf.MoveTowards(
                currentPlayerVelocityX,
                targetVelocityX,
                accelRate * Time.fixedDeltaTime
            );
        }

        float finalVelocityX = currentPlayerVelocityX;

        if (currentPlatform != null) finalVelocityX += currentPlatform.PlatformVelocity.x;

        rb.linearVelocity = new Vector2(finalVelocityX, rb.linearVelocity.y);
    }

    public void ConsumeCoyoteTime()
    {
        CoyoteTimeCounter = 0f;
    }

    private void OnDisable()
    {
        // 컷씬이나 메뉴 창을 열어서 스크립트가 꺼지면 관성 없이 즉시 멈춤
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
        currentPlayerVelocityX = 0f;
    }
}
