using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {
    public Camera player_camera;
    public float maxSpeed;
    public float RunSpeed;
    public float AirSpeed;
    public float GroundAcceleration;
    public float AirAcceleration;
    public float SpeedDamp;
    public float AirSpeedDamp;
    public float slideMultiplier;
    public CharacterController cc;

    public float DownGravityAdd;
    public float ShortHopGravityAdd;
    public float JumpVelocity;
    public Vector3 StartPos;

    private bool isJumping;
    private bool isFalling;
    private bool canJump;
    private bool willJump;
    private float LandingTimeDelta;
    private float jumpGracePeriod;
    private float BufferJumpTimeDelta;
    private float BufferJumpGracePeriod;

    private Vector3 current_velocity;
    private Vector3 accel;
    private Vector3 current_slide;
    private float GravityMult;

    // Use this for initialization
    void Start () {
        // Movement values
        maxSpeed = 4;
        RunSpeed = 3;
        AirSpeed = 0.25f;
        GroundAcceleration = 20;
        AirAcceleration = 300;
        SpeedDamp = 10f;
        AirSpeedDamp = 0.01f;
        slideMultiplier = 1;

        // Gravity modifiers
        DownGravityAdd = 0;
        ShortHopGravityAdd = 0;
        
        // Jump states/values
        JumpVelocity = 4;
        isJumping = false;
        isFalling = false;
        canJump = true;
        willJump = false;
        // Jump timers for early/late jumps
        jumpGracePeriod = 0.3f;
        LandingTimeDelta = 0;
        BufferJumpGracePeriod = 0.1f;
        BufferJumpTimeDelta = 0;

        // Initial state
        current_velocity = Vector3.zero;
        current_slide = Vector3.zero;
        StartPos = new Vector3(0.5f, 1.5f, 0.5f);
        transform.position = StartPos;
    }
	
	// Update is called once per frame
	void Update () {
        // Get starting values
        current_velocity = cc.velocity;
        GravityMult = 1;
        LandingTimeDelta = Mathf.Clamp(LandingTimeDelta + Time.deltaTime, 0, 2*jumpGracePeriod);
        BufferJumpTimeDelta = Mathf.Clamp(BufferJumpTimeDelta + Time.deltaTime, 0, 2*BufferJumpGracePeriod);
        Debug.Log("Current velocity: " + Vector3.ProjectOnPlane(current_velocity, transform.up).magnitude.ToString());
        Debug.Log("Velocity error: " + (current_velocity - cc.velocity).ToString());
        accel = current_slide;
        current_slide = Vector3.zero;

        HandleMovement();

        HandleJumping();

        // Update character state based on desired movement
        accel += Physics.gravity * GravityMult;
        current_velocity += accel * Time.deltaTime;
        cc.Move(current_velocity * Time.deltaTime);
	}

    // Apply movement forces from input (FAST edition)
    private void HandleMovement()
    {
        Vector3 movVec = Input.GetAxisRaw("Vertical") * transform.forward + Input.GetAxisRaw("Horizontal") * transform.right;
        Debug.DrawRay(transform.position, Vector3.ProjectOnPlane(current_velocity, transform.up).normalized, Color.green, 0);
        Debug.DrawRay(transform.position, movVec.normalized, Color.blue, 0);

        if (OnGround())
        {
            AccelerateTo(movVec, RunSpeed, GroundAcceleration);
            if (!willJump)
            {
                accel += -current_velocity * SpeedDamp;
            }
        }
        else
        {
            AccelerateTo(movVec, AirSpeed, AirAcceleration);
            accel += -Vector3.ProjectOnPlane(current_velocity, transform.up) * AirSpeedDamp;
        }
    }

    // Try to accelerate to the desired speed in the direction specified
    private void AccelerateTo(Vector3 direction, float desiredSpeed, float acceleration)
    {
        direction.Normalize();
        float moveAxisSpeed = Vector3.Dot(current_velocity, direction);
        float deltaSpeed = desiredSpeed - moveAxisSpeed;
        if (deltaSpeed < 0)
        {
            // Gotta go fast
            return;
        }

        // Scale acceleration by speed because we want to go fast
        deltaSpeed = Mathf.Clamp(acceleration * Time.deltaTime * desiredSpeed, 0, deltaSpeed);
        current_velocity += deltaSpeed * direction;
    }

    // Handle jumping
    private void HandleJumping()
    {
        // Ground detection for friction and jump state
        if (OnGround())
        {
            isJumping = false;
            isFalling = false;
        }
        else
        {
            //accel += -current_velocity * AirSpeedDamp;
        }

        // Add additional gravity when going down
        if (current_velocity.y < 0)
        {
            GravityMult += DownGravityAdd;
        }

        // Handle jumping and falling
        if (Input.GetButtonDown("Jump") || Mathf.Abs(Input.GetAxis("Mouse ScrollWheel")) > 0 || willJump)
        {
            BufferJumpTimeDelta = 0;
            if (OnGround())
            {
                DoJump();
            }
        }
        if (isFalling || isJumping && !Input.GetButton("Jump"))
        {
            GravityMult += ShortHopGravityAdd;
            isFalling = true;
        }
    }

    // Double check if on ground using a separate canJump test
    private bool OnGround()
    {
        canJump = canJump && (LandingTimeDelta < jumpGracePeriod);
        return canJump;
    }

    // Set the player to a jumping state
    private void DoJump()
    {
        current_velocity.y = JumpVelocity;
        isJumping = true;
        canJump = false;
        willJump = false;

        // Intentionally set the timer over the limit
        BufferJumpTimeDelta = BufferJumpGracePeriod;
    }

    // Handle collisions on player move
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // isGrounded doesn't work properly on slopes, replace with this.
        if (hit.normal.y > 0.5)
        {
            Debug.DrawRay(transform.position, hit.normal, Color.red, 10);
            canJump = true;
            LandingTimeDelta = 0;

            // Handle buffered jumps
            if (BufferJumpTimeDelta <  BufferJumpGracePeriod)
            {
                // Defer the jump so that it happens in update
                willJump = true;
            }
        }
        // Use this for detecting slopes to slide down
        else
        {
            Vector3 slideVec = Vector3.ProjectOnPlane(Physics.gravity, hit.normal);
            float slideC = 1f; //Mathf.Abs(Vector3.Dot(hit.normal, transform.right));
            current_slide = slideVec * slideMultiplier * slideC / (hit.collider.material.staticFriction + 1);
        }
        Debug.Log(hit.gameObject.tag);
        if (hit.gameObject.tag == "Respawn")
        {
            Debug.Log("we got here");
            StartCoroutine(DeferedTeleport(StartPos));
            //GetComponent<Collider>().enabled = true;
        }
    }

    // Teleport coroutine (needed due to bug in character controller teleport)
    IEnumerator DeferedTeleport(Vector3 position)
    {
        yield return new WaitForEndOfFrame();
        transform.position = position;
    }
}
