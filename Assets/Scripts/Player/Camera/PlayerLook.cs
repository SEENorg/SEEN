
using UnityEngine;

[System.Serializable]
public class PlayerLook
{
    [Header("Cámara")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private float mouseSensitivity = 0.15f;
    [SerializeField] private float controllerSensitivity = 120f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;
    [SerializeField] private float cameraHeight = 0.65f;

    private float yaw;
    private float pitch;

    public float Yaw => yaw;
    public float AlturaCamara => cameraHeight;

    public float MouseSensitivity
    {
        get => mouseSensitivity;
        set => mouseSensitivity = Mathf.Max(0.01f, value);
    }

    public void Iniciar(Transform jugador)
    {
        mouseSensitivity = PlayerPrefs.GetFloat(
            AjustesJuego.CLAVE_SENSIBILIDAD,
            mouseSensitivity
        );

        yaw = jugador.eulerAngles.y;
        AsegurarCamara(jugador);
    }

    public void FijarYaw(float nuevoYaw)
    {
        yaw = nuevoYaw;
        pitch = 0f;
        if (playerCamera != null)
            playerCamera.localRotation = Quaternion.identity;
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

    public void Mirar(Vector2 movimiento, bool esMando)
    {
        if (playerCamera == null) return;

        Vector2 look;

        if (esMando)
        {
            look = movimiento * controllerSensitivity * Time.deltaTime;
        }
        else
        {
            look = movimiento * mouseSensitivity;
        }

        yaw += look.x;
        pitch -= look.y;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        playerCamera.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

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