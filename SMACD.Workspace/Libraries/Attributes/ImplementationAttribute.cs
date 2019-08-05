using System;

namespace SMACD.Workspace.Libraries.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ImplementationAttribute : Attribute
    {
        /// <summary>
        /// Denotes this class as providing the implementation for an Action for the system
        /// </summary>
        /// <param name="role">Action Role</param>
        /// <param name="identifier">Local Action identifier</param>
        public ImplementationAttribute(ExtensionRoles role, string identifier)
        {
            Role = role;
            if (identifier.Contains('.'))
            {
                throw new Exception("Identifier must not contain a period");
            }

            Identifier = identifier;
        }

        /// <summary>
        /// Role of Action in the system
        /// </summary>
        public ExtensionRoles Role { get; }

        /// <summary>
        /// Local, unique Identifier representing the Action within the system
        /// </summary>
        public string Identifier { get; }

        /// <summary>
        /// Full Identifier that represents the Identifier with Role
        /// </summary>
        public string FullIdentifier => $"{Role.ToString().ToLower()}.{Identifier}";
    }
}
