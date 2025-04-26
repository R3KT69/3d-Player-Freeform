using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Animation")]
    public float acceleration;
    public Animator animator;

    [Header("Movement Settings")]
    //public float walkSpeed = 2.5f;
    public float sprintSpeed = 6f;
    public float jumpForce = 5f;
    public CharacterController controller;
    private Vector3 moveDirection;

    //private bool isWalking = false;
    private bool hasFallen = false; 
    public bool isStrafing = false;
    public bool isWalkingBackwards = false;
    public float moveX, moveZ;

    [Header("Physics Settings")]
    public GroundChecker groundTrigger;
    private float gravity = -9.81f;
    private Vector3 velocity;
    private float currentSpeed = 0f;
    public bool isGrounded_Physics;
    
    [Header("Slope Settings")]
    public float slopeLimit = 45f;
    public float slideSpeed = 5f;

    [Header("Ground Check Settings")]
    public bool isGrounded_Spherical;
    public Transform feetTransform; 
    public float sphereRadius = 0.3f;  
    public float checkDistance = 1f;   
    public LayerMask groundMask;  
    private static Collider[] groundHits = new Collider[10];  // This array is for NonAlloc version of GroundCheckSphere (more fast), array size = accuracy

    private void OnDrawGizmos()
    {
        if (feetTransform == null) return;
        bool grounded = GroundCheckV1(feetTransform, sphereRadius, groundMask);
        Gizmos.color = grounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(feetTransform.position, sphereRadius);
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (!TouchScreenCameraMovement.inAndroid)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
    }

    void Update()
    {
        HandleMovementInput();
        CheckGroundStatus();
        Jump();
        ApplyGravity();
        MovePlayer();
        Blendtree();
        isGrounded_Spherical = GroundCheckV1(feetTransform, sphereRadius, groundMask); // ** THIS IS THE ACCURATE GROUND CHECK, PRIMARILY USED FOR MASKING JUMP.
        //Debug.Log("Is Flying?: " + isGrounded_Spherical);
    }


    // This will provide accurate information, more costly.
    public bool GroundCheckV2(Transform feetTransform, float sphereRadius, LayerMask groundMask)
    {
        Collider[] colliders = Physics.OverlapSphere(feetTransform.position, sphereRadius, groundMask);
        return colliders.Length > 0;
    }
    
    // NonAlloc version of the GroundChecker
    public static bool GroundCheckV1(Transform feetTransform, float sphereRadius, LayerMask groundMask)
    {
        int hitCount = Physics.OverlapSphereNonAlloc(feetTransform.position, sphereRadius, groundHits, groundMask);
        return hitCount > 0;
    }
    
    void HandleMovementInput()
    {
        // Get horizontal and vertical axis input
        moveX = Input.GetAxis("Horizontal") + JoystickVirtual.VirtualHorizontal;  // A/D or Left/Right
        moveZ = Input.GetAxis("Vertical") + JoystickVirtual.VirtualVertical;    // W/S or Up/Down

        isStrafing = Mathf.Abs(moveX) > 0.1f && Mathf.Abs(moveZ) < 0.1f;
        // Calculate movement direction
        moveDirection = transform.right * moveX + transform.forward * moveZ;

        // Check if walking backwards
        if (moveZ < 0)
        {
            isWalkingBackwards = true;
        }
        else
        {
            isWalkingBackwards = false;
        }
    }


     

    void MovePlayer()
    {
        if (moveDirection.magnitude > 1)
            moveDirection.Normalize();

        float targetSpeed = sprintSpeed;
        

        // Smoothly transition currentSpeed towards targetSpeed
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 8f); // 8f is the smoothing speed

        velocity.y += gravity * Time.deltaTime; 

        Vector3 moveVelocity = moveDirection * currentSpeed;
        moveVelocity.y = velocity.y; 

        if (IsOnSteepSlope() && isGrounded_Physics)
        {
            Vector3 slideDir = GetSlideDirection();
            moveVelocity += slideDir * slideSpeed;
        }

        controller.Move(moveVelocity * Time.deltaTime);
    }


    void CheckGroundStatus()
    {
        isGrounded_Physics = controller.isGrounded; // ** THIS GROUND CHECK IS PURELY USED FOR PUSHING DOWN PLAYER, NOT IDEAL FOR DETECTING GROUNDING. **
        //Debug.Log($"{velocity.y}");
        
        if (isGrounded_Physics && velocity.y < 0)
        {
            velocity.y = -1f; // Small value to keep character grounded
        }

        animator.SetBool("isGrounded", isGrounded_Physics);

        if (!isGrounded_Physics && velocity.y < -10f && !hasFallen)
        {
            animator.SetTrigger("Falling");
            hasFallen = true; 
        }

        if (isGrounded_Physics)
        {
            hasFallen = false; 
        }
    }

    void Jump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded_Spherical && !IsOnSteepSlope())
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity); // Apply jump force
            animator.SetTrigger("Jump");
        }
    }

    public void AndroidJump()
    {
        if (isGrounded_Spherical && !IsOnSteepSlope())
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity); // Apply jump force
            animator.SetTrigger("Jump");
        }
        
    }

    void ApplyGravity()
    {
        if (!isGrounded_Physics)
        {
            velocity.y += gravity * Time.deltaTime; // Apply gravity when not grounded
        }
    }
    

    private float smoothedMouseX = 0f;
    void Blendtree()
    {
        //float moveX = Input.GetAxis("Horizontal"); 
        //float moveZ = Input.GetAxis("Vertical");   
        float rawMouseX = Input.GetAxis("Mouse X") * 10f; 

        rawMouseX = Mathf.Clamp(rawMouseX, -1f, 1f);
        smoothedMouseX = Mathf.Lerp(smoothedMouseX, rawMouseX, 5f * Time.deltaTime);

        // These represent the intended direction, scaled to [-1, 1]
        float targetAcceleration = moveZ;  
        float targetHorizontal = moveX;  

        if (Mathf.Abs(moveX) > 0.01f && Mathf.Abs(moveZ) > 0.01f)
        {
            float diagonalMagnitude = Mathf.Sqrt(moveX * moveX + moveZ * moveZ);
            targetAcceleration = moveZ / diagonalMagnitude;
            targetHorizontal = moveX / diagonalMagnitude;
        }

        if (isWalkingBackwards)
        {
            acceleration = Mathf.Lerp(acceleration, -Mathf.Abs(targetAcceleration), Time.deltaTime * 10f);
        }
        else
        {
            acceleration = Mathf.Lerp(acceleration, targetAcceleration, Time.deltaTime * 10f);
        }

        float horizontal = Mathf.Lerp(animator.GetFloat("Horizontal"), targetHorizontal, Time.deltaTime * 10f);
        
        if (Mathf.Abs(acceleration) < 0.001f) acceleration = 0f;
        if (Mathf.Abs(horizontal) < 0.001f) horizontal = 0f;
        if (Mathf.Abs(smoothedMouseX) < 0.001f) smoothedMouseX = 0f;

        animator.SetFloat("Acceleration", acceleration);
        animator.SetFloat("Horizontal", horizontal);
        animator.SetFloat("MouseHorizontal", smoothedMouseX);
        
    }




    bool IsOnSteepSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1.5f))
        {
            float angle = Vector3.Angle(hit.normal, Vector3.up);
            return angle > slopeLimit;
        }
        return false;
    }

    Vector3 GetSlideDirection()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1.5f))
        {
            // Vector3.down projected on the slope normal gives the slide direction
            return Vector3.ProjectOnPlane(Vector3.down, hit.normal).normalized;
        }
        return Vector3.zero;
    }

}
