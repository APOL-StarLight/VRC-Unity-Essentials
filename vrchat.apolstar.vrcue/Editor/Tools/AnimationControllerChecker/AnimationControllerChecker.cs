#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;

using APOLStar.VRCUE.Common.UI.Footer;

public class AnimationControllerChecker : EditorWindow
{
	private AnimatorController animatorController;
	private Vector2 scrollPos;
	private List<string> outputLog = new List<string>();
	private int wdOnCount = 0;
	private int wdOffCount = 0;
	private List<string> wdSummaryLog = new List<string>();

	[MenuItem("Tools/VRC Unity Essentials/Check Animation Controller")]
	public static void ShowWindow()
	{
		GetWindow<AnimationControllerChecker>("Check Animation Controller");
	}

	void OnGUI()
	{
		GUILayout.Label("Check Animation Controller", EditorStyles.boldLabel);

		// Description
		EditorGUILayout.HelpBox("This tool checks the selected Animator Controller for the following:\n- States with empty motion fields\n- Animations with no keyframes\n- Missing identifiers\n- Mixed WD (Write Defaults On/Off).", MessageType.Info);
		GUILayout.Space(10);

		// Object field for AnimatorController
		animatorController = (AnimatorController)EditorGUILayout.ObjectField("Animator Controller", animatorController, typeof(AnimatorController), false);

		if (animatorController != null)
		{
			CheckEntireAnimatorController();
			DisplayWriteDefaultsSummary();
			Repaint();

			GUILayout.Label("Write Defaults Summary:", EditorStyles.boldLabel);
			GUILayout.Label($"Write Defaults ON states: {wdOnCount}");
			GUILayout.Label($"Write Defaults OFF states: {wdOffCount}");
			GUILayout.Space(10);

			GUILayout.Label("Output:", EditorStyles.boldLabel);
			scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
			if (outputLog.Count > 0)
			{
				foreach (string log in outputLog)
				{
					GUILayout.Label(log);
				}
			}
			else
			{
				GUILayout.Label("No issues detected.");
			}
			GUILayout.Space(10);
			if (wdSummaryLog.Count > 0)
			{
				foreach (string log in wdSummaryLog)
				{
					GUILayout.Label(log);
				}
			}
			EditorGUILayout.EndScrollView();
		}
		Credits.DrawFooter("APOL Assets");
	}

	private void CheckEntireAnimatorController()
	{
		outputLog.Clear();
		wdSummaryLog.Clear();
		wdOnCount = 0;
		wdOffCount = 0;

		// Check the main AnimatorController object
		SerializedObject controllerObject = new SerializedObject(animatorController);
		CheckSerializedObjectForMissingReferences(controllerObject, "Animator Controller");

		foreach (var layer in animatorController.layers)
		{
			// Check the state's state machine
			CheckStateMachineForIssues(layer.stateMachine, layer.name);

			// Check the AvatarMask if present
			if (layer.avatarMask != null)
			{
				SerializedObject avatarMaskObject = new SerializedObject(layer.avatarMask);
				CheckSerializedObjectForMissingReferences(avatarMaskObject, $"AvatarMask in Layer: {layer.name}");
			}
		}

		if (outputLog.Count == 0)
		{
			outputLog.Add("No issues detected.");
		}
	}

