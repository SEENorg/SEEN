using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Selector "◀  valor  ▶" para menús: se cambia con izquierda/derecha (mando, flechas)
/// o haciendo clic (avanza a la siguiente opción). Va en el mismo objeto que un Button.
/// </summary>
public class SelectorOpcionUI : MonoBehaviour, IMoveHandler
{
    private string[] opciones;
    private int indice;
    private Text textoValor;
    private Action<int> alCambiar;
    private bool circular;

    public int Indice
    {
        get { return indice; }
    }

    public void Configurar(string[] opciones, int indiceInicial, Text textoValor, bool circular, Action<int> alCambiar)
    {
        this.opciones = opciones;
        this.textoValor = textoValor;
        this.circular = circular;
        this.alCambiar = alCambiar;
        indice = Mathf.Clamp(indiceInicial, 0, Mathf.Max(0, opciones.Length - 1));

        Button boton = GetComponent<Button>();
        if (boton != null) boton.onClick.AddListener(Siguiente);
        Mostrar();
    }

    /// <summary>Cambia el valor mostrado sin avisar (por ejemplo, al abrir el menú).</summary>
    public void PonerIndice(int nuevo)
    {
        if (opciones == null || opciones.Length == 0) return;
        indice = Mathf.Clamp(nuevo, 0, opciones.Length - 1);
        Mostrar();
    }

    public void OnMove(AxisEventData datos)
    {
        if (datos.moveDir == MoveDirection.Left) Cambiar(-1);
        else if (datos.moveDir == MoveDirection.Right) Cambiar(1);
    }

    private void Siguiente()
    {
        Cambiar(1);
    }

    private void Cambiar(int paso)
    {
        if (opciones == null || opciones.Length == 0) return;
        int nuevo = indice + paso;
        if (circular)
        {
            nuevo = (nuevo + opciones.Length) % opciones.Length;
        }
        else
        {
            nuevo = Mathf.Clamp(nuevo, 0, opciones.Length - 1);
        }
        if (nuevo == indice) return;

        indice = nuevo;
        Mostrar();
        if (alCambiar != null) alCambiar(indice);
    }

    private void Mostrar()
    {
        if (textoValor == null || opciones == null || opciones.Length == 0) return;
        textoValor.text = "◀    " + opciones[indice] + "    ▶";
    }
}
