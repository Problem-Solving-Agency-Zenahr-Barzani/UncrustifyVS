using System.Collections.Generic;
using System.Linq;

namespace UncrustifyVs
{
    /// <summary>
    /// A global collection of profiles.
    /// </summary>
    public static class Profiles
    {
        /// <summary>
        /// All profiles.
        /// </summary>
        public static List<Profile> All = new List<Profile>();
        /// <summary>
        /// The name of the default profile.
        /// </summary>
        public const string DefaultProfileName = "Default Profile";
        /// <summary>
        /// The default profile.
        /// </summary>
        public static Profile DefaultProfile => EnsureDefaultProfileExists();
        /// <summary>
        /// The active profile.
        /// </summary>
        private static Profile _active = null;
        /// <summary>
        /// The active profile.
        /// </summary>
        public static Profile Active
        {
            get { return _active; }
            set
            {
                if (_active != value)
                {
                    _active = value;
                    CanModifyActiveProfile = Active != null && !IsDefault(Active.Name);
                }
            }
        }
        /// <summary>
        /// Indicates whether the active profile can be modified.
        /// </summary>
        public static bool CanModifyActiveProfile { get; private set; }
        /// <summary>
        /// Resets all profiles.
        /// </summary>
        public static void Reset()
        {
            All.Clear();
            EnsureDefaultProfileExists();
        }
        /// <summary>
        /// Creates a new default profile, if it doesn't exist.
        /// </summary>
        /// <returns>The default profile.</returns>
        private static Profile EnsureDefaultProfileExists()
        {
            // See if the default profile already exists
            var defaultProfile = Find(DefaultProfileName);
            if (defaultProfile != null)
            {
                return defaultProfile;
            }

            // Create a new profile
            Active = new Profile(DefaultProfileName);
            All.Add(Active);
            return Active;
        }
        /// <summary>
        /// Deletes a profile.
        /// </summary>
        /// <param name="name">The profile name.</param>
        /// <returns>True, if the profile was deleted. Otherwise false.</returns>
        public static bool Delete(string name)
        {
            var profile = Find(name);
            if (profile == null || IsDefault(profile.Name))
            {
                return false;
            }

            All.Remove(profile);

            if (Active == profile)
            {
                // Switch to default profile.
                Active = Find(DefaultProfileName);
            }

            return true;
        }
        /// <summary>
        /// Creates a new profile.
        /// </summary>
        /// <param name="name">The profile name.</param>
        /// <returns>The profile or null if profile creation failed.</returns>
        public static Profile Create(string name)
        {
            if (!IsValidName(ref name))
            {
                return null;
            }

            // Make sure there are no duplicates
            if (Find(name) != null)
            {
                return null;
            }

            var newProfile = new Profile(name);
            All.Add(newProfile);
            Active = newProfile;
            return newProfile;
        }
        /// <summary>
        /// Returns whether a given profile name is valid.
        /// </summary>
        /// <param name="name">The profile name.</param>
        /// <returns>True, if <see cref="name"/> is a valid profile name. Otherwise false.</returns>
        public static bool IsValidName(ref string name)
        {
            name = name.Trim();
            return name.Length > 0 && name[0] != '>';
        }
        /// <summary>
        /// Renames a profile.
        /// </summary>
        /// <param name="oldName">The old profile name.</param>
        /// <param name="newName">The new profile name.</param>
        /// <returns>The renamed profile or null if no matching profile was found.</returns>
        public static Profile Rename(string oldName, string newName)
        {
            if (!IsValidName(ref newName) || Find(newName) != null)
            {
                return null;
            }

            var profile = Find(oldName);
            if (profile == null || IsDefault(profile.Name))
            {
                return null;
            }

            profile.Name = newName;

            return profile;
        }
        /// <summary>
        /// Returns whether a profile name matches the default profile name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>True, if the names match. Otherwise false.</returns>
        public static bool IsDefault(string name) => name == DefaultProfileName;
        /// <summary>
        /// Returns a profile by name.
        /// </summary>
        /// <param name="name">The profile name.</param>
        /// <returns>The profile or null if no match was found.</returns>
        public static Profile Find(string name) => All.FirstOrDefault(e => e.Name == name);
    }
}
