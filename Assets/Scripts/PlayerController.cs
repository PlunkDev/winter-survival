using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerController : MonoBehaviour
{
    public TextMeshProUGUI debug;

    [Header("Player Stats")]
    public float health = 100f;
    public float stamina = 100f;
    public float hunger = 100f;
    public float thirst = 100f;
    public float sleep = 100f;


    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float sprintMultiplier = 2.5f;
    public float crouchSpeed = 1.5f;
    public float crouchHeight = 0.6f;
    public float standHeight = 2f;
    public float jumpForce = 4f;
    public float gravity = -20f;

    [Header("Mouse Settings")]
    public float mouseSensitivity = 150f;
    public float lookUpLimit = 80f;
    public float lookDownLimit = 80f;

    [Header("Audio Settings")]
    public AudioClip[] footstepSounds;
    public AudioClip[] jumpSounds;
    public AudioClip landImpactSound;
    public AudioClip[] landPlayerSounds;
    public AudioClip heavyBreathingSound;

    private AudioSource audioSource;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    private Transform playerCamera;
    private float xRotation = 0f;

    private bool isCrouching = false;

    private bool isWalking = false;
    private bool isJumping = false;
    private bool isPlayingFootstep = false;

    private float footstepDelay;
    private float footstepTime;

    private float staminaRecoveryTime = 5f;
    private bool isSprinting = false;
    private bool isBreathingHeavy = false;

    private float sprintStaminaCost = 10f;
    private float JumpStaminaCost = 15f;
    private float recoveryRate = 5f;

    private float idleTime = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
        playerCamera = Camera.main.transform;
        Cursor.lockState = CursorLockMode.Locked;

        footstepDelay = 0.5f;

        GameManager.playerController = this;
    }

    void Update()
    {
        HandleMovement();
        HandleMouseLook();
        HandleCrouch();
        HandleStamina();
        HandleBreathing();

        // Wyœwietlanie debugowych informacji
        debug.text = $"FPS: {1f / Time.unscaledDeltaTime}\n" +
            $"Time: N/A\n\n" +
            $"Health: {health}\n" +
            $"Stamina: {stamina}\n" +
            $"Hunger: {hunger}\n" +
            $"Thirst: {thirst}\n" +
            $"Sleep: {sleep} \n\n" +
            $"Debug UI";
    }


    void HandleMovement()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        float currentMoveSpeed = moveSpeed;
        isSprinting = Input.GetKey(KeyCode.LeftShift) && !isCrouching && stamina > 0;

        if (isSprinting)
        {
            currentMoveSpeed *= sprintMultiplier;
            footstepDelay = 0.5f;
            stamina -= sprintStaminaCost * Time.deltaTime;

            // Sprawdzenie, czy stamina spad³a do 0
            if (stamina <= 0f)
            {
                stamina = 0f;
                isSprinting = false;  // Zatrzymanie sprintu, gdy stamina jest wyczerpana
            }
        }
        else if (isCrouching)
        {
            footstepDelay = 1f;
        }
        else
        {
            footstepDelay = 0.75f;
        }

        isWalking = moveX != 0 || moveZ != 0;

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        controller.Move(move * currentMoveSpeed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded && !isJumping && stamina > 15)
        {
            isJumping = true;
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            stamina -= JumpStaminaCost;
            PlayJumpSound();
        }
        else if (Input.GetButtonDown("Jump") && isGrounded && !isJumping && stamina < 15)
        {
            isJumping = true;
            velocity.y = Mathf.Sqrt(1f * -2f * gravity);
            PlayJumpSound();
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        if (isGrounded && velocity.y < 0)
        {
            if (isJumping)
            {
                PlayLandSound();
                isJumping = false;
            }
        }

        if (isWalking && !isJumping && !isPlayingFootstep)
        {
            footstepTime += Time.deltaTime;
            if (footstepTime >= footstepDelay)
            {
                PlayFootstepSound();
                footstepTime = 0f;
            }
        }
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -lookDownLimit, lookUpLimit);

        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleCrouch()
    {
        if (Input.GetKey(KeyCode.C))
        {
            if (!isCrouching)
            {
                isCrouching = true;
                controller.height = crouchHeight;
                playerCamera.localPosition = new Vector3(playerCamera.localPosition.x, crouchHeight / 2, playerCamera.localPosition.z);
            }
        }
        else
        {
            if (isCrouching)
            {
                isCrouching = false;
                controller.height = standHeight;
                playerCamera.localPosition = new Vector3(playerCamera.localPosition.x, standHeight / 2, playerCamera.localPosition.z);
            }
        }
    }

    void HandleStamina()
    {
        float hungerPercentage = hunger / 100f;
        float thirstPercentage = thirst / 100f;
        float healthPercentage = health / 100f;
        float sleepPercentage = sleep / 100f;

        float averagePercentage = (hungerPercentage + thirstPercentage + healthPercentage + sleepPercentage) / 4f;

        float maxStamina = Mathf.Max(25f, averagePercentage * 100f);

        if (!isSprinting && !isJumping)
        {
            idleTime += Time.deltaTime;

            if (idleTime >= staminaRecoveryTime)
            {
                stamina += recoveryRate * Time.deltaTime;
            }
        }
        else
        {
            idleTime = 0f;
        }

        stamina = Mathf.Clamp(stamina, 0f, maxStamina);
    }

    void HandleBreathing()
    {
        if (stamina <= 10f && !isBreathingHeavy)
        {
            isBreathingHeavy = true;
            audioSource.PlayOneShot(heavyBreathingSound);
        }
        else if (stamina > 10f && isBreathingHeavy)
        {
            isBreathingHeavy = false;
        }
    }



    void PlayFootstepSound()
    {
        if (!isPlayingFootstep)
        {
            isPlayingFootstep = true;
            AudioClip footstepClip = footstepSounds[Random.Range(0, footstepSounds.Length)];
            audioSource.PlayOneShot(footstepClip);
            isPlayingFootstep = false;
        }
    }

    void PlayJumpSound()
    {
        AudioClip jumpClip = jumpSounds[Random.Range(0, jumpSounds.Length)];
        audioSource.PlayOneShot(jumpClip);
    }

    void PlayLandSound()
    {
        audioSource.PlayOneShot(landImpactSound);
        AudioClip landPlayerClip = landPlayerSounds[Random.Range(0, landPlayerSounds.Length)];
        audioSource.PlayOneShot(landPlayerClip);
    }
}