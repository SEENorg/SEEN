using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Avisa con un número cuando se hace clic en este elemento de UI (por ejemplo, la casilla 3).
/// No selecciona nada en el EventSystem, así no interfiere con el mando ni con Enter.
/// El elemento necesita un Graphic con Raycast Target activo (por ejemplo, un Image).
/// </summary>
public class ClicIndiceUI : MonoBehaviour, IPointerClickHandler
{
    private int indice;
    private Action<int> alHacerClic;

    public void Configurar(int indice, Action<int> alHacerClic)
    {
        this.indice = indice;
        this.alHacerClic = alHacerClic;
    }

    public void OnPointerClick(PointerEventData datos)
    {
        if (alHacerClic != null) alHacerClic(indice);
    }
}
