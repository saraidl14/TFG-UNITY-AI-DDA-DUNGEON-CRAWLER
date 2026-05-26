/*  Nombre:      PlayerController.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       25/03/2026
 *  Descripcion: Controla el movimiento, salto, cámara y gravedad del jugador en primera persona.
 */
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 5f;
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.3f;
    public LayerMask groundMask;

    [Header("Camara")]
    public Camera playerCamera;
    [Tooltip("Sensibilidad base. En juego se sobreescribe con el valor guardado en PlayerPrefs.")]
    public float mouseSensitivity = 50f;

    [Header("Boost de Velocidad")]
    [Tooltip("Velocidad extra ganada por cada boss derrotado.")]
    public float speedBoostPerBoss = 0.5f;
    [Tooltip("Maximo boost de velocidad acumulable.")]
    public float maxSpeedBoost = 3f;

    private CharacterController controller;
    private Vector3 velocity;
    private float xRotation = 0f;
    private bool isGrounded;

    // Boost acumulado (se reinicia al morir, persiste entre mazmorras via GameManager)
    private float _speedBoost = 0f;

    // Boost temporal de pociones (se acumula y expira por corrutina)
    private float _potionSpeedBoost = 0f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;

        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();
    }

    private void Start()
    {
        // Recuperar boost acumulado de mazmorras anteriores
        if (GameManager.Instance != null)
            _speedBoost = GameManager.Instance.AccumulatedSpeedBoost;

        // Cargar sensibilidad guardada (si existe)
        mouseSensitivity = PlayerPrefs.GetFloat("mouseSensitivity", 50f);
    }

    /// <summary>Boost de velocidad temporal por poción (se revierte al expirar).</summary>
    public void ApplyPotionSpeedBoost(float amount, float duration)
    {
        StartCoroutine(PotionSpeedBoostCoroutine(amount, duration));
    }

    private System.Collections.IEnumerator PotionSpeedBoostCoroutine(float amount, float duration)
    {
        _potionSpeedBoost += amount;
        Debug.Log($"[PlayerController] Speed boost poción: +{amount} durante {duration}s");
        yield return new WaitForSeconds(duration);
        _potionSpeedBoost = Mathf.Max(0f, _potionSpeedBoost - amount);
        Debug.Log("[PlayerController] Speed boost poción expirado.");
    }

    /// <summary>Añade boost de velocidad permanente (llamado por GameManager al derrotar al boss).</summary>
    public void AddSpeedBoost()
    {
        _speedBoost = Mathf.Min(_speedBoost + speedBoostPerBoss, maxSpeedBoost);
        Debug.Log($"[PlayerController] Speed boost: +{speedBoostPerBoss} | Total: {_speedBoost} | Velocidad: {moveSpeed + _speedBoost}");
    }

    /// <summary>Reinicia el boost a 0 (llamado al morir el jugador).</summary>
    public void ResetSpeedBoost()
    {
        _speedBoost = 0f;
        Debug.Log("[PlayerController] Speed boost reiniciado.");
    }

    private void Update()
    {
        // No mover ni girar cámara mientras el inventario esté abierto
        if (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen) return;

        HandleMovement();
        HandleMouseLook();
    }

    private void HandleMovement()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        float x = Input.GetAxis("Horizontal"); // A/D
        float z = Input.GetAxis("Vertical");   // W/S

        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * (moveSpeed + _speedBoost + _potionSpeedBoost) * Time.deltaTime);

        // Pasos: loop mientras se mueve en el suelo, para al quedarse quieto
        if (isGrounded && move.sqrMagnitude > 0.01f)
            SoundManager.Instance?.StartFootsteps();
        else
            SoundManager.Instance?.StopFootsteps();

        // Gravedad y salto
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        if (isGrounded && Input.GetButtonDown("Jump"))
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f); // Limita mirar arriba/abajo

        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX); // Gira el cuerpo izquierda/derecha
    }
}
