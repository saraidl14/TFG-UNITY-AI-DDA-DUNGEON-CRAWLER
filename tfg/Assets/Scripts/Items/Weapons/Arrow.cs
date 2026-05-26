/*  Nombre:      Arrow.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       26/05/2026
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
    [HideInInspector] public float maxLifetime   = 5f;   // segundos antes de auto-destruirse
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
        // Ignorar el propio jugador
        if (other.CompareTag("Player")) return;

        // ¿Es un enemigo?
        EnemyBase enemy = other.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);

            // Knockback en la direccion de vuelo de la flecha
            if (knockbackForce > 0f)
                enemy.Knockback(transform.forward, knockbackForce);

            Debug.Log($"[Arrow] Impacto en {other.name} | Daño: {damage}");
        }

        // Destruir la flecha al impactar (con o sin enemigo)
        Destroy(gameObject);
    }
}
