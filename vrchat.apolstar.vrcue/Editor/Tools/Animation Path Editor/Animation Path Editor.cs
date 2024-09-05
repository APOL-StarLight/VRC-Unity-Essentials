#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.Animations;

public class AnimationPathEditor : EditorWindow
{
	private enum EditMode { Rename, Copy, Delete }
	private EditMode editMode = EditMode.Rename;

	private Object animationObject;
	private string pathFind = "";
	private string pathReplace = "";
	private bool usePathPrefixes = true;

	[MenuItem("Tools/VRC Unity Essentials/Animation Path Editor")]
	public static void ShowWindow()
	{
		GetWindow<AnimationPathEditor>("Animation Path Editor");
	}

	private void OnGUI()
	{
		// Help box description
		EditorGUILayout.HelpBox("This tool allows you to rename, copy, or delete animation property paths in an Animator Controller's Animation Clips or in a single Animation Clip.", MessageType.Info);

		// Instructions
		EditorGUILayout.LabelField("Instructions:", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("1. Drag an Animator Controller or Animation Clip into the 'Animation/Controller' field.");
		EditorGUILayout.LabelField("2. Select the desired edit mode from the dropdown.");
		EditorGUILayout.LabelField("3. Enter the required paths based on the selected mode.");
		EditorGUILayout.LabelField("4. Click 'Generate' to apply the changes.");

		// Add space
		EditorGUILayout.Space();
		
		// Explanatory text for Path Prefixes
		EditorGUILayout.LabelField("Note: When 'Use Path Prefixes' is checked, the tool will operate on paths that start with the specified prefix. Uncheck it to match exact paths.", EditorStyles.wordWrappedLabel);

		// Add space
		EditorGUILayout.Space(20);

		// Input field for Animation/Controller
		animationObject = EditorGUILayout.ObjectField("Animation/Controller", animationObject, typeof(Object), false);

		// Dropdown menu for editing mode selection
		editMode = (EditMode)EditorGUILayout.EnumPopup("Edit Mode", editMode);

		// Path Prefixes checkbox
		usePathPrefixes = EditorGUILayout.Toggle("Use Path Prefixes", usePathPrefixes);

		// Input fields based on the selected mode and whether prefixes are used
		switch (editMode)
		{
			case EditMode.Rename:
				pathFind = EditorGUILayout.TextField(usePathPrefixes ? "Path Prefix to Find" : "Exact Path to Find", pathFind);
				pathReplace = EditorGUILayout.TextField(usePathPrefixes ? "Path Prefix to Replace" : "Exact Path to Replace", pathReplace);
				break;

			case EditMode.Copy:
				pathFind = EditorGUILayout.TextField(usePathPrefixes ? "Path Prefix to Copy" : "Exact Path to Copy", pathFind);
				pathReplace = EditorGUILayout.TextField(usePathPrefixes ? "Path Prefix to Paste" : "Exact Path to Paste", pathReplace);
				break;

			case EditMode.Delete:
				pathFind = EditorGUILayout.TextField(usePathPrefixes ? "Path Prefix to Delete" : "Exact Path to Delete", pathFind);
				break;
		}

		// Add space
		EditorGUILayout.Space(20);

		// Generate button
		if (GUILayout.Button("Generate"))
		{
			if (animationObject != null)
			{
				int affectedAnimations = 0;
				int affectedProperties = CountAffectedProperties(ref affectedAnimations);

				if (EditorUtility.DisplayDialog("Confirm Path Operation",
					$"Are you sure you want to {editMode.ToString().ToLower()} all instances of {(usePathPrefixes ? "path prefix" : "exact path")} '{pathFind}'{(editMode != EditMode.Delete ? $" with '{pathReplace}'" : "")} in the selected Animation Object?\n\nThis action will affect {affectedProperties} properties over {affectedAnimations} animations. This action is undoable.",
					"Yes", "No"))
				{
					if (animationObject is AnimatorController)
					{
						ProcessAnimatorController(animationObject as AnimatorController);
					}
					else if (animationObject is AnimationClip)
					{
						ProcessAnimationClip(animationObject as AnimationClip);
					}
					else
					{
						EditorUtility.DisplayDialog("Error", "Please assign either an Animator Controller or an Animation Clip.", "OK");
					}
				}
			}
			else
			{
				EditorUtility.DisplayDialog("Error", "Please assign an Animation Controller or an Animation Clip.", "OK");
			}
		}
		Credits.DrawFooter("APOL Assets");
	}

	private int CountAffectedProperties(ref int affectedAnimations)
	{
		int propertyCount = 0;

		if (animationObject is AnimatorController animatorController)
		{
			foreach (var layer in animatorController.layers)
			{
				foreach (var state in layer.stateMachine.states)
				{
					var clip = state.state.motion as AnimationClip;
					if (clip == null) continue;

					int propertiesInClip = CountAffectedPropertiesInClip(clip);
					if (propertiesInClip == 0) continue;

					affectedAnimations++;
					propertyCount += propertiesInClip;
				}
			}
		}
		else if (animationObject is AnimationClip clip)
		{
			int propertiesInClip = CountAffectedPropertiesInClip(clip);
			if (propertiesInClip > 0)
			{
				affectedAnimations = 1;
				propertyCount = propertiesInClip;
			}
		}

		return propertyCount;
	}

	private int CountAffectedPropertiesInClip(AnimationClip clip)
	{
		int count = 0;
		var bindings = AnimationUtility.GetCurveBindings(clip);

		foreach (var binding in bindings)
		{
			if ((usePathPrefixes && binding.path.StartsWith(pathFind)) || (!usePathPrefixes && binding.path == pathFind))
			{
				count++;
			}
		}

		return count;
	}

	private void ProcessAnimatorController(AnimatorController animatorController)
	{
		// Start an undo group
		Undo.RegisterCompleteObjectUndo(animatorController, $"{editMode.ToString()} Animation Property Path Prefixes");

		// Iterate over all layers in the animation controller
		foreach (var layer in animatorController.layers)
		{
			// Iterate over all states in the layer
			foreach (var state in layer.stateMachine.states)
			{
				var clip = state.state.motion as AnimationClip;
				if (clip != null)
				{
					ProcessAnimationClip(clip);
				}
			}
		}

		// Mark the animation controller as dirty to ensure the changes are saved
		EditorUtility.SetDirty(animatorController);

		// Refresh the AssetDatabase to apply changes
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}

	private void ProcessAnimationClip(AnimationClip clip)
	{
		// Start an undo group
		Undo.RegisterCompleteObjectUndo(clip, $"{editMode.ToString()} Animation Property Path Prefixes");

		// Get all the curve bindings in the clip
		var bindings = AnimationUtility.GetCurveBindings(clip);

		// Iterate over all animation curve bindings
		foreach (var binding in bindings)
		{
			if ((usePathPrefixes && binding.path.StartsWith(pathFind)) || (!usePathPrefixes && binding.path == pathFind))
			{
				switch (editMode)
				{
					case EditMode.Rename:
						RenamePathInBinding(clip, binding);
						break;

					case EditMode.Copy:
						CopyPathInBinding(clip, binding);
						break;

					case EditMode.Delete:
						DeletePathInBinding(clip, binding);
						break;
				}
			}
		}

		// Mark the animation clip as dirty to ensure the changes are saved
		EditorUtility.SetDirty(clip);

		// Refresh the AssetDatabase to apply changes
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}

	private void RenamePathInBinding(AnimationClip clip, EditorCurveBinding binding)
	{
		var newBinding = binding;
		newBinding.path = pathReplace + binding.path.Substring(pathFind.Length);

		// Replace the curve
		AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
		AnimationUtility.SetEditorCurve(clip, binding, null); // Clear the old curve
		AnimationUtility.SetEditorCurve(clip, newBinding, curve); // Set the new curve

		// Update animation events if necessary
		UpdateAnimationEvents(clip, binding.path, newBinding.path);
	}

	private void CopyPathInBinding(AnimationClip clip, EditorCurveBinding binding)
	{
		var newBinding = binding;
		newBinding.path = pathReplace + binding.path.Substring(pathFind.Length);

		// Copy the curve
		AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
		AnimationUtility.SetEditorCurve(clip, newBinding, curve); // Set the new curve, keeping the old one

		// Copy animation events if necessary
		CopyAnimationEvents(clip, binding.path, newBinding.path);
	}

	private void DeletePathInBinding(AnimationClip clip, EditorCurveBinding binding)
	{
		// Remove the curve
		AnimationUtility.SetEditorCurve(clip, binding, null);

		// Remove animation events if necessary
		RemoveAnimationEvents(clip, binding.path);
	}

	private void UpdateAnimationEvents(AnimationClip clip, string oldPath, string newPath)
	{
		AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
		bool updated = false;

		foreach (var evt in events)
		{
			if (evt.functionName.StartsWith(pathFind) && evt.stringParameter.StartsWith(oldPath))
			{
				evt.functionName = pathReplace + evt.functionName.Substring(pathFind.Length);
				evt.stringParameter = newPath;
				updated = true;
			}
		}

		if (updated)
		{
			AnimationUtility.SetAnimationEvents(clip, events);
		}
	}

	private void CopyAnimationEvents(AnimationClip clip, string oldPath, string newPath)
	{
		AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
		AnimationEvent[] newEvents = new AnimationEvent[events.Length];

		for (int i = 0; i < events.Length; i++)
		{
			newEvents[i] = new AnimationEvent
			{
				functionName = events[i].functionName.StartsWith(pathFind)
					? pathReplace + events[i].functionName.Substring(pathFind.Length)
					: events[i].functionName,
				time = events[i].time,
				stringParameter = events[i].stringParameter.StartsWith(oldPath)
					? newPath
					: events[i].stringParameter,
				floatParameter = events[i].floatParameter,
				intParameter = events[i].intParameter,
				objectReferenceParameter = events[i].objectReferenceParameter
			};
		}

		AnimationUtility.SetAnimationEvents(clip, newEvents);
	}

	private void RemoveAnimationEvents(AnimationClip clip, string path)
	{
		AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
		AnimationEvent[] newEvents = System.Array.FindAll(events, e => !e.stringParameter.StartsWith(path));
		AnimationUtility.SetAnimationEvents(clip, newEvents);
	}
}
#endif
