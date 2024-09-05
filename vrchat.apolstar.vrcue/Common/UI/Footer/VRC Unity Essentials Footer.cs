#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using APOLStar.VRCUE.Common.Devs;


public static class Credits
{
	private static Texture2D XTexture;
	private static Texture2D GumroadTexture;
	private static Texture2D DiscordTexture;
	private static bool texturesLoaded = false;


	private static GUIStyle iconButtonStyle;

	private static void LoadTextures()
	{
		if (texturesLoaded) return;
	
		XTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/vrchat.apolstar.vrcue/common/UI/Images/UI_X.png");
		GumroadTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/vrchat.apolstar.vrcue/common/UI/Images/UI_Gumroad.png");
		DiscordTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/vrchat.apolstar.vrcue/common/UI/Images/UI_Discord.png");

		if (XTexture == null) Debug.LogWarning("X Texture failed to load.");
		if (GumroadTexture == null) Debug.LogWarning("Gumroad Texture failed to load.");
		if (DiscordTexture == null) Debug.LogWarning("Discord Texture failed to load.");

		texturesLoaded = true;
	}

	private static void InitializeStyles()
	{
		if (iconButtonStyle != null) return;

		iconButtonStyle = new GUIStyle();
		iconButtonStyle.normal.background = null; 
		iconButtonStyle.active.background = null;
		iconButtonStyle.hover.background = null;
		iconButtonStyle.focused.background = null;
		iconButtonStyle.onNormal.background = null;
		iconButtonStyle.onActive.background = null;
		iconButtonStyle.onHover.background = null;
		iconButtonStyle.onFocused.background = null;
		iconButtonStyle.border = new RectOffset(0, 0, 0, 0);
		iconButtonStyle.margin = new RectOffset(0, 0, 0, 0);
		iconButtonStyle.padding = new RectOffset(0, 0, 0, 0);
		iconButtonStyle.overflow = new RectOffset(0, 0, 0, 0);
		iconButtonStyle.alignment = TextAnchor.UpperCenter;
	}


	static void DrawFooter(DevProfile DevProfile) {

		if (DevProfile == null) {
			DevProfile = new DevProfile("missing", "missing", "missing", "missing");
		}
		DrawFooter(DevProfile.name, DevProfile.twitter, DevProfile.discord, DevProfile.gumroad);
	}

	static void DrawFooter(string name) {
		DrawFooter(DevProfile.Get(name));
	}
	public static void DrawFooter(string name, string twitter, string discord, string gumroad)
	{


		LoadTextures();
		InitializeStyles();

		GUILayout.Space(10);
		GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
		GUILayout.Label("Tool by " + name, EditorStyles.miniBoldLabel);

		GUILayout.BeginHorizontal();
		GUILayout.Label("", GUILayout.Width(10), GUILayout.Height(20));

		if (XTexture != null && GUILayout.Button(XTexture, iconButtonStyle, GUILayout.Width(20), GUILayout.Height(20)))
		{
			Application.OpenURL(twitter);
		}

		GUILayout.Label("", GUILayout.Width(10), GUILayout.Height(30));

		if (DiscordTexture != null && GUILayout.Button(DiscordTexture, iconButtonStyle, GUILayout.Width(20), GUILayout.Height(20)))
		{
			Application.OpenURL(discord);
		}

		GUILayout.Label("", GUILayout.Width(10), GUILayout.Height(30));

		if (GumroadTexture != null && GUILayout.Button(GumroadTexture, iconButtonStyle, GUILayout.Width(20), GUILayout.Height(20)))
		{
			Application.OpenURL(gumroad);
		}

		GUILayout.Label("", GUILayout.Width(10), GUILayout.Height(30));
		GUILayout.EndHorizontal();
	}
}
#endif
