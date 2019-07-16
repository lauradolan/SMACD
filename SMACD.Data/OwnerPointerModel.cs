namespace SMACD.Data
{
    /// <summary>
    ///     Stores information about a business owner
    /// </summary>
    public class OwnerPointerModel : IModel
    {
        /// <summary>
        ///     Name of the owner
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Contact e-mail for the owner
        /// </summary>
        public string Email { get; set; }
    }
}