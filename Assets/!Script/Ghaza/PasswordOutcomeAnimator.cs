using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GameTopic2
{
    /// <summary>
    /// Manages the 3 outcome animation sequences after the player clicks
    /// "Renew Password". Each method is designed to be called from a UnityEvent
    /// on the PasswordValidator component.
    ///
    /// Setup:
    /// 1. Create 3 child panels under your Canvas: SuccessPanel, BruteforcePanel, InstantHackPanel
    /// 2. Drag this script onto a GameObject (e.g. "OutcomeAnimator")
    /// 3. Assign the panels and their child elements in the Inspector
    /// 4. On PasswordValidator, wire each UnityEvent to the matching method here:
    ///    - onSuccessSequence   -> PlaySuccessAnimation()
    ///    - onBruteforceSequence -> PlayBruteforceAnimation()
    ///    - onInstantHackSequence -> PlayInstantHackAnimation()
    /// </summary>
    public class PasswordOutcomeAnimator : MonoBehaviour
    {
        [Header("Panels (assign the root GameObjects)")]
        [Tooltip("Panel shown on Success route")]
        public GameObject successPanel;

        [Tooltip("Panel shown on Bruteforce route")]
        public GameObject bruteforcePanel;

        [Tooltip("Panel shown on Instant Hack route")]
        public GameObject instantHackPanel;

        [Header("Success Panel Elements")]
        [Tooltip("Icon image (e.g. green shield / checkmark)")]
        public Image successIcon;
        [Tooltip("Title text (e.g. 'Login Secure!')")]
        public TMP_Text successTitle;
        [Tooltip("Description text explaining why the account is safe")]
        public TMP_Text successDescription;

        [Header("Bruteforce Panel Elements")]
        [Tooltip("Icon image (e.g. orange warning)")]
        public Image bruteforceIcon;
        [Tooltip("Title text (e.g. 'Account Breached via Brute Force!')")]
        public TMP_Text bruteforceTitle;
        [Tooltip("Description explaining the brute force attack")]
        public TMP_Text bruteforceDescription;
        [Tooltip("Optional: a progress bar simulating the brute force attempt")]
        public Slider bruteforceProgressBar;
        [Tooltip("Optional: text showing brute force progress status")]
        public TMP_Text bruteforceStatusText;

        [Header("Instant Hack Panel Elements")]
        [Tooltip("Icon image (e.g. red skull / danger)")]
        public Image instantHackIcon;
        [Tooltip("Title text (e.g. 'Account Instantly Compromised!')")]
        public TMP_Text instantHackTitle;
        [Tooltip("Description explaining the instant compromise")]
        public TMP_Text instantHackDescription;

        [Header("Timing")]
        [Tooltip("Duration of the fade-in effect in seconds")]
        public float fadeInDuration = 0.5f;

        [Tooltip("Duration of the brute force progress bar fill simulation")]
        public float bruteforceFillDuration = 3.0f;

        [Tooltip("How long to display the outcome panel before auto-hiding (0 = never auto-hide)")]
        public float autoHideDelay = 0f;

        // ─────────────────────── PUBLIC API (wire to UnityEvents) ───────────────────────

        /// <summary>
        /// Route 1: Strong password (with or without MFA), or Medium + MFA.
        /// Shows a green success panel with a fade-in.
        /// </summary>
        public void PlaySuccessAnimation()
        {
            HideAllPanels();
            StartCoroutine(SuccessRoutine());
        }

        /// <summary>
        /// Route 2: Medium without MFA, or Weak with MFA.
        /// Shows a brute force simulation (progress bar filling up) then a breach message.
        /// </summary>
        public void PlayBruteforceAnimation()
        {
            HideAllPanels();
            StartCoroutine(BruteforceRoutine());
        }

        /// <summary>
        /// Route 3: Weak without MFA.
        /// Shows an immediate "hacked" panel with a dramatic flash.
        /// </summary>
        public void PlayInstantHackAnimation()
        {
            HideAllPanels();
            StartCoroutine(InstantHackRoutine());
        }

        // ─────────────────────── COROUTINE ANIMATIONS ───────────────────────

        private IEnumerator SuccessRoutine()
        {
            if (successPanel == null) yield break;

            // Set starting text
            if (successTitle != null) successTitle.text = "Login Secure!";
            if (successDescription != null)
                successDescription.text =
                    "Your password is strong and your account is protected.\n" +
                    "The attacker was unable to breach your login.";

            // Activate and fade in
            successPanel.SetActive(true);
            yield return StartCoroutine(FadeInCanvasGroup(successPanel, fadeInDuration));

            // Scale-pop the icon
            if (successIcon != null)
                yield return StartCoroutine(ScalePop(successIcon.transform, 0.3f));

            if (autoHideDelay > 0)
            {
                yield return new WaitForSeconds(autoHideDelay);
                successPanel.SetActive(false);
            }
        }

        private IEnumerator BruteforceRoutine()
        {
            if (bruteforcePanel == null) yield break;

            // Reset
            if (bruteforceProgressBar != null) bruteforceProgressBar.value = 0f;
            if (bruteforceStatusText != null) bruteforceStatusText.text = "Attempting brute force...";
            if (bruteforceTitle != null) bruteforceTitle.text = "Brute Force Attack In Progress...";
            if (bruteforceDescription != null)
                bruteforceDescription.text = "An attacker is trying password combinations...";

            bruteforcePanel.SetActive(true);
            yield return StartCoroutine(FadeInCanvasGroup(bruteforcePanel, fadeInDuration));

            // Simulate brute force progress bar
            if (bruteforceProgressBar != null)
            {
                float elapsed = 0f;
                string[] statusMessages = new[]
                {
                    "Trying common passwords...",
                    "Running dictionary attack...",
                    "Attempting variations...",
                    "Cracking password hash...",
                    "Almost there...",
                    "Password found!"
                };

                while (elapsed < bruteforceFillDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / bruteforceFillDuration;
                    bruteforceProgressBar.value = Mathf.Lerp(0f, 1f, t);

                    // Update status text at intervals
                    if (bruteforceStatusText != null)
                    {
                        int idx = Mathf.Clamp(
                            Mathf.FloorToInt(t * statusMessages.Length),
                            0, statusMessages.Length - 1);
                        bruteforceStatusText.text = statusMessages[idx];
                    }

                    yield return null;
                }

                bruteforceProgressBar.value = 1f;
            }

            // Final breach message
            if (bruteforceTitle != null) bruteforceTitle.text = "Account Breached!";
            if (bruteforceDescription != null)
                bruteforceDescription.text =
                    "The attacker successfully brute-forced your password.\n" +
                    "A stronger password or MFA would have prevented this.";

            if (bruteforceIcon != null)
                yield return StartCoroutine(ScalePop(bruteforceIcon.transform, 0.3f));

            if (autoHideDelay > 0)
            {
                yield return new WaitForSeconds(autoHideDelay);
                bruteforcePanel.SetActive(false);
            }
        }

        private IEnumerator InstantHackRoutine()
        {
            if (instantHackPanel == null) yield break;

            if (instantHackTitle != null) instantHackTitle.text = "Account Instantly Compromised!";
            if (instantHackDescription != null)
                instantHackDescription.text =
                    "Your password was so weak it was cracked instantly.\n" +
                    "Without MFA, there was no second line of defense.\n" +
                    "Always use a strong password AND enable MFA!";

            instantHackPanel.SetActive(true);

            // Flash effect: rapidly toggle visibility
            CanvasGroup cg = GetOrAddCanvasGroup(instantHackPanel);
            for (int i = 0; i < 4; i++)
            {
                cg.alpha = 0f;
                yield return new WaitForSeconds(0.08f);
                cg.alpha = 1f;
                yield return new WaitForSeconds(0.08f);
            }

            // Shake effect on the icon
            if (instantHackIcon != null)
                yield return StartCoroutine(ShakeEffect(instantHackIcon.transform, 0.4f, 8f));

            if (autoHideDelay > 0)
            {
                yield return new WaitForSeconds(autoHideDelay);
                instantHackPanel.SetActive(false);
            }
        }

        // ─────────────────────── UTILITY HELPERS ───────────────────────

        private void HideAllPanels()
        {
            if (successPanel != null) successPanel.SetActive(false);
            if (bruteforcePanel != null) bruteforcePanel.SetActive(false);
            if (instantHackPanel != null) instantHackPanel.SetActive(false);
        }

        private CanvasGroup GetOrAddCanvasGroup(GameObject go)
        {
            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            return cg;
        }

        /// <summary>Fades a panel in from alpha 0 to 1 using a CanvasGroup.</summary>
        private IEnumerator FadeInCanvasGroup(GameObject panel, float duration)
        {
            CanvasGroup cg = GetOrAddCanvasGroup(panel);
            cg.alpha = 0f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }

            cg.alpha = 1f;
        }

        /// <summary>Quick scale-up pop effect on a Transform (1 -> 1.3 -> 1).</summary>
        private IEnumerator ScalePop(Transform target, float duration)
        {
            Vector3 original = target.localScale;
            Vector3 popped = original * 1.3f;
            float half = duration / 2f;

            float elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                target.localScale = Vector3.Lerp(original, popped, elapsed / half);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                target.localScale = Vector3.Lerp(popped, original, elapsed / half);
                yield return null;
            }

            target.localScale = original;
        }

        /// <summary>Horizontal shake effect on a Transform.</summary>
        private IEnumerator ShakeEffect(Transform target, float duration, float magnitude)
        {
            Vector3 original = target.localPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float x = original.x + Random.Range(-magnitude, magnitude);
                target.localPosition = new Vector3(x, original.y, original.z);
                yield return null;
            }

            target.localPosition = original;
        }
    }
}
