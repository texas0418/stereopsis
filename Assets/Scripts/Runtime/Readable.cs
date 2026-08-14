using UnityEngine;

namespace Stereopsis
{
    /// <summary>
    /// A document lying in the world: tap to read it in the viewer.
    /// The body text here is grey-box placeholder — the real documents
    /// live in ~/Documents/OneRoom/documents and are Simon's voice.
    /// </summary>
    public sealed class Readable : MonoBehaviour
    {
        [SerializeField] string title = "";
        [TextArea(4, 20)]
        [SerializeField] string body = "";

        [Tooltip("Knowledge flag set when this is read. Empty = none.")]
        [SerializeField] string setsFlag = "";

        public string Title => title;
        public string Body => body;
        public string SetsFlag => setsFlag;
    }
}
