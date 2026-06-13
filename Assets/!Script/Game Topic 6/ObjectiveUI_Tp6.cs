/// <summary>
/// Topic 6 alias for the reusable <see cref="ObjectiveRedesignUI"/>.
/// Kept as a thin subclass so existing Tp6 scene components and the
/// `objectiveRedesign` fields in GameManager_Tp6 / Topic6ProgressionController
/// keep resolving unchanged (script GUID preserved, IS-A ObjectiveRedesignUI).
/// New topics (1–5) use ObjectiveRedesignUI directly.
/// </summary>
public class ObjectiveUI_Tp6 : ObjectiveRedesignUI { }
