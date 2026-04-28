using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SidebarNavigation : MonoBehaviour
{
    [Header("Pages / Panels")]
    public GameObject dashboardPage;
    public GameObject classPage;
    public GameObject profilePage;
    public GameObject leaderboardPage;
    public GameObject getHelpPage;
    public GameObject settingsPage;

    void Start()
    {
        OpenDashboard(); // default page saat game mulai
    }

    // =====================
    // BUTTON FUNCTIONS
    // =====================

    public void OpenDashboard()
    {
        HideAllPages();
        dashboardPage.SetActive(true);
    }

    public void OpenClass()
    {
        HideAllPages();
        classPage.SetActive(true);
    }

    public void OpenProfile()
    {
        HideAllPages();
        profilePage.SetActive(true);
    }

    public void OpenLeaderboard()
    {
        HideAllPages();
        leaderboardPage.SetActive(true);

    }

    public void OpenGetHelp()
    {
        HideAllPages();
        getHelpPage.SetActive(true);

    }

    public void OpenSettings()
    {
        HideAllPages();
        settingsPage.SetActive(true);

    }

    public void CloseGetHelp()
    {
        getHelpPage.SetActive(false);
        OpenDashboard(); // kembali ke dashboard setelah tutup get help
    }

    public void CloseSettings()
    {
        settingsPage.SetActive(false);
        OpenDashboard(); // kembali ke dashboard setelah tutup settings
    }

    // =====================
    // HELPERS
    // =====================

    void HideAllPages()
    {
        dashboardPage.SetActive(false);
        classPage.SetActive(false);
        profilePage.SetActive(false);
        leaderboardPage.SetActive(false);
        getHelpPage.SetActive(false);
        settingsPage.SetActive(false);
    }
}