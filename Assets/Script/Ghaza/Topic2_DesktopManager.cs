using UnityEngine;
using UnityEngine.Events;

namespace GameTopic2
{
    /// <summary>
    /// Manages the desktop UI panels for Topic 2.
    /// Handles navigation between desktop, website (login/password renewal),
    /// messenger, and MFA activation panels.
    /// Tracks game state: whether the player has renewed their password
    /// and whether MFA has been activated.
    /// </summary>
    public class Topic2_DesktopManager : MonoBehaviour
    {
        [Header("Panels")]
        public GameObject desktopPanel;
        public GameObject chromePanel;
        public GameObject messengerPanel;
        public GameObject loginPanel;
        public GameObject mfaActivationPanel;

        [Header("Game State")]
        [Tooltip("Set to true after the player completes the one-time password renewal")]
        public bool hasRenewedPassword = false;

        [Tooltip("Set to true after the player activates MFA on their account")]
        public bool hasMFAEnabled = false;

        [Tooltip("The password strength chosen during renewal, used by brute force simulation later")]
        public PasswordStrength renewedPasswordStrength = PasswordStrength.Weak;

        [Header("Events")]
        [Tooltip("Fired when the player completes password renewal")]
        public UnityEvent onPasswordRenewed;

        [Tooltip("Fired when the player activates MFA")]
        public UnityEvent onMFAActivated;

        void Start()
        {
            // Initialize with only desktopPanel active
            ShowDesktopOnly();
        }

        /// <summary>
        /// Opens the Chrome/Website panel.
        /// If the player hasn't renewed their password yet, shows the login panel
        /// for mandatory password renewal (first clock-in).
        /// Otherwise just shows the chrome panel.
        /// </summary>
        public void OpenChrome()
        {
            CloseAllPanels();
            chromePanel.SetActive(true);

            if (!hasRenewedPassword)
            {
                // First interaction: mandatory password renewal for clock-in
                if (loginPanel != null)
                {
                    loginPanel.SetActive(true);
                }
            }
        }

        /// <summary>
        /// Opens the Messenger panel.
        /// </summary>
        public void OpenMessenger()
        {
            CloseAllPanels();
            messengerPanel.SetActive(true);
        }

        /// <summary>
        /// Opens the MFA activation panel.
        /// Called when the player returns to the PC later in the game
        /// to enable multi-factor authentication.
        /// </summary>
        public void OpenMFAActivation()
        {
            CloseAllPanels();
            if (mfaActivationPanel != null)
            {
                mfaActivationPanel.SetActive(true);
            }
        }

        /// <summary>
        /// Called by PasswordValidator when the player completes the
        /// one-time password renewal. Records the chosen password strength
        /// for the brute force simulation later.
        /// </summary>
        /// <param name="strength">The strength of the renewed password</param>
        public void OnPasswordRenewed(PasswordStrength strength)
        {
            hasRenewedPassword = true;
            renewedPasswordStrength = strength;

            onPasswordRenewed?.Invoke();

            // Return to desktop after successful renewal
            ReturnToDesktop();
        }

        /// <summary>
        /// Called by MFAActivationManager when the player enables MFA.
        /// </summary>
        public void OnMFAActivated()
        {
            hasMFAEnabled = true;

            onMFAActivated?.Invoke();
        }

        /// <summary>
        /// Closes all sub-panels and returns to the desktop view.
        /// </summary>
        public void ReturnToDesktop()
        {
            CloseAllPanels();
            desktopPanel.SetActive(true);
        }

        /// <summary>
        /// Closes all panels except the desktop panel.
        /// </summary>
        public void CloseAllPanels()
        {
            if (chromePanel != null) chromePanel.SetActive(false);
            if (messengerPanel != null) messengerPanel.SetActive(false);
            if (loginPanel != null) loginPanel.SetActive(false);
            if (mfaActivationPanel != null) mfaActivationPanel.SetActive(false);
        }

        private void ShowDesktopOnly()
        {
            CloseAllPanels();
            if (desktopPanel != null) desktopPanel.SetActive(true);
        }
    }
}
