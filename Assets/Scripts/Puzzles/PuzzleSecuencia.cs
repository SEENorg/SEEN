using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static HackUI;

/// <summary>
/// Minijuego: completar una secuencia lógica (progresión geométrica, aritmética,
/// cuadrados o letras). Se elige la respuesta entre 4 opciones.
/// </summary>
public class PuzzleSecuencia : IPuzzle
{
    private readonly IContextoHack ctx;
    private string respuestaCorrecta;

    public PuzzleSecuencia(IContextoHack ctx) { this.ctx = ctx; }

    public void Actualizar() { }
    public void Detener() { }

    public void Construir()
    {
        Transform contenido = ctx.Contenido;

        CrearTexto(contenido, "LblLog", "COMPLETA LA SECUENCIA", 26, TextAnchor.MiddleCenter,
                   new Vector2(0f, 165f), new Vector2(700f, 36f), ColAcento);

        int tipo = Random.Range(0, 4);
        string pregunta = "";
        string respuesta = "";
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

        // Sin distractores repetidos con la respuesta
        for (int i = distractores.Count - 1; i >= 0; i--)
        {
            if (distractores[i] == respuesta)
                distractores.RemoveAt(i);
        }
        while (distractores.Count < 3)
            distractores.Add("?");

        respuestaCorrecta = respuesta;

        Text textoPregunta = CrearTexto(contenido, "PreguntaSec", pregunta, 42, TextAnchor.MiddleCenter,
                                        new Vector2(0f, 80f), new Vector2(760f, 60f), new Color(0.9f, 0.95f, 1f));
        textoPregunta.fontStyle = FontStyle.Bold;

        List<string> opciones = new List<string>();
        opciones.Add(respuesta);
        opciones.AddRange(distractores);

        // Mezclar las opciones (Fisher-Yates): así la respuesta correcta no queda siempre en el mismo botón
        for (int i = opciones.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            string temporal = opciones[i];
            opciones[i] = opciones[j];
            opciones[j] = temporal;
        }

        for (int i = 0; i < opciones.Count; i++)
        {
            int fila = i / 2;
            int col = i % 2;
            float x = 170f;
            if (col == 0) x = -170f;
            float y = 0f - fila * 70f;

            Button b = CrearBoton(contenido, "Opcion_" + i, opciones[i], new Vector2(x, y),
                                  new Vector2(280f, 60f));
            int idx = i;
            b.onClick.AddListener(() =>
            {
                if (opciones[idx] == respuestaCorrecta)
                {
                    ctx.MostrarFeedback("MÓDULO COMPLETADO", true);
                    ctx.CompletarModulo();
                }
                else
                {
                    ctx.MostrarFeedback("RESPUESTA INCORRECTA", false);
                    ctx.Regenerar();   // pregunta nueva
                }
            });
        }
    }
}