using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed;
    public float walkSpeed;
    public float sprintSpeed;
    private enum MovementState
    {
        walking, sprinting, crouching, falling
    }
    private MovementState movementState;
    public float groundDrag;

    [Header("Jumping")]
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    bool readyToJump;

    [Header("Crouching")]
    public float crouchSpeed;
    public float crouchYScale;
    private float startYScale;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    public bool grounded;

    [Header("Slope Handling")]
    public float maxSlopeAngle;
    private RaycastHit slopeHit;
    private bool exitingSlope;

    [Header("Statistics")]
    public int health;
    public enum MoraleState {bold, determined, calm, frightened, panicked}
    public MoraleState moraleState;
    public CharacterStats stats;
    //Inputs
    private PlayerInput playerInput;

    //Physics
    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public bool canJump;
    [HideInInspector] public bool isCrouching;
    Vector3 moveDirection;
    //public Transform orientation; Not needed, we can use camera direction

    //Camera
    Camera camera;


    //Grabbing


    private void Start()
    {
        stats = new CharacterStats(); //Todo add settings list ref/character creation??
        rb = GetComponent<Rigidbody>();
        canJump = true;
        startYScale = 0.1f;
        camera = GetComponentInChildren<Camera>();
        playerInput = GetComponent<PlayerInput>();
    }

    private void Update()
    {
        // ground check
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);

        HandleInput();
        SpeedControl();
        MovementStateHandler();

        // handle drag
        if (grounded)
            rb.drag = groundDrag;
        else
            rb.drag = 0;
    }
    private void FixedUpdate()
    {
        HandleMovement();
    }

    private void MovementStateHandler()
    {
        // Mode - Crouching
        if (playerInput.actions["Crouch"].triggered)
        {
            movementState = MovementState.crouching;
            moveSpeed = crouchSpeed;
        }

        // Mode - Walking
        else if (grounded && playerInput.actions["Walk"].triggered)
        {
            movementState = MovementState.walking;
            moveSpeed = walkSpeed;
        }

        // Mode - Walking
        else if (grounded)
        {
            movementState = MovementState.sprinting;
            moveSpeed = sprintSpeed;
        }

        // Mode - Air
        else
        {
            movementState = MovementState.falling;
        }
    }

    private void SpeedControl()
    {
        // limiting speed on slope
        if (OnSlope() && !exitingSlope)
        {
            if (rb.velocity.magnitude > moveSpeed)
                rb.velocity = rb.velocity.normalized * moveSpeed;
        }
        // limiting speed on ground or in air
        else
        {
            Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            // limit velocity if needed
            if (flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
            }
        }
    }

    public void HandleMovement()
    {
        // calculate movement direction
        //moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput; //TODO Remove
        Vector2 moveInput = playerInput.actions["Move"].ReadValue<Vector2>();
        Vector3 moveDirection = new Vector3(moveInput.x, 0, moveInput.y);
        moveDirection = moveDirection.x * camera.transform.right + moveDirection.z * camera.transform.forward;
        //moveSpeed = moveSpeed + stats.Attributes.agility.GetValue(); //Maybe we should calculate this when setting up character stats //TODO
        
        // on slope
        if (OnSlope() && !exitingSlope)
        {
            rb.AddForce(GetSlopeMoveDirection() * (moveSpeed + stats.Attributes.agility.GetValue()) * 20f, ForceMode.Force);

            if (rb.velocity.y > 0)
                rb.AddForce(Vector3.down * 80f, ForceMode.Force);
        }

        // on ground
        else if (grounded)
            rb.AddForce(moveDirection.normalized * (moveSpeed + stats.Attributes.agility.GetValue()) * 10f, ForceMode.Force);

        // in air
        else if (!grounded)
            rb.AddForce(moveDirection.normalized * (moveSpeed + stats.Attributes.agility.GetValue()) * 10f * airMultiplier, ForceMode.Force);

        // turn gravity off while on slope
        rb.useGravity = !OnSlope();
    }
    private void HandleInput()
    {
        // when to value -- todo
        
        // when to jump
        if (playerInput.actions["Jump"].triggered && readyToJump && grounded)
        {
            readyToJump = false;

            Jump();

            Invoke(nameof(ResetJump), jumpCooldown);
        }

        // start crouch
        if (playerInput.actions["Crouch"].triggered && !isCrouching)
        {
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
            isCrouching = true;
        }

        // stop crouch
        if (playerInput.actions["Crouch"].triggered && isCrouching)
        {
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
            isCrouching = false;
        }
    }

    private void Jump() 
    {
        exitingSlope = true;

        // reset y velocity
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump() 
    {
        readyToJump = true;

        exitingSlope = false;
    }

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }

        return false;
    }

    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
    }
}
