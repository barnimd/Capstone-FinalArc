using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenChat : MonoBehaviour
{
    public GameObject ChatPanel;

    public void OpenPopup()
    {
        ChatPanel.SetActive(true);
    }
}
