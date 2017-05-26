using System;

namespace Slalom.Stacks.Logging.SqlServer.Settings
{
    /// <summary>
    /// Settings for locations using MaxMind.
    /// </summary>
    public class LocationSettings
    {
        /// <summary>
        /// Gets or sets the account key.
        /// </summary>
        /// <value>
        /// The account key.
        /// </value>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this locations should be on.
        /// </summary>
        /// <value>
        ///   <c>true</c> if on; otherwise, <c>false</c>.
        /// </value>
        public bool On { get; set; } = false;

        /// <summary>
        /// Gets or sets the MaxMind user ID.
        /// </summary>
        /// <value>
        /// The MaxMind user ID.
        /// </value>
        public int UserId { get; set; }
    }
}