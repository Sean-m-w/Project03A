using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerSliding : MonoBehaviour
{
    [Header("References")]
    public Transform _orientation;
    public Transform _playerObj;
    public playerCam cam;
    private Rigidbody rb;
    private playerMovement pm;

    [Header("Sliding")]
    public float _maxSlideTime;
    public float _slideForce;
    private float _slideTimer;

    public float _slideYScale;
    private float _startYScale;

    [Header("Input")]
    public KeyCode _slideKey = KeyCode.LeftControl;
    private float _horizontalInput;
    private float _verticalInput;


    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        pm = GetComponent<playerMovement>();

        _startYScale = _playerObj.localScale.y;
    }

    private void Update()
    {
        _horizontalInput = Input.GetAxisRaw("Horizontal");
        _verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(_slideKey) && (_horizontalInput != 0 || _verticalInput != 0))
            StartSlide();

        if (Input.GetKeyUp(_slideKey) && pm.sliding)
            StopSlide();
    }

    private void FixedUpdate()
    {
        if (pm.sliding)
            SlidingMovement();
    }

    private void StartSlide()
    {
        pm.sliding = true;

        _playerObj.localScale = new Vector3(_playerObj.localScale.x, _slideYScale, _playerObj.localScale.z);
        rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);

        _slideTimer = _maxSlideTime;

        cam.DoFov(50f);
        cam.DoTilt(4);
    }

    private void SlidingMovement()
    {
        Vector3 inputDirection = _orientation.forward * _verticalInput + _orientation.right * _horizontalInput;

        // sliding normal
        if (!pm.OnSlope() || rb.velocity.y > -0.1f)
        {
            rb.AddForce(inputDirection.normalized * _slideForce, ForceMode.Force);

            _slideTimer -= Time.deltaTime;
        }

        // sliding down a slope
        else
        {
            rb.AddForce(pm.GetSlopeMoveDirection(inputDirection) * _slideForce, ForceMode.Force);
        }

        if (_slideTimer <= 0)
            StopSlide();
    }

    private void StopSlide()
    {
        pm.sliding = false;

        _playerObj.localScale = new Vector3(_playerObj.localScale.x, _startYScale, _playerObj.localScale.z);

        cam.DoFov(60);
        cam.DoTilt(0);
    }
}
