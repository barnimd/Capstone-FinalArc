using System.Collections.Generic;
using UnityEngine;

public enum FileLocation { Desktop, WorkFiles, Personal, Downloads, ImportantProject, Trash }

public class FileSystemManager : MonoBehaviour
{
    public static FileSystemManager Instance { get; private set; }

    public List<FileEntry> allFiles = new List<FileEntry>();
    public System.Action OnDataChanged;
    public System.Action OnImportantLost;

    private void Awake()
    {
        Instance = this;
        ResetFiles();
    }

    public void ResetFiles()
    {
        allFiles.Clear();
        allFiles.Add(new FileEntry { name = "work_report.pdf",     category = "Work",     location = FileLocation.Desktop });
        allFiles.Add(new FileEntry { name = "presentation.pptx",   category = "Work",     location = FileLocation.Desktop });
        allFiles.Add(new FileEntry { name = "image_pribadi.jpg",   category = "Personal", location = FileLocation.Desktop });
        allFiles.Add(new FileEntry { name = "vacation.png",        category = "Personal", location = FileLocation.Desktop });
        allFiles.Add(new FileEntry { name = "random.zip",          category = "Other",    location = FileLocation.Desktop });
        allFiles.Add(new FileEntry { name = "temp_file.tmp",       category = "Other",    location = FileLocation.Desktop });
        allFiles.Add(new FileEntry { name = "Important_Project",   category = "System",   location = FileLocation.Desktop, isImportant = true });
    }

    public FileEntry GetFile(string name) => allFiles.Find(f => f.name == name);

    public List<FileEntry> GetFilesAt(FileLocation loc) => allFiles.FindAll(f => f.location == loc && !f.isDeleted);

    public List<FileEntry> GetDeletedFiles() => allFiles.FindAll(f => f.isDeleted);

    public void MoveFile(string name, FileLocation destination)
    {
        var f = GetFile(name);
        if (f == null || f.isLocked) return;
        f.location = destination;
        f.isDeleted = (destination == FileLocation.Trash);
        if (f.isImportant) CheckImportant();
        OnDataChanged?.Invoke();
    }

    public void RenameFile(string oldName, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) return;
        var f = GetFile(oldName);
        if (f == null || f.isLocked) return;
        f.name = newName;
        if (f.isImportant) CheckImportant();
        OnDataChanged?.Invoke();
    }

    public void DeleteFile(string name) => MoveFile(name, FileLocation.Trash);

    public void RestoreFile(string name)
    {
        var f = GetFile(name);
        if (f == null) return;
        f.isDeleted = false;
        f.location = FileLocation.Desktop;
        OnDataChanged?.Invoke();
    }

    public void LockAllFiles()
    {
        foreach (var f in allFiles) f.isLocked = true;
        OnDataChanged?.Invoke();
    }

    public void RestoreAllFiles()
    {
        foreach (var f in allFiles)
        {
            f.isDeleted = false; f.isLocked = false;
            f.location = FileLocation.Desktop;
        }
        OnDataChanged?.Invoke();
    }

    private void CheckImportant()
    {
        var imp = GetFile("Important_Project");
        if (imp == null || imp.isLocked) return;
        bool lost = imp.isDeleted || imp.name != "Important_Project" ||
                    (imp.location != FileLocation.Desktop && imp.location != FileLocation.ImportantProject);
        if (lost) OnImportantLost?.Invoke();
    }

    public static FileLocation ParseLocation(string s)
    {
        switch (s)
        {
            case "Work Files":       return FileLocation.WorkFiles;
            case "Personal":          return FileLocation.Personal;
            case "Downloads":         return FileLocation.Downloads;
            case "Important_Project": return FileLocation.ImportantProject;
            case "Trash":             return FileLocation.Trash;
            default:                  return FileLocation.Desktop;
        }
    }

    public static string LocationName(FileLocation loc)
    {
        switch (loc)
        {
            case FileLocation.Desktop:         return "Desktop";
            case FileLocation.WorkFiles:        return "Work Files";
            case FileLocation.Personal:         return "Personal";
            case FileLocation.Downloads:        return "Downloads";
            case FileLocation.ImportantProject: return "Important_Project";
            case FileLocation.Trash:            return "Trash";
            default:                            return "Desktop";
        }
    }
}

[System.Serializable]
public class FileEntry
{
    public string name;
    public string category;
    public FileLocation location;
    public bool isDeleted;
    public bool isLocked;
    public bool isImportant;
}