	private void CheckStateMachineForIssues(AnimatorStateMachine stateMachine, string layerName)
	{
		SerializedObject stateMachineObject = new SerializedObject(stateMachine);
		CheckSerializedObjectForMissingReferences(stateMachineObject, $"StateMachine: {stateMachine.name} in Layer: {layerName}");

		foreach (var state in stateMachine.states)
		{
			SerializedObject stateObject = new SerializedObject(state.state);
			CheckSerializedObjectForMissingReferences(stateObject, $"State: {state.state.name} in Layer: {layerName}");

			// Check Write Defaults status
			if (state.state.writeDefaultValues)
			{
				wdOnCount++;
			}
			else
			{
				wdOffCount++;
			}

			if (state.state.motion == null)
			{
				outputLog.Add($"State: {state.state.name}, Layer: {layerName} - Empty Motion");
			}
			else
			{
				AnimationClip clip = state.state.motion as AnimationClip;
				if (clip != null && !HasKeyframes(clip))
				{
					outputLog.Add($"State: {state.state.name}, Layer: {layerName} - Animation with No Keyframes");
				}

				CheckBlendTree(state.state.motion as BlendTree, state.state.name, layerName);
			}
		}

		foreach (var transition in stateMachine.anyStateTransitions)
		{
			if (transition.destinationState == null)
			{
				outputLog.Add($"AnyState Transition in Layer: {layerName} - Missing destination state");
			}
			else
			{
				CheckSerializedObjectForMissingReferences(new SerializedObject(transition), $"AnyState Transition to {transition.destinationState.name} in Layer: {layerName}");
			}
		}

		foreach (var transition in stateMachine.entryTransitions)
		{
			if (transition.destinationState == null)
			{
				outputLog.Add($"Entry Transition in Layer: {layerName} - Missing destination state");
			}
			else
			{
				CheckSerializedObjectForMissingReferences(new SerializedObject(transition), $"Entry Transition to {transition.destinationState.name} in Layer: {layerName}");
			}
		}

		foreach (var subStateMachine in stateMachine.stateMachines)
		{
			CheckStateMachineForIssues(subStateMachine.stateMachine, layerName);
		}
	}

	private void CheckBlendTree(BlendTree blendTree, string stateName, string layerName)
	{
		if (blendTree == null) return;

		SerializedObject blendTreeObject = new SerializedObject(blendTree);
		CheckSerializedObjectForMissingReferences(blendTreeObject, $"BlendTree: {blendTree.name} in State: {stateName}, Layer: {layerName}");

		foreach (var child in blendTree.children)
		{
			if (child.motion == null)
			{
				outputLog.Add($"BlendTree: {blendTree.name} in State: {stateName}, Layer: {layerName} - Empty Motion in BlendTree");
			}

			CheckBlendTree(child.motion as BlendTree, stateName, layerName); // Recursive check for nested blend trees
		}
	}

	private bool HasKeyframes(AnimationClip clip)
	{
		if (clip == null) return false;

		EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(clip);
		foreach (EditorCurveBinding binding in curveBindings)
		{
			AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
			if (curve != null && curve.keys.Length > 0)
			{
				return true;
			}
		}

		EditorCurveBinding[] objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
		foreach (EditorCurveBinding binding in objectBindings)
		{
			ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(clip, binding);
			if (keyframes != null && keyframes.Length > 0)
			{
				return true;
			}
		}

		return false;
	}

	private void CheckSerializedObjectForMissingReferences(SerializedObject serializedObject, string context)
	{
		SerializedProperty prop = serializedObject.GetIterator();
		while (prop.NextVisible(true))
		{
			if (prop.propertyType == SerializedPropertyType.ObjectReference)
			{
				if (prop.objectReferenceValue == null && prop.objectReferenceInstanceIDValue != 0)
				{
					outputLog.Add($"Broken reference detected in {context}: {prop.displayName} has a missing reference.");
				}
			}
		}
	}

	private void DisplayWriteDefaultsSummary()
	{
		wdSummaryLog.Clear();

		if (wdOnCount < wdOffCount && wdOnCount > 0)
		{
			wdSummaryLog.Add("List of states with Write Defaults ON:");
			ListStatesWithWriteDefaults(true);
		}
		else if (wdOffCount < wdOnCount && wdOffCount > 0)
		{
			wdSummaryLog.Add("List of states with Write Defaults OFF:");
			ListStatesWithWriteDefaults(false);
		}
	}

	private void ListStatesWithWriteDefaults(bool writeDefaultsOn)
	{
		foreach (var layer in animatorController.layers)
		{
			foreach (var state in layer.stateMachine.states)
			{
				if (state.state.writeDefaultValues == writeDefaultsOn)
				{
					wdSummaryLog.Add($"State: {state.state.name}, Layer: {layer.name} - Write Defaults {(writeDefaultsOn ? "ON" : "OFF")}");
				}
			}
		}
	}
}
#endif
