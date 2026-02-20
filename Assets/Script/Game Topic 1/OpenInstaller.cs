using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenInstaller : MonoBehaviour
{
    public GameObject InstallPopup;

    public void OpenPopup()
    {
        InstallPopup.SetActive(true);
    }
}
