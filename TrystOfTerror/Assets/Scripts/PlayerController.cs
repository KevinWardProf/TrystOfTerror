using Assets.Scripts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.HID;
using UnityEngine.Pool;

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

    [Header("Looking")]
    public float xRotation;
    public float yRotation;
    public float mouseSensitivity;
    public GameObject head;
    public float lookSpeed;
    GameObject camera;

    [Header("Jumping")]
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    bool readyToJump;

    [Header("Crouching")]
    public float crouchSpeed;
    public float crouchYScale;
    private float startYScale;
    public bool isCrouching;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    public bool grounded;

    [Header("Slope Handling")]
    public float maxSlopeAngle;
    private RaycastHit slopeHit;
    private bool exitingSlope;

    [Header("Grabbing")]
    public Transform BothHandGrabPos;
    public Transform RightHandGrabPos;
    public Transform LeftHandGrabPos;
    //Throw Force is Calculated in stats
    //pickUpRange is Calculated in stats
    [Tooltip("Whether we want to use raw value or the calculated throw force.")]
    public bool rawPower;
    //grabbedObjectRotationSpeed is Calculated in stats
    public bool IsObjectInBothHand;
    public bool IsObjectInRightHand;
    public bool IsObjectInLeftHand;
    [SerializeField]
    private GameObject currentlyGrabbedObject; 
    public GameObject ObjectGrabbedInBothHands;
    public GameObject ObjectGrabbedInRightHand;
    public GameObject ObjectGrabbedInLeftHand;
    public Rigidbody currentlyGrabbedObjectRigidbody;
    public bool canDrop;
    public enum HandsState { lowered, raised };
    public HandsState handsState;

    [Header("Statistics")]
    public int health;
    public enum MoraleState { bold, determined, calm, frightened, panicked }
    public MoraleState moraleState;
    public CharacterStats stats;
    //Inputs
    private PlayerInput playerInput;

    [Header("Misc")]
    public Collider col;
    Vector3 moveDirection;
    public Transform orientation;
    [HideInInspector] public Rigidbody rb;


    private void Start()
    {
        stats = new CharacterStats(); //Todo add settings list ref/character creation??
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        readyToJump = true;
        startYScale = 1f;
        camera = GameObject.FindWithTag("MainCamera");
        playerInput = GetComponent<PlayerInput>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = true;
    }

    private void Update()
    {
        // ground check
        RaycastHit hitInfo;
        Color rayColor;
        //grounded = !(Physics.Raycast(col.transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround));
        grounded = Physics.Raycast(col.bounds.center, Vector3.down, out hitInfo, col.bounds.extents.y + 5f, whatIsGround);
        if (grounded)
        {
            rayColor = Color.green;
        }
        else
        {
            rayColor = Color.red;
        }
        Debug.DrawRay(col.bounds.center, Vector3.down * (col.bounds.extents.y + 5f), rayColor);

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
        HandleLook();
    }

    private void MovementStateHandler()
    {
        // Mode - Crouching
        if (playerInput.actions["Crouch"].ReadValue<float>() == 1)
        {
            movementState = MovementState.crouching;
            moveSpeed = crouchSpeed;
        }

        // Mode - Walking
        else if (grounded && playerInput.actions["Walk"].ReadValue<float>() == 1)
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
        Vector2 moveInput = playerInput.actions["Move"].ReadValue<Vector2>();
        Vector3 moveDirection = new Vector3(moveInput.x, 0, moveInput.y);
        //moveDirection = moveDirection.x * camera.transform.right + moveDirection.z * camera.transform.forward; //Unless we are flying, this does not work.
        //moveSpeed = moveSpeed + stats.Attributes.agility.GetValue(); //Maybe we should calculate this when setting up character stats //TODO
        moveDirection = orientation.forward * moveInput.y + orientation.right * moveInput.x;

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

    private void HandleLook()
    {
        camera.transform.position = head.transform.position;
        Physics.Raycast(camera.transform.position, Vector3.forward, camera.transform.position.y + 5f);
        Debug.DrawRay(camera.transform.position, Vector3.forward * (camera.transform.position.y + 5f), Color.blue);

        Vector2 lookInput = playerInput.actions["Look"].ReadValue<Vector2>();

        yRotation += lookInput.x * Time.deltaTime * mouseSensitivity;
        xRotation -= lookInput.y * Time.deltaTime * mouseSensitivity;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        head.transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        camera.transform.rotation = Quaternion.Lerp(camera.transform.rotation, head.transform.rotation, Time.deltaTime * lookSpeed);
        orientation.rotation = Quaternion.Euler(0, yRotation, 0);
        //orientation.transform.Rotate(new Vector3(0, yRotation,0) * lookSpeed, Space.World);
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

        //Crouch

        // start crouch
        if (playerInput.actions["Crouch"].ReadValue<float>() == 1)
        {
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
            if (isCrouching == false)
            {
                rb.AddForce(Vector3.down * 1f, ForceMode.Impulse);
                isCrouching = true;
            }
        }
        else // stop crouch 
        {
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
            isCrouching = false;
        }

        //Handle Grabbing
        //Two Hands
        if ((handsState == HandsState.lowered && playerInput.actions["RightHandHeavy"].triggered)
            && (handsState == HandsState.lowered && playerInput.actions["LeftHandHeavy"].triggered))
        {
            //Check if Both Hand Obj are not occupied
            GrabObject(IsObjectInBothHand, stats, false, false, true);
        }
        //Right Hand
        else if (handsState == HandsState.lowered && playerInput.actions["RightHandHeavy"].triggered)
        {
            //Check if Right Hand Obj occupied
            GrabObject(IsObjectInRightHand, stats, true, false, false);

        }
        //Left Hand
        else if (handsState == HandsState.lowered && playerInput.actions["LeftHandHeavy"].triggered)
        {
            //Check if Left Hand Obj occupied
            GrabObject(IsObjectInLeftHand, stats, false, true, false);
        }
    }

    private void GrabObject(bool ObjectInHand, CharacterStats stats, bool isRightHand, bool isLeftHand, bool isBothHands)
    {
        if (!ObjectInHand)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, stats.grabRange))
            {
                if (hit.transform.gameObject.tag == "Prop")
                {
                    if (isBothHands && hit.transform.gameObject.GetComponent<Prop>().needsTwoHandsToPickUp)
                    {
                        HoldObject(hit.transform.gameObject, isRightHand, isLeftHand, isBothHands);
                    }
                    else
                    {
                        if (isRightHand)
                        {
                            HoldObject(hit.transform.gameObject, isRightHand, isLeftHand, isBothHands);
                        }
                        else if (isLeftHand)
                        {
                            HoldObject(hit.transform.gameObject, isRightHand, isLeftHand, isBothHands);
                        }
                    }
                }
            }
        }
        else
        {
            if (canDrop)
            {
                //So we have a problem, how do know which object to drop
                //When I call HoldObject, we the grabbed object as the grabbedObject
                //This is unnecessary and we can remove references to it, but here we
                //we need three containers to 'store' the obejct as a referenceble object
                //Also maybe we should store/pass the rigidbody component, rather than constantly
                //access it each time that we need to manipulate it
                if (isBothHands)
                {
                    //StopClipping();
                    DropObject(ObjectGrabbedInBothHands, ref isBothHands);
                }
                else if (isRightHand)
                {
                    //StopClipping();
                    DropObject(ObjectGrabbedInRightHand, ref isRightHand);
                }
                else if (isLeftHand) 
                {
                    //StopClipping();
                    DropObject(ObjectGrabbedInLeftHand, ref isLeftHand);
                }
            }
        }
    }
    private void HoldObject(GameObject grabbedObject, bool isRight, bool isLeft, bool isBothHands)
    {
        currentlyGrabbedObject = grabbedObject;
        currentlyGrabbedObjectRigidbody = currentlyGrabbedObject.GetComponent<Rigidbody>(); //assign Rigidbody
        currentlyGrabbedObjectRigidbody.isKinematic = true;
        if (isBothHands)
        {
            currentlyGrabbedObjectRigidbody.transform.parent = BothHandGrabPos.transform; //parent object to holdposition
        }
        else if (isRight)
        {
            currentlyGrabbedObjectRigidbody.transform.parent = RightHandGrabPos.transform; //parent object to holdposition
        }
        else if (isLeft)
        {
            currentlyGrabbedObjectRigidbody.transform.parent = LeftHandGrabPos.transform; //parent object to holdposition
        }
        currentlyGrabbedObject.layer = LayerMask.NameToLayer("holdLayer"); //change the object layer to the holdLayer
                                                                  //make sure object doesnt collide with player, it can cause weird bugs
        Physics.IgnoreCollision(currentlyGrabbedObject.GetComponent<Collider>(), transform.GetComponent<Collider>(), true);
    }
    private void DropObject(GameObject grabbedObject, ref bool objectInHand)
    {
        currentlyGrabbedObject = grabbedObject;
        currentlyGrabbedObjectRigidbody = currentlyGrabbedObject.GetComponent<Rigidbody>(); //assign Rigidbody
        Physics.IgnoreCollision(currentlyGrabbedObject.GetComponent<Collider>(), transform.GetComponent<Collider>(), true);
        currentlyGrabbedObjectRigidbody.isKinematic = false;
        currentlyGrabbedObjectRigidbody.transform.parent = null;
        currentlyGrabbedObject.layer = 0; //change the object layer to the holdLayer
        currentlyGrabbedObject = null;
        objectInHand = false;
    }
    void StopClipping(GameObject grabbedObject) //function only called when dropping/throwing
    {
        var clipRange = Vector3.Distance(grabbedObject.transform.position, transform.position); //distance from holdPos to the camera
        //have to use RaycastAll as object blocks raycast in center screen
        //RaycastAll returns array of all colliders hit within the cliprange
        RaycastHit[] hits;
        hits = Physics.RaycastAll(transform.position, transform.TransformDirection(Vector3.forward), clipRange);
        //if the array length is greater than 1, meaning it has hit more than just the object we are carrying
        if (hits.Length > 1)
        {
            //change object position to camera position 
            grabbedObject.transform.position = transform.position + new Vector3(0f, -0.5f, 0f); //offset slightly downward to stop object dropping above player 
            //if your player is small, change the -0.5f to a smaller number (in magnitude) ie: -0.1f
        }
    }
    void ThrowObject(GameObject grabbedObject, ref bool objectInHand, CharacterStats stats)
    {
        //same as drop function, but add force to object before undefining it
        currentlyGrabbedObject = grabbedObject;
        currentlyGrabbedObjectRigidbody = currentlyGrabbedObject.GetComponent<Rigidbody>(); //assign Rigidbody
        Physics.IgnoreCollision(currentlyGrabbedObject.GetComponent<Collider>(), GetComponent<Collider>(), true);
        currentlyGrabbedObjectRigidbody.isKinematic = false;
        currentlyGrabbedObjectRigidbody.transform.parent = null;
        currentlyGrabbedObject.layer = 0; //change the object layer to the holdLayer
        currentlyGrabbedObject = null;
        currentlyGrabbedObjectRigidbody.AddForce(transform.forward * stats.throwForce);
        objectInHand = false;
    }
    void RotateObject(GameObject grabbedObject, bool isBothHands, bool isRightHand, bool isLeftHand)
    {
        //if (playerInput.actions[""]) 
        //{ }

        if (Input.GetKey(KeyCode.R))//hold R key to rotate, change this to whatever key you want
        {
            canDrop = false; //make sure throwing can't occur during rotating

            //disable player being able to look around
            //mouseLookScript.verticalSensitivity = 0f;
            //mouseLookScript.lateralSensitivity = 0f;

            //float XaxisRotation = Input.GetAxis("Mouse X") * rotationSensitivity;
            //float YaxisRotation = Input.GetAxis("Mouse Y") * rotationSensitivity;
            //rotate the object depending on mouse X-Y Axis
            //grabbedObject.transform.Rotate(Vector3.down, XaxisRotation);
            //grabbedObject.transform.Rotate(Vector3.right, YaxisRotation);
        }
        else
        {
            //re-enable player being able to look around
            //mouseLookScript.verticalSensitivity = originalvalue;
            //mouseLookScript.lateralSensitivity = originalvalue;
            canDrop = true;
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
