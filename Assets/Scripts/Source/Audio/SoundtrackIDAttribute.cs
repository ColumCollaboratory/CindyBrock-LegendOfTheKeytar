using UnityEngine;

namespace CindyBrock.Audio
{
    /// <summary>
    /// Attribute that marks an integer field as
    /// an identity of a soundtrack set, prompting
    /// a drop down for selecting that soundtrack set
    /// from the soundtrack settings.
    /// </summary>
    public sealed class SoundtrackIDAttribute : PropertyAttribute { }
}
