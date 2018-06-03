using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace UncrustifyVs
{
    /// <summary>
    /// A global converter to allow editing and selecting profiles.
    /// </summary>
    class ProfilePropertyConverter : StringConverter
    {
        /// <inheritdoc />
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
        /// <inheritdoc />
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => true;
        /// <inheritdoc />
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            // Add all profile actions and profiles to this list.
            var returnValue = new List<string>();

            // Add all commands in sorted order, depending on whether the profile can be modified.
            returnValue.Add(ProfileActionNames.NewProfile);
            returnValue.Add(ProfileActionNames.CloneProfile);

            if (Profiles.CanModifyActiveProfile)
            {
                returnValue.Add(ProfileActionNames.DeleteProfile);
                returnValue.Add(ProfileActionNames.ExportProfile);
            }

            returnValue.Add(ProfileActionNames.ImportProfile);

            if (Profiles.CanModifyActiveProfile)
            {
                returnValue.Add(ProfileActionNames.RenameProfile);
            }

            returnValue.Add(ProfileActionNames.ResetProfile);

            // Add all profiles names in sorted order.
            var profileNames = new List<string>(Profiles.All.Select(e => e.Name));
            profileNames.Sort();
            returnValue.AddRange(profileNames);

            return new StandardValuesCollection(returnValue);
        }
    }
}
