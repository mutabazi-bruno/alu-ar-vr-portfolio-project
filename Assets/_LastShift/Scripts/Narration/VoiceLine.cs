using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LastShift.Narration
{
    /// <summary>
    /// One recorded line: the clip, who says it, and the subtitle. Long lines are shown in
    /// pieces, timed to the clip, so the subtitle keeps up with the voice.
    /// Works without a clip too: the subtitles then run at reading speed.
    /// </summary>
    [CreateAssetMenu(menuName = "Last Shift/Voice Line", fileName = "VoiceLine")]
    public class VoiceLine : ScriptableObject
    {
        public AudioClip clip;
        public string speaker = "HABIMANA";
        [TextArea(3, 8)] public string subtitle = "";
        [Tooltip("Longest piece of subtitle shown at once, in characters")]
        public int maxPieceLength = 90;
        [Range(0f, 2f)] public float volume = 1f;

        const float SecondsPerChar = 0.065f, MinPieceSeconds = 1.6f;

        /// <summary>How long the line lasts: the clip, or a reading-speed guess without one.</summary>
        public float Duration
        {
            get
            {
                if (clip != null) return clip.length;
                float total = 0f;
                foreach (var piece in Pieces()) total += ReadingTime(piece);
                return total;
            }
        }

        static float ReadingTime(string piece) => Mathf.Max(MinPieceSeconds, piece.Length * SecondsPerChar);

        /// <summary>The subtitle in pieces, each with the time it starts at.</summary>
        public List<(float at, string text)> Timed()
        {
            var pieces = Pieces();
            var timed = new List<(float, string)>();
            if (pieces.Count == 0) return timed;

            if (clip == null)
            {
                float t = 0f;
                foreach (var p in pieces) { timed.Add((t, p)); t += ReadingTime(p); }
                return timed;
            }

            // spread the pieces over the clip by their length
            int chars = 0;
            foreach (var p in pieces) chars += p.Length;
            float at = 0f;
            foreach (var p in pieces)
            {
                timed.Add((at, p));
                at += clip.length * p.Length / Mathf.Max(1, chars);
            }
            return timed;
        }

        /// <summary>Splits the subtitle at sentence ends, joining short sentences up to the max length.</summary>
        public List<string> Pieces()
        {
            var sentences = new List<string>();
            var text = (subtitle ?? "").Replace('\n', ' ').Trim();
            var current = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                current.Append(text[i]);
                bool end = (text[i] == '.' || text[i] == '?' || text[i] == '!') &&
                           (i + 1 >= text.Length || text[i + 1] == ' ' || text[i + 1] == '"');
                if (end && !(i + 1 < text.Length && text[i + 1] == '.'))
                {
                    // keep a closing quote with its sentence
                    if (i + 1 < text.Length && text[i + 1] == '"') { current.Append('"'); i++; }
                    sentences.Add(current.ToString().Trim());
                    current.Clear();
                }
            }
            if (current.ToString().Trim().Length > 0) sentences.Add(current.ToString().Trim());

            var pieces = new List<string>();
            var piece = "";
            foreach (var s in sentences)
            {
                if (piece.Length > 0 && piece.Length + 1 + s.Length > maxPieceLength)
                {
                    pieces.Add(piece);
                    piece = s;
                }
                else piece = piece.Length == 0 ? s : piece + " " + s;
            }
            if (piece.Length > 0) pieces.Add(piece);
            return pieces;
        }
    }
}
