using UnityEngine;

/// <summary>
/// Tableta de hackeo: un panel naranja emisivo (el "botón" del puzzle).
/// La interacción la dispara la mirilla (Crosshair) al mirarla y presionar E.
/// Al resolver todos sus módulos se pone verde y abre la puerta asociada.
/// </summary>
[RequireComponent(typeof(Collider))]
public class HackTerminal : MonoBehaviour, IInteractuable
{
    [Header("Módulos a resolver, en orden")]
    public HackableType[] modulos;

    [Tooltip("Nombre del acceso (se muestra en la UI)")]
    public string nombreAcceso = "PUERTA";

    [Tooltip("Estado del hackeo")]
    public bool resuelto;

    [Header("Prompt y tutorial")]
    [Tooltip("Texto 3D junto a la tableta")]
    public string textoPrompt = "Presione E para hackear";

    [Tooltip("Si es true, muestra una pantalla de instrucciones antes de cada módulo (tutorial)")]
    public bool esTutorial;

    [Tooltip("Puertas que se abren al completar todos los módulos")]
    public DoorController[] puertas;

    private TextMesh etiqueta;

    private void Start()
    {
        // Etiqueta flotante 3D
        var etiquetaGO = new GameObject("PromptFlotante");
        etiquetaGO.transform.SetParent(transform, false);
        etiquetaGO.transform.localPosition = new Vector3(0f, 1.3f, 0f);
        etiqueta = etiquetaGO.AddComponent<TextMesh>();
        etiqueta.text = textoPrompt;
        etiqueta.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        etiqueta.fontSize = 40;
        etiqueta.characterSize = 0.06f;
        etiqueta.anchor = TextAnchor.MiddleCenter;
        etiqueta.alignment = TextAlignment.Center;
        etiqueta.color = new Color(0.3f, 1f, 0.45f);
        etiquetaGO.AddComponent<MeshRenderer>();
    }

    private void Update()
    {
        // Billboard: el texto siempre mira a la cámara
        if (etiqueta != null)
        {
            var cam = Camera.main;
            if (cam != null)
                etiqueta.transform.rotation = Quaternion.LookRotation(etiqueta.transform.position - cam.transform.position);
        }
    }

    /// <summary>Llamado por la mirilla cuando el jugador mira la tableta y presiona E.</summary>
    
    public string TextoPrompt
    {
        get { return textoPrompt; }
    }
    public bool PuedeInteractuar
    {
        get { return !resuelto; }
    }
    public void Interactuar()
    {
        if (resuelto || HackManager.Abierto) return;
        HackManager.Abrir(modulos, nombreAcceso, OnHackCompletado, esTutorial);
    }

    /// <summary>Se llama cuando el jugador resolvió todos los módulos.</summary>
    public void OnHackCompletado()
    {
        resuelto = true;

        if (etiqueta != null)
        {
            etiqueta.text = "ACCESO CONCEDIDO";
            etiqueta.color = new Color(0.15f, 1f, 0.35f);
        }
        if (puertas != null)
        {
            foreach (var p in puertas)
                if (p != null) p.Abrir();
        }
        Debug.Log($"[HackTerminal] '{nombreAcceso}' hackeado. Puerta abierta.");
    }
}
