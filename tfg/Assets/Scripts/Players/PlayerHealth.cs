using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Player Stats")]
    public float maxHealth = 100f;
    private float currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount) // Metodo publico para que los enemigos puedan llamar a este metodo y hacer daño al jugador
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    private void Die() 
    {
        Debug.Log("Player Dead");
        //Meter sistema y anim muerte del jugador.
    }
    public float GetCurrentHealth() 
    {
        return currentHealth;
    }
    public float GetMaxHealth() 
    {
        return maxHealth;
    }
}
