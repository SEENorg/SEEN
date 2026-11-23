using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Minijuego: completar una secuencia lógica (progresión geométrica, aritmética,
/// cuadrados o letras). Se elige la respuesta entre 4 opciones.
///
/// Controles: ratón (clic), mando (cruceta + A) o teclado (flechas + Enter).
/// Si la respuesta es incorrecta, la opción se marca en rojo y aparece una pregunta nueva.
/// </summary>
public class PuzzleSecuencia : IPuzzle
{
    private const float AnchoTarjeta = 640f;
    private const float AltoTarjeta = 500f;
    private const float DuracionError = 1f;

    private readonly IContextoHack ctx;
    private CajasRedondeadasUI cajas;
    private TarjetaHackUI tarjeta;
    private string respuestaCorrecta;
    private List<string> opciones;
    private Button[] botones;
    private GameObject ultimaSeleccion;
    private float tiempoError;
    private bool resuelto;

    public PuzzleSecuencia(IContextoHack ctx)
    {
        this.ctx = ctx;
    }

    public void Construir()
    {
        cajas = new CajasRedondeadasUI();
        ctx.MostrarMarco(false);
        tarjeta = new TarjetaHackUI(ctx, cajas, AnchoTarjeta, AltoTarjeta, "ANÁLISIS DE PATRONES: COMPLETA LA SECUENCIA");
        tiempoError = 0f;
        resuelto = false;

        string pregunta = GenerarPregunta();

        // Caja con la secuencia
        Image bordePregunta = cajas.CrearCaja(tarjeta.Raiz, "CajaSecuencia", new Vector2(0f, 50f),
                                              new Vector2(580f, 96f), tarjeta.ColorCaja,
                                              tarjeta.ColorBordeTarjeta, 1.2f);
        Text textoPregunta = HackUI.CrearTexto(bordePregunta.transform.parent, "Secuencia", pregunta, 44,
                                               TextAnchor.MiddleCenter, Vector2.zero, new Vector2(560f, 80f),
                                               new Color(0.9f, 0.95f, 1f));
        textoPregunta.fontStyle = FontStyle.Bold;

        // Opciones en 2 x 2
        botones = new Button[opciones.Count];
        for (int i = 0; i < opciones.Count; i++)
        {
            float x = 145f;
            if (i % 2 == 0) x = -145f;
            float y = -55f - (i / 2) * 78f;

            botones[i] = tarjeta.CrearBoton(tarjeta.Raiz, "Opcion_" + i, new Vector2(x, y), new Vector2(270f, 64f),
                                            tarjeta.ColorBoton, tarjeta.ColorBotonSeleccion, tarjeta.ColorBordeBoton,
                                            i.ToString(), ElegirOpcion, true);
            Text texto = HackUI.CrearTexto(botones[i].transform, "Texto", opciones[i], 28, TextAnchor.MiddleCenter,
                                           Vector2.zero, new Vector2(270f, 64f), Color.white);
            texto.fontStyle = FontStyle.Bold;
        }

        tarjeta.MostrarEstadoNormal("¿QUÉ VALOR SIGUE?");
        ultimaSeleccion = botones[0].gameObject;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(ultimaSeleccion);
    }

    public void Actualizar()
    {
        if (tiempoError > 0f)
        {
            tiempoError -= Time.unscaledDeltaTime;
            if (tiempoError <= 0f)
            {
                ctx.Regenerar();   // pregunta nueva
                return;
            }
        }
        MantenerSeleccion();
    }

    public void Detener()
    {
        ctx.MostrarMarco(true);
        if (cajas != null) cajas.Liberar();
        cajas = null;
        tarjeta = null;
        botones = null;
        ultimaSeleccion = null;
    }

    // ── Lógica ──────────────────────────────────────────────────────

    private void ElegirOpcion(string valor)
    {
        if (ctx.InputBloqueado || ctx.EnTransicion) return;
        if (resuelto || tiempoError > 0f) return;

        int indice = int.Parse(valor);
        TeclaCodigoUI tecla = botones[indice].GetComponent<TeclaCodigoUI>();

        if (opciones[indice] == respuestaCorrecta)
        {
            resuelto = true;
            if (tecla != null) tecla.CambiarColoresBorde(HackUI.ColVerde, HackUI.ColVerde);
            tarjeta.MostrarEstado("SECUENCIA COMPLETADA", HackUI.ColVerde);
            ctx.CompletarModulo();
        }
        else
        {
            if (tecla != null) tecla.CambiarColoresBorde(HackUI.ColRojo, HackUI.ColRojo);
            tarjeta.MostrarEstado("RESPUESTA INCORRECTA: NUEVA SECUENCIA", HackUI.ColRojo);
            tiempoError = DuracionError;
        }
    }

    /// <summary>Elige uno de los 4 tipos de secuencia, arma las opciones y devuelve el texto a mostrar.</summary>
    private string GenerarPregunta()
    {
        int tipo = Random.Range(0, 4);
        string pregunta;
        string respuesta;
        List<string> distractores = new List<string>();

        if (tipo == 0)
        {
            // Cada número es el doble del anterior
            int a = Random.Range(2, 6);
            pregunta = a + "   " + (a * 2) + "   " + (a * 4) + "   ?";
            respuesta = (a * 8).ToString();
            distractores.Add((a * 8 + 2).ToString());
            distractores.Add((a * 8 - 2).ToString());
            distractores.Add((a * 6).ToString());
        }
        else if (tipo == 1)
        {
            // Suma constante
            int d = Random.Range(2, 7);
            int s = Random.Range(1, 8);
            pregunta = s + "   " + (s + d) + "   " + (s + 2 * d) + "   " + (s + 3 * d) + "   ?";
            respuesta = (s + 4 * d).ToString();
            distractores.Add((s + 4 * d + d).ToString());
            distractores.Add((s + 3 * d).ToString());
            distractores.Add((s + 4 * d - d).ToString());
        }
        else if (tipo == 2)
        {
            // Cuadrados perfectos
            pregunta = "1   4   9   16   ?";
            respuesta = "25";
            distractores.Add("20");
            distractores.Add("24");
            distractores.Add("30");
        }
        else
        {
            // Letras con salto constante
            int salto = Random.Range(2, 4);
            int inicio = Random.Range(0, 20 - 4 * salto);
            char c0 = (char)('A' + inicio);
            char c1 = (char)('A' + inicio + salto);
            char c2 = (char)('A' + inicio + 2 * salto);
            char c3 = (char)('A' + inicio + 3 * salto);
            pregunta = c0 + "   " + c1 + "   " + c2 + "   " + c3 + "   ?";
            respuesta = ((char)('A' + inicio + 4 * salto)).ToString();
            distractores.Add(((char)('A' + inicio + 4 * salto + salto)).ToString());
            distractores.Add(((char)('A' + inicio + 3 * salto)).ToString());
            distractores.Add("Z");
        }

        // Sin distractores iguales a la respuesta ni repetidos entre sí
        for (int i = distractores.Count - 1; i >= 0; i--)
        {
            bool repetido = distractores[i] == respuesta;
            for (int j = 0; j < i; j++)
            {
                if (distractores[j] == distractores[i]) repetido = true;
            }
            if (repetido) distractores.RemoveAt(i);
        }
        while (distractores.Count < 3) distractores.Add("?");

        respuestaCorrecta = respuesta;

        opciones = new List<string>();
        opciones.Add(respuesta);
        opciones.AddRange(distractores);

        // Mezclar (Fisher-Yates): la respuesta correcta no queda siempre en el mismo botón
        for (int i = opciones.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            string temporal = opciones[i];
            opciones[i] = opciones[j];
            opciones[j] = temporal;
        }

        return pregunta;
    }

    // ── Selección (para el mando) ───────────────────────────────────

    private void MantenerSeleccion()
    {
        if (EventSystem.current == null || botones == null) return;
        GameObject actual = EventSystem.current.currentSelectedGameObject;

        for (int i = 0; i < botones.Length; i++)
        {
            if (botones[i] != null && botones[i].gameObject == actual)
            {
                ultimaSeleccion = actual;
                return;
            }
        }

        if (actual == null && ultimaSeleccion != null)
            EventSystem.current.SetSelectedGameObject(ultimaSeleccion);
    }
}
