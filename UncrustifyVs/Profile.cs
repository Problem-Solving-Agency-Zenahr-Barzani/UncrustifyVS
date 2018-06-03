using System.IO;

namespace UncrustifyVs
{
    /// <summary>
    /// An extension profile to conveniently switch settings.
    /// </summary>
    public class Profile
    {
        /// <summary>
        /// The default command line.
        /// </summary>
        public const string DefaultCommandLine = "-c \"%CFGFILE%\" -q -l %LANGUAGE% --no-backup \"%FILE%\"";
        /// <summary>
        /// The profile name.
        /// </summary>
        public string Name;
        /// <summary>
        /// The file path of the program to run.
        /// </summary>
        public string ProgramFilePath;
        /// <summary>
        /// The command line string to pass to the program pointed to by <see cref="ProgramFilePath"/>.
        /// </summary>
        public string CommandLine;
        /// <summary>
        /// The configuration file name for this profile.
        /// </summary>
        public string CfgFilePath;
        /// <summary>
        /// The language filter.
        /// </summary>
        public LanguageFilters LanguageFilter;
        /// <summary>
        /// If set, sets the Uncrustify --frag flag when formatting.
        /// </summary>
        public bool EnableFragmentFormatting;
        /// <summary>
        /// If set, formats a document when it is opened.
        /// </summary>
        public bool FormatOnDocumentOpened;
        /// <summary>
        /// If set, formats a document when it is saved.
        /// </summary>
        public bool FormatOnDocumentSaved;
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">The profile name.</param>
        public Profile(string name)
        {
            Reset(name);
        }
        /// <summary>
        /// Resets the profile to its default values.
        /// </summary>
        /// <param name="newName">The new profile name.</param>
        public void Reset(string newName = null)
        {
            Name = newName ?? Name;
            ProgramFilePath = string.Empty;
            CommandLine = DefaultCommandLine;
            CfgFilePath = string.Empty;
            LanguageFilter = LanguageFilters.All;
            EnableFragmentFormatting = true;
            FormatOnDocumentOpened = false;
            FormatOnDocumentSaved = false;
        }
        /// <summary>
        /// Copies all settings from another profile.
        /// </summary>
        /// <param name="other">The other profile.</param>
        public void CopyFrom(Profile other)
        {
            // NOTE(gokhan.ozdogan): the profile name is not copied on purpose.
            ProgramFilePath = other.ProgramFilePath;
            CommandLine = other.CommandLine;
            CfgFilePath = other.CfgFilePath;
            LanguageFilter = other.LanguageFilter;
            EnableFragmentFormatting = other.EnableFragmentFormatting;
            FormatOnDocumentOpened = other.FormatOnDocumentOpened;
            FormatOnDocumentSaved = other.FormatOnDocumentSaved;
        }
        /// <summary>
        /// Returns a parseable representation of the profile.
        /// </summary>
        public string ToParseString()
        {
            return
            "{\n" +
                $"ConfigFilePath {CfgFilePath}\n" +
                $"CmdLine {CommandLine}\n" +
                $"EnableFragmentProcessing {EnableFragmentFormatting}\n" +
                $"ExeName {ProgramFilePath}\n" +
                $"FormatOnDocOpened {FormatOnDocumentOpened}\n" +
                $"FormatOnDocSaved {FormatOnDocumentSaved}\n" +
                $"LanguageFilter {LanguageFilter}\n" +
                $"ProfileName {Name}\n" +
            "}\n";
        }
        /// <summary>
        /// Creates a profile from a parseable representation.
        /// </summary>
        /// <param name="text">The parseable representation.</param>
        /// <returns>The profile.</returns>
        public static Profile FromParseString(ref string text)
        {
            var p = new Profile(string.Empty);
            using (var reader = new StringReader(text))
            {
                // Read until the next profile section begins
                string line;
                while ((line = reader.ReadLine()?.Trim()) != "{")
                {
                    if (line == null)
                    {
                        // Invalid profile string
                        return null;
                    }
                }

                while ((line = reader.ReadLine()?.Trim()) != null)
                {
                    // Stop if the profile ends here
                    if (line == "}")
                    {
                        // Return the rest of the string
                        text = reader.ReadToEnd();
                        return p;
                    }

                    // Split key-value pairs.
                    var splitPos = line.IndexOf(' ');
                    if (splitPos > 0)
                    {
                        var key = line.Substring(0, splitPos);
                        var value = line.Substring(splitPos + 1);

                        if (key == "ProfileName")
                        {
                            p.Name = value;
                        }
                        else if (key == "CmdLine")
                        {
                            p.CommandLine = value;
                        }
                        else if (key == "ExeName")
                        {
                            p.ProgramFilePath = value;
                        }
                        else if (key == "ConfigFilePath")
                        {
                            p.CfgFilePath = value;
                        }
                        else if (key == "LanguageFilter")
                        {
                            System.Enum.TryParse(value, out p.LanguageFilter);
                        }
                        else if (key == "EnableFragmentProcessing")
                        {
                            bool.TryParse(value, out p.EnableFragmentFormatting);
                        }
                        else if (key == "FormatOnDocOpened")
                        {
                            bool.TryParse(value, out p.FormatOnDocumentOpened);
                        }
                        else if (key == "FormatOnDocSaved")
                        {
                            bool.TryParse(value, out p.FormatOnDocumentSaved);
                        }
                    }
                }
            }

            return null;
        }
    }
}
