using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Un botón iluminable de los puzzles de hackeo (las teclas de PuzzleIngresarCodigo
/// y los conectores de PuzzleCables).
/// Se ilumina (borde de color y un poco más grande) mientras está seleccionada,
/// ya sea con el mando, con las flechas del teclado o pasando el ratón por encima.
/// Al pulsarla avisa al puzzle con el valor de la tecla ("1", "BORRAR", "VERIFICAR"...).
/// </summary>
public class TeclaCodigoUI : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
{
    private const float EscalaSeleccion = 1.05f;

    private Image borde;
    private Color colorBordeNormal;
    private Color colorBordeSeleccion;
    private string valor;
    private Action<string> alPresionar;
    private bool seleccionada;

    public bool Seleccionada
    {
        get { return seleccionada; }
    }

    /// <summary>Prepara la tecla. alPresionar recibe el valor cuando se pulsa.</summary>
    public void Configurar(string valor, Image borde, Color colorBordeNormal,
                           Color colorBordeSeleccion, Action<string> alPresionar)
    {
        this.valor = valor;
        this.borde = borde;
        this.colorBordeNormal = colorBordeNormal;
        this.colorBordeSeleccion = colorBordeSeleccion;
        this.alPresionar = alPresionar;
        Aplicar();
    }

    /// <summary>Cambia los colores del borde (por ejemplo, cuando un cable queda conectado).</summary>
    public void CambiarColoresBorde(Color normal, Color seleccion)
    {
        colorBordeNormal = normal;
        colorBordeSeleccion = seleccion;
        Aplicar();
    }

    /// <summary>Lo llama el Button (clic, botón A del mando, Enter o Espacio).</summary>
    public void Presionar()
    {
        if (alPresionar != null) alPresionar(valor);
    }

    public void OnSelect(BaseEventData datos)
    {
        seleccionada = true;
        Aplicar();
    }

    public void OnDeselect(BaseEventData datos)
    {
        seleccionada = false;
        Aplicar();
    }

    /// <summary>
    /// Con el ratón, pasar por encima también selecciona la tecla. Así solo hay
    /// una tecla iluminada a la vez, uses ratón o mando.
    /// </summary>
    public void OnPointerEnter(PointerEventData datos)
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(gameObject);
    }

    private void Aplicar()
    {
        if (borde != null)
        {
            if (seleccionada) borde.color = colorBordeSeleccion;
            else borde.color = colorBordeNormal;
        }

        if (seleccionada) transform.localScale = Vector3.one * EscalaSeleccion;
        else transform.localScale = Vector3.one;
    }
}
