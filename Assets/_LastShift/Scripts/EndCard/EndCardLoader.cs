using System.Collections;
using HeyHeyThere.EscapePuzzleKitFree;
using UnityEngine;
using UnityEngine.SceneManagement;

// Takes the player to the end card once they escape (the exit door has endsGame ticked).
// Nothing has to be added to the room scene for this, it hooks in when the game starts.
public class EndCardLoader : MonoBehaviour
{
    public const string EndCardScene = "EndCard";

    // how long the dawn shows before the end card, long enough for the door and the light
    const float dawnSeconds = 5f;

    // the kit's timer keeps counting after the escape, so the end card uses this one
    public static float EscapeTime { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ListenForEscape()
    {
        Run.Escaped += OnEscaped;
    }

    static void OnEscaped()
    {
        // kit demo scenes and test builds without the end card just keep the kit's own screen
        if (!Application.CanStreamedLevelBeLoaded(EndCardScene))
            return;

        EscapeTime = Run.PlayTime;
        new GameObject("End card loader").AddComponent<EndCardLoader>();
    }

    IEnumerator Start()
    {
        // by now the kit hud has put up its ESCAPED screen and frozen the game.
        // hide it and unfreeze so the door opens and the dawn actually plays
        if (Hud.Current != null)
            Hud.Current.enabled = false;
        Time.timeScale = 1f;

        yield return new WaitForSecondsRealtime(dawnSeconds);
        SceneManager.LoadScene(EndCardScene);
    }
}
