using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIcon : MonoBehaviour
{
    public Transform player;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (player == null)
            return;

        float playerRotationY = player.eulerAngles.y;

        // 1. Calcula la rotación base (negada, para que coincida con el UI).
        float baseRotationZ = -playerRotationY;

        // 2. AÑADE LA COMPENSACIÓN (Offset):
        // El offset más común es 90 grados. Si la flecha apunta a la izquierda cuando el coche va
        // recto, se debe sumar 90.
        float compensatedRotationZ = baseRotationZ + 90f;

        transform.localRotation = Quaternion.Euler(0f, 0f, compensatedRotationZ);
    }
}
