using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarquesinaScroll : MonoBehaviour
{
    // 1. Asigna el material del cartel en el Inspector
    public Renderer rend;

    // 2. Controla la velocidad de desplazamiento (ajusta a tu gusto)
    public float scrollSpeed = 0.5f;

    // Propiedad que almacenará el valor del offset (desplazamiento)
    private float offset;

    void Update()
    {
        // El Offset de la textura se incrementa constantemente con el tiempo
        // Unity usa Time.time (tiempo de juego) para garantizar un movimiento suave
        offset = Time.time * scrollSpeed;

        // Asignamos el nuevo valor de desplazamiento (offset) al material.
        // El nombre "_MainTex" es la propiedad de la textura principal (Albedo)
        // Usamos (offset, 0) para que se mueva horizontalmente (Eje X)
        rend.material.SetTextureOffset("_MainTex", new Vector2(offset, 0));
    }
}