using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para cambiar de escena

public class RandomSceneChanger : MonoBehaviour
{
    [Header("Configuración de Tiempo (segundos)")]
    [SerializeField] private float tiempoMinimo = 5f;
    [SerializeField] private float tiempoMaximo = 15f;

    [Header("Grupo de Escenas Posibles")]
    [SerializeField]
    private List<string> escenasPermitidas = new List<string>
    {
        "EscenaNivel1",
        "EscenaNivel2",
        "EscenaNivel3"
    };

    private void Start()
    {
        // Inicia el temporizador aleatorio al comenzar
        StartCoroutine(CambiarEscenaDespuesDeTiempo());
    }

    private IEnumerator CambiarEscenaDespuesDeTiempo()
    {
        // Genera un tiempo aleatorio dentro del rango configurado
        float tiempoEspecial = Random.Range(tiempoMinimo, tiempoMaximo);
        yield return new WaitForSeconds(tiempoEspecial);

        CambiarAEscenaAleatoria();
    }

    private void CambiarAEscenaAleatoria()
    {
        if (escenasPermitidas.Count == 0)
        {
            Debug.LogWarning("No hay escenas asignadas en la lista.");
            return;
        }

        // Elige un índice aleatorio de la lista
        int indiceAleatorio = Random.Range(0, escenasPermitidas.Count);
        string escenaObjetivo = escenasPermitidas[indiceAleatorio];

        // Carga la escena elegida
        SceneManager.LoadScene(escenaObjetivo);
    }
}