using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BehaviorTree;

/// <summary>
/// Controlador del Mago. Enemigo de largo alcance que huye del cuerpo a cuerpo.
///
/// BT:
///   Selector
///   ├─ Sequence (ataque en área si el jugador está muy cerca)
///   │   ├─ CheckPlayerDetected
///   │   └─ TaskAreaAttack
///   ├─ Sequence (huir si el jugador está demasiado cerca)
///   │   ├─ CheckPlayerDetected
///   │   ├─ CheckPlayerTooClose
///   │   └─ TaskFleeFromPlayer
///   ├─ Sequence (disparar si el jugador está en rango)
///   │   ├─ CheckPlayerDetected
///   │   └─ TaskShootProjectile
///   ├─ Sequence (perseguir si está demasiado lejos)
///   │   ├─ CheckPlayerDetected
///   │   └─ TaskChasePlayer
///   └─ TaskPatrol (fallback)
///
/// Animaciones (parámetros Animator):
///   Speed      (float)   → 0 = Idle, 1 = moviéndose
///   Attack     (trigger) → lanza hechizo
///   AtaqueArea (trigger) → ataque en área
///   Death      (trigger) → muerte
///
/// NOTA: La rotación la gestiona el NavMeshAgent directamente (updateRotation = true),
/// igual que el OrcController. Si el modelo da la espalda, selecciona el hijo del mesh
/// en el Inspector y ponle Rotation Y = 180.
/// </summary>
public class MageController : EnemyBase
{
    // ─────────────────────────────────────────────
    // STATS MAGO
    // ─────────────────────────────────────────────

    [Header("Ataque a distancia")]
    [Tooltip("Prefab de la bola de fuego. Debe tener EnemyProjectile.")]
    public GameObject spellPrefab;

    [Tooltip("Velocidad del proyectil magico.")]
    public float spellSpeed = 10f;

    [Tooltip("Rango maximo de disparo del mago.")]
    public float castRange = 10f;

    [Tooltip("Cooldown entre lanzamientos.")]
    public float castCooldown = 2.5f;

    [Tooltip("Distancia a la que el mago empieza a huir del jugador.")]
    public float fleeRange = 3f;

    [Header("Ataque en área")]
    [Tooltip("Radio del ataque en área.")]
    public float areaRange = 4f;

    [Tooltip("Cooldown del ataque en área.")]
    public float areaCooldown = 15f;

    [Tooltip("Daño por segundo del ataque en área.")]
    public float areaDamagePerSecond = 2f;

    [Tooltip("Duración del efecto de área en segundos.")]
    public float areaDuration = 10f;

    // ─────────────────────────────────────────────
    // ANIMACIÓN
    // ─────────────────────────────────────────────

    private Animator _animator;
    private static readonly int HashSpeed = Animator.StringToHash("Speed");
    private static readonly int HashAttack = Animator.StringToHash("Attack");
    private static readonly int HashAtaqueArea = Animator.StringToHash("AtaqueArea");
    private static readonly int HashDeath = Animator.StringToHash("Death");

    // ─────────────────────────────────────────────
    // BT
    // ─────────────────────────────────────────────

    private Node _btRoot;

    // ─────────────────────────────────────────────
    // CICLO DE VIDA
    // ─────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        maxHealth = 40f;
        damage = 12f;
        moveSpeed = 2.5f;
        attackRange = castRange;
        attackCooldown = castCooldown;
        detectionRange = 12f;
        maxRoamDistance = 14f;
        coinReward = 25;
        currentHealth = maxHealth;

