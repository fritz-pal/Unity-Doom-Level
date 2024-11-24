using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public Camera playerCamera;
    public float walkSpeed = 5f;
    public float runSpeed = 10f;
    public float sensitivity = 5f;
    public float jumpForce = 100f;
    public float dashForce = 100f;
    public float grappleForce = 1f;
    public float gravity = 9.81f;
    public float maxGrappleDistance = 100f;
    public Button cooldownIndicatorDash;
    public Button cooldownIndicatorGrapple;
    public LineRenderer grappleLine;
    public GameObject gunPoint;
    private bool isSprinting = false;
    private CharacterController characterController;
    private Vector3 moveInput;
    private Vector2 lookInput;
    private Vector2 lookDirection;
    private Vector3 moveVelocity;
    private Vector3 acc = Vector3.zero;
    private bool jump;
    private float dashUsed = 0;
    private float grappleUsed = 0;
    private bool grappling = false;
    private bool grapplePulling = false;
    private Vector3 grapplePoint;


    // Start is called before the first frame update
    void Start()
    {
        characterController = gameObject.GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        grappleLine.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {

        lookDirection.x += lookInput.x * sensitivity;
        lookDirection.y -= lookInput.y * sensitivity;
        lookDirection.y = Mathf.Clamp(lookDirection.y, -90f, 90f);
        playerCamera.transform.localRotation = Quaternion.Euler(lookDirection.y, 0, 0);
        transform.rotation = Quaternion.Euler(0, lookDirection.x, 0);


        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        if (characterController.isGrounded)
        {
            acc += (forward * moveInput.y + right * moveInput.x) * (isSprinting ? runSpeed : walkSpeed);
            moveVelocity.y = 0;
            if (jump)
            {
                acc.y += jumpForce;
            }
        }
        else
        {
            acc.y -= gravity;
            acc += (forward * moveInput.y + right * moveInput.x) * (isSprinting ? runSpeed : walkSpeed) * 0.2f;
        }
        moveVelocity += acc;
        characterController.Move(moveVelocity * Time.deltaTime);

        if (characterController.isGrounded)
        {
            moveVelocity.x *= 0.9f;
            moveVelocity.z *= 0.9f;
        }
        else
        {
            moveVelocity.x *= 0.99f;
            moveVelocity.z *= 0.99f;
        }
        acc = Vector3.zero;

        if (grappling)
        {
            grappleLine.SetPosition(1, gunPoint.transform.position);
        }
        if (grapplePulling)
        {
            Vector3 direction = (grapplePoint - gunPoint.transform.position).normalized * grappleForce;
            ApplyForce(direction);
            if (Time.time - grappleUsed > 5 || Vector3.Distance(gameObject.transform.position, grapplePoint) < 1)
            {
                grapplePulling = false;
                grappling = false;
                grappleLine.enabled = false;
                moveVelocity = Mathf.Min(moveVelocity.magnitude, jumpForce * 2) * moveVelocity.normalized;
            }
        }

        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1.1f))
        {
            if (hit.collider.gameObject.CompareTag("Trampoline"))
            {
                moveVelocity.y = jumpForce * 2;
            }
        }
    }

    public void HandleMoveInput(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void HandleLookInput(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    public void ApplyForce(Vector3 force)
    {
        acc += force;
    }

    public void HandleSprintInput(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            isSprinting = true;
        }
        if (context.canceled)
        {
            isSprinting = false;
        }
    }

    public void HandleDashInput(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (Time.time - dashUsed < 3)
            {
                return;
            }
            ApplyForce(transform.forward * dashForce);
            dashUsed = Time.time;
            cooldownIndicatorDash.interactable = false;
            StartCoroutine(StartCooldownDash());
        }
    }

    private IEnumerator StartCooldownDash()
    {
        TextMeshProUGUI text = cooldownIndicatorDash.GetComponentInChildren<TextMeshProUGUI>();
        for (int i = 3; i > 0; i--)
        {
            text.text = i.ToString();
            yield return new WaitForSeconds(1);
        }
        text.text = "F";
        cooldownIndicatorDash.interactable = true;
    }

    public void HandGrappleInput(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (Time.time - grappleUsed < 5)
            {
                return;
            }
            grappling = true;
            RaycastHit hit;
            if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, maxGrappleDistance))
            {
                grapplePoint = hit.point;
                Invoke(nameof(ExecuteGrapple), 0.5f);
            }
            else
            {
                grapplePoint = playerCamera.transform.position + playerCamera.transform.forward * maxGrappleDistance;
                Invoke(nameof(StopGrapple), 0.5f);
            }
            grappleLine.SetPosition(0, grapplePoint);
            grappleLine.enabled = true;
            grappleUsed = Time.time;
            cooldownIndicatorGrapple.interactable = false;
            StartCoroutine(StartCooldownGrapple());
        }
    }

    private void ExecuteGrapple()
    {
        moveVelocity = Vector3.zero;
        Vector3 direction = (grapplePoint - gunPoint.transform.position).normalized * 10;
        ApplyForce(direction);
        grapplePulling = true;
    }

    private void StopGrapple()
    {
        grappling = false;
        grappleLine.enabled = false;
    }

    private IEnumerator StartCooldownGrapple()
    {
        TextMeshProUGUI text = cooldownIndicatorGrapple.GetComponentInChildren<TextMeshProUGUI>();
        for (int i = 5; i > 0; i--)
        {
            text.text = i.ToString();
            yield return new WaitForSeconds(1);
        }
        text.text = "Q";
        cooldownIndicatorGrapple.interactable = true;
    }

    public void HandleJumpInput(InputAction.CallbackContext context)
    {
        jump = context.performed;
    }
}
