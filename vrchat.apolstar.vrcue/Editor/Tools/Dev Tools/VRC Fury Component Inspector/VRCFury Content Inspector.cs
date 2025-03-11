#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Text;
using System.Reflection;
using APOLStar.VRCUE.Common.UI.Footer;

public class VRCFuryContentInspectorEditor : EditorWindow
{
	private GameObject selectedObject;
	private Vector2 scrollPosition;  // Add a scroll position for the scroll view

	[MenuItem("Tools/VRC Unity Essentials/Dev Tools/VRCFury Content Inspector")]
	public static void ShowWindow()
	{
		GetWindow<VRCFuryContentInspectorEditor>("VRCFury Content Inspector");
	}

	private void OnSelectionChange()
	{
		// When a new object is selected, update the selected object and refresh the window
		selectedObject = Selection.activeGameObject;
		Repaint();
	}

	private void OnGUI()
	{
		// Display the HelpBox at the top of the UI
		EditorGUILayout.HelpBox("This tool allows you to inspect VRCFury components attached to the selected GameObject. It will display the content fields of any VRCFury components found and provide options to copy their names.", MessageType.Info);

		EditorGUILayout.LabelField("Select a GameObject with VRCFury components to inspect their content fields.", EditorStyles.boldLabel);

		if (selectedObject != null)
		{
			// Dynamically search for the VF.Model.VRCFury type in all loaded assemblies
			Type vrcFuryType = FindVRCFuryType();
			if (vrcFuryType == null)
			{
				EditorGUILayout.LabelField("Could not find the VRCFury type in any loaded assembly.");
				return;
			}

			// Get all components of type VF.Model.VRCFury
			var vrcFuryComponents = selectedObject.GetComponents(vrcFuryType);
			if (vrcFuryComponents.Length > 0)
			{
				EditorGUILayout.LabelField($"Inspecting {vrcFuryComponents.Length} VRCFury component(s) on GameObject: {selectedObject.name}", EditorStyles.boldLabel);

				StringBuilder allContent = new StringBuilder();

				// Add a scroll view to handle many components or content fields
				scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

				// Iterate over each VRCFury component and show its content field
				foreach (var vrcFuryComponent in vrcFuryComponents)
				{
					string contentName = ShowContentField(vrcFuryComponent);
					if (!string.IsNullOrEmpty(contentName))
					{
						allContent.AppendLine(contentName);
					}
				}

				EditorGUILayout.EndScrollView();  // End of scroll view

				EditorGUILayout.Space();

				// Add a "Copy All" button to copy all content names
				if (allContent.Length > 0)
				{
					if (GUILayout.Button("Copy All Content Names"))
					{
						EditorGUIUtility.systemCopyBuffer = allContent.ToString();
						Debug.Log($"Copied all content: {allContent}");
					}
				}
			}
			else
			{
				EditorGUILayout.LabelField("No VRCFury components found on the selected GameObject.");
			}
		}
		else
		{
			EditorGUILayout.LabelField("No GameObject selected.");
		}

		// Add credits UI
		APOLStar.VRCUE.Common.UI.Footer.Credits.DrawFooter("APOL Assets");
	}

	private Type FindVRCFuryType()
	{
		// Iterate over all loaded assemblies to find VF.Model.VRCFury
		foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			Type vrcFuryType = assembly.GetType("VF.Model.VRCFury");
			if (vrcFuryType != null)
			{
				Debug.Log($"Found VRCFury type in assembly: {assembly.FullName}");
				return vrcFuryType;
			}
		}

		Debug.LogError("Could not find VF.Model.VRCFury in any loaded assemblies.");
		return null;
	}

	private string ShowContentField(object vrcFuryComponent)
	{
		// Use reflection to get the 'content' field
		var contentField = vrcFuryComponent.GetType().GetField("content", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
		if (contentField != null)
		{
			object contentValue = contentField.GetValue(vrcFuryComponent);
			if (contentValue != null)
			{
				string contentTypeName = contentValue.GetType().FullName;
				EditorGUILayout.LabelField("Content:", contentTypeName);

				// Add a copy button for this content field
				if (GUILayout.Button("Copy Content Name"))
				{
					EditorGUIUtility.systemCopyBuffer = contentTypeName;
					Debug.Log($"Copied content: {contentTypeName}");
				}

				return contentTypeName;
			}
			else
			{
				EditorGUILayout.LabelField("Content: (null)");
			}
		}
		else
		{
			EditorGUILayout.LabelField("Content field not found.");
		}

		return null;
	}
}
#endif
