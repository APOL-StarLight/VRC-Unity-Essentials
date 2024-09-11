#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using APOLStar.VRCUE.Common.UI.Footer; // Import the footer for credits.

public class AdvancedHierarchySearch : EditorWindow
{
    private string searchQuery = "";
    private GameObject limitToObject = null;
    private List<string> availableTags = new List<string>();
    private List<string> selectedTags = new List<string>();
    private List<string> componentDisplayNames = new List<string>();
    private List<string> selectedComponents = new List<string>();
    private List<Type> componentTypes = new List<Type>();
    private bool searchActive = false; // Active/Inactive filter
    private bool searchInactive = false;

    private const string componentsCsvPath = "Packages/vrchat.apolstar.vrcue/Editor/Tools/Advanced Hierarchy Search/SearchableComponents.csv"; // Path to the CSV file.
    
    [MenuItem("Tools/VRC Unity Essentials/Advanced Hierarchy Search")]
    public static void ShowWindow()
    {
        GetWindow<AdvancedHierarchySearch>("Advanced Hierarchy Search");
    }

    private void OnEnable()
    {
        // Load available tags from the project.
        availableTags = UnityEditorInternal.InternalEditorUtility.tags.ToList();
        
        // Load component types from the CSV file.
        LoadComponentsFromCsv();
    }

    private void OnGUI()
    {
        // Helpbox explaining the tool.
        EditorGUILayout.HelpBox("Use this tool to search for GameObjects in the hierarchy by name, tag, components, or activity status. You can also limit the search to a specific object in the hierarchy.", MessageType.Info);

        // Search by Name
        searchQuery = EditorGUILayout.TextField("Search by Name", searchQuery);

        // Limit Search To (Object Field)
        limitToObject = (GameObject)EditorGUILayout.ObjectField("Limit Search To", limitToObject, typeof(GameObject), true);

        // Search by Tag
        EditorGUILayout.LabelField("Search by Tags");
        for (int i = 0; i < availableTags.Count; i++)
        {
            bool isSelected = selectedTags.Contains(availableTags[i]);
            bool newIsSelected = EditorGUILayout.ToggleLeft(availableTags[i], isSelected);

            if (newIsSelected && !isSelected)
                selectedTags.Add(availableTags[i]);
            else if (!newIsSelected && isSelected)
                selectedTags.Remove(availableTags[i]);
        }

        // Search by Components (Multi-select)
        EditorGUILayout.LabelField("Search by Components");
        for (int i = 0; i < componentDisplayNames.Count; i++)
        {
            bool isSelected = selectedComponents.Contains(componentDisplayNames[i]);
            bool newIsSelected = EditorGUILayout.ToggleLeft(componentDisplayNames[i], isSelected);

            if (newIsSelected && !isSelected)
                selectedComponents.Add(componentDisplayNames[i]);
            else if (!newIsSelected && isSelected)
                selectedComponents.Remove(componentDisplayNames[i]);
        }

        // Search Active/Inactive
        searchActive = EditorGUILayout.Toggle("Search Active Objects", searchActive);
        searchInactive = EditorGUILayout.Toggle("Search Inactive Objects", searchInactive);

        // Search Button
        if (GUILayout.Button("Search"))
        {
            PerformSearch();
        }

        // Display the footer credit
        APOLStar.VRCUE.Common.UI.Footer.Credits.DrawFooter("APOL Assets");
    }

    private void LoadComponentsFromCsv()
    {
        componentDisplayNames.Clear();
        componentTypes.Clear();

        if (File.Exists(componentsCsvPath))
        {
            string[] lines = File.ReadAllLines(componentsCsvPath);
            foreach (string line in lines)
            {
                string[] values = line.Split(',');
                if (values.Length == 2)
                {
                    string displayName = values[0];
                    string typeName = values[1];

                    Type type = Type.GetType(typeName);
                    if (type != null)
                    {
                        componentDisplayNames.Add(displayName);
                        componentTypes.Add(type);
                    }
                }
            }
        }
        else
        {
            Debug.LogError($"Component CSV file not found at path: {componentsCsvPath}");
        }
    }

    private void PerformSearch()
    {
        // List to store search results
        List<GameObject> searchResults = new List<GameObject>();

        // Get root objects in the scene
        GameObject[] allObjects = limitToObject != null ? limitToObject.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject).ToArray() : GameObject.FindObjectsOfType<GameObject>(true);

        foreach (GameObject obj in allObjects)
        {
            bool matches = true;

            // Filter by name
            if (!string.IsNullOrEmpty(searchQuery) && !obj.name.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
            {
                matches = false;
            }

            // Filter by tags
            if (selectedTags.Count > 0 && !selectedTags.Contains(obj.tag))
            {
                matches = false;
            }

            // Filter by components
            foreach (var componentName in selectedComponents)
            {
                Type componentType = componentTypes[componentDisplayNames.IndexOf(componentName)];
                if (obj.GetComponent(componentType) == null)
                {
                    matches = false;
                    break;
                }
            }

            // Filter by active/inactive state
            if ((searchActive && !obj.activeSelf) || (searchInactive && obj.activeSelf))
            {
                matches = false;
            }

            if (matches)
            {
                searchResults.Add(obj);
            }
        }

        // Output the number of results and select them in the scene
        Debug.Log($"Search found {searchResults.Count} results.");
        Selection.objects = searchResults.ToArray();
    }
}
#endif
