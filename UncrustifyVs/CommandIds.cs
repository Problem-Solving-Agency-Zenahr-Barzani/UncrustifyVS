namespace UncrustifyVs
{
    /// <summary>
    /// An enumeration of command IDs in this package.
    /// </summary>
    public enum CommandIds
    {
        /// <summary>
        /// The command to Uncrustify the current document.
        /// </summary>
        UncrustifyDocument = 0x100,
        /// <summary>
        /// The command to Uncrustify the current selection.
        /// </summary>
        UncrustifySelection,
        /// <summary>
        /// The command to open the options page.
        /// </summary>
        Options,
    }
}