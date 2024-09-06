#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using Newtonsoft.Json;

using APOLStar.VRCUE.Common.UI.Footer;

public class AutoAvatarUploader : EditorWindow
{
	private string JsonFilePath => Path.Combine(Application.persistentDataPath, "AutoAvatarUploaderSettings.json");

	private List<GameObject> availableAvatars = new List<GameObject>();
	private List<GameObject> avatarsForUpload = new List<GameObject>();
	private bool showInactiveAvatars = false;
	private Vector2 availableAvatarsScrollPos;
	private Vector2 avatarsForUploadScrollPos;

	private bool isUploading = false;

	[MenuItem("Tools/VRC Unity Essentials/Auto Avatar Uploader")]
	public static void ShowWindow()
	{
		GetWindow<AutoAvatarUploader>("Auto Avatar Uploader");
	}

	private void OnEnable()
	{
		LoadSettings();
		FindAvatarsInScene();
	}

	private void OnGUI()
	{
		GUILayout.Label("Auto Avatar Uploader", EditorStyles.boldLabel);
		EditorGUILayout.HelpBox("This tool helps you upload multiple avatars automatically in VRChat. " +
			"Select avatars from the available list and move them to the upload list.", MessageType.Info);

		GUILayout.Label("Instructions", EditorStyles.boldLabel);
		GUILayout.Label("1. Use the buttons to the left of the avatar names to move avatars between the lists.");
		GUILayout.Label("2. Open the VRChat SDK and go to the Build tab.");
		GUILayout.Label("3. Click 'Upload Selected Avatars' to upload the avatars on the left list.");

		// Toggle to show/hide inactive avatars
		bool newShowInactiveAvatars = EditorGUILayout.Toggle("Show Inactive Avatars", showInactiveAvatars);
		if (newShowInactiveAvatars != showInactiveAvatars)
		{
			showInactiveAvatars = newShowInactiveAvatars;
			FindAvatarsInScene();
		}

		GUILayout.Space(10);

		// Avatar List Titles
		EditorGUILayout.BeginHorizontal();
		GUILayout.Label("Avatars for Upload", EditorStyles.boldLabel);
		GUILayout.Label("Available Avatars", EditorStyles.boldLabel);
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();

		float panelHeight = Mathf.Max(100, position.height - 250);

		avatarsForUploadScrollPos = EditorGUILayout.BeginScrollView(avatarsForUploadScrollPos, GUILayout.Width(position.width / 2 - 10), GUILayout.Height(panelHeight));
		EditorGUILayout.BeginVertical("box");

		for (int i = 0; i < avatarsForUpload.Count; i++)
		{
			DrawAvatarWithArrowButton(avatarsForUpload[i], false);
		}

		EditorGUILayout.EndVertical();
		EditorGUILayout.EndScrollView();

		availableAvatarsScrollPos = EditorGUILayout.BeginScrollView(availableAvatarsScrollPos, GUILayout.Width(position.width / 2 - 10), GUILayout.Height(panelHeight));
		EditorGUILayout.BeginVertical("box");

		for (int i = 0; i < availableAvatars.Count; i++)
		{
			DrawAvatarWithArrowButton(availableAvatars[i], true);
		}

		EditorGUILayout.EndVertical();
		EditorGUILayout.EndScrollView();

		EditorGUILayout.EndHorizontal();

		GUILayout.Space(10);

		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Add All", GUILayout.ExpandWidth(true)))
		{
			MoveAllAvatars(true);
		}
		if (GUILayout.Button("Remove All", GUILayout.ExpandWidth(true)))
		{
			MoveAllAvatars(false);
		}
		EditorGUILayout.EndHorizontal();

		GUILayout.Space(10);

		GUI.backgroundColor = isUploading ? Color.red : new Color(0.75f, 0.25f, 0.25f);
		if (GUILayout.Button("Upload Selected Avatars", GUILayout.ExpandWidth(true)))
		{
			isUploading = true;
			UploadSelectedAvatars();
			isUploading = false;
		}
		GUI.backgroundColor = Color.white;

		GUILayout.Space(10);
		APOLStar.VRCUE.Common.UI.Footer.Credits.DrawFooter("Star");
	}

	private void DrawAvatarWithArrowButton(GameObject avatar, bool moveToUpload)
	{
		EditorGUILayout.BeginHorizontal();

		// Arrow button
		if (moveToUpload)
		{
			if (GUILayout.Button("<", GUILayout.Width(30))) // Move to upload list
			{
				MoveAvatar(avatar, true);
			}
		}

		// Avatar name
		GUILayout.Label(avatar.name, GUILayout.ExpandWidth(true));

		// Arrow button on the right for avatars in the upload list
		if (!moveToUpload)
		{
			if (GUILayout.Button(">", GUILayout.Width(30))) // Move to available avatars list
			{
				MoveAvatar(avatar, false);
			}
		}

		EditorGUILayout.EndHorizontal();
	}

	// Moves all avatars between lists
	private void MoveAllAvatars(bool toUpload)
	{
		if (toUpload)
		{
			avatarsForUpload.AddRange(availableAvatars);
			availableAvatars.Clear();
		}
		else
		{
			availableAvatars.AddRange(avatarsForUpload);
			avatarsForUpload.Clear();
		}
		SaveSettings();
	}

	// Moves a single avatar between lists (based on toUpload flag)
	private void MoveAvatar(GameObject avatar, bool toUpload)
	{
		if (toUpload)
		{
			avatarsForUpload.Add(avatar);
			availableAvatars.Remove(avatar);
		}
		else
		{
			availableAvatars.Add(avatar);
			avatarsForUpload.Remove(avatar);
		}
		SaveSettings(); // Save after moving the avatar
	}

	// Finds all avatars in the scene with the VRCAvatarDescriptor component, properly handles inactive avatars
	private void FindAvatarsInScene()
	{
		var allAvatarObjects = FindObjectsOfType<VRCAvatarDescriptor>(true)
			.Select(avatar => avatar.gameObject);

		availableAvatars.Clear();

		foreach (var avatar in allAvatarObjects)
		{
			if (avatar.activeInHierarchy || showInactiveAvatars)
			{
				// If the avatar is already in the upload list, keep it there
				if (avatarsForUpload.Contains(avatar))
				{
					continue;
				}

				availableAvatars.Add(avatar);
			}
		}

		SaveSettings();
	}

	private bool IsAvatarForUpload(GameObject avatar)
	{
		return avatarsForUpload.Contains(avatar);
	}

	// Saves avatar selection states and inactive avatar toggle to a JSON file
	private void SaveSettings()
	{
		var settings = new AutoAvatarUploaderSettings
		{
			avatarsForUpload = avatarsForUpload.Select(a => a.name).ToList(),
			showInactiveAvatars = showInactiveAvatars
		};
		string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
		File.WriteAllText(JsonFilePath, json);
	}

	// Loads settings from a JSON file located in persistentDataPath (safe, non-tracked by Unity)
	private void LoadSettings()
	{
		if (File.Exists(JsonFilePath))
		{
			string json = File.ReadAllText(JsonFilePath);
			var settings = JsonConvert.DeserializeObject<AutoAvatarUploaderSettings>(json);
			showInactiveAvatars = settings.showInactiveAvatars;
		}
	}

	private void UploadSelectedAvatars()
	{
		foreach (var avatar in avatarsForUpload)
		{
			Debug.Log($"Uploading avatar: {avatar.name}");
		}
	}

	[System.Serializable]
	public class AutoAvatarUploaderSettings
	{
		public List<string> avatarsForUpload;
		public bool showInactiveAvatars;
	}
}
#endif
