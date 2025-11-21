using UnityEngine;
using UnityEngine.UI;

public class SkillBars : MonoBehaviour
{
    [Header("Turbo")]
    public Image turboFill;
    public float turboCooldown = 5f;
    private float turboTimer;

    [Header("Jump")]
    public Image jumpFill;
    public float jumpCooldown = 5f;
    private float jumpTimer;

    void Start()
    {
        turboTimer = turboCooldown;
        jumpTimer = jumpCooldown;
    }

    void Update()
    {
        // Recarga turbo
        if (turboTimer < turboCooldown)
        {
            turboTimer += Time.deltaTime;
            turboFill.fillAmount = turboTimer / turboCooldown;
        }

        // Recarga salto
        if (jumpTimer < jumpCooldown)
        {
            jumpTimer += Time.deltaTime;
            jumpFill.fillAmount = jumpTimer / jumpCooldown;
        }
    }

    public void UseTurbo()
    {
        if (turboTimer >= turboCooldown)
        {
            turboTimer = 0;
        }
    }

    public void UseJump()
    {
        if (jumpTimer >= jumpCooldown)
        {
            jumpTimer = 0;
        }
    }
}

