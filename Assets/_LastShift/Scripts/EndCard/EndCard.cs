using HeyHeyThere.EscapePuzzleKitFree;
using UnityEngine;
using UnityEngine.UI;

public class EndCard : MonoBehaviour
{
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

        float time = EndCardLoader.EscapeTime > 0f ? EndCardLoader.EscapeTime : Run.PlayTime;
        timeText.text = time > 0f ? "Escaped in " + FormatTime(time) : "";
        quitNote.gameObject.SetActive(false);

        playAgainButton.onClick.AddListener(PlayAgain);
        quitButton.onClick.AddListener(Quit);
    }

    // straight back into Room B, not the title screen (that one goes first in the build list)
    const string FirstRoom = "RecordsRoomB";

    void PlayAgain()
    {
        // Restart clears items, flags and the timer too, so it's a fresh shift
        SaveGame.Restart(FirstRoom);
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
