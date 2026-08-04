using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GameTopic2
{
    /// <summary>
    /// Manages the MFA choice and code verification flow inside the MFA Panel.
    ///
    /// Panel structure expected:
    ///   MFA Panel  (this script attached here)
    ///   ├── MFA Holder UI        ← shown first: "Aktifkan" / "Nanti Saja" buttons
    ///   └── MFA inputPanel       ← shown after "Aktifkan": code entry UI
    ///       └── MFA Kode Panel   ← popup showing the 6-digit code
    ///
    /// Wire each button's onClick in the Inspector to the matching public method.
    /// </summary>
    public class MFAFlowController : MonoBehaviour
    {
        [Header("Panels")]
        [Tooltip("The initial panel with Aktifkan / Nanti Saja buttons")]
        public GameObject mfaHolderUI;

        [Tooltip("The code-entry panel shown after player clicks Aktifkan")]
        public GameObject mfaInputPanel;

        [Tooltip("The popup that displays the 6-digit OTP code")]
        public GameObject mfaKodePanel;

        [Header("Input Panel Elements")]
        [Tooltip("The TMP_InputField where the player types the code")]
        public TMP_InputField codeInputField;

        [Tooltip("The TextMeshProUGUI inside MFA Kode Panel that displays the code")]
        public TMP_Text isiKodeText;

        [Header("Reference")]
        [Tooltip("Drag the RenewPasswordButton GameObject here (it has PasswordValidator)")]
        public PasswordValidator passwordValidator;

        // The currently valid 6-digit code. Empty string means no code is active.
        private string _validCode = "";
        private int _verificationAttempts;
        private int _resendCount;

        // ─────────────────────── UNITY LIFECYCLE ───────────────────────

        /// <summary>
        /// Whenever this panel becomes active, reset to the initial Holder UI state.
        /// </summary>
        private void OnEnable()
        {
            ShowHolderUI();
        }

        // ─────────────────────── BUTTON HANDLERS ───────────────────────

        /// <summary>
        /// "Aktifkan MFA" button.
        /// Hides HolderUI, shows InputPanel, generates and displays a new code.
        /// </summary>
        public void OnAktifkanClicked()
        {
            SetHolderUI(false);
            SetInputPanel(true);
            GenerateAndShowCode();
        }

        /// <summary>
        /// "Nanti Saja" button.
        /// Closes the whole MFA panel and proceeds without MFA.
        /// </summary>
        public void OnNantiSajaClicked()
        {
            RecordVerification("skipped", "not_verified");
            _validCode = "";
            gameObject.SetActive(false);

            if (passwordValidator != null)
                passwordValidator.OnMFAChosen(false);
            else
                Debug.LogWarning("MFAFlowController: passwordValidator reference not assigned.");
        }

        /// <summary>
        /// "Kirim Ulang" button.
        /// Closes the existing KodePanel, invalidates the old code, and shows a new one.
        /// </summary>
        public void OnKirimUlangClicked()
        {
            _resendCount++;
            // Close existing popup first, then generate new code
            if (mfaKodePanel != null)
                mfaKodePanel.SetActive(false);

            _validCode = ""; // invalidate old code immediately
            GenerateAndShowCode();
        }

        /// <summary>
        /// "Submit Kode" button.
        /// Validates the typed code against the current valid code.
        /// On success: closes panel and calls OnMFAChosen(true).
        /// On failure: clears input and shows error in the placeholder.
        /// </summary>
        public void OnSubmitKodeClicked()
        {
            if (codeInputField == null) return;

            string entered = codeInputField.text.Trim();

            if (string.IsNullOrEmpty(_validCode))
            {
                ShowInputError("Belum ada kode aktif. Tekan Kirim Ulang terlebih dahulu.");
                return;
            }

            if (entered == _validCode)
            {
                _verificationAttempts++;
                RecordVerification("verified", "otp_verified");
                // Code is correct — MFA activated
                _validCode = "";
                gameObject.SetActive(false);

                if (passwordValidator != null)
                    passwordValidator.OnMFAChosen(true);
                else
                    Debug.LogWarning("MFAFlowController: passwordValidator reference not assigned.");
            }
            else
            {
                _verificationAttempts++;
                // Wrong code
                ShowInputError("Kode tidak valid. Coba kirim ulang atau masukan kode kembali.");
            }
        }

        /// <summary>
        /// "Batalkan" button inside InputPanel.
        /// Returns to the Holder UI without activating MFA.
        /// </summary>
        public void OnBatalkanClicked()
        {
            _validCode = "";

            if (mfaKodePanel != null)
                mfaKodePanel.SetActive(false);

            ResetInputField();
            SetInputPanel(false);
            SetHolderUI(true);
        }

        /// <summary>
        /// "close btn" inside MFA Kode Panel.
        /// Only closes the code popup; the InputPanel stays open.
        /// </summary>
        public void OnCloseKodePanelClicked()
        {
            if (mfaKodePanel != null)
                mfaKodePanel.SetActive(false);
        }

        // ─────────────────────── PRIVATE HELPERS ───────────────────────

        /// <summary>Resets to initial state: HolderUI visible, everything else hidden.</summary>
        private void ShowHolderUI()
        {
            ResetInputField();
            _validCode = "";
            _verificationAttempts = 0;
            _resendCount = 0;

            if (mfaKodePanel != null)  mfaKodePanel.SetActive(false);

            SetInputPanel(false);
            SetHolderUI(true);
        }

        private void SetHolderUI(bool active)
        {
            if (mfaHolderUI != null) mfaHolderUI.SetActive(active);
        }

        private void SetInputPanel(bool active)
        {
            if (mfaInputPanel != null) mfaInputPanel.SetActive(active);
        }

        /// <summary>Generates a new 6-digit code, stores it, and shows the KodePanel.</summary>
        private void GenerateAndShowCode()
        {
            _validCode = Random.Range(100000, 1000000).ToString();

            if (isiKodeText != null)
                isiKodeText.text = _validCode;

            if (mfaKodePanel != null)
                mfaKodePanel.SetActive(true);

            Debug.Log($"MFAFlowController: New OTP generated — {_validCode}");
        }

        /// <summary>
        /// Clears the input field and resets the placeholder to its default text and color.
        /// </summary>
        private void RecordVerification(string choiceId, string outcomeId)
        {
            PlayerRunRecorder.RecordDetailed(
                "mfa.verification",
                choiceId,
                outcomeId,
                0,
                "attempt_count", CountBucket(_verificationAttempts),
                "resend_count", CountBucket(_resendCount));
        }

        private static string CountBucket(int count)
        {
            return count >= 5 ? "5_plus" : count.ToString();
        }

        private void ResetInputField()
        {
            if (codeInputField == null) return;

            codeInputField.text = "";

            TMP_Text placeholder = codeInputField.placeholder as TMP_Text;
            if (placeholder != null)
            {
                placeholder.text = "Masukan kode MFA...";
                placeholder.color = new Color(1f, 1f, 1f, 0.5f); // default white semi-transparent
            }
        }

        /// <summary>
        /// Clears the input field and shows an error message in the placeholder.
        /// </summary>
        private void ShowInputError(string message)
        {
            codeInputField.text = "";

            TMP_Text placeholder = codeInputField.placeholder as TMP_Text;
            if (placeholder != null)
            {
                placeholder.text = message;
                placeholder.color = new Color(1f, 0.27f, 0.27f, 1f); // merah
            }
        }
    }
}
