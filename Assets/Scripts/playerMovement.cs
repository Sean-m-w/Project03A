using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerMovement : MonoBehaviour
{
    [Header("Movement")]
    private float _moveSpeed;
    [SerializeField] float walkSpeed;
    [SerializeField] float sprintSpeed;

    [SerializeField] float _groundDrag;

    [Header("Jumping")]
    [SerializeField] float _jumpForce;
    [SerializeField] float _jumpCooldown;
    [SerializeField] float _airMultiply;
    bool readyToJump;

    [Header("Crouching")]
    [SerializeField] float _crouchSpeed;
    [SerializeField] float _crouchYScale;
    private float _startYScale;

    [Header("Keybinds")]
    [SerializeField] KeyCode _jumpKey = KeyCode.Space;
    [SerializeField] KeyCode _sprintKey = KeyCode.LeftShift;
    [SerializeField] KeyCode _crouchKey = KeyCode.LeftControl;

    [Header("Ground Check")]
    [SerializeField] float _playerHeight;
    [SerializeField] LayerMask _whatIsGround;
    bool grounded;

    [Header("Slope Handling")]
    public float _maxSlopeAngle;
    private RaycastHit _slopeHit;
    private bool _exitingSlope;

    public Transform _orientation;

    private float _horizontalInput;
    private float _verticalInput;

    Vector3 _moveDirection;

    Rigidbody rb;

    public MovementState state;
    public enum MovementState
    {
        walking,
        sprinting,
        crouching,
        air
    }

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        readyToJump = true;

        _startYScale = transform.localScale.y;
    }

    // Update is called once per frame
    void Update()
    {
        //Ground check
        grounded = Physics.Raycast(transform.position, Vector3.down, _playerHeight * 0.5f + 0.2f, _whatIsGround);

        MyInput();
        SpeedControl();
        StateHandler();

        //Handle drag
        if (grounded)
        {
            rb.drag = _groundDrag;
        }
        else
        {
            rb.drag = 0;
        }
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void StateHandler()
    {
        //Crouching Engaged
        if (Input.GetKey(_crouchKey))
        {
            state = MovementState.crouching;
            _moveSpeed = _crouchSpeed;
        }

        //Sprinting Engaged
        else if (grounded && Input.GetKey(_sprintKey))
        {
            state = MovementState.sprinting;
            _moveSpeed = sprintSpeed;
        }

        //Walking Engaged
        else if (grounded)
        {
            state = MovementState.walking;
            _moveSpeed = walkSpeed;
        }

        //In Air
        else
        {
            state = MovementState.air;
        }
    }

    void MyInput()
    {
        _horizontalInput = Input.GetAxisRaw("Horizontal");
        _verticalInput = Input.GetAxisRaw("Vertical");

        //When to jump
        if(Input.GetKeyDown(_jumpKey) && readyToJump && grounded)
        {
            readyToJump = false;

            Jump();

            Invoke(nameof(ResetJump), _jumpCooldown);
        }

        //Start crouching
        if (Input.GetKeyDown(_crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, _crouchYScale, transform.localScale.z);
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }

        //Stop crouching
        if (Input.GetKeyUp(_crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, _startYScale, transform.localScale.z);
        }
    }

    void MovePlayer()
    {
        //Calculate movement direction
        _moveDirection = _orientation.forward * _verticalInput + _orientation.right * _horizontalInput;

        if (OnSlope() && !_exitingSlope)
        {
            rb.AddForce(GetSlopeMoveDirection() * _moveSpeed * 20f, ForceMode.Force);

            if (rb.velocity.y > 0)
                rb.AddForce(Vector3.down * 80f, ForceMode.Force);
        }

        //On ground
        else if (grounded)
        {
            rb.AddForce(_moveDirection.normalized * _moveSpeed * 10f, ForceMode.Force);
        }
        //In air
        else if (!grounded)
        {
            rb.AddForce(_moveDirection.normalized * _moveSpeed * 10f * _airMultiply, ForceMode.Force);
        }

        //Disable gravity on slope
        rb.useGravity = !OnSlope();
    }

    private void SpeedControl()
    {
        //Limit speed on slope
        if (OnSlope() && !_exitingSlope)
        {
            if (rb.velocity.magnitude > _moveSpeed)
                rb.velocity = rb.velocity.normalized * _moveSpeed;
        }

        //Limit speed on ground or in air
        else
        {
            Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            // limit velocity if needed
            if (flatVel.magnitude > _moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * _moveSpeed;
                rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
            }
        }
    }

    void Jump()
    {
        _exitingSlope = true;

        //Reset Y velocity
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        rb.AddForce(transform.up * _jumpForce, ForceMode.Impulse);
    }

    void ResetJump()
    {
        readyToJump = true;

        _exitingSlope = false;
    }

    public bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out _slopeHit, _playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, _slopeHit.normal);
            return angle < _maxSlopeAngle && angle != 0;
        }

        return false;
    }

    public Vector3 GetSlopeMoveDirection(Vector3 direction)
    {
        return Vector3.ProjectOnPlane(direction, _slopeHit.normal).normalized;
    }
}
