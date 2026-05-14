using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DropTarget : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public FileLocation targetLocation;
    public Image bg;
    private Color _orig;

    void Start() { if (bg) _orig = bg.color; }

    public void OnPointerEnter(PointerEventData e) { if (bg) bg.color = new Color(1, 0.85f, 0.3f, 0.4f); }
    public void OnPointerExit(PointerEventData e) { if (bg) bg.color = _orig; }

    public void OnDrop(PointerEventData e)
    {
        if (bg) bg.color = _orig;
        var fi = e.pointerDrag?.GetComponent<FileItem>();
        if (fi != null && fi.file != null && !fi.file.isLocked)
            FileSystemManager.Instance.MoveFile(fi.file.name, targetLocation);
    }
}
