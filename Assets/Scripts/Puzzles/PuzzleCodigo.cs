using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pantalla de la sala con el código de la puerta.
///  - Al iniciar genera un código aleatorio (solo dígitos).
///  - Bloqueado: muestra "CÓDIGO BLOQUEADO".
///  - Habilitado: muestra el código en grande.
/// Se pone en el objeto de la pantalla (figura 2D) y se enlaza su texto en el Inspector.
/// </summary>
public class PuzzleCodigo : MonoBehaviour, IPuzzleCodigo
{
    [Header("Dónde se escribe el texto (arrastra UNO de los dos)")]
    [Tooltip("Texto de UI (Legacy Text) dentro de un Canvas en World Space")]
    [SerializeField] private Text textoUI;
    [Tooltip("Texto 3D (componente TextMesh)")]
    [SerializeField] private TextMesh textoMesh;

    [Header("Código")]
    [Tooltip("Cantidad de dígitos del código")]
    [SerializeField, Range(3, 8)] private int longitud = 4;

    [Header("Mensajes y estilo")]
    [SerializeField] private string mensajeBloqueado = "CÓDIGO BLOQUEADO";
    [SerializeField] private int tamanoBloqueado = 40;
    [SerializeField] private int tamanoCodigo = 140;
    [SerializeField] private Color colorBloqueado = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private Color colorCodigo = new Color(0.3f, 1f, 0.45f);

    [Header("Pruebas en el editor (marca/desmarca con el juego en Play)")]
    [Tooltip("Habilita o bloquea el código saltándose la condición de HabilitarCodigo()")]
    [SerializeField] private bool forzarHabilitado;

    public string Codigo { get; private set; }
    public bool CodigoHabilitado { get; private set; }

    private bool forzarAplicado;
    private ICondicionCodigo condicion;   // regla para habilitar (opcional, ver ICondicionCodigo)

    private void Awake()
    {
        // Busca, en este mismo objeto, un script que diga cuándo se puede habilitar el código.
        // Si no hay ninguno, condicion queda en null y el código se puede habilitar siempre.
        condicion = GetComponent<ICondicionCodigo>();

        PrepararTexto();
        GenerarCodigo();
        forzarAplicado = forzarHabilitado;
        Mostrar(forzarHabilitado);   // por defecto empieza bloqueado
    }

    // ── IPuzzleCodigo ────────────────────────────────────────────

    /// <summary>
    /// Habilita el código SOLO si se cumple la condición.
    /// La condición NO se escribe aquí: la da un script que implemente ICondicionCodigo
    /// y esté en el mismo objeto. Si no hay ninguno, se habilita siempre (útil para probar).
    /// </summary>
    public void HabilitarCodigo()
    {
        bool condicionCumplida = (condicion == null) || condicion.SeCumple();

        if (condicionCumplida)
        {
            Mostrar(true);
            Debug.Log("[PuzzleCodigo] Código habilitado: " + Codigo);
        }
        else
        {
            Debug.Log("[PuzzleCodigo] Todavía no se cumple la condición para habilitar el código.");
        }
    }

    public void DeshabilitarCodigo()
    {
        Mostrar(false);
        Debug.Log("[PuzzleCodigo] Código bloqueado.");
    }

    // ── Lógica interna ───────────────────────────────────────────

    /// <summary>Genera un código nuevo de 'longitud' dígitos.</summary>
    private void GenerarCodigo()
    {
        var sb = new StringBuilder(longitud);
        for (int i = 0; i < longitud; i++)
            sb.Append(Random.Range(0, 10));
        Codigo = sb.ToString();
    }

    /// <summary>Genera otro código distinto (por si quieres uno nuevo en una prueba).</summary>
    [ContextMenu("Regenerar código")]
    public void RegenerarCodigo()
    {
        if (!Application.isPlaying) return;
        GenerarCodigo();
        Mostrar(CodigoHabilitado);
    }

    /// <summary>Ajustes para que el texto grande no se recorte.</summary>
    private void PrepararTexto()
    {
        if (textoUI == null && textoMesh == null)
        {
            Debug.LogWarning("[PuzzleCodigo] Falta enlazar el texto (Texto UI o Texto Mesh) en el Inspector.", this);
            return;
        }
        if (textoUI != null)
        {
            textoUI.alignment = TextAnchor.MiddleCenter;
            textoUI.horizontalOverflow = HorizontalWrapMode.Overflow;
            textoUI.verticalOverflow = VerticalWrapMode.Overflow;
            textoUI.fontStyle = FontStyle.Bold;
        }
        if (textoMesh != null)
        {
            textoMesh.anchor = TextAnchor.MiddleCenter;
            textoMesh.alignment = TextAlignment.Center;
            textoMesh.fontStyle = FontStyle.Bold;
        }
    }

    /// <summary>Actualiza el texto: código en grande si está habilitado, o el mensaje de bloqueo.</summary>
    private void Mostrar(bool habilitado)
    {
        CodigoHabilitado = habilitado;

        string texto = habilitado ? Codigo : mensajeBloqueado;
        int tamano = habilitado ? tamanoCodigo : tamanoBloqueado;
        Color color = habilitado ? colorCodigo : colorBloqueado;

        if (textoUI != null)
        {
            textoUI.text = texto;
            textoUI.fontSize = tamano;
            textoUI.color = color;
        }
        if (textoMesh != null)
        {
            textoMesh.text = texto;
            textoMesh.fontSize = tamano;
            textoMesh.color = color;
        }
    }

    /// <summary>
    /// Casilla de prueba: al marcarla/desmarcarla en Play, bloquea o muestra el código.
    /// Solo reacciona cuando cambia la casilla (otros cambios del Inspector no lo tocan).
    /// </summary>
    private void OnValidate()
    {
        if (!Application.isPlaying || Codigo == null) return;
        if (forzarHabilitado == forzarAplicado) return;

        forzarAplicado = forzarHabilitado;
        Mostrar(forzarHabilitado);
    }
}
