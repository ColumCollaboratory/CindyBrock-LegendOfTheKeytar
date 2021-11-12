using UnityEngine;

namespace Tools
{
    /// <summary>
    /// Marks an inspector field as readonly, disabling
    /// editing for this field. Useful for debug information.
    /// </summary>
    public sealed class ReadonlyFieldAttribute : PropertyAttribute { }
}
