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
        // Ignorar al propio jugador
        if (other.CompareTag("Player")) return;

        // Buscar EnemyBase en el objeto o en cualquier padre (el collider puede estar en un hueso hijo)
        // No se usa tag ni layer para evitar problemas de configuración en el Inspector
        EnemyBase enemy = other.GetComponent<EnemyBase>()
                       ?? other.GetComponentInParent<EnemyBase>();

        if (enemy == null) return;   // no es un enemigo → la flecha sigue volando

        enemy.TakeDamage(damage);
        if (knockbackForce > 0f)
            enemy.Knockback(transform.forward, knockbackForce);

        Debug.Log($"[Arrow] Impacto en {enemy.name} | Daño: {damage}");
        Destroy(gameObject);
    }
}