        _animator = GetComponent<Animator>();
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();
    }

    protected override void Start()
    {
        base.Start();
        SetupBT();

        // La rotación la gestiona el NavMeshAgent (updateRotation = true por defecto),
        // igual que el OrcController. No se toca updateRotation aquí.
    }

    // ─────────────────────────────────────────────
    // BT
    // ─────────────────────────────────────────────

    private void SetupBT()
    {
        // Prioridad 1: ataque en área si el jugador está muy cerca
        var areaSequence = new Sequence(new List<Node>
        {
            new CheckPlayerDetected(transform, detectionRange),
            new TaskAreaAttack(transform, areaRange, areaCooldown, OnAreaAttackAnimation)
        });

        // Prioridad 2: huir si el jugador está demasiado cerca y el área está en cooldown
        var fleeSequence = new Sequence(new List<Node>
        {
            new CheckPlayerDetected(transform, detectionRange),
            new CheckPlayerTooClose(transform, fleeRange),
            new TaskFleeFromPlayer(transform, moveSpeed, maxRoamDistance, spawnPosition)
        });

        // Prioridad 3: disparar bola de fuego si en rango
        var shootSequence = new Sequence(new List<Node>
        {
            new CheckPlayerDetected(transform, detectionRange),
            new TaskShootProjectile(transform, spellPrefab, damage, spellSpeed,
                                    castCooldown, castRange, OnShootAnimation)
        });

        // Prioridad 4: perseguir si está demasiado lejos
        var chaseSequence = new Sequence(new List<Node>
        {
            new CheckPlayerDetected(transform, detectionRange),
            new TaskChasePlayer(transform, moveSpeed, maxRoamDistance, spawnPosition)
        });

        // Prioridad 5 (fallback): patrullar cuando no detecta al jugador
        var patrolNode = new TaskPatrol(transform, moveSpeed, maxRoamDistance, spawnPosition, waitTime: 2f);

        _btRoot = new Selector(new List<Node> { areaSequence, fleeSequence, shootSequence, chaseSequence, patrolNode });

        if (player != null)
            _btRoot.SetData("player", player);
    }

    // ─────────────────────────────────────────────
    // COMPORTAMIENTO
    // ─────────────────────────────────────────────

    protected override void UpdateBehavior()
    {
        if (player != null) _btRoot?.SetData("player", player);
        _btRoot?.Evaluate();
        UpdateAnimationSpeed();
    }

    // Attack() no se usa: lo gestionan las Tasks del BT vía callbacks
    protected override void Attack() { }

    // ─────────────────────────────────────────────
    // ANIMACIÓN
    // ─────────────────────────────────────────────

    private void OnShootAnimation()
    {
        if (_animator == null) return;
        _animator.SetTrigger(HashAttack);
    }

    private void OnAreaAttackAnimation()
    {
        if (_animator == null) return;
        _animator.SetTrigger(HashAtaqueArea);
        StartCoroutine(AreaDamageCoroutine());
    }

    private IEnumerator AreaDamageCoroutine()
    {
        float elapsed = 0f;
        while (elapsed < areaDuration)
        {
            yield return new WaitForSeconds(1f);
            elapsed += 1f;
            if (player == null) yield break;
            if (Vector3.Distance(transform.position, player.position) <= areaRange)
            {
                PlayerHealth ph = player.GetComponent<PlayerHealth>()
                               ?? player.GetComponentInParent<PlayerHealth>();
                ph?.TakeDamage(areaDamagePerSecond);
            }
        }
    }

    private void UpdateAnimationSpeed()
    {
        if (_animator == null) return;

        var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        float speed = (agent != null) ? agent.velocity.magnitude : 0f;
        _animator.SetFloat(HashSpeed, speed > 0.1f ? 1f : 0f);
    }

    // ─────────────────────────────────────────────
    // MUERTE
    // ─────────────────────────────────────────────

    protected override void Die()
    {
        Debug.Log("[MageController] Mago eliminado.");

        if (_animator != null)
            _animator.SetTrigger(HashDeath);

        StartCoroutine(DestroyAfterDelay(2f));
        enabled = false;
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        base.Die();
    }

    // ─────────────────────────────────────────────
    // DDA
    // ─────────────────────────────────────────────

    /// <summary>
    /// areaDmgMult escala el daño por segundo del ataque en area.
    ///   dificultad 1  → 1.0  → 2 HP/s
    ///   dificultad 10 → 3.5  → 7 HP/s  (cap 7)
    ///   hardcore      → 5.0  → 10 HP/s (cap 10)
    /// </summary>
    public void ApplyDifficultyScaling(float hpMult, float dmgMult, float spdMult,
                                       float areaDmgMult = 1f)
    {
        maxHealth = 40f * hpMult;
        currentHealth = maxHealth;
        damage = 12f * dmgMult;
        moveSpeed = 2.5f * spdMult;
        areaDamagePerSecond = Mathf.Clamp(2f * areaDmgMult, 2f, 10f);
        SetupBT();
    }
}