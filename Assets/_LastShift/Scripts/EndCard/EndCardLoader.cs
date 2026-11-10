using System.Collections;
using HeyHeyThere.EscapePuzzleKitFree;
using UnityEngine;
using UnityEngine.SceneManagement;

// Takes the player to the end card once they escape (the exit door has endsGame ticked).
// Nothing has to be added to the room scene for this, it hooks in when the game starts.
public class EndCardLoader : MonoBehaviour
{
    public const string EndCardScene = "EndCard";

    // the room the player escaped from, so Play again goes back to the right one
    public static string CameFrom { get; private set; } = "";

    const float delay = 2f;

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

        CameFrom = SceneManager.GetActiveScene().name;
        new GameObject("End card loader").AddComponent<EndCardLoader>();
    }

    IEnumerator Start()
    {
        // the kit sets timeScale to 0 on escape, so this has to be real time.
        // the short wait lets the door finish opening before we cut away
        yield return new WaitForSecondsRealtime(delay);
        SceneManager.LoadScene(EndCardScene);
    }
}
