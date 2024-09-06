#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

using APOLStar.VRCUE.Common.UI.Footer;

public class FullHierarchyTransformCopier : EditorWindow
{
	GameObject copyTransformsFromObject;
	GameObject pasteTransformsIntoObject;

	[MenuItem("Tools/VRC Unity Essentials/Full Hierarchy Transform Copier")]
	public static void ShowWindow()
	{
		GetWindow<FullHierarchyTransformCopier>("Full Hierarchy Transform Copier");
	}

	private void OnGUI()
	{
		// Display a HelpBox with a description of the tool
		EditorGUILayout.HelpBox("This tool copies the transforms (position, rotation, and scale) from the 'Copy Transforms From' object to the 'Paste Transforms Into' object, including all their children. The operation is undoable.", MessageType.Info);

		GUILayout.Label("Select 'Copy Transforms From' and 'Paste Transforms Into' Objects", EditorStyles.boldLabel);

		copyTransformsFromObject = EditorGUILayout.ObjectField("Copy Transforms From", copyTransformsFromObject, typeof(GameObject), true) as GameObject;
		pasteTransformsIntoObject = EditorGUILayout.ObjectField("Paste Transforms Into", pasteTransformsIntoObject, typeof(GameObject), true) as GameObject;

		// Create a red button for copying transforms
		GUI.backgroundColor = Color.red;
		if (GUILayout.Button("Copy Transforms"))
		{
			GUI.backgroundColor = Color.white;  // Reset color after button
			if (copyTransformsFromObject == null || pasteTransformsIntoObject == null)
			{
				Debug.LogError("'Copy Transforms From' or 'Paste Transforms Into' object is not assigned.");
				return;
			}

			// Start an undo operation
			Undo.RegisterFullObjectHierarchyUndo(pasteTransformsIntoObject, "Copy Transforms");

			// Copy transforms
			CopyTransformRecursively(copyTransformsFromObject.transform, pasteTransformsIntoObject.transform);

			// Mark the destination object as dirty to ensure the changes are saved
			EditorUtility.SetDirty(pasteTransformsIntoObject);

			Debug.Log("Transform copying completed.");
		}
		GUI.backgroundColor = Color.white; 
		APOLStar.VRCUE.Common.UI.Footer.DrawFooter("APOL Assets");
	}

	private void CopyTransformRecursively(Transform copyFrom, Transform pasteInto)
	{
		if (pasteInto == null)
		{
			Debug.LogWarning("No matching object found for: " + copyFrom.name);
			return;
		}

		// Copy the transform
		pasteInto.position = copyFrom.position;
		pasteInto.rotation = copyFrom.rotation;
		pasteInto.localScale = copyFrom.localScale;

		foreach (Transform copyChild in copyFrom)
		{
			Transform pasteChild = pasteInto.Find(copyChild.name);
			CopyTransformRecursively(copyChild, pasteChild);
		}
	}
}
#endif
