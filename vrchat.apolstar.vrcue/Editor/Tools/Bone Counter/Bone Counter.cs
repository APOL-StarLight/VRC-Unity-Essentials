#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

using APOLStar.VRCUE.Common.UI.Footer;

public class BoneCounterWindow : EditorWindow
{
	private GameObject avatar;
	private int boneCount;
	private string performanceRanking;
	private List<Transform> boneTransforms = new List<Transform>();

	[MenuItem("Tools/VRC Unity Essentials/Bone Counter")]
	public static void ShowWindow()
	{
		GetWindow<BoneCounterWindow>("Bone Counter");
	}

	private void OnGUI()
	{
		GUILayout.Label("Bone Counter", EditorStyles.boldLabel);

		EditorGUILayout.Space();

		// Description
		// EditorGUILayout.HelpBox("This tool allows you to count the number of bones in an avatar based on VRChat's Avatar Performance Requirements."
		// 			  + " It excludes bones tagged as 'EditorOnly' and their child bones, which the official VRChat SDK does not account for."
		// 			  + " This can be especially useful when optimizing your avatar for VRChat, ensuring an accurate bone count for performance evaluation.",
		// 			  MessageType.Info);

		EditorGUILayout.HelpBox("This tool allows you to count the number of bones in your avatar, this excludes 'EditorOnly' bones and their children.", MessageType.Info);

		EditorGUILayout.Space();


		// Avatar field
		avatar = (GameObject)EditorGUILayout.ObjectField("Avatar", avatar, typeof(GameObject), true);

		EditorGUILayout.Space();

		if (avatar != null)
		{
			if (GUILayout.Button("Count Bones"))
			{
				boneTransforms.Clear();
				boneCount = CountBones(avatar);
				performanceRanking = GetPerformanceRanking(boneCount);
			}

			EditorGUILayout.Space();

			GUILayout.Label($"Bone Count: {boneCount}", EditorStyles.boldLabel);
			GUILayout.Label($"Performance Ranking: {performanceRanking}", EditorStyles.boldLabel);

			EditorGUILayout.Space();

			if (GUILayout.Button("Select Bones"))
			{
				SelectBonesInHierarchy();
			}
		}
		APOLStar.VRCUE.Common.UI.Footer.DrawFooter("APOL Assets");
	}

	private int CountBones(GameObject avatar)
	{
		HashSet<Transform> uniqueBones = new HashSet<Transform>();

		// Find all SkinnedMeshRenderer components in the avatar
		SkinnedMeshRenderer[] skinnedMeshRenderers = avatar.GetComponentsInChildren<SkinnedMeshRenderer>();

		foreach (SkinnedMeshRenderer smr in skinnedMeshRenderers)
		{
			foreach (Transform bone in smr.bones)
			{
				if (bone != null && !IsChildOfEditorOnly(bone))
				{
					uniqueBones.Add(bone);
				}
			}
		}

		boneTransforms.AddRange(uniqueBones);
		return uniqueBones.Count;
	}

	private bool IsChildOfEditorOnly(Transform bone)
	{
		// Traverse up the hierarchy to check if any parent is tagged as "EditorOnly"
		Transform current = bone;
		while (current != null)
		{
			if (current.CompareTag("EditorOnly"))
			{
				return true; // This bone or one of its parents is tagged as "EditorOnly"
			}
			current = current.parent;
		}
		return false; // No parent is tagged as "EditorOnly"
	}

	private string GetPerformanceRanking(int boneCount)
	{
		if (boneCount <= 75)
		{
			return "Excellent";
		}
		else if (boneCount <= 150)
		{
			return "Good";
		}
		else if (boneCount <= 256)
		{
			return "Medium";
		}
		else if (boneCount <= 400)
		{
			return "Poor";
		}
		else
		{
			return "Very Poor";
		}
	}

	private void SelectBonesInHierarchy()
	{
		if (boneTransforms.Count > 0)
		{
			Selection.objects = boneTransforms.ConvertAll(bone => bone.gameObject).ToArray();
		}
		else
		{
			Debug.LogWarning("No bones were counted or selected.");
		}
	}
}
#endif
