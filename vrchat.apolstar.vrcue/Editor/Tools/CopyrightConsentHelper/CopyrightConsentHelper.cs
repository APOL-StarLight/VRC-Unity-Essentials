#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System;
using VRC.SDKBase.Editor.Api;   // VRCApi calls

/*
 ──────────────────────────────────────────────────────────────────────────────
 APOL Assets – Copyright Consent Helper (Fixed)
 -------------------------------------------------------------------------------
 • Collects all avatar blueprint IDs at Unity load and injects them into
   VRChat SDK’s agreement system to bypass the copyright modal.
 • Updates SessionState *before* any API calls to avoid timing issues.
 ──────────────────────────────────────────────────────────────────────────────
*/

[InitializeOnLoad]
public static class APOL_CopyrightConsentHelper
{
    private const string SessionKey  = "VRCSdkControlPanel.CopyrightAgreement.ContentList";
    private const string PrefsPrefix = "VRC.SDKBase.Editor.CopyrightAgreed.";

    static APOL_CopyrightConsentHelper() =>
        EditorApplication.delayCall += () => _ = InjectBlueprintsAsync();

    private static async Task InjectBlueprintsAsync()
    {
        var collected = CollectBlueprintIds();
        if (collected.Count == 0)
        {
            Debug.Log("[ConsentHelper] No VRChat blueprint IDs detected.");
            return;
        }

        var currentRaw = SessionState.GetString(SessionKey, "");
        var consentSet = string.IsNullOrEmpty(currentRaw)
                         ? new HashSet<string>()
                         : new HashSet<string>(currentRaw.Split(';'));

        int newlyAdded = 0;

        // --- STEP 1: Set EditorPrefs and update SessionState early ---
        foreach (var bpId in collected)
        {
            if (!EditorPrefs.GetBool(PrefsPrefix + bpId, false))
                EditorPrefs.SetBool(PrefsPrefix + bpId, true);

            if (consentSet.Add(bpId))
                newlyAdded++;
        }

        // Commit early to SessionState before any awaits
        SessionState.SetString(SessionKey, string.Join(";", consentSet));
        Debug.Log($"[ConsentHelper] Session pre-load complete – {newlyAdded} IDs injected.");

        // --- STEP 2: (Optional) Enhance robustness with internal avatar IDs ---
        foreach (var bpId in collected)
        {
            try
            {
                dynamic avatar = await VRCApi.GetAvatar(bpId);
                string intId = Convert.ToString(avatar?.id);

                if (!string.IsNullOrEmpty(intId))
                {
                    EditorPrefs.SetBool(PrefsPrefix + intId, true);
                    if (consentSet.Add(intId))
                    {
                        Debug.Log($"[ConsentHelper] Added internal ID: {intId}");
                        // Refresh SessionState (optional per addition)
                        SessionState.SetString(SessionKey, string.Join(";", consentSet));
                    }
                }
            }
            catch { /* Ignore network failures or SDK mismatches */ }
        }

        Debug.Log($"[ConsentHelper] Completed – Total agreed: {consentSet.Count} IDs.");
    }

    private static HashSet<string> CollectBlueprintIds()
    {
        var ids = new HashSet<string>();

        // Scenes
        foreach (string guid in AssetDatabase.FindAssets("t:Scene"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            foreach (var root in scene.GetRootGameObjects())
                GatherFromHierarchy(root.transform, ids);
            EditorSceneManager.CloseScene(scene, true);
        }

        // Prefabs
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
                GatherFromHierarchy(prefab.transform, ids);
        }

        // Beevali preset text files
        ScanBeevaliPresets(ids);

        return ids;
    }

    private static void GatherFromHierarchy(Transform root, HashSet<string> dst)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            var pm = t.GetComponent("PipelineManager");
            if (pm == null) continue;

            var so = new SerializedObject(pm);
            var sp = so.FindProperty("blueprintId");
            if (sp != null && !string.IsNullOrEmpty(sp.stringValue))
                dst.Add(sp.stringValue);
        }
    }

    private static void ScanBeevaliPresets(HashSet<string> dst)
    {
        var baseDir = Path.Combine(Application.dataPath,
                                   "APOL Assets/Beevali Editor/Avatars");
        if (!Directory.Exists(baseDir)) return;

        foreach (var avatarDir in Directory.GetDirectories(baseDir))
        {
            var presetDir = Path.Combine(avatarDir, "Presets");
            if (!Directory.Exists(presetDir)) continue;

            foreach (var file in Directory.GetFiles(presetDir, "*.txt"))
            {
                try
                {
                    var line = File.ReadLines(file).FirstOrDefault()?.Trim();
                    const string prefix = "BLUEPRINT:avtr_";
                    if (line != null && line.StartsWith(prefix))
                        dst.Add(line.Substring("BLUEPRINT:".Length));
                }
                catch { /* unreadable file – skip */ }
            }
        }
    }
}
#endif
