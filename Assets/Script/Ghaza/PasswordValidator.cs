using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

namespace GameTopic2
{
    /// <summary>
    /// Real-time password strength validator for the mandatory password renewal flow.
    /// The player (an existing employee) must renew their expired password
    /// before they can clock in for attendance.
    /// 
    /// Evaluates passwords based on length and complexity criteria,
    /// updating UI elements to provide immediate educational feedback.
    /// The "Renew Password" button only becomes active when the password is Strong.
    /// </summary>
    public class PasswordValidator : MonoBehaviour
    {
        [Header("UI Elements")]
        public TMP_InputField passwordInput;
        public Slider strengthBar;
        public TMP_Text strengthLabel;
        public GameObject tooltipWeak;
        public GameObject tooltipNoCaps;

        [Header("Renewal UI")]
        [Tooltip("The button the player clicks to confirm their new password. Enabled as long as it's not empty.")]
        public Button renewButton;

        [Tooltip("Optional text showing the old/expired password hint (e.g. 'Your password has expired')")]
        public TMP_Text expirationNoticeText;

        [Header("Manager Reference")]
        [Tooltip("Reference to the desktop manager to notify when renewal is complete")]
        public Topic2_DesktopManager desktopManager;

        [Header("Animation Sequences")]
        [Tooltip("Fired when the password is Strong, or Medium with MFA (Success)")]
        public UnityEvent onSuccessSequence;
        
        [Tooltip("Fired when the password is Medium with no MFA, or Weak with MFA (Bruteforce)")]
        public UnityEvent onBruteforceSequence;
        
        [Tooltip("Fired when the password is Weak with no MFA (Instant Hack)")]
        public UnityEvent onInstantHackSequence;

        /// <summary>
        /// Current password strength classification.
        /// Updated on every password input change.
        /// </summary>
        public PasswordStrength CurrentStrength { get; private set; }

        /// <summary>
        /// Initialize component and subscribe to password input events.
        /// </summary>
        void Start()
        {
            if (passwordInput != null)
            {
                passwordInput.onValueChanged.AddListener(OnPasswordChanged);
            }
            else
            {
                Debug.LogError("PasswordValidator: passwordInput reference is null");
            }

            // Ensure renew button starts disabled
            if (renewButton != null)
            {
                renewButton.interactable = false;
                renewButton.onClick.AddListener(OnRenewClicked);
            }

            // Set initial expiration notice
            if (expirationNoticeText != null)
            {
                expirationNoticeText.text = "Password Anda telah kedaluwarsa. Silakan buat password baru.";
            }
        }

        /// <summary>
        /// Event handler for password input changes.
        /// Triggers real-time validation and UI updates.
        /// </summary>
        /// <param name="password">Current password string from input field</param>
        public void OnPasswordChanged(string password)
        {
            // Evaluate password and store result
            CurrentStrength = EvaluatePassword(password);

            // Calculate a progressive bar value based on score
            float barValue = CalculateBarValue(password);

            // Update strength bar
            if (strengthBar != null)
            {
                strengthBar.value = barValue;

                // Update strength bar color via fill rect
                Image fillImage = strengthBar.fillRect?.GetComponent<Image>();
                if (fillImage != null)
                {
                    switch (CurrentStrength)
                    {
                        case PasswordStrength.Weak:
                            fillImage.color = Color.red;
                            break;
                        case PasswordStrength.Medium:
                            fillImage.color = Color.yellow;
                            break;
                        case PasswordStrength.Strong:
                            fillImage.color = Color.green;
                            break;
                    }
                }
            }

            // Update strength label text
            if (strengthLabel != null)
            {
                switch (CurrentStrength)
                {
                    case PasswordStrength.Weak:
                        strengthLabel.text = "Weak";
                        strengthLabel.color = Color.red;
                        break;
                    case PasswordStrength.Medium:
                        strengthLabel.text = "Medium";
                        strengthLabel.color = Color.yellow;
                        break;
                    case PasswordStrength.Strong:
                        strengthLabel.text = "Strong";
                        strengthLabel.color = Color.green;
                        break;
                }
            }

            // Analyze individual criteria for tooltip display
            bool hasCapitals = password.Any(char.IsUpper);
            bool hasNumbers = password.Any(char.IsDigit);
            bool hasSymbols = password.Any(c => "!@#$%^&*()_+-=[]{}|;:',.<>?/~`".Contains(c));

            // tooltipWeak: shown when password is Weak (too short / no complexity)
            if (tooltipWeak != null)
            {
                tooltipWeak.SetActive(CurrentStrength == PasswordStrength.Weak && password.Length > 0);
            }

            // tooltipNoCaps: shown when password has no capitals and isn't Strong yet
            if (tooltipNoCaps != null)
            {
                tooltipNoCaps.SetActive(!hasCapitals && password.Length > 0 && CurrentStrength != PasswordStrength.Strong);
            }

            // Enable renew button only when password is not empty
            if (renewButton != null)
            {
                renewButton.interactable = !string.IsNullOrEmpty(password);
            }
        }

        /// <summary>
        /// Calculates a progressive bar value (0.0 - 1.0) based on password characteristics.
        /// The bar fills gradually as the user adds length and complexity.
        /// </summary>
        private float CalculateBarValue(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return 0f;
            }

            // Length contributes up to 0.5 (maxes out at 12 chars)
            float lengthScore = Mathf.Clamp01(password.Length / 12f) * 0.5f;

            // Complexity contributes up to 0.5 (each criterion = ~0.167)
            float complexityScore = 0f;
            if (password.Any(char.IsUpper)) complexityScore += 0.167f;
            if (password.Any(char.IsDigit)) complexityScore += 0.167f;
            if (password.Any(c => "!@#$%^&*()_+-=[]{}|;:',.<>?/~`".Contains(c))) complexityScore += 0.167f;

            return Mathf.Clamp01(lengthScore + complexityScore);
        }

        /// <summary>
        /// Called when the player clicks the "Renew Password" button.
        /// Evaluates strength + MFA state to determine which outcome sequence to trigger.
        /// </summary>
        public void OnRenewClicked()
        {
            string passwordText = passwordInput != null ? passwordInput.text : "";
            
            if (string.IsNullOrEmpty(passwordText))
            {
                Debug.LogWarning("PasswordValidator: Renew clicked but password is empty.");
                return;
            }

            Debug.Log($"PasswordValidator: Password renewed. Strength: {CurrentStrength}");

            // Notify the desktop manager with both string and strength (optional)
            if (desktopManager != null)
            {
                desktopManager.OnPasswordRenewed(passwordText, CurrentStrength);
            }
            else
            {
                Debug.LogWarning("PasswordValidator: desktopManager reference is not assigned. Skipping manager notification.");
            }

            // Determine which sequence to play based on Strength and MFA status
            bool mfaEnabled = desktopManager != null && desktopManager.hasMFAEnabled;

            if (CurrentStrength == PasswordStrength.Strong)
            {
                // Strong always succeeds
                onSuccessSequence?.Invoke();
            }
            else if (CurrentStrength == PasswordStrength.Medium)
            {
                if (mfaEnabled)
                {
                    // Medium + MFA = Success
                    onSuccessSequence?.Invoke();
                }
                else
                {
                    // Medium + No MFA = Bruteforce success
                    onBruteforceSequence?.Invoke();
                }
            }
            else if (CurrentStrength == PasswordStrength.Weak)
            {
                if (mfaEnabled)
                {
                    // Weak + MFA = Slowed down, but still brute forced
                    onBruteforceSequence?.Invoke();
                }
                else
                {
                    // Weak + No MFA = Instant hack
                    onInstantHackSequence?.Invoke();
                }
            }
        }

        /// <summary>
        /// Evaluates password strength based on length and complexity criteria.
        /// 
        /// 🔴 Weak: password is too short (under 8 chars) or has zero complexity
        ///    - Empty or less than 5 chars = always Weak
        ///    - 5-7 chars with no numbers, no symbols, no capitals = Weak
        ///    - Under 8 chars regardless = Weak
        /// 
        /// 🟢 Strong: 12+ chars AND has numbers AND symbols AND capitals
        /// 
        /// 🟡 Medium: everything in between (8-11 chars with some complexity,
        ///    or 12+ chars missing one or more criteria)
        /// </summary>
        /// <param name="password">Password string to evaluate</param>
        /// <returns>PasswordStrength classification (Weak, Medium, or Strong)</returns>
        public PasswordStrength EvaluatePassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return PasswordStrength.Weak;
            }

            int length = password.Length;
            bool hasNumbers = password.Any(char.IsDigit);
            bool hasSymbols = password.Any(c => "!@#$%^&*()_+-=[]{}|;:',.<>?/~`".Contains(c));
            bool hasCapitals = password.Any(char.IsUpper);
            bool hasLowercase = password.Any(char.IsLower);

            int complexityCount = 0;
            if (hasNumbers) complexityCount++;
            if (hasSymbols) complexityCount++;
            if (hasCapitals) complexityCount++;

            // Strong: 12+ chars with all three complexity criteria
            if (length >= 12 && hasNumbers && hasSymbols && hasCapitals)
            {
                return PasswordStrength.Strong;
            }

            // Weak: too short or no complexity at all
            if (length < 8 || complexityCount == 0)
            {
                return PasswordStrength.Weak;
            }

            // Medium: 8+ chars with at least some complexity (but not Strong)
            return PasswordStrength.Medium;
        }
    }
}
