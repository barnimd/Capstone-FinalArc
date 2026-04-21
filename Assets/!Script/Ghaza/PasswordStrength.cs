namespace GameTopic2
{
    /// <summary>
    /// Enum representing password strength levels for brute force simulation.
    /// Used by PasswordValidator to classify passwords based on length and complexity.
    /// </summary>
    public enum PasswordStrength
    {
        /// <summary>
        /// Weak password: 5-6 characters, no complexity (no numbers, symbols, or capitals)
        /// </summary>
        Weak,
        
        /// <summary>
        /// Medium password: 7-11 characters or partial complexity (1-2 criteria met)
        /// </summary>
        Medium,
        
        /// <summary>
        /// Strong password: 12+ characters with full complexity (numbers, symbols, capitals)
        /// </summary>
        Strong
    }
}
