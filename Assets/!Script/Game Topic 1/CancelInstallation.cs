using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CancelInstallation : MonoBehaviour
{
    public GameObject popup;

    public void Close()
    {
        popup.SetActive(false);
    }
}
