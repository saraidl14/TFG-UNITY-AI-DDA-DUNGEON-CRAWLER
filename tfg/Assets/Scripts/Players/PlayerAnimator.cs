/*  Nombre:      PlayerAnimator.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       18/05/2026
 *  Descripcion: Controla las animaciones del jugador.
 *               Se comunica con PlayerCombat para saber el arma equipada
 *               y con CharacterController para saber si se mueve.
 *
 *  PARAMETROS DEL ANIMATOR CONTROLLER:
 *    Speed      (Float)   → 0 = idle, > 0.1 = walk
 *    WeaponType (Int) → 0=sin arma, 1=espada, 2=daga, 3=arco
 *    Attack     (Trigger) → dispara la animacion de ataque
 */

using UnityEngine;

/// <summary>Tipos de animacion de ataque segun el arma equipada.</summary>
public enum WeaponAnimationType
{
    Unarmed = 0,
    Sword   = 1,
    Dagger  = 2,
    Bow     = 3
}

/// <summary>
/// Gestiona los parametros del Animator del jugador:
/// movimiento (Speed), tipo de arma (WeaponType) y ataque (Attack trigger).
/// Añadir este componente al mismo GameObject que PlayerCombat y PlayerController.
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // HASHES (mas eficiente que strings en Update)
    // ─────────────────────────────────────────────
    private static readonly int HashSpeed      = Animator.StringToHash("Speed");
    private static readonly int HashWeaponType = Animator.StringToHash("WeaponType");
    private static readonly int HashAttack     = Animator.StringToHash("Attack");

    // ─────────────────────────────────────────────
    // REFERENCIAS
    // ─────────────────────────────────────────────
    private Animator     _animator;
    private PlayerCombat _combat;

    // ─────────────────────────────────────────────
    // CICLO DE VIDA
    // ─────────────────────────────────────────────
    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _combat   = GetComponentInParent<PlayerCombat>();

        if (_combat == null) Debug.LogWarning("[PlayerAnimator] PlayerCombat no encontrado.");
    }

    private void Update()
    {
        UpdateSpeed();
        UpdateWeaponType();
    }

    // ─────────────────────────────────────────────
    // MOVIMIENTO
    // ─────────────────────────────────────────────

    private void UpdateSpeed()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        float speed = new Vector2(h, v).magnitude;
        _animator.SetFloat(HashSpeed, speed);
    }

    // ─────────────────────────────────────────────
    // TIPO DE ARMA
    // ─────────────────────────────────────────────

    private void UpdateWeaponType()
    {
        if (_combat == null) return;

        WeaponData weapon = _combat.EquippedWeapon;

        int type = (int)WeaponAnimationType.Unarmed;
        if (weapon != null)
            type = (int)weapon.weaponAnimationType;

        _animator.SetInteger(HashWeaponType, type);
    }

    // ─────────────────────────────────────────────
    // ATAQUE  (llamado desde PlayerCombat)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Dispara el trigger Attack del Animator.
    /// Llamar desde PlayerCombat al ejecutar un ataque.
    /// </summary>
    public void TriggerAttack()
    {
        _animator.SetTrigger(HashAttack);
    }
}
