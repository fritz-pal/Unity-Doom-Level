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
    public int health = 100;
    public GameObject gameOverScreen;
    public Slider healthBar;
    public Button cooldownIndicatorDash;
    public Button cooldownIndicatorGrapple;
    public LineRenderer grappleLine;
    public LineRenderer shotLine;
    public GameObject gunPoint;
    private bool isSprinting = false;
    private CharacterController characterController;
    private Vector3 moveInput;
    private Vector2 lookInput;
    private Vector2 lookDirection;
    private Vector3 moveVelocity;
    private Vector3 acc = Vector3.zero;
    public AudioClip gunShot;
    private bool jump;
    private float dashUsed = 0;
    private float grappleUsed = 0;
    private float gunUsed = 0;
    private bool grappling = false;
    private bool grapplePulling = false;
    private Vector3 grapplePoint;
    private bool climbing = false;	


    void Start()
    {
        characterController = gameObject.GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        grappleLine.enabled = false;
        shotLine.enabled = false;
        healthBar.maxValue = health;
        healthBar.value = health;
        gameOverScreen.SetActive(false);
    }

    void Update()
    {
        lookDirection.x += lookInput.x * sensitivity;
        lookDirection.y -= lookInput.y * sensitivity;
        lookDirection.y = Mathf.Clamp(lookDirection.y, -90f, 90f);
        playerCamera.transform.localRotation = Quaternion.Euler(lookDirection.y, 0, 0);
        transform.rotation = Quaternion.Euler(0, lookDirection.x, 0);

        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);
        Vector3 up = transform.TransformDirection(Vector3.up);

        if (characterController.isGrounded || climbing)
        {
            
            acc += (forward * moveInput.y + right * moveInput.x) * ((isSprinting ? runSpeed : walkSpeed) * Time.deltaTime);
            if (moveVelocity.y < 0) moveVelocity.y = 0;
            if (jump)
            {
                if (!climbing)
                {
                    acc += up * jumpForce;
                }
                else
                {
                    acc += up * walkSpeed / 2 * Time.deltaTime ;
                }
            }
        }
        else
        {
            acc += (forward * moveInput.y + right * moveInput.x) * ((isSprinting ? runSpeed : walkSpeed) * 0.2f * Time.deltaTime);
        }
        if (!climbing)
        {
            acc.y -= gravity*Time.deltaTime;
        }
        moveVelocity += acc;
        characterController.Move(moveVelocity * Time.deltaTime);

        if (characterController.isGrounded || climbing)
        {
            moveVelocity.x *= 0.9f;
            moveVelocity.z *= 0.9f;
            if (climbing)
            {
                moveVelocity.y *= 0.9f;
            }
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
            if (Time.time - grappleUsed > 5 || Vector3.Distance(gameObject.transform.position, grapplePoint) < 1.5)
            {
                grapplePulling = false;
                grappling = false;
                grappleLine.enabled = false;
                moveVelocity = Mathf.Min(moveVelocity.magnitude, jumpForce * 1.5f) * moveVelocity.normalized;
            }
        }

        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1.2f))
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
            if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.TransformDirection(Vector3.forward), out hit, maxGrappleDistance))
            {
                grapplePoint = hit.point;
                Invoke(nameof(ExecuteGrapple), 0.5f);
            }
            else
            {
                grapplePoint = playerCamera.transform.position + playerCamera.transform.TransformDirection(Vector3.forward) * maxGrappleDistance;
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

    public void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Climbable"))
        {
            climbing = true;
        }
    }

    public void HandleFireInput(InputAction.CallbackContext context)
    {
        if (context.performed && !grappling && !grapplePulling)
        {
            if (Time.time - gunUsed < 0.5)
            {
                return;
            }
            gunUsed = Time.time;
            AudioSource.PlayClipAtPoint(gunShot, playerCamera.transform.position);
            shotLine.SetPosition(0, gunPoint.transform.position);
            shotLine.SetPosition(1, playerCamera.transform.position + playerCamera.transform.TransformDirection(Vector3.forward) * 100);
            shotLine.enabled = true;
            Invoke(nameof(StopShot), 0.1f);
            RaycastHit hit;
            if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.TransformDirection(Vector3.forward), out hit, 100))
            {
                if (hit.collider.gameObject.CompareTag("Enemy"))
                {
                    hit.collider.gameObject.GetComponent<Enemy>().TakeDamage(100);
                }
            }
        }
    }

    private void StopShot()
    {
        shotLine.enabled = false;
    }

    public void TakeDamage(int damage, Vector3 attacker)
    {
        health -= damage;
        healthBar.value = health;
        ApplyForce((transform.position - attacker).normalized * 10);
        if (health <= 0)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            gameOverScreen.SetActive(true);
            Time.timeScale = 0;
            
        }   
    }

    public void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Climbable"))
        {
            climbing = false;
        }
    }
}
