using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerMovement : MonoBehaviour
{
    [Header("Movement")]
    private float _moveSpeed;
    [SerializeField] float _walkSpeed;
    [SerializeField] float _sprintSpeed;
    [SerializeField] float _slideSpeed;

    private float _desiredMoveSpeed;
    private float _lastDesiredMoveSpeed;

    [SerializeField] float _wallRunSpeed;

    [SerializeField] float _speedIncreaseMultiplier;
    [SerializeField] float _slopeIncreaseMultiplier;

    [SerializeField] float _groundDrag;

    public bool wallrunning;

    [Header("Jumping")]
    [SerializeField] float _jumpForce;
    [SerializeField] float _jumpCooldown;
    [SerializeField] float _airMultiply;
    bool readyToJump;
    bool canDoubleJump;

    [Header("Crouching")]
    [SerializeField] float _crouchSpeed;
    [SerializeField] float _crouchYScale;
    private float _startYScale;
    public bool crouching;

    [Header("Keybinds")]
    [SerializeField] KeyCode _jumpKey = KeyCode.Space;
    [SerializeField] KeyCode _sprintKey = KeyCode.LeftShift;
    [SerializeField] KeyCode _crouchKey = KeyCode.LeftControl;

    [Header("Ground Check")]
    [SerializeField] float _playerHeight;
    [SerializeField] LayerMask _whatIsGround;
    public bool grounded;
    //public bool airBourne;

    [Header("Slope Handling")]
    public float _maxSlopeAngle;
    private RaycastHit _slopeHit;
    private bool _exitingSlope;

    public Transform _orientation;

    private float _horizontalInput;
    private float _verticalInput;

    Vector3 _moveDirection;

    Rigidbody rb;

    public bool sliding;
    public bool restricted;

    //EXPERIMENTAL
    /*
    private int _currentJump = 0;
    private int _totalJump = 2;
    */
    
    public MovementState state;
    public enum MovementState
    {
        walking,
        sprinting,
        crouching,
        sliding,
        wallrunning,
        restricted,
        air
    }

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        readyToJump = true;
        canDoubleJump = false;

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
        if(state != MovementState.restricted)
        {
            MovePlayer();
        }
    }

    private void StateHandler()
    {
        //Restricted
        if (restricted)
        {
            state = MovementState.restricted;
        }

        //Wallrunning Engaged
        if (wallrunning)
        {
            state = MovementState.wallrunning;
            _desiredMoveSpeed = _wallRunSpeed;
        }

        //Sliding Engaged
        if (sliding)
        {
            state = MovementState.sliding;

            if (OnSlope() && rb.velocity.y < 0.1f)
                _desiredMoveSpeed = _slideSpeed;

            else
                _desiredMoveSpeed = _sprintSpeed;
        }

        //Crouching Engaged
        else if (crouching)
        {
            state = MovementState.crouching;
            _desiredMoveSpeed = _crouchSpeed;
        }

        //Sprinting Engaged
        else if (grounded && Input.GetKey(_sprintKey))
        {
            state = MovementState.sprinting;
            _desiredMoveSpeed = _sprintSpeed;
        }

        //Walking Engaged
        else if (grounded)
        {
            state = MovementState.walking;
            _desiredMoveSpeed = _walkSpeed;
        }

        //In Air
        else
        {
            state = MovementState.air;
        }

        //Check if desired movement speed has changed significantly.
        if(Mathf.Abs(_desiredMoveSpeed - _lastDesiredMoveSpeed) > 4f && _moveSpeed != 0)
        {
            StopAllCoroutines();
            StartCoroutine(SmoothlyLerpMoveSpeed());
        }
        else
        {
            _moveSpeed = _desiredMoveSpeed;
        }

        _lastDesiredMoveSpeed = _desiredMoveSpeed;
    }

    void MyInput()
    {
        _horizontalInput = Input.GetAxisRaw("Horizontal");
        _verticalInput = Input.GetAxisRaw("Vertical");

        //When to jump
        if (Input.GetKeyDown(_jumpKey) && readyToJump && grounded)
        {
            readyToJump = true;

            Jump();

            //_currentJump++;

            //Invoke(nameof(ResetJump), _jumpCooldown);
        }

        
        if (Input.GetKeyDown(_jumpKey) && readyToJump && !grounded)
        {
            readyToJump = false;

            Jump();

            //Invoke(nameof(ResetJump), _jumpCooldown);
        }

        //Will limit to just two jumps, but does not work with wall jumps.
        if(readyToJump == false && grounded == true)
        {
            Invoke(nameof(ResetJump), _jumpCooldown);
        }
        

        //Start crouching
        if (Input.GetKeyDown(_crouchKey) && _horizontalInput == 0 && _verticalInput == 0)
        {
            transform.localScale = new Vector3(transform.localScale.x, _crouchYScale, transform.localScale.z);
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);

            crouching = true;
        }

        //Stop crouching
        if (Input.GetKeyUp(_crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, _startYScale, transform.localScale.z);

            crouching = false;
        }
    }

    private IEnumerator SmoothlyLerpMoveSpeed()
    {
        //Smoothly lerp movement speed to desired value
        float time = 0;
        float difference = Mathf.Abs(_desiredMoveSpeed - _moveSpeed);
        float startValue = _moveSpeed;

        while (time < difference)
        {
            _moveSpeed = Mathf.Lerp(startValue, _desiredMoveSpeed, time / difference);

            if (OnSlope())
            {
                float slopeAngle = Vector3.Angle(Vector3.up, _slopeHit.normal);
                float slopeAngleIncrease = 1 + (slopeAngle / 90f);

                time += Time.deltaTime * _speedIncreaseMultiplier * _slopeIncreaseMultiplier * slopeAngleIncrease;
            }
            else
                time += Time.deltaTime * _speedIncreaseMultiplier;

            yield return null;
        }

        _moveSpeed = _desiredMoveSpeed;
    }

    void MovePlayer()
    {
        //Calculate movement direction
        _moveDirection = _orientation.forward * _verticalInput + _orientation.right * _horizontalInput;

        if (OnSlope() && !_exitingSlope)
        {
            rb.AddForce(GetSlopeMoveDirection(_moveDirection) * _moveSpeed * 20f, ForceMode.Force);

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
        if (!wallrunning)
        {
            rb.useGravity = !OnSlope();
        }
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

    //public Transform _markerSphere;
    //public float _maxJumpHeight;

    public void Jump()
    {
        _exitingSlope = true;

        //Reset Y velocity
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        //Jump force calculation
        rb.AddForce(transform.up * _jumpForce, ForceMode.Impulse);
    }

    void ResetJump()
    {
        readyToJump = true;

        canDoubleJump = true;

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
