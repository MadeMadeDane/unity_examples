﻿using System;
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
    public float SlideSpeed;
    private float SlideGracePeriod;
    private float SlideTimeDelta;
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

    private float _input_vertical_axis;
    private float _input_horizontal_axis;
    private bool _input_jump_button;
    private bool _input_jump_buttondown;
    private float _input_scroll_axis;

    private Vector3 current_velocity;
    private Vector3 accel;
    private ControllerColliderHit lastHit;
    private float GravityMult;

    // Use this for initialization
    void Start () {
        // Movement values
        maxSpeed = 4;
        RunSpeed = 3;
        AirSpeed = 0.30f;
        GroundAcceleration = 20;
        AirAcceleration = 500;
        SpeedDamp = 10f;
        AirSpeedDamp = 0.01f;
        SlideGracePeriod = 0.2f;
        SlideTimeDelta = SlideGracePeriod;
        SlideSpeed = 4f;

        // Gravity modifiers
        DownGravityAdd = 0;
        ShortHopGravityAdd = 0;
        
        // Jump states/values
        JumpVelocity = 4;
        isJumping = false;
        isFalling = false;
        canJump = false;
        willJump = false;
        // Jump timers for early/late jumps
        jumpGracePeriod = 0.1f;
        LandingTimeDelta = 0;
        BufferJumpGracePeriod = 0.1f;
        BufferJumpTimeDelta = BufferJumpGracePeriod;

        // Initial state
        _input_vertical_axis = 0f;
        _input_horizontal_axis = 0f;
        _input_jump_button = false;
        _input_jump_buttondown = false;
        _input_scroll_axis = 0f;

        current_velocity = Vector3.zero;
        StartPos = new Vector3(0.5f, 1.5f, 0.5f);
        transform.position = StartPos;
    }

    // Update is called once per frame
    private void Update()
    {
        GetInputs();
    }

    private void GetInputs()
    {
        _input_vertical_axis = Input.GetAxisRaw("Vertical");
        _input_horizontal_axis = Input.GetAxisRaw("Horizontal");
        _input_jump_button = Input.GetButton("Jump");
        _input_jump_buttondown = Input.GetButtonDown("Jump");
        _input_scroll_axis = Input.GetAxis("Mouse ScrollWheel");
    }

    // Fixed Update is called once per physics tick
    void FixedUpdate () {
        // Get starting values
        GravityMult = 1;
        //Debug.Log("Current velocity: " + Vector3.ProjectOnPlane(current_velocity, transform.up).magnitude.ToString());
        Debug.Log("Velocity error: " + (current_velocity - cc.velocity).ToString());
        accel = Vector3.zero;

        HandleMovement();

        HandleJumping();

        // Update character state based on desired movement
        accel += Physics.gravity * GravityMult;
        current_velocity += accel * Time.deltaTime;
        cc.Move(current_velocity * Time.deltaTime);

        // Increment timers
        LandingTimeDelta = Mathf.Clamp(LandingTimeDelta + Time.deltaTime, 0, 2 * jumpGracePeriod);
        SlideTimeDelta = Mathf.Clamp(SlideTimeDelta + Time.deltaTime, 0, 2 * SlideGracePeriod);
        BufferJumpTimeDelta = Mathf.Clamp(BufferJumpTimeDelta + Time.deltaTime, 0, 2 * BufferJumpGracePeriod);
    }

    // Apply movement forces from input (FAST edition)
    private void HandleMovement()
    {
        Vector3 planevelocity;
        Vector3 movVec = _input_vertical_axis * transform.forward + _input_horizontal_axis * transform.right;
        // Do this first so we cancel out incremented time from update before checking it
        if (!OnGround())
        {
            // We are in the air (for atleast LandingGracePeriod). We will slide on landing if moving fast enough.
            SlideTimeDelta = 0;
            planevelocity = current_velocity;
        }
        else
        {
            planevelocity = Vector3.ProjectOnPlane(current_velocity, lastHit.normal);
        }
        // Normal ground behavior
        if (OnGround() && !willJump && (SlideTimeDelta >= SlideGracePeriod || planevelocity.magnitude < SlideSpeed))
        {
            // If we weren't fast enough we aren't going to slide
            SlideTimeDelta = SlideGracePeriod;
            // Use character controller grounded check to be certain we are actually on the ground
            if (lastHit != null && cc.isGrounded)
            {
                Debug.Log("We are on the ground");
                movVec = Vector3.ProjectOnPlane(movVec, lastHit.normal);
            }
            AccelerateTo(movVec, RunSpeed, GroundAcceleration);
            accel += -current_velocity * SpeedDamp;
        }
        // We are either in the air, buffering a jump, or sliding (recent contact with ground). Use air accel.
        else
        {
            AccelerateTo(movVec, AirSpeed, AirAcceleration);
            accel += -Vector3.ProjectOnPlane(current_velocity, transform.up) * AirSpeedDamp;
        }
        Debug.DrawRay(transform.position, Vector3.ProjectOnPlane(current_velocity, transform.up).normalized, Color.green, 0);
        Debug.DrawRay(transform.position, movVec.normalized, Color.blue, 0);
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

        // Add additional gravity when going down (optional)
        if (current_velocity.y < 0)
        {
            GravityMult += DownGravityAdd;
        }

        // Handle jumping and falling
        if (_input_jump_buttondown || Mathf.Abs(_input_scroll_axis) > 0 || willJump)
        {
            BufferJumpTimeDelta = 0;
            if (OnGround())
            {
                DoJump();
            }
        }
        // Fall fast when we let go of jump (optional)
        if (isFalling || isJumping && !_input_jump_button)
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
        // Conserve velocity along plane, zero it out on the normal
        lastHit = hit;
        //Debug.Log("Hit normal: " + hit.normal);
        // isGrounded doesn't work properly on slopes, replace with this.
        if (hit.normal.y > 0.6)
        {
            //Debug.Log("On the ground");
            Debug.DrawRay(transform.position, hit.normal, Color.red, 100);
            canJump = true;
            LandingTimeDelta = 0;

            // Handle buffered jumps
            if (BufferJumpTimeDelta < BufferJumpGracePeriod)
            {
                // Defer the jump so that it happens in update
                willJump = true;
            }
        }
        // Use this for detecting slopes to slide down
        else
        {
            Debug.Log("On a slide");
            if (Vector3.Dot(current_velocity, hit.normal) < 0)
            {
                current_velocity = Vector3.ProjectOnPlane(current_velocity, hit.normal);
            }
        }
        if (hit.gameObject.tag == "Respawn")
        {
            StartCoroutine(DeferedTeleport(StartPos));
        }
    }

    // Teleport coroutine (needed due to bug in character controller teleport)
    IEnumerator DeferedTeleport(Vector3 position)
    {
        yield return new WaitForEndOfFrame();
        transform.position = position;
    }
}
