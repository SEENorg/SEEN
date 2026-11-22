using UnityEngine;

/// <summary>
/// Banco de palabras sobre el BUEN USO de las redes sociales (18 palabras, 6 por nivel).
/// Responsabilidad única: guardar las palabras y sortear una según el nivel.
/// Solo letras A-Z, porque el cifrado César solo desplaza esas letras.
/// </summary>
public class BancoPalabrasRedes : IBancoPalabras
{
    // Nivel 0: 5-6 letras
    private readonly string[] faciles = { "CLAVE", "APOYO", "VERDAD", "AMABLE", "CUIDAR", "BONDAD" };

    // Nivel 1: 7-8 letras
    private readonly string[] medias = { "RESPETO", "EMPATIA", "PRIVADO", "REPORTAR", "BLOQUEAR", "DENUNCIA" };

    // Nivel 2: 9-10 letras
    private readonly string[] dificiles = { "SEGURIDAD", "CONFIANZA", "TOLERANCIA", "PRIVACIDAD", "VERIFICAR", "SOLIDARIO" };

    private string ultima = "";

    public string ObtenerPalabra(int nivel)
    {
        string[] lista = ListaDelNivel(nivel);

        // Sortea; si sale la misma de la vez anterior, vuelve a sortear
        string palabra = lista[Random.Range(0, lista.Length)];
        if (palabra == ultima)
            palabra = lista[Random.Range(0, lista.Length)];

        ultima = palabra;
        return palabra;
    }

    private string[] ListaDelNivel(int nivel)
    {
        if (nivel <= 0) return faciles;
        if (nivel == 1) return medias;
        return dificiles;
    }
}
