using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddRooms : MonoBehaviour
{
    private RoomGenerateTemplates templates;
    void Start()
    {
        templates= GameObject.FindGameObjectWithTag("Rooms").GetComponent<RoomGenerateTemplates>();
        templates.rooms.Add(this.gameObject);
    }

   
}
