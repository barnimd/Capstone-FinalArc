using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class FileItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public FileEntry file;
    public TMP_Text txtName;
    public Image bgImage;
    public Image lockIcon;
    public TMP_InputField inputRename;
    public Button renameBtn;
    public Button deleteBtn;

    [HideInInspector] public Transform originalParent;
    private Vector3 _dragOff;

    public void Setup(FileEntry f)
    {
        file = f;
        if (txtName) txtName.text = f.isLocked ? f.name + ".encrypted" : f.name;
        if (lockIcon) lockIcon.gameObject.SetActive(f.isLocked);
        if (bgImage)
        {
            if (f.isLocked) bgImage.color = new Color(0.8f, 0.15f, 0.15f, 0.45f);
            else if (f.isDeleted) bgImage.color = new Color(0.3f, 0.3f, 0.3f, 0.25f);
            else if (f.isImportant) bgImage.color = new Color(1f, 0.76f, 0.2f, 0.35f);
            else if (f.category == "Work") bgImage.color = new Color(0.2f, 0.5f, 0.9f, 0.2f);
            else if (f.category == "Personal") bgImage.color = new Color(0.2f, 0.8f, 0.5f, 0.2f);
            else bgImage.color = new Color(1, 1, 1, 0.12f);
        }
    }

    public void OnBeginDrag(PointerEventData e)
    {
        if (file == null || file.isLocked) return;
        originalParent = transform.parent;
        transform.SetParent(transform.root);
        _dragOff = transform.position - Input.mousePosition;
        GetComponent<CanvasGroup>().blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData e)
    {
        if (file == null || file.isLocked) return;
        transform.position = Input.mousePosition + _dragOff;
    }

    public void OnEndDrag(PointerEventData e)
    {
        GetComponent<CanvasGroup>().blocksRaycasts = true;

        // INTERCEPT: Important_Project selalu masuk Trash
        if (file != null && file.isImportant && !file.isLocked)
        {
            FileSystemManager.Instance.MoveFile(file.name, FileLocation.Trash);
            transform.SetParent(originalParent ?? transform.parent);
            return;
        }

        if (e.pointerEnter == null) { transform.SetParent(originalParent); return; }
        var dt = e.pointerEnter.GetComponent<DropTarget>();
        if (dt != null && file != null && !file.isLocked)
        {
            FileSystemManager.Instance.MoveFile(file.name, dt.targetLocation);
            transform.SetParent(originalParent ?? transform.parent);
        }
        else
            transform.SetParent(originalParent ?? transform.parent);
    }
}
