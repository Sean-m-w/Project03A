using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerWallRunning : MonoBehaviour
{
    [Header("Wallrunning")]
    [SerializeField] LayerMask _whatIsWall;
    [SerializeField] LayerMask _whatIsGround;
    [SerializeField] float _wallRunForce;
    [SerializeField] float _wallJumpUpForce;
    [SerializeField] float _wallJumpSideForce;
    [SerializeField] float _wallJumpForwardForce;
    [SerializeField] float _maxWallRunTime;
    private float _wallRunTimer;

    [Header("Input")]
    public KeyCode _jumpKey = KeyCode.Space;
    private float _horizontalInput;
    private float _verticalInput;

    //Look into seeing if these public values can be Serialized.
    [Header("Limitations")]
    public bool _doJumpOnEndOfTimer = false;
    public bool _resetDoubleJumpsOnNewWall = true;
    public bool _resetDoubleJumpsOnEveryWall = false;
    public int _allowedWallJumps = 2;

    [Header("Detection")]
    [SerializeField] float _wallCheckDistance;
    [SerializeField] float _minJumpHeight;
    private RaycastHit _leftWallHit;
    private RaycastHit _rightWallHit;
    private bool _wallLeft;
    private bool _wallRight;

    [Header("Exiting")]
    private bool exitingWall;
    [SerializeField] float _exitWallTime;
    private float _exitWallTimer;

    [Header("Gravity")]
    public bool useGravity;
    [SerializeField] float _gravityCounterForce;

    [Header("References")]
    [SerializeField] Transform _orientation;
    public playerCam _cam;
    private playerMovement pm;
    private Rigidbody rb;

    private bool _wallRemembered;
    private Transform _lastWall;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        pm = GetComponent<playerMovement>();

        
        if (_whatIsWall.value == 0)
            _whatIsWall = LayerMask.GetMask("Default");

        if (_whatIsGround.value == 0)
            _whatIsGround = LayerMask.GetMask("Default");
        
    }

    private void Update()
    {
        CheckForWall();
        StateMachine();

        /*
        //If grounded, next wall is a new one
        if (pm.grounded && _lastWall != null)
            _lastWall = null;
        */

        if (_lastWall != null)
        {
            _lastWall = null;
        }
    }

    private void FixedUpdate()
    {
        if (pm.wallrunning)
        {
            WallRunningMovement();
        }
    }

    private void CheckForWall()
    {
        _wallRight = Physics.Raycast(transform.position, _orientation.right, out _rightWallHit, _wallCheckDistance, _whatIsWall);
        _wallLeft = Physics.Raycast(transform.position, -_orientation.right, out _leftWallHit, _wallCheckDistance, _whatIsWall);

        //Reset readyToClimb and wallJumps whenever player hits a new wall
        if ((_wallLeft || _wallRight) && NewWallHit())
        {
            _wallRunTimer = _maxWallRunTime;
        }
    }

    private void RememberLastWall()
    {
        if (_wallLeft)
        {
            _lastWall = _leftWallHit.transform;
        }

        if (_wallRight)
        {
            _lastWall = _rightWallHit.transform;
        }
    }

    private bool NewWallHit()
    {
        if (_lastWall == null)
        {
            return true;
        }

        if (_wallLeft && _leftWallHit.transform != _lastWall)
        {
            return true;
        }

        else if (_wallRight && _rightWallHit.transform != _lastWall)
        {
            return true;
        }

        return false;
    }

    private bool AboveGround()
    {
        return !Physics.Raycast(transform.position, Vector3.down, _minJumpHeight, _whatIsGround);
    }

    private void StateMachine()
    {
        //Getting inputs
        _horizontalInput = Input.GetAxisRaw("Horizontal");
        _verticalInput = Input.GetAxisRaw("Vertical");

        //Mode01 - Wallrunning
        if((_wallLeft || _wallRight) && _verticalInput > 0 && AboveGround() && !exitingWall)
        {
            //Start wallrunning
            if (!pm.wallrunning)
            {
                StartWallRun();
            }

            //Wallrun timer
            if (_wallRunTimer > 0)
            {
                _wallRunTimer -= Time.deltaTime;
            }

            //If timer less than or equal to 0 and wallrunning, and if timer ends, then WallJump. Else, exiting wall is true and timer resets.
            if (_wallRunTimer <= 0 && pm.wallrunning)
            {
                if (_doJumpOnEndOfTimer)
                    WallJump();

                else
                {
                    exitingWall = true;
                    _exitWallTimer = _exitWallTime;
                }
            }

            //Wall jump
            if (Input.GetKeyDown(_jumpKey))
            {
                WallJump();
            }
        }

        //Mode02 - Exiting
        else if (exitingWall)
        {
            if (pm.wallrunning)
            {
                StopWallRun();
            }

            if (_exitWallTimer > 0)
            {
                _exitWallTimer -= Time.deltaTime;
            }

            if (_exitWallTimer <= 0)
            {
                exitingWall = false;
            }
        }

        //Mode03 - None
        else
        {
            if (pm.wallrunning)
            {
                StopWallRun();
            }
        }
    }

    private void StartWallRun()
    {
        pm.wallrunning = true;

        //EXPERIMENTAL
        pm._currentJump = 0;

        pm._secondJumpSound.enabled = false;
        pm._firstJumpSound.enabled = false;
        pm._wallRunningSound.enabled = true;

        _wallRunTimer = _maxWallRunTime;

        rb.useGravity = useGravity;

        //rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        _wallRemembered = false;

        //Apply camera effects
        _cam.DoFov(80f);
        if (_wallLeft)
        {
            _cam.DoTilt(-5f);
        }
        if (_wallRight)
        {
            _cam.DoTilt(5f);
        }
    }

    private void WallRunningMovement()
    {
        rb.useGravity = useGravity;

        Vector3 wallNormal = _wallRight ? _rightWallHit.normal : _leftWallHit.normal;

        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

        if((_orientation.forward - wallForward).magnitude > (_orientation.forward - -wallForward).magnitude)
        {
            wallForward = -wallForward;
        }

        //Forward force
        rb.AddForce(wallForward * _wallRunForce, ForceMode.Force);

        //Stick to walls
        if(!(_wallLeft && _horizontalInput > 0) && !(_wallRight && _horizontalInput < 0))
        rb.AddForce(-wallNormal * 100, ForceMode.Force);

        //Weaken gravity
        if (useGravity)
        {
            rb.AddForce(transform.up * _gravityCounterForce, ForceMode.Force);
        }

        //Remember previous wall
        if (!_wallRemembered)
        {
            RememberLastWall();
            _wallRemembered = true;
        }
    }

    private void StopWallRun()
    {
        rb.useGravity = true;

        pm.wallrunning = false;

        pm._firstJumpSound.enabled = true;

        pm._wallRunningSound.enabled = false;

        pm._currentJump = 0;

        //Reset camera effect
        _cam.DoFov(60f);
        _cam.DoTilt(0f);
    }

    private void WallJump()
    {
        //Enter exitingWall state
        exitingWall = true;

        pm.readyToJump = true;

        _exitWallTimer = _exitWallTime;

        if (Input.GetKeyDown(_jumpKey) && (pm._currentJump != pm._totalJump))
        {
            pm.Jump();
        }
        if (Input.GetKeyDown(_jumpKey) && (pm._currentJump > 2))
        {
            pm.readyToJump = false;
        }

        Vector3 wallNormal = _wallRight ? _rightWallHit.normal : _leftWallHit.normal;

        Vector3 forceToApply = transform.up * _wallJumpUpForce + wallNormal * _wallJumpSideForce;

        //Reset Y velocity and add force
        rb.velocity = new Vector3(rb.velocity.x, 0f, 30f);
        rb.AddForce(forceToApply, ForceMode.Impulse);

        RememberLastWall();
    }
}
