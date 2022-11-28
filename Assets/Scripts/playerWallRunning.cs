using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerWallRunning : MonoBehaviour
{
    [Header("Wallrunning")]
    [SerializeField] LayerMask _whatIsWall;
    [SerializeField] LayerMask _whatIsGround;
    [SerializeField] float _wallRunForce;
    [SerializeField] float _maxWallRunTime;
    private float _wallRunTimer;

    [Header("Input")]
    private float _horizontalInput;
    private float _verticalInput;

    [Header("Detection")]
    [SerializeField] float _wallCheckDistance;
    [SerializeField] float _minJumpHeight;
    private RaycastHit _leftWallHit;
    private RaycastHit _rightWallHit;
    private bool _wallLeft;
    private bool _wallRight;

    [Header("References")]
    [SerializeField] Transform _orientation;
    private playerMovement pm;
    private Rigidbody rb;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        pm = GetComponent<playerMovement>();
    }

    private void Update()
    {
        CheckForWall();
        StateMachine();
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

        //State 1 - Wallrunning
        if((_wallLeft || _wallRight) && _verticalInput > 0 && AboveGround())
        {
            //Start wallrunning
            if (!pm.wallrunning)
            {
                StartWallRun();
            }
        }

        //State 3 - None
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
    }

    private void WallRunningMovement()
    {
        rb.useGravity = false;
        rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y, rb.velocity.z);
        //Below code is to have no downward velocity
        //rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

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
    }

    private void StopWallRun()
    {
        pm.wallrunning = false;
    }
}
