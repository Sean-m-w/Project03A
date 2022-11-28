using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class playerCam : MonoBehaviour
{
    [SerializeField] float _sensX;
    [SerializeField] float _sensY;

    public Transform _orientation;
    public Transform _camHolder;

    private float _xRotation;
    private float _yRotation;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        //Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * Time.deltaTime * _sensX;
        float mouseY = Input.GetAxis("Mouse Y") * Time.deltaTime * _sensY;

        _yRotation += mouseX;
        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);

        //Rotate cam and orientation
        _camHolder.rotation = Quaternion.Euler(_xRotation, _yRotation, 0);
        _orientation.rotation = Quaternion.Euler(0, _yRotation, 0);

    }

    public void DoFov(float endValue)
    {
        GetComponent<Camera>().DOFieldOfView(endValue, 0.25f);
    }

    public void DoTilt(float zTilt)
    {
        transform.DOLocalRotate(new Vector3(0, 0, zTilt), 0.25f);
    }
}
