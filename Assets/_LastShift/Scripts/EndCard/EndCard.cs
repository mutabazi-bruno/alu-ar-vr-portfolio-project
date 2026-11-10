using HeyHeyThere.EscapePuzzleKitFree;
using UnityEngine;
using UnityEngine.UI;

public class EndCard : MonoBehaviour
{
    [Tooltip("Used when the end card is opened on its own, e.g. straight from the editor")]
    [SerializeField] string roomScene = "RecordsRoomB";

    [SerializeField] Text timeText;
    [SerializeField] Text quitNote;
    [SerializeField] Button playAgainButton;
    [SerializeField] Button quitButton;

    void Start()
    {
        // the kit pauses the game on escape and the walker may still have the cursor locked
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        timeText.text = Run.PlayTime > 0f ? "Escaped in " + FormatTime(Run.PlayTime) : "";
        quitNote.gameObject.SetActive(false);

        playAgainButton.onClick.AddListener(PlayAgain);
        quitButton.onClick.AddListener(Quit);
    }

    void PlayAgain()
    {
        string room = EndCardLoader.CameFrom != "" ? EndCardLoader.CameFrom : roomScene;

        // Restart clears items, flags and the timer too, so it's a fresh shift
        SaveGame.Restart(room);
    }

    void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
        // a web page isn't allowed to close its own tab
        quitNote.gameObject.SetActive(true);
#else
        Application.Quit();
#endif
    }

    static string FormatTime(float seconds)
    {
        int total = Mathf.FloorToInt(seconds);
        return $"{total / 60}:{total % 60:00}";
    }
}
