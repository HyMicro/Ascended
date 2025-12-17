using UnityEngine;
using System.Collections.Generic;

public class PlayerMovement : MonoBehaviour
{
    // Komponen 2D
    private Rigidbody2D rb;
    private float horizontalInput;

    // --- Movement Settings ---
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 6f;

    // --- Air Control ---
    [Header("Air Control")]
    [SerializeField] private float airAcceleration = 20f; 

    // --- Ground Check Settings ---
    [Header("Ground Check")]
    [SerializeField] private Transform groundCheckPoint; 
    [SerializeField] private float groundCheckRadius = 0.2f; 
    [SerializeField] private LayerMask groundLayer; 
    private bool isGrounded; 

    // --- Jump Charge Settings ---
    [Header("Jump Charge Settings")]
    [SerializeField] private float maxJumpPower = 15f; 
    [SerializeField] private float minJumpPower = 5f;  
    [SerializeField] private float maxChargeTime = 1.5f; 
    private float currentChargeTime; 
    private bool isCharging = false; 
    private Vector2 jumpDirection = Vector2.up; 

    // --- Jump Visualization ---
    [Header("Jump Visualization")]
    [SerializeField] private LineRenderer LineRenderer;
    [SerializeField] private float maxAngle = 60f;
    [SerializeField] private float visualLineScale = 0.3f; 

    // --- Wall Interaction Settings ---
    [Header("Wall Interaction")]
    [SerializeField] private Transform wallCheckPoint; 
    [SerializeField] private float wallCheckDistance = 0.5f; 
    [SerializeField] private float wallSlideSpeed = 1f; 
    [SerializeField] private float wallJumpForce = 12f; 
    [SerializeField] private LayerMask wallLayer;

    // --- Grapple Settings ---
    [Header("Grapple Ability")]
    [SerializeField] private float grappleRange = 15f; 
    [SerializeField] private float grappleSpeed = 30f; 
    [SerializeField] private float grappleCooldown = 2f; 
    [SerializeField] private LayerMask grappleLayer; 
    
    private float grappleCooldownTimer;
    private bool isGrappling = false;
    private Vector2 grappleTargetPosition; 

    // --- Checkpoint System ---
    private Vector3 currentCheckpointPosition;

    // Batasan Durasi Wall Grab/Slide
    [SerializeField] private float maxWallTime = 1.5f; 
    private float currentWallTime; 

    // State
    private bool isWallDetected;
    private int wallSide; 
    private bool isWallSliding;
    private bool canWallGrab = true; 

