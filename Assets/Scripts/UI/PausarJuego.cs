using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PausarJuego : MonoBehaviour
{
    public GameObject menuPausa;
    public bool juegoPausado = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (juegoPausado)
            {
                Reanudar();
            }
            else
            {
                Pausar();
            }
        }
    }

    public void Reanudar()
    {
        menuPausa.SetActive(false);
        Time.timeScale = 1;
        juegoPausado = false;

        // Ocultar cursor cuando vuelve al juego
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void Pausar()
    {
        menuPausa.SetActive(true);
        Time.timeScale = 0;
        juegoPausado = true;

        // Mostrar cursor en el menú
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void SalirAlMenu()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("Menu");
    }
}
