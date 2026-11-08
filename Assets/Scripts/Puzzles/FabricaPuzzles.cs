using UnityEngine;

/// <summary>Factory: crea el minijuego que corresponde a cada tipo.</summary>
public static class FabricaPuzzles
{
    public static IPuzzle Crear(HackableType tipo, IContextoHack ctx)
    {
        switch (tipo)
        {
            case HackableType.Escanear: return new PuzzleEscanear(ctx);
            case HackableType.UnirCables: return new PuzzleCables(ctx);
            case HackableType.Secuencia: return new PuzzleSecuencia(ctx);
            case HackableType.Patron: return new PuzzlePatron(ctx);
            case HackableType.Sincronizar: return new PuzzleSincronizar(ctx);
            default: return null;   // todavia no migrado: lo sigue manejando HackManager
            
        }
    }
}