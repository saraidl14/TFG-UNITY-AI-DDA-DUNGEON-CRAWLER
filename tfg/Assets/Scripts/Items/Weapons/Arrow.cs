/*  Nombre:      Arrow.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       18/04/2026
 *  Descripcion: Proyectil del arco que se mueve en línea recta y daña al primer enemigo.
 */
using UnityEngine;

/// <summary>
/// Proyectil disparado por armas de largo alcance (arco, ballesta).
/// Se mueve en línea recta, daña al primer enemigo que toca y se destruye.
///
/// SETUP DEL PREFAB:
///   - Mesh del modelo de flecha (hijo)
///   - Collider (CapsuleCollider o BoxCollider) con Is Trigger = true
///   - Capa: "Arrow" (o la que prefieras — asegúrate de que no colisione con Player)
/// </summary>
public class Arrow : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // PARÁMETROS (asignados por PlayerCombat al disparar)
    // ─────────────────────────────────────────────
    [HideInInspector] public float damage        = 10f;
    [HideInInspector] public float speed         = 15f;
    [HideInInspector] public float maxLifetime   = 7f;   // segundos antes de auto-destruirse
    [HideInInspector] public float knockbackForce = 4f;  // fuerza de retroceso al impactar

    private float _spawnTime;

    // ─────────────────────────────────────────────
    // CICLO DE VIDA
    // ─────────────────────────────────────────────
    private void Start()
    {
        _spawnTime = Time.time;
    }

    private void Update()
    {
        // Mover hacia adelante
        transform.position += transform.forward * speed * Time.deltaTime;

        // Auto-destruir tras el tiempo de vida máximo
        if (Time.time - _spawnTime >= maxLifetime)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) return;

        bool isEnemy = other.CompareTag("Enemy")
                    || other.gameObject.layer == LayerMask.NameToLayer("Enemy");

        if (!isEnemy) return;

        EnemyBase enemy = other.GetComponent<EnemyBase>() ?? other.GetComponentInParent<EnemyBase>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            if (knockbackForce > 0f)
                enemy.Knockback(transform.forward, knockbackForce);
            Debug.Log($"[Arrow] Impacto en {other.name} | Daño: {damage}");
        }

        Destroy(gameObject);
    }
}
