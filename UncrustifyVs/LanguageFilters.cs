using System.ComponentModel;

namespace UncrustifyVs
{
    /// <summary>
    /// An enumeration of supported language filters.
    /// </summary>
    /// <remarks>The description is displayed in the VS options page.</remarks>
    public enum LanguageFilters
    {
        /// <summary>
        /// All languages (no filter).
        /// </summary>
        [Description("<All languages>")]
        All,
        /// <summary>
        /// Format C/C++ documents only.
        /// </summary>
        [Description("C/C++")]
        Cpp,
        /// <summary>
        /// Format C# documents only.
        /// </summary>
        [Description("C#")]
        Cs,
        /// <summary>
        /// Format D documents only.
        /// </summary>
        [Description("D")]
        D,
        /// <summary>
        /// Format Java documents only.
        /// </summary>
        [Description("Java")]
        Java,
    }
}
