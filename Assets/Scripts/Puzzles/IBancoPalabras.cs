/// <summary>
/// Puerto pequeño: "alguien que me da una palabra para el nivel indicado".
/// El puzzle solo conoce esta interfaz (DIP); no sabe de dónde salen las palabras.
/// Para otro tema de palabras basta crear otra clase que la implemente (OCP).
/// </summary>
public interface IBancoPalabras
{
    /// <param name="nivel">0 = fácil (cortas) · 1 = medio · 2 o más = difícil (más largas)</param>
    /// <returns>Una palabra en MAYÚSCULAS, solo letras A-Z (sin tildes ni Ñ).</returns>
    string ObtenerPalabra(int nivel);
}
