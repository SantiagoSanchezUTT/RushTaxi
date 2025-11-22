using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class UniversalCarController : MonoBehaviour
{
    [Header("Wheel Colliders (FL, FR, RL, RR)")]
    public WheelCollider[] wheelColliders = new WheelCollider[4];

    [Header("Wheel Meshes (FL, FR, RL, RR)")]
    public Transform[] wheelMeshes = new Transform[4];

    [Header("Car Settings")]
    public float maxReverseSpeedKMH = 30f;
    public float maxMotorTorque = 1500f;
    public float maxSteerAngle = 30f;
    public float maxReverseTorque = 1000f;
    public float maxSpeedKMH = 120f;
    public float decelerationFactor = 1500f;
    public AnimationCurve steerCurve = AnimationCurve.Linear(0, 1, 100, 0.5f);

    [Header("Reverse/Brake")]
    public float brakeThreshold = 2.0f;

    [Header("Drive Modes")]
    public bool isFrontWheelDrive = false;
    public bool isRearWheelDrive = true;
    public bool isAllWheelDrive = false;

    [Header("Other Settings")]
    public Rigidbody rb;
    public Vector3 centerOfMassOffset = new Vector3(0, -0.5f, 0);

    [Header("Stability Settings")]
    public float antiRollStrength = 10000f;

    [Header("Flip Car")]
    public KeyCode flipKey = KeyCode.Q;
    public float flipHeight = 1.5f;
    public float flipSpeed = 5f;

    [Header("Radio Reference")]
    public TaxiRadio taxiRadio;

    [Header("Km/H Reference")]
    public TextMeshProUGUI speedText;

    [Header("Cooldown Power-Ups")]
    public TextMeshProUGUI jumpCooldownText;
    public TextMeshProUGUI nitroCooldownText;

    [Header("Power-Ups")]
    public float jumpForce = 10000f;

    // Nitro Flexible Settings
    [Header("Nitro Settings")]
    public float nitroForce = 6000f;
    public float nitroDuration = 2.0f;
    public float nitroCooldown = 3.0f;
    public float maxNitroBonusKMH = 50f;
    public float nitroVelocityDropTime = 1.2f;

    [Header("Cooldown Bars")]
    public UnityEngine.UI.Image jumpBarFill;
    public UnityEngine.UI.Image nitroBarFill;

    // ==========================================
    // NUEVO: SECCIÓN DE AUDIO INTEGRADA
    // ==========================================
    [Header("Audio Settings")]
    public AudioSource engineSource; // Arrastra aquí el AudioSource que tiene el loop del motor
    public AudioSource sfxSource;    // Arrastra aquí un AudioSource vacío para efectos
    public AudioSource brakeSource; // Arrastra el 3er Audio Source aquí


    public AudioClip crashClip;      // Sonido de choque
    public AudioClip jumpClip;       // Sonido de salto
    public AudioClip nitroClip;      // Sonido de nitro

    [Range(0.5f, 3.0f)] public float maxEnginePitch = 2.0f; // Qué tan agudo se pone el motor a máxima velocidad
    public float minCrashForce = 3.0f; // Fuerza mínima para que suene el choque

    public SkillBars skills;

    float inputSteer, inputMotor, inputBrake;
    public bool canJump = true;
    public float jumpCooldown = 4.0f;
    private float jumpTimer = 0f;

    // Nitro internals
    private bool isNitroActive = false;
    private float nitroTimer = 0f;
    private float nitroCooldownTimer = 0f;
    private float currentMaxSpeedKMH;
    private Coroutine nitroDropRoutine;

    void Start()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        rb.centerOfMass += centerOfMassOffset;
        currentMaxSpeedKMH = maxSpeedKMH;
        skills = FindObjectOfType<SkillBars>();

        // Configurar motor si se asignó
        if (engineSource != null)
        {
            engineSource.loop = true;
            if (!engineSource.isPlaying) engineSource.Play();
        }

        if (brakeSource != null)
        {
            brakeSource.loop = true; // Asegurar que sea bucle
            brakeSource.volume = 0f; // Empezar en silencio
            brakeSource.Play();      // Darle Play (sonará mudo hasta que frenes)
        }
    }

    void Update()
    {
        inputSteer = Input.GetAxis("Horizontal");
        inputMotor = Input.GetAxis("Vertical");
        inputBrake = Input.GetKey(KeyCode.Space) ? 1f : 0f;

        if (Input.GetKeyDown(KeyCode.M) && taxiRadio != null)
        {
            taxiRadio.CycleNextStation();
        }

        // Flip car
        if (Input.GetKeyDown(flipKey))
        {
            FlipCar();
        }

        float speed = rb.velocity.magnitude * 3.6f; // Km/h
        if (speedText != null)
            speedText.text = $"{speed:F0}";

        // Lógica de Sonido de Motor
        if (engineSource != null)
        {
            // El tono va de 0.8 (quieto) a maxEnginePitch (velocidad máxima)
            float pitch = Mathf.Lerp(0.8f, maxEnginePitch, speed / maxSpeedKMH);
            engineSource.pitch = pitch;
        }

        jumpTimer -= Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.J) && canJump && jumpTimer <= 0f && IsGrounded())
        {
            Jump();
        }

        nitroCooldownTimer -= Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.LeftShift) && !isNitroActive && nitroCooldownTimer <= 0f)
        {
            StartCoroutine(NitroBoostFlexible());
        }

        UpdateCooldownUI();
        HandleBrakeSound();
    }

    void FixedUpdate()
    {
        float speed = rb.velocity.magnitude * 3.6f; // Km/h
        float grip = Mathf.Lerp(7f, 12f, Mathf.InverseLerp(50f, 200f, speed));
        for (int i = 0; i < wheelColliders.Length; i++)
        {
            var friction = wheelColliders[i].sidewaysFriction;
            friction.stiffness = grip;
            wheelColliders[i].sidewaysFriction = friction;
        }

        float motor = 0f;
        float brake = 0f;

        float localForwardSpeed = transform.InverseTransformDirection(rb.velocity).z;
        float localZSpeed = transform.InverseTransformDirection(rb.velocity).z;
        float maxReverseSpeedMS = maxReverseSpeedKMH / 3.6f;

        if (inputMotor > 0)
        {
            if (speed < currentMaxSpeedKMH)
                motor = inputMotor * maxMotorTorque;
        }
        else if (inputMotor < 0)
        {
            if (localForwardSpeed > brakeThreshold)
            {
                brake = Mathf.Abs(inputMotor) * maxMotorTorque;
            }
            else if (localForwardSpeed < -brakeThreshold)
            {
                if (Mathf.Abs(localZSpeed) < maxReverseSpeedMS)
                    motor = inputMotor * maxReverseTorque;
                else
                    motor = 0;
            }
            else
            {
                if (Mathf.Abs(localZSpeed) < maxReverseSpeedMS)
                    motor = inputMotor * maxReverseTorque;
                else
                    motor = 0;
            }
        }

        if (inputBrake > 0f)
        {
            brake = inputBrake * 2000f;
        }

        float steerCoef = steerCurve.Evaluate(speed);
        float steer = inputSteer * maxSteerAngle * steerCoef;
        wheelColliders[0].steerAngle = steer;
        wheelColliders[1].steerAngle = steer;

        if (isFrontWheelDrive || isAllWheelDrive)
        {
            wheelColliders[0].motorTorque = motor;
            wheelColliders[1].motorTorque = motor;
        }
        else { wheelColliders[0].motorTorque = 0; wheelColliders[1].motorTorque = 0; }
        if (isRearWheelDrive || isAllWheelDrive)
        {
            wheelColliders[2].motorTorque = motor;
            wheelColliders[3].motorTorque = motor;
        }
        else { wheelColliders[2].motorTorque = 0; wheelColliders[3].motorTorque = 0; }

        foreach (var wc in wheelColliders)
            wc.brakeTorque = brake;

        if (Mathf.Approximately(inputMotor, 0) && brake == 0)
        {
            foreach (var wc in wheelColliders)
                wc.brakeTorque = decelerationFactor;
        }

        for (int i = 0; i < 4; i++)
            UpdateWheelVisual(wheelColliders[i], wheelMeshes[i]);

        ApplyAntiRollBar();
        LimitMaxSpeed();

        if (localZSpeed < -maxReverseSpeedMS)
        {
            Vector3 velocity = rb.velocity;
            Vector3 localVel = transform.InverseTransformDirection(velocity);
            localVel.z = -maxReverseSpeedMS;
            rb.velocity = transform.TransformDirection(localVel);
        }
    }

    // ==========================================
    // LÓGICA DE CHOQUES (INTEGRADA)
    // ==========================================
    void OnCollisionEnter(Collision collision)
    {
        // Solo procesar si tenemos configuración de sonido
        if (sfxSource == null || crashClip == null) return;

        float impactForce = collision.relativeVelocity.magnitude;

        if (impactForce > minCrashForce)
        {
            // Variar un poco el tono para realismo
            sfxSource.pitch = Random.Range(0.8f, 1.2f);

            // Volumen según fuerza (máx 1.0)
            float volume = Mathf.Clamp01(impactForce / 15f);

            sfxSource.PlayOneShot(crashClip, volume);
        }
    }

    void LimitMaxSpeed()
    {
        if (IsGrounded())
        {
            float maxSpeedMS = currentMaxSpeedKMH / 3.6f;
            Vector3 velocity = rb.velocity;
            Vector3 velocityXZ = new Vector3(velocity.x, 0f, velocity.z);

            if (velocityXZ.magnitude > maxSpeedMS)
            {
                Vector3 newVelocityXZ = velocityXZ.normalized * maxSpeedMS;
                rb.velocity = new Vector3(newVelocityXZ.x, velocity.y, newVelocityXZ.z);
            }
        }
    }

    void UpdateWheelVisual(WheelCollider collider, Transform mesh)
    {
        collider.GetWorldPose(out var pos, out var rot);
        mesh.position = pos;
        mesh.rotation = rot;
    }

    void ApplyAntiRollBar()
    {
        for (int axle = 0; axle < 2; axle++)
        {
            int left = axle * 2;
            int right = axle * 2 + 1;

            WheelHit hit;
            float travelL = 1.0f;
            float travelR = 1.0f;

            bool groundedL = wheelColliders[left].GetGroundHit(out hit);
            if (groundedL)
                travelL = (-wheelColliders[left].transform.InverseTransformPoint(hit.point).y - wheelColliders[left].radius) / wheelColliders[left].suspensionDistance;

            bool groundedR = wheelColliders[right].GetGroundHit(out hit);
            if (groundedR)
                travelR = (-wheelColliders[right].transform.InverseTransformPoint(hit.point).y - wheelColliders[right].radius) / wheelColliders[right].suspensionDistance;

            float antiRollForce = (travelL - travelR) * antiRollStrength;

            if (groundedL)
                rb.AddForceAtPosition(wheelColliders[left].transform.up * -antiRollForce, wheelColliders[left].transform.position);
            if (groundedR)
                rb.AddForceAtPosition(wheelColliders[right].transform.up * antiRollForce, wheelColliders[right].transform.position);
        }
    }

    void FlipCar()
    {
        if (Vector3.Dot(transform.up, Vector3.up) < 0.5f)
        {
            Vector3 newPos = transform.position + Vector3.up * flipHeight;
            Quaternion newRot = Quaternion.Euler(0, transform.eulerAngles.y, 0);
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.MovePosition(newPos);
            rb.MoveRotation(newRot);
        }
    }

    void Jump()
    {
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        jumpTimer = jumpCooldown;
        if (skills != null) skills.UseJump();

        // SONIDO SALTO
        if (sfxSource != null && jumpClip != null)
            sfxSource.PlayOneShot(jumpClip);
    }

    bool IsGrounded()
    {
        int groundedCount = 0;
        foreach (var wc in wheelColliders)
        {
            if (wc.isGrounded)
                groundedCount++;
        }
        return groundedCount >= 2;
    }

    IEnumerator NitroBoostFlexible()
    {
        isNitroActive = true;
        nitroTimer = nitroDuration;
        nitroCooldownTimer = nitroCooldown + nitroDuration;
        if (skills != null) skills.UseTurbo();

        // SONIDO NITRO
        if (sfxSource != null && nitroClip != null)
            sfxSource.PlayOneShot(nitroClip);

        if (nitroDropRoutine != null) StopCoroutine(nitroDropRoutine);
        currentMaxSpeedKMH = maxSpeedKMH + maxNitroBonusKMH;

        while (nitroTimer > 0f)
        {
            rb.AddForce(transform.forward * nitroForce * Time.deltaTime, ForceMode.Acceleration);
            nitroTimer -= Time.deltaTime;
            yield return null;
        }

        isNitroActive = false;

        nitroDropRoutine = StartCoroutine(NitroReleaseSmooth());
    }

    IEnumerator NitroReleaseSmooth()
    {
        float startMax = currentMaxSpeedKMH;
        float endMax = maxSpeedKMH;
        float t = 0f;
        while (t < nitroVelocityDropTime)
        {
            t += Time.deltaTime;
            currentMaxSpeedKMH = Mathf.Lerp(startMax, endMax, t / nitroVelocityDropTime);
            yield return null;
        }
        currentMaxSpeedKMH = endMax;
    }

    void UpdateCooldownUI()
    {
        if (jumpCooldownText != null)
        {
            if (jumpTimer > 0f)
            {
                jumpCooldownText.gameObject.SetActive(true);
                jumpCooldownText.text = jumpTimer.ToString("F1");
            }
            else
            {
                jumpCooldownText.text = "LISTO";
            }
        }

        if (nitroCooldownText != null)
        {
            if (nitroCooldownTimer > 0f)
            {
                nitroCooldownText.gameObject.SetActive(true);
                nitroCooldownText.text = nitroCooldownTimer.ToString("F1");
            }
            else
            {
                nitroCooldownText.text = "LISTO";
            }
        }

        if (jumpBarFill != null)
        {
            float jumpNormalized = Mathf.Clamp01((jumpCooldown - jumpTimer) / jumpCooldown);
            jumpBarFill.fillAmount = jumpNormalized;
        }

        if (nitroBarFill != null)
        {
            float nitroTotal = nitroCooldown + nitroDuration;
            float nitroNormalized = Mathf.Clamp01((nitroTotal - nitroCooldownTimer) / nitroTotal);
            nitroBarFill.fillAmount = nitroNormalized;
        }
    }

    void HandleBrakeSound()
    {
        if (brakeSource == null) return;

        // Lógica de detección (igual que antes)
        bool isBrakingInput = inputBrake > 0 || (inputMotor < 0 && transform.InverseTransformDirection(rb.velocity).z > 5f);
        float speed = rb.velocity.magnitude; // Velocidad en m/s
        bool isMoving = speed > 1f;
        bool isGrounded = IsGrounded();

        float targetVolume = 0f;
        float targetPitch = 1f;

        if (isBrakingInput && isMoving && isGrounded)
        {
            // VOLUMEN: Máximo 0.8. 
            targetVolume = 0.8f;

            // PITCH (TONO): Mágia de realismo
            // Si vas rápido (20 m/s), el tono sube a 1.2 (agudo).
            // Si vas lento (2 m/s), el tono baja a 0.6 (grave).
            // Ajusta los valores 0.5f y 1.3f a tu gusto.
            targetPitch = Mathf.Lerp(0.5f, 1.3f, speed / 20f);
        }
        else
        {
            targetVolume = 0f;
            targetPitch = 0.5f; // Que baje de tono al apagarse
        }

        // Lerp del Volumen (Suavizado)
        brakeSource.volume = Mathf.Lerp(brakeSource.volume, targetVolume, Time.deltaTime * 8f);

        // Lerp del Pitch (Para que no cambie de golpe robótico)
        brakeSource.pitch = Mathf.Lerp(brakeSource.pitch, targetPitch, Time.deltaTime * 2f);
    }
}