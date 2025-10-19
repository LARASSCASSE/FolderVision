namespace FolderVision.Core.Logging
{
    /// <summary>
    /// Defines the severity levels for logging
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Detailed diagnostic information for debugging
        /// </summary>
        Debug = 0,

        /// <summary>
        /// Informational messages about normal application flow
        /// </summary>
        Info = 1,

        /// <summary>
        /// Warning messages for potentially harmful situations
        /// </summary>
        Warning = 2,

        /// <summary>
        /// Error messages for error events that might still allow the application to continue
        /// </summary>
        Error = 3,

        /// <summary>
        /// Critical error messages for severe errors causing application failure
        /// </summary>
        Critical = 4,

        /// <summary>
        /// No logging
        /// </summary>
        None = 5
    }
}
