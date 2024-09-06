#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

using APOLStar.VRCUE.Common.UI.Footer;

public class HierarchyPathWindow : EditorWindow
{
	private string hierarchyPath = "";
	private int removePrefixCount = 0;

	[MenuItem("Tools/VRC Unity Essentials/Hierarchy Path Viewer")]
	public static void ShowWindow()
	{
		GetWindow<HierarchyPathWindow>("Hierarchy Path Viewer");
	}

	private void OnGUI()
	{
		GUILayout.Label("Hierarchy Path Viewer", EditorStyles.boldLabel);

		GUILayout.Space(10);

		EditorGUILayout.HelpBox("This tool displays the hierarchy path of the selected object in the Unity Editor. "
								+ "You can adjust the number of parent objects to exclude from the path. "
								+ "The hierarchy path updates automatically when a new object is selected.", MessageType.Info);

		GUILayout.Space(10);

		GUILayout.Label("Hierarchy Path:");
		EditorGUILayout.TextArea(hierarchyPath);

		if (GUILayout.Button("Copy to Clipboard"))
		{
			EditorGUIUtility.systemCopyBuffer = hierarchyPath;
			Debug.Log("Path copied to clipboard.");
		}

		GUILayout.Space(10);

		GUILayout.Label("Remove Prefix Parents:");
		removePrefixCount = EditorGUILayout.IntField(removePrefixCount);

		GUILayout.Space(10);

		if (GUI.changed)
		{
			UpdateHierarchyPath();
		}
		APOLStar.VRCUE.Common.UI.Footer.Credits.DrawFooter("APOL Assets");
	}

	private void OnSelectionChange()
	{
		UpdateHierarchyPath();
		Repaint(); // Refresh the window UI
	}

	private void UpdateHierarchyPath()
	{
		if (Selection.activeTransform != null)
		{
			hierarchyPath = GetHierarchyPath(Selection.activeTransform);
			hierarchyPath = RemovePrefixParents(hierarchyPath, removePrefixCount);
		}
		else
		{
			hierarchyPath = "No object selected!";
		}
	}

	private string GetHierarchyPath(Transform obj)
	{
		string path = obj.name;
		while (obj.parent != null)
		{
			obj = obj.parent;
			path = obj.name + "/" + path;
		}
		return path;
	}

	private string RemovePrefixParents(string path, int removeCount)
	{
		string[] parts = path.Split('/');
		if (removeCount > 0 && removeCount < parts.Length)
		{
			string[] newParts = new string[parts.Length - removeCount];
			System.Array.Copy(parts, removeCount, newParts, 0, newParts.Length);
			path = string.Join("/", newParts);
		}
		return path;
	}
}
#endif
