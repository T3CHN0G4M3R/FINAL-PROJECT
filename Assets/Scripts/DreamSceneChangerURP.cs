using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DreamSceneChangerURP : MonoBehaviour
{
    [Header("Configuración de Tiempo")]
    [SerializeField] private float tiempoMinimo = 5f;
    [SerializeField] private float tiempoMaximo = 15f;

    [Header("UI Parpadeo")]
    [SerializeField] private CanvasGroup parpadeoCanvasGroup;
    [SerializeField] private int cantidadParpadeos = 3;
    [SerializeField] private float duracionParpadeo = 2f;

    [Header("Post-Processing (URP)")]
    [SerializeField] private Volume globalVolume;

    [Header("Grupo de Escenas")]
    [SerializeField]
    private List<string> escenasPermitidas = new List<string>
    {
        "EscenaNivel1",
        "EscenaNivel2",
        "EscenaNivel3"
    };

    private Vignette vignetteEffect;
    private ChromaticAberration chromaticEffect;
    private DepthOfField dofEffect;

    private void Start()
    {
        // 1. Inicializar Canvas
        if (parpadeoCanvasGroup != null)
        {
            parpadeoCanvasGroup.alpha = 0f;
        }

        // 2. Extraer perfiles y RESETEARLOS inmediatamente a estado normal
        ObtenerEfectosVolume();
        ResetearEfectos();

        // 3. Iniciar secuencia
        StartCoroutine(RutinaTransicionSueno());
    }

    private void ObtenerEfectosVolume()
    {
        if (globalVolume != null && globalVolume.profile != null)
        {
            globalVolume.profile.TryGet(out vignetteEffect);
            globalVolume.profile.TryGet(out chromaticEffect);
            globalVolume.profile.TryGet(out dofEffect);
        }
        else
        {
            Debug.LogWarning("Global Volume no asignado o sin Profile configurado.");
        }
    }

    private void ResetearEfectos()
    {
        // Forzamos los valores iniciales limpios (sin distorsión)
        if (vignetteEffect != null) vignetteEffect.intensity.value = 0f;
        if (chromaticEffect != null) chromaticEffect.intensity.value = 0f;
        if (dofEffect != null) dofEffect.focusDistance.value = 10f; // Distancia enfocada
    }

    private IEnumerator RutinaTransicionSueno()
    {
        float tiempoEspera = Random.Range(tiempoMinimo, tiempoMaximo);
        float tiempoTranscurrido = 0f;
        float inicioDistorsion = tiempoEspera * 0.6f; // La distorsión empieza en el 60% del tiempo total

        while (tiempoTranscurrido < tiempoEspera)
        {
            tiempoTranscurrido += Time.deltaTime;

            if (tiempoTranscurrido >= inicioDistorsion)
            {
                // Calcula la transición progresiva del 0 al 1
                float progresoEfectos = (tiempoTranscurrido - inicioDistorsion) / (tiempoEspera - inicioDistorsion);
                AplicarEfectosPostProcesado(progresoEfectos);
            }
            else
            {
                // Garantiza que antes del 60% la pantalla se mantenga completamente limpia
                ResetearEfectos();
            }

            yield return null;
        }

        // Parpadeo de ojos
        if (parpadeoCanvasGroup != null)
        {
            for (int i = 0; i < cantidadParpadeos; i++)
            {
                yield return StartCoroutine(FadeCanvasGroup(parpadeoCanvasGroup, 0f, 1f, duracionParpadeo / (cantidadParpadeos * 2)));
                yield return StartCoroutine(FadeCanvasGroup(parpadeoCanvasGroup, 1f, 0f, duracionParpadeo / (cantidadParpadeos * 2)));
            }

            yield return StartCoroutine(FadeCanvasGroup(parpadeoCanvasGroup, 0f, 1f, 0.6f));
        }

        // Carga de escena
        if (escenasPermitidas.Count > 0)
        {
            int indiceAleatorio = Random.Range(0, escenasPermitidas.Count);
            string escenaSeleccionada = escenasPermitidas[indiceAleatorio];

            AsyncOperation operacionCarga = SceneManager.LoadSceneAsync(escenaSeleccionada);
            while (!operacionCarga.isDone)
            {
                yield return null;
            }
        }
    }

    private void AplicarEfectosPostProcesado(float progreso)
    {
        if (vignetteEffect != null)
        {
            vignetteEffect.intensity.value = Mathf.Lerp(0f, 0.6f, progreso);
        }

        if (chromaticEffect != null)
        {
            chromaticEffect.intensity.value = Mathf.Lerp(0f, 1f, progreso);
        }

        if (dofEffect != null)
        {
            dofEffect.focusDistance.value = Mathf.Lerp(10f, 0.1f, progreso);
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float inicio, float fin, float duracion)
    {
        float tiempo = 0f;
        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            cg.alpha = Mathf.Lerp(inicio, fin, tiempo / duracion);
            yield return null;
        }
        cg.alpha = fin;
    }
}