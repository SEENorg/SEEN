namespace Game.Scanner
{
    /// <summary>Nivel de peligro que la pantalla muestra con un color distinto.</summary>
    public enum NivelAmenaza
    {
        Baja,
        Media,
        Alta,
        Critica
    }

    /// <summary>
    /// Paquete de datos de un enemigo tal como lo "lee" la Pantalla del Sistema.
    /// Es un struct con campos de solo lectura: quien lo recibe tiene una copia
    /// y no puede modificar los datos del enemigo por accidente.
    /// </summary>
    public struct EnemyScanData
    {
        public readonly string nombre;
        public readonly string tipo;
        public readonly NivelAmenaza amenaza;
        public readonly float vidaActual;
        public readonly float vidaMaxima;
        public readonly string descripcion;

        public EnemyScanData(string nombre, string tipo, NivelAmenaza amenaza,
                             float vidaActual, float vidaMaxima, string descripcion)
        {
            // Nunca se guardan textos nulos: así la vista no tiene que comprobarlo.
            if (nombre == null) nombre = "";
            if (tipo == null) tipo = "";
            if (descripcion == null) descripcion = "";
            if (vidaMaxima < 0f) vidaMaxima = 0f;
            if (vidaActual < 0f) vidaActual = 0f;
            if (vidaActual > vidaMaxima) vidaActual = vidaMaxima;

            this.nombre = nombre;
            this.tipo = tipo;
            this.amenaza = amenaza;
            this.vidaActual = vidaActual;
            this.vidaMaxima = vidaMaxima;
            this.descripcion = descripcion;
        }

        /// <summary>False si el enemigo no muestra vida (vidaMaxima = 0).</summary>
        public bool TieneVida
        {
            get { return vidaMaxima > 0f; }
        }
    }
}