    // --- Progression & Fall Behavior ---
    [Header("Progression & Fall Behavior")]
    public List<BiomeData> towerBiomes; 
    private int currentBiomeIndex = 0; 

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody2D component not found on Player!");
        }
        currentWallTime = maxWallTime; 
        grappleCooldownTimer = 0f;

        // Set Checkpoint awal di posisi start
        currentCheckpointPosition = transform.position;
        
        if (towerBiomes == null || towerBiomes.Count == 0)
        {
             Debug.LogWarning("Tower Biomes List is empty! Please fill the data in the Inspector.");
        }
    }

    void Update()
    {
        CheckIfGrounded(); 
        CheckWallDetection(); 

        HandleHorizontalInput();
        FlipPlayer(); 
        HandleGrappleInput(); 
        
        if (!isGrappling) 
        {
            HandleJumpInput(); 
        }

        HandleFallConsequence();
        UpdateBiomeTransition(); 

        // Logic Charging Visuals
        if (isCharging)
        {
            currentChargeTime += Time.deltaTime;
            currentChargeTime = Mathf.Clamp(currentChargeTime, 0f, maxChargeTime); 
            UpdateJumpDirection();
            UpdateJumpVisualization();

            if (currentChargeTime >= maxChargeTime)
            {
                ReleaseJump(); 
            }
        }
        else if (LineRenderer.enabled)
        {
            LineRenderer.enabled = false;
        }
    }

    void FixedUpdate()
    {
        if (isGrappling)
        {
            ApplyGrapple();
            return;
        }

        WallSlide(); 
        
        if (!isWallSliding) 
        {
            ApplyMovement();
        }
    }

    // --- Checkpoint & Ground Check ---
    private void CheckIfGrounded()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayer);
        
        if (isGrounded)
        {
            currentWallTime = maxWallTime;
            canWallGrab = true;

            // Update Checkpoint jika mendarat di posisi yang lebih tinggi
            if (transform.position.y > currentCheckpointPosition.y + 1f)
            {
                currentCheckpointPosition = transform.position;
            }
        }
    }

    // --- Grapple Logic ---
    private void HandleGrappleInput()
    {
        if (grappleCooldownTimer > 0) grappleCooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Q) && grappleCooldownTimer <= 0 && !isGrappling && !isWallSliding)
        {
            TryGrapple();
        }
    }

    private void TryGrapple()
    {
        Vector2 startPosition = transform.position;
        float aimX = transform.localScale.x;
        Vector2 fireDirection = new Vector2(aimX, 0.6f).normalized; 

        RaycastHit2D hit = Physics2D.Raycast(startPosition, fireDirection, grappleRange, grappleLayer);

        if (hit.collider != null)
        {
            isGrappling = true;
            grappleTargetPosition = hit.point;
            if (LineRenderer.enabled) LineRenderer.enabled = false;
            rb.gravityScale = 0; 
            Debug.DrawRay(startPosition, hit.point - (Vector2)startPosition, Color.green, 1f);
        }
        else
        {
            Debug.DrawRay(startPosition, fireDirection * grappleRange, Color.red, 0.5f);
        }
    }

    private void ApplyGrapple()
    {
        Vector2 targetVector = grappleTargetPosition - (Vector2)transform.position;
        float distance = targetVector.magnitude;

        if (distance < 0.5f)
        {
            isGrappling = false;
            grappleCooldownTimer = grappleCooldown; 
            rb.linearVelocity = Vector2.zero; 
            rb.gravityScale = 1; 
            return;
        }

        rb.linearVelocity = targetVector.normalized * grappleSpeed;
    }

    // --- Movement & Jump ---
    private void HandleHorizontalInput() => horizontalInput = Input.GetAxisRaw("Horizontal");

    private void FlipPlayer()
    {
        if (isWallSliding)
        {
            if (transform.localScale.x != wallSide) transform.localScale = new Vector3(wallSide, 1, 1);
        }
        else if (horizontalInput != 0)
        {
            if (transform.localScale.x != horizontalInput) transform.localScale = new Vector3(horizontalInput, 1, 1);
        }
    }

    private void HandleJumpInput()
    {
        if (isWallSliding && Input.GetButtonDown("Jump") && canWallGrab) 
        {
            isCharging = false;
            LineRenderer.enabled = false;
            WallPushJump(); 
            return; 
        }

        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            isCharging = true;
            currentChargeTime = 0f;
            LineRenderer.enabled = true; 
        }
        
        if (Input.GetButtonUp("Jump") && isCharging) ReleaseJump();
    }

    private void ReleaseJump()
    {
        isCharging = false;
        LineRenderer.enabled = false; 
        float chargePercent = currentChargeTime / maxChargeTime;
        float finalJumpPower = Mathf.Lerp(minJumpPower, maxJumpPower, chargePercent);
        
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f); 
        rb.linearVelocity = jumpDirection * finalJumpPower; 
    }

    private void ApplyMovement()
    {
        float targetSpeed = horizontalInput * walkSpeed;
        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(targetSpeed, rb.linearVelocity.y);
        }
        else 
        {
            float speedDif = targetSpeed - rb.linearVelocity.x;
            float force = speedDif * airAcceleration;
            rb.AddForce(Vector2.right * force, ForceMode2D.Force); 
        }
    }

    // --- Wall Interaction ---
    private void CheckWallDetection()
    {
        if (wallCheckPoint == null) return; 
        RaycastHit2D hitRight = Physics2D.Raycast(wallCheckPoint.position, Vector2.right, wallCheckDistance, wallLayer);
        RaycastHit2D hitLeft = Physics2D.Raycast(wallCheckPoint.position, Vector2.left, wallCheckDistance, wallLayer);

        if (hitRight.collider != null) { isWallDetected = true; wallSide = 1; }
        else if (hitLeft.collider != null) { isWallDetected = true; wallSide = -1; }
        else isWallDetected = false;
    }

    private void WallSlide()
    {
        isWallSliding = isWallDetected && !isGrounded && (horizontalInput == wallSide);
        if (isWallSliding && canWallGrab) 
        {
            currentWallTime -= Time.deltaTime; 
            if (currentWallTime <= 0f) canWallGrab = false;
            if (rb.linearVelocity.y < -wallSlideSpeed) rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed); 
        }
    }

    private void WallPushJump()
    {
        Vector2 jumpVector = new Vector2(-wallSide * wallJumpForce * 0.7f, wallJumpForce * 0.8f);
        rb.linearVelocity = jumpVector;
        isWallSliding = false; 
    }

    // --- Progression & Biomes ---
    private void UpdateBiomeTransition()
    {
        if (towerBiomes == null || towerBiomes.Count == 0) return;
        if (currentBiomeIndex < towerBiomes.Count - 1)
        {
            if (transform.position.y > towerBiomes[currentBiomeIndex + 1].biomeStartY)
            {
                currentBiomeIndex++;
                Debug.Log($"[PROGRESS] Masuk Biome: {towerBiomes[currentBiomeIndex].biomeName}");
            }
        }
    }

    private void HandleFallConsequence()
    {
        if (towerBiomes == null || towerBiomes.Count == 0) return;
        BiomeData currentBiome = towerBiomes[currentBiomeIndex];

        if (transform.position.y < currentBiome.fallResetY)
        {
            int resetIndex = currentBiomeIndex > 0 ? currentBiomeIndex - 1 : 0; 
            
            // Tentukan posisi respawn (Checkpoint vs Biome Start)
            Vector3 respawnPos = (currentCheckpointPosition.y > towerBiomes[resetIndex].biomeStartY) 
                                 ? currentCheckpointPosition 
                                 : new Vector3(transform.position.x, towerBiomes[resetIndex].biomeStartY, 0);

            transform.position = respawnPos; 
            rb.linearVelocity = Vector2.zero; 
            currentBiomeIndex = resetIndex;
            Debug.Log($"[FALL] Reset ke Biome: {towerBiomes[resetIndex].biomeName}");
        }
    }

    // --- Helpers ---
    private void UpdateJumpDirection()
    {
        float targetAngle = horizontalInput * maxAngle;
        float angleInRad = (90f + targetAngle) * Mathf.Deg2Rad;
        jumpDirection = new Vector2(Mathf.Cos(angleInRad), Mathf.Sin(angleInRad)).normalized;
    }

    private void UpdateJumpVisualization()
    {
        float currentLineLength = Mathf.Lerp(minJumpPower, maxJumpPower, currentChargeTime / maxChargeTime) * visualLineScale;
        Vector2 startPoint = (Vector2)transform.position - new Vector2(0, 0.5f); 
        LineRenderer.SetPosition(0, startPoint);
        LineRenderer.SetPosition(1, startPoint + jumpDirection * currentLineLength);
    }

    private void OnDrawGizmos()
    {
        if (groundCheckPoint) Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
        if (wallCheckPoint)
        {
            Gizmos.DrawLine(wallCheckPoint.position, wallCheckPoint.position + Vector3.right * wallCheckDistance);
            Gizmos.DrawLine(wallCheckPoint.position, wallCheckPoint.position + Vector3.left * wallCheckDistance);
        }
    }
}