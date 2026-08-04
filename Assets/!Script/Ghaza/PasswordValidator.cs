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

        [Header("MFA Choice Panel")]
        [Tooltip("Panel shown after clicking Renew — asks player whether to enable MFA before evaluating outcome")]
        public GameObject mfaChoicePanel;

        [Header("Animation Sequences")]
        [Tooltip("Fired when the password is Strong, or Medium with MFA (Success)")]
        public UnityEvent onSuccessSequence;

        [Tooltip("Fired when the password is Medium with no MFA, or Weak with MFA (Bruteforce)")]
        public UnityEvent onBruteforceSequence;

        [Tooltip("Fired when the password is Weak with no MFA (Instant Hack)")]
        public UnityEvent onInstantHackSequence;

        [Header("Scoring")]
        [Tooltip("Score awarded for a Strong password")]
        public int scoreStrong = 50;
        [Tooltip("Score awarded for a Medium password")]
        public int scoreMedium = 25;
        [Tooltip("Score awarded for a Weak password")]
        public int scoreWeak = 10;
        [Tooltip("Bonus score awarded when MFA is successfully activated")]
        public int scoreMFABonus = 50;
        [Tooltip("Optional: TMP_Text to display the final score on screen")]
        public TMP_Text scoreDisplayText;

        /// <summary>
        /// Current password strength classification.
        /// Updated on every password input change.
        /// </summary>
        public PasswordStrength CurrentStrength { get; private set; }

        /// <summary>
        /// Final score calculated after the player completes the flow.
        /// Password score (10/25/50) + MFA bonus (0/50).
        /// </summary>
        public int FinalScore { get; private set; }

        /// <summary>
        /// Initialize component and subscribe to password input events.
        /// </summary>
        void Start()
        {
            PlayerRunRecorder.Ensure("2fa");
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

            // Length contributes up to 0.5 (maxes out at the taught 12-char minimum)
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
        /// Caches the current password strength, then opens the MFA choice panel
        /// instead of immediately triggering an outcome.
        /// </summary>
        public void OnRenewClicked()
        {
            string passwordText = passwordInput != null ? passwordInput.text : "";

            if (string.IsNullOrEmpty(passwordText))
            {
                Debug.LogWarning("PasswordValidator: Renew clicked but password is empty.");
                return;
            }

            // Cache strength now — will be used once player makes the MFA choice
            CurrentStrength = EvaluatePassword(passwordText);
            Debug.Log($"PasswordValidator: Password evaluated. Strength: {CurrentStrength}. Awaiting MFA choice.");

            if (mfaChoicePanel != null)
            {
                mfaChoicePanel.SetActive(true);
            }
            else
            {
                Debug.LogWarning("PasswordValidator: mfaChoicePanel not assigned. Falling back to no-MFA outcome.");
                TriggerOutcome(false);
            }
        }

        /// <summary>
        /// Called by the "Aktifkan MFA" and "Lewati" buttons inside the MFA choice panel.
        /// Closes the panel, notifies the manager, and triggers the correct outcome animation.
        /// </summary>
        /// <param name="enableMFA">True if the player chose to enable MFA, false otherwise.</param>
        public void OnMFAChosen(bool enableMFA)
        {
            string passwordText = passwordInput != null ? passwordInput.text : "";
            bool hasUppercase = passwordText.Any(char.IsUpper);
            bool hasLowercase = passwordText.Any(char.IsLower);
            bool hasNumber = passwordText.Any(char.IsDigit);
            bool hasSymbol = passwordText.Any(c => "!@#$%^&*()_+-=[]{}|;:',.<>?/~`".Contains(c));
            string strengthId = CurrentStrength.ToString().ToLowerInvariant();
            string outcomeId = ResolveSecurityOutcome(enableMFA);
            int passwordScore = CurrentStrength == PasswordStrength.Strong ? scoreStrong :
                                CurrentStrength == PasswordStrength.Medium ? scoreMedium : scoreWeak;

            PlayerRunRecorder.RecordDetailed(
                "password.creation",
                strengthId,
                "classified",
                passwordScore,
                "length_bucket", GetLengthBucket(passwordText.Length),
                "has_uppercase", hasUppercase ? "yes" : "no",
                "has_lowercase", hasLowercase ? "yes" : "no",
                "has_number", hasNumber ? "yes" : "no",
                "has_symbol", hasSymbol ? "yes" : "no");
            PlayerRunRecorder.RecordDetailed(
                "mfa.choice",
                enableMFA ? "enable" : "skip",
                enableMFA ? "protected" : "single_factor",
                enableMFA ? scoreMFABonus : 0);
            PlayerRunRecorder.RecordDetailed("password.outcome", outcomeId, outcomeId, 0);
            if (mfaChoicePanel != null)
                mfaChoicePanel.SetActive(false);

            // Update MFA state on manager before notifying renewal
            if (desktopManager != null)
            {
                desktopManager.hasMFAEnabled = enableMFA;
                desktopManager.OnPasswordRenewed(passwordText, CurrentStrength);
            }
            else
            {
                Debug.LogWarning("PasswordValidator: desktopManager reference is not assigned. Skipping manager notification.");
            }

            TriggerOutcome(enableMFA);
        }

        private string ResolveSecurityOutcome(bool mfaEnabled)
        {
            if (CurrentStrength == PasswordStrength.Strong ||
                (CurrentStrength == PasswordStrength.Medium && mfaEnabled))
                return "success";
            if (CurrentStrength == PasswordStrength.Weak && !mfaEnabled)
                return "instant_hack";
            return "brute_force";
        }

        private static string GetLengthBucket(int length)
        {
            if (length < 6) return "under_6";
            if (length < 8) return "6_to_7";
            if (length < 12) return "8_to_11";
            return "12_plus";
        }

        /// <summary>
        /// Fires the correct outcome UnityEvent based on password strength and MFA choice.
        /// Also calculates and stores the FinalScore.
        /// </summary>
        private void TriggerOutcome(bool mfaEnabled)
        {
            // ── Scoring ──────────────────────────────────────────────────
            int passwordScore = CurrentStrength == PasswordStrength.Strong ? scoreStrong :
                                CurrentStrength == PasswordStrength.Medium ? scoreMedium : scoreWeak;
            int mfaScore = mfaEnabled ? scoreMFABonus : 0;
            FinalScore = passwordScore + mfaScore;

            Debug.Log($"PasswordValidator: Final Score = {FinalScore} (Password: {passwordScore}, MFA: {mfaScore})");

            if (scoreDisplayText != null)
                scoreDisplayText.text = $"Score: {FinalScore}";

            // ── Kirim ke ScoreManager ─────────────────────────────────────
            if (ScoreManager.instance != null)
                ScoreManager.instance.SetScore(FinalScore);

            // ── Outcome animation ─────────────────────────────────────────
            if (CurrentStrength == PasswordStrength.Strong)
            {
                // Strong always succeeds
                onSuccessSequence?.Invoke();
            }
            else if (CurrentStrength == PasswordStrength.Medium)
            {
                if (mfaEnabled)
                    onSuccessSequence?.Invoke();       // Medium + MFA = Success
                else
                    onBruteforceSequence?.Invoke();    // Medium + No MFA = Bruteforce
            }
            else if (CurrentStrength == PasswordStrength.Weak)
            {
                if (mfaEnabled)
                    onBruteforceSequence?.Invoke();    // Weak + MFA = Bruteforce
                else
                    onInstantHackSequence?.Invoke();   // Weak + No MFA = Instant hack
            }
        }

        /// <summary>
        /// Evaluates password strength based on length and complexity criteria.
        /// 
        /// 🔴 Weak: kurang dari 6 karakter ATAU tidak ada complexity sama sekali
        ///
        /// 🟢 Strong: 12+ karakter AND ada angka AND ada simbol AND ada huruf besar
        ///
        /// 🟡 Medium: antara Weak dan Strong (6+ karakter dengan sebagian criteria)
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

            // Strong: 12+ chars with all three complexity criteria.
            // This matches the minimum length taught by EvaluationData_Tp2.
            if (length >= 12 && hasNumbers && hasSymbols && hasCapitals)
            {
                return PasswordStrength.Strong;
            }

            // Weak: too short or no complexity at all
            if (length < 6 || complexityCount == 0)
            {
                return PasswordStrength.Weak;
            }

            // Medium: 6+ chars with at least some complexity (but not Strong)
            return PasswordStrength.Medium;
        }
    }
}
