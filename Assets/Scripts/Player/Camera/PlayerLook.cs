using UnityEngine;

/// <summary>
/// Cámara en primera persona: giro con el ratón (yaw/pitch), altura de la cámara
/// (baja al agacharse) y bloqueo del cursor. Es una clase serializable: sus valores
/// aparecen en el Inspector dentro de PlayerController.
/// </summary>
[System.Serializable]
public class PlayerLook
{
    [Header("Cámara")]
    [Tooltip("Cámara hija del jugador (se crea sola si no la asignás)")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private float mouseSensitivity = 0.15f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;
    [SerializeField] private float cameraHeight = 0.65f;
    private float yaw;
    private float pitch;
    public float Yaw
    {
        get { return yaw; }
    }
    public float AlturaCamara
    {
        get { return cameraHeight; }
    }

    public float MouseSensitivity
    {
        get { return mouseSensitivity; }
        set { mouseSensitivity = Mathf.Max(0.01f, value); }
    }

    /// <summary>Se llama una vez, desde Awake del jugador.</summary>
    public void Iniciar(Transform jugador)
    {
        // La sensibilidad se puede cambiar desde el menú de Ajustes
        mouseSensitivity = PlayerPrefs.GetFloat("sensibilidad", mouseSensitivity);

        yaw = jugador.eulerAngles.y;

        AsegurarCamara(jugador);
    }

    private void AsegurarCamara(Transform jugador)
    {
        if (playerCamera == null)
        {
            Camera cam = jugador.GetComponentInChildren<Camera>();
            if (cam == null) cam = Camera.main;
            if (cam != null) playerCamera = cam.transform;
        }

        if (playerCamera == null) return;

        if (playerCamera.parent != jugador)
            playerCamera.SetParent(jugador, false);
        playerCamera.localPosition = new Vector3(0f, cameraHeight, 0f);
        playerCamera.localRotation = Quaternion.identity;
    }

    /// <summary>Gira la cámara según el movimiento del ratón.</summary>
    public void Mirar(Vector2 movimientoRaton)
    {
        if (playerCamera == null) return;

        Vector2 look = movimientoRaton * mouseSensitivity;
        yaw += look.x;
        pitch -= look.y;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        playerCamera.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    /// <summary>Libera el cursor (Esc) o lo vuelve a capturar (clic izquierdo).</summary>
    public void ManejarCursor(bool liberar, bool capturar)
    {
        if (liberar)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (capturar && Cursor.lockState == CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}