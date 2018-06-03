using System.Drawing.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.ComponentModel;
using System.IO;
using Microsoft.VisualStudio.Shell;

namespace UncrustifyVs
{
    /// <summary>
    /// The page to display in the Tools > Options menu in Visual Studio.
    /// </summary>
    class OptionsPageGeneral : DialogPage
    {
        /// <summary>
        /// The last active profile key.
        /// </summary>
        private const string LastActiveProfileKey = "LastActiveProfile";
        /// <summary>
        /// The command line.
        /// </summary>
        [Category("General")]
        [DisplayName("Command Line")]
        [Description("Command line parameters to pass directly to Uncrustify.\n\n" +
                     "%CFGFILE% - The fully qualified filename of the configuration file.\n" +
                     "%FILE% - The fully qualified filename of the document that is passed to Uncrustify.\n" +
                     "%FILENAME% - The filename of the active document.\n" +
                     "%FILE_DIR% - The filepath of the active document.\n" +
                     "%LANGUAGE% - The programming language of the file.\n" +
                     "%SOLUTION% - The fully qualified filename of the active solution.\n" +
                     "\n" +
                     "Default value: " + Profile.DefaultCommandLine)]
        [DefaultValue(Profile.DefaultCommandLine)]
        public string CommandLine
        {
            get { return Profiles.Active.CommandLine; }
            set { Profiles.Active.CommandLine = value; }
        }
        /// <summary>
        /// The program file path.
        /// </summary>
        [Category("General")]
        [DisplayName("Uncrustify")]
        [Description("The Uncrustify executable.")]
        [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
        public string ProgramFilePath
        {
            get { return Profiles.Active.ProgramFilePath; }
            set { Profiles.Active.ProgramFilePath = value; }
        }
        /// <summary>
        /// The Uncrustify configuration file.
        /// </summary>
        [Category("General")]
        [DisplayName("Configuration File")]
        [Description("The Uncrustify configuration file.")]
        [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
        public string CfgFilePath
        {
            get { return Profiles.Active.CfgFilePath; }
            set { Profiles.Active.CfgFilePath = value; }
        }
        /// <summary>
        /// The language filter.
        /// </summary>
        [Category("Filters")]
        [DisplayName("Language Filter")]
        [Description("Only submits files of the selected language to Uncrustify.")]
        [TypeConverter(typeof(EnumDescriptionConverter))]
        [DefaultValue(LanguageFilters.All)]
        public LanguageFilters LanguageFilter
        {
            get { return Profiles.Active.LanguageFilter; }
            set { Profiles.Active.LanguageFilter = value; }
        }
        /// <summary>
        /// Indicates whether to use fragment formatting.
        /// </summary>
        [Browsable(false)]
        [Category("Conditional Formatting")]
        [DisplayName("Enable Fragment Formatting")]
        [Description("Appends the --frag flag to the command-line when formatting a selection.")]
        [DefaultValue(true)]
        public bool EnableFragmentFormatting
        {
            get { return Profiles.Active.EnableFragmentFormatting; }
            set { Profiles.Active.EnableFragmentFormatting = value; }
        }
        /// <summary>
        /// If set, formats a document after it is opened.
        /// </summary>
        [Category("Event-driven Formatting")]
        [DisplayName("On Document Opened")]
        [Description("Formats a document after it is opened.")]
        [DefaultValue(false)]
        public bool FormatOnDocOpened
        {
            get { return Profiles.Active.FormatOnDocumentOpened; }
            set { Profiles.Active.FormatOnDocumentOpened = value; }
        }
        /// <summary>
        /// If set, formats a document before it is saved.
        /// </summary>
        [Category("Event-driven Formatting")]
        [DisplayName("On Document Saved")]
        [Description("Formats a document after it is saved.")]
        [DefaultValue(false)]
        public bool FormatOnDocSaved
        {
            get { return Profiles.Active.FormatOnDocumentSaved; }
            set { Profiles.Active.FormatOnDocumentSaved = value; }
        }
        /// <summary>
        /// The active profile.
        /// </summary>
        [Category("Profiles")]
        [DisplayName("Active Profile")]
        [Description("The active profile.")]
        [TypeConverter(typeof(ProfilePropertyConverter))]
        [DefaultValue(Profiles.DefaultProfileName)]
        public string ActiveProfileName
        {
            get { return Profiles.Active.Name; }
            set
            {
                if (value == ProfileActionNames.NewProfile)
                {
                    CreateNewProfileDialog();
                }
                else if (value == ProfileActionNames.DeleteProfile)
                {
                    if (!Profiles.IsDefault(Profiles.Active.Name))
                    {
                        var result = MessageBox.Show($"Do you really want to delete profile '{Profiles.Active.Name}'?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (result == DialogResult.Yes)
                        {
                            Profiles.Delete(Profiles.Active.Name);
                        }
                    }
                }
                else if (value == ProfileActionNames.RenameProfile)
                {
                    // Rename Active Profile
                    if (!Profiles.IsDefault(Profiles.Active.Name))
                    {
                        do
                        {
                            var newProfileName = Microsoft.VisualBasic.Interaction.InputBox($"Please enter a new name for profile '{Profiles.Active.Name}'.", "Rename Profile");
                            if (newProfileName == "")
                            {
                                break;
                            }

                            if (Profiles.Rename(Profiles.Active.Name, newProfileName) == null)
                            {
                                if (MessageBox.Show($"'{newProfileName}' is not a valid profile name.", "Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) != DialogResult.Retry)
                                {
                                    break;
                                }
                            }
                        }
                        while (true);
                    }
                }
                else if (value == ProfileActionNames.CloneProfile)
                {
                    var previousProfile = Profiles.Active;
                    CreateNewProfileDialog()?.CopyFrom(previousProfile);
                }
                else if (value == ProfileActionNames.ResetProfile)
                {
                    Profiles.Find(Profiles.Active.Name)?.Reset();
                }
                else if (value == ProfileActionNames.ImportProfile)
                {
                    var dialog = new OpenFileDialog();
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            var parseStr = File.ReadAllText(dialog.FileName);
                            var profile = Profile.FromParseString(ref parseStr);
                            if (profile == null)
                            {
                                MessageBox.Show($"'{dialog.FileName}' is not a valid profile file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            else
                            {
                                // Add the new profile
                                var newProfile = Profiles.Create(profile.Name);
                                if (newProfile != null)
                                {
                                    newProfile.CopyFrom(profile);
                                }
                                else
                                {
                                    MessageBox.Show("Could not add the profile to the existing profile set. Please check for name collisions.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }
                        }
                        catch
                        {
                            MessageBox.Show($"Failed to import '{dialog.FileName}'.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                else if (value == ProfileActionNames.ExportProfile)
                {
                    var dialog = new SaveFileDialog();
                    if(dialog.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllText(dialog.FileName, Profiles.Active.ToParseString());
                    }
                }
                else
                {
                    Profiles.Active = Profiles.Find(value) ?? Profiles.DefaultProfile;
                }
            }
        }
        /// <summary>
        /// A dummy property to load and save the profiles from internal extension storage.
        /// </summary>
        [Browsable(false)]
        public string SerializeProfiles
        {
            get
            {
                // Write all profile properties out to a single string
                var result = string.Empty;

                // Write out the active profile first
                result = LastActiveProfileKey + " " + Profiles.Active.Name + "\n";

                // Append all other profile strings
                foreach(var p in Profiles.All)
                {
                    result += p.ToParseString();
                }

                return result;
            }
            set
            {
                // Remove all profiles
                Profiles.All.Clear();

                // Parse the string back
                var parseStr = value;

                // The first line is the name of the active profile
                string lastActiveProfileName = Profiles.DefaultProfileName;
                if (parseStr.Length > 0 || parseStr.StartsWith(LastActiveProfileKey))
                {
                    parseStr = parseStr.Remove(0, LastActiveProfileKey.Length + 1);
                    var i = parseStr.IndexOf('\n');
                    if (i > 0)
                    {
                        lastActiveProfileName = parseStr.Substring(0, i);
                        parseStr = parseStr.Remove(0, i + 1);
                    }
                }

                while (parseStr != null && parseStr.Length > 0)
                {
                    var p = Profile.FromParseString(ref parseStr);
                    if(p == null)
                    {
                        break;
                    }

                    // Create a new profile
                    Profiles.Create(p.Name)?.CopyFrom(p);
                }

                // Ensure the default profile exists.
                var defaultProfile = Profiles.DefaultProfile;

                // Restore the last active profile
                Profiles.Active = Profiles.Find(lastActiveProfileName) ?? defaultProfile;
            }
        }
        /// <summary>
        /// Shows the "New Profile" dialog.
        /// </summary>
        /// <returns>The new profile or null, if the operation was canelled.</returns>
        public static Profile CreateNewProfileDialog()
        {
            do
            {
                var name = Microsoft.VisualBasic.Interaction.InputBox("Please assign a name to the new profile.", "New Profile");
                if (string.IsNullOrEmpty(name))
                {
                    return null;
                }

                var profile = Profiles.Create(name);
                if (profile != null)
                {
                    return profile;
                }

                if (MessageBox.Show($"Could not create profile '{name}'.", "Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) != DialogResult.Retry)
                {
                    return null;
                }
            }
            while (true);
        }
        /// <inheritdoc />
        public OptionsPageGeneral()
        {
            Profiles.Reset();
        }
        /// <inheritdoc />
        public override void ResetSettings()
        {
            Profiles.Reset();
            base.ResetSettings();
        }
    }
}
