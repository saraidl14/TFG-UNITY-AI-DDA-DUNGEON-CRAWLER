/*  Nombre:      Destruction.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       26/03/2026
 *  Descripcion: Destruye objetos que entren en el trigger excepto al jugador.
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destruction : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            Destroy(other.gameObject);
    }
}
