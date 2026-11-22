using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class CinematicSequenceController : MonoBehaviour
{
    [Header("Second Cinematic")]
    [SerializeField] private GameObject loreVideoPlayer;

    private VideoPlayer introVideo;
    private VideoPlayer loreVideo;

    private bool sequenceStarted;

    private void Start()
    {
        introVideo = GetComponent<VideoPlayer>();

        if (introVideo == null || loreVideoPlayer == null)
        {
            Debug.LogError("Cinematic players are not configured.");
            return;
        }

        loreVideo = loreVideoPlayer.GetComponent<VideoPlayer>();

        if (loreVideo == null)
        {
            Debug.LogError("Lore Video Player component not found.");
            return;
        }

        if (introVideo.clip == null || loreVideo.clip == null)
        {
            Debug.LogError("One or both cinematic clips are missing.");
            return;
        }

        // El segundo reproductor permanece inactivo
        loreVideoPlayer.SetActive(false);

        introVideo.loopPointReached += PlayLore;
        introVideo.Play();
    }

    private void PlayLore(VideoPlayer player)
    {
        if (sequenceStarted) return;

        sequenceStarted = true;

        introVideo.loopPointReached -= PlayLore;
        introVideo.Stop();

        loreVideoPlayer.SetActive(true);

        loreVideo.loopPointReached += LoadMainMenu;
        loreVideo.Play();
    }

    private void LoadMainMenu(VideoPlayer player)
    {
        loreVideo.loopPointReached -= LoadMainMenu;
        loreVideo.Stop();

        SceneManager.LoadScene("MainMenu");
    }

    private void OnDestroy()
    {
        if (introVideo != null)
            introVideo.loopPointReached -= PlayLore;

        if (loreVideo != null)
            loreVideo.loopPointReached -= LoadMainMenu;
    }
}