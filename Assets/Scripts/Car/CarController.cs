using System.Collections;
using TMPro;
using UnityEngine;

public class UniversalCarController : MonoBehaviour
{
    [Header("Wheel Colliders (FL, FR, RL, RR)")]
    public WheelCollider[] wheelColliders = new WheelCollider[4]; // 0: FL, 1: FR, 2: RL, 3: RR

    [Header("Wheel Meshes (FL, FR, RL, RR)")]
    public Transform[] wheelMeshes = new Transform[4];

    [Header("Car Settings")]
    public float maxReverseSpeedKMH = 30f;
    public float maxMotorTorque = 1500f;
    public float maxSteerAngle = 30f;
    public float maxReverseTorque = 1000f;
    public float maxSpeedKMH = 120f; // Velocidad máxima base editable por prefab o instancia
    public float decelerationFactor = 1500f; // Desaceleración automática
    public AnimationCurve steerCurve = AnimationCurve.Linear(0, 1, 100, 0.5f);

    [Header("Reverse/Brake")]
    public float brakeThreshold = 2.0f; // [m/s] a menos de esto activa reversa

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
    public KeyCode flipKey = KeyCode.R;
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
    public float maxNitroBonusKMH = 50f; // Cuánto suma el nitro a la máxima normal
    public float nitroVelocityDropTime = 1.2f; // Segundos que tarda en bajar la máxima al valor original tras el nitro

    float inputSteer, inputMotor, inputBrake;
    public bool canJump = true;
    public float jumpCooldown = 4.0f; // segundos
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
        if (speedText != null) { }
            speedText.text = $"{speed:F0}";

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

        // Un solo eje para reversa/freno y limitador de reversa
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
            // Si aún tienes buena velocidad adelante, el botón "reverse" frena
            if (localForwardSpeed > brakeThreshold)
            {
                brake = Mathf.Abs(inputMotor) * maxMotorTorque;
            }
            else if (localForwardSpeed < -brakeThreshold)
            {
                // Si ya vas para atrás, limita el motor si no superó la velocidad máxima en reversa
                if (Mathf.Abs(localZSpeed) < maxReverseSpeedMS)
                    motor = inputMotor * maxReverseTorque;
                else
                    motor = 0;
            }
            else
            {
                // Cuando casi detenido, activa reversa limitada
                if (Mathf.Abs(localZSpeed) < maxReverseSpeedMS)
                    motor = inputMotor * maxReverseTorque;
                else
                    motor = 0;
            }
        }

        // Espacio = freno a fondo
        if (inputBrake > 0f)
        {
            brake = inputBrake * 2000f;
        }

        // Giro dinámico
        float steerCoef = steerCurve.Evaluate(speed);
        float steer = inputSteer * maxSteerAngle * steerCoef;
        wheelColliders[0].steerAngle = steer;
        wheelColliders[1].steerAngle = steer;

        // Tracción
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

        // Asignar freno
        foreach (var wc in wheelColliders)
            wc.brakeTorque = brake;

        // Desaceleración automática si no acelera ni frena
        if (Mathf.Approximately(inputMotor, 0) && brake == 0)
        {
            foreach (var wc in wheelColliders)
                wc.brakeTorque = decelerationFactor;
        }

        // Mallas ruedas
        for (int i = 0; i < 4; i++)
            UpdateWheelVisual(wheelColliders[i], wheelMeshes[i]);

        ApplyAntiRollBar();

        // Limitador de velocidad flexible (sólo cuando "conduce" en el piso, no en salto o vuelo)
        LimitMaxSpeed();

        // Limitador de reversa físico (por si algún bug/fuerza externa la supera)
        if (localZSpeed < -maxReverseSpeedMS)
        {
            Vector3 velocity = rb.velocity;
            Vector3 localVel = transform.InverseTransformDirection(velocity);
            localVel.z = -maxReverseSpeedMS;
            rb.velocity = transform.TransformDirection(localVel);
        }
    }

    void LimitMaxSpeed()
    {
        // Sólo limita el vector XZ (para no cortar salto o caída), sólo si está en piso
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
        for (int axle = 0; axle < 2; axle++) // 0 = front, 1 = rear
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
        // Solo permite voltear si está bastante volcado (ejemplo: boca abajo o ladeado)
        if (Vector3.Dot(transform.up, Vector3.up) < 0.5f)
        {
            // Levanta y resetea orientación
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

    // Nitro flexible: permite velocidad máxima mayor sólo durante el nitro y baja suave el límite tras acabar
    IEnumerator NitroBoostFlexible()
    {
        isNitroActive = true;
        nitroTimer = nitroDuration;
        nitroCooldownTimer = nitroCooldown + nitroDuration;

        // Aumenta max speed durante nitro
        if (nitroDropRoutine != null) StopCoroutine(nitroDropRoutine);
        currentMaxSpeedKMH = maxSpeedKMH + maxNitroBonusKMH;

        while (nitroTimer > 0f)
        {
            rb.AddForce(transform.forward * nitroForce * Time.deltaTime, ForceMode.Acceleration);
            nitroTimer -= Time.deltaTime;
            yield return null;
        }

        isNitroActive = false;

        // Tras acabar, baja la máxima suavemente
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
        currentMaxSpeedKMH = endMax; // Asegura justo el valor base al final
    }

    void UpdateCooldownUI()
    {
        // --- Actualizar Texto del Salto ---
        if (jumpCooldownText != null) // Buena práctica: checar si está asignado
        {
            if (jumpTimer > 0f)
            {
                // Muestra el tiempo restante con un decimal (ej: "3.9")
                jumpCooldownText.gameObject.SetActive(true);
                jumpCooldownText.text = jumpTimer.ToString("F1");
            }
            else
            {
                // Cuando está listo, oculta el texto (o puedes poner "LISTO")
                //jumpCooldownText.gameObject.SetActive(false);
                jumpCooldownText.text = "LISTO"; // <-- Alternativa
            }
        }

        // --- Actualizar Texto del Nitro ---
        if (nitroCooldownText != null)
        {
            if (nitroCooldownTimer > 0f)
            {
                // Muestra el tiempo restante
                nitroCooldownText.gameObject.SetActive(true);
                nitroCooldownText.text = nitroCooldownTimer.ToString("F1");
            }
            else
            {
                // Cuando está listo, oculta el texto (o pones "LISTO")
                //nitroCooldownText.gameObject.SetActive(false);
                nitroCooldownText.text = "LISTO"; // <-- Alternativa
            }
        }
    }


}