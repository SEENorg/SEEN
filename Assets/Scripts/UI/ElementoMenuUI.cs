using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Elemento de los menús (tarjetas de nivel, filas de ajustes, botón VOLVER) que
/// crece suavemente y enciende su borde mientras está seleccionado, ya sea con el
/// mando, con las flechas del teclado o pasando el ratón por encima.
/// </summary>
public class ElementoMenuUI : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
{
    private const float Velocidad = 14f;

    private Transform objetivo;
    private float escalaSeleccion = 1.1f;
    private Image borde;
    private Color colorBordeNormal;
    private Color colorBordeSeleccion;
    private Text etiqueta;
    private Color colorEtiquetaNormal;
    private Color colorEtiquetaSeleccion;
    private bool seleccionado;

    public bool Seleccionado
    {
        get { return seleccionado; }
    }

    /// <summary>
    /// objetivo: lo que se agranda (si es null, este mismo objeto).
    /// borde y etiqueta pueden ser null si no se quieren colorear.
    /// </summary>
    public void Configurar(Transform objetivo, float escalaSeleccion, Image borde, Color colorBordeNormal,
                           Color colorBordeSeleccion, Text etiqueta, Color colorEtiquetaNormal,
                           Color colorEtiquetaSeleccion)
    {
        if (objetivo == null) objetivo = transform;
        this.objetivo = objetivo;
        this.escalaSeleccion = escalaSeleccion;
        this.borde = borde;
        this.colorBordeNormal = colorBordeNormal;
        this.colorBordeSeleccion = colorBordeSeleccion;
        this.etiqueta = etiqueta;
        this.colorEtiquetaNormal = colorEtiquetaNormal;
        this.colorEtiquetaSeleccion = colorEtiquetaSeleccion;
        AplicarColores();
    }

    private void OnEnable()
    {
        seleccionado = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
        if (objetivo != null) objetivo.localScale = Vector3.one;
        AplicarColores();
    }

    public void OnSelect(BaseEventData datos)
    {
        seleccionado = true;
        AplicarColores();
    }

    public void OnDeselect(BaseEventData datos)
    {
        seleccionado = false;
        AplicarColores();
    }

    /// <summary>Pasar el ratón por encima también selecciona: solo hay un elemento iluminado a la vez.</summary>
    public void OnPointerEnter(PointerEventData datos)
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(gameObject);
    }

    private void Update()
    {
        if (objetivo == null) return;
        float meta = 1f;
        if (seleccionado) meta = escalaSeleccion;
        float actual = objetivo.localScale.x;
        float nueva = Mathf.Lerp(actual, meta, 1f - Mathf.Exp(-Velocidad * Time.unscaledDeltaTime));
        objetivo.localScale = new Vector3(nueva, nueva, 1f);
    }

    private void AplicarColores()
    {
        if (borde != null)
        {
            if (seleccionado) borde.color = colorBordeSeleccion;
            else borde.color = colorBordeNormal;
        }
        if (etiqueta != null)
        {
            if (seleccionado) etiqueta.color = colorEtiquetaSeleccion;
            else etiqueta.color = colorEtiquetaNormal;
        }
    }
}
