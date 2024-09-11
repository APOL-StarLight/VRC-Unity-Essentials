#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using APOLStar.VRCUE.Common.UI.Footer;

public class AdvancedHierarchySearch : EditorWindow
{
    private string searchQuery = "";
    private GameObject limitToObject = null;
    private List<string> availableTags = new List<string>();
    private List<string> componentDisplayNames = new List<string>();
    private List<Type> componentTypes = new List<Type>();
    private List<string> activeSearchFilters = new List<string>();
    private List<string> suggestionList = new List<string>();

    private const string componentsCsvPath = "Packages/vrchat.apolstar.vrcue/Editor/Tools/Advanced Hierarchy Search/SearchableComponents.csv";
    
    [MenuItem("Tools/VRC Unity Essentials/Advanced Hierarchy Search")]
    public static void ShowWindow()
    {
        GetWindow<AdvancedHierarchySearch>("Advanced Hierarchy Search");
    }

    private void OnEnable()
    {
        availableTags = UnityEditorInternal.InternalEditorUtility.tags.ToList();
        LoadComponentsFromCsv();
    }

    private void OnGUI()
    {
        // Helpbox explaining the tool.
        EditorGUILayout.HelpBox("Use this tool to search for GameObjects by name, tag, components, or activity status. You can also limit the search to a specific hierarchy.", MessageType.Info);

        // Search Bar
        EditorGUILayout.LabelField("Search");
        string newSearchQuery = EditorGUILayout.TextField(searchQuery);
        if (newSearchQuery != searchQuery)
        {
            searchQuery = newSearchQuery;
            UpdateSuggestions();
        }

        // Display suggestions dynamically
        foreach (var suggestion in suggestionList)
        {
            if (GUILayout.Button(suggestion))
            {
                AddSearchFilter(suggestion);
            }
        }

        // Display active filters with 'X' button to remove
        if (activeSearchFilters.Count > 0)
        {
            EditorGUILayout.LabelField("Active Filters:");
            for (int i = 0; i < activeSearchFilters.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(activeSearchFilters[i]);
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    activeSearchFilters.RemoveAt(i);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        // Limit Search To (Object Field)
        limitToObject = (GameObject)EditorGUILayout.ObjectField("Limit Search To", limitToObject, typeof(GameObject), true);

        // Search Button
        if (GUILayout.Button("Search"))
        {
            PerformSearch();
        }

        // Footer for credits
        APOLStar.VRCUE.Common.UI.Footer.Credits.DrawFooter("APOL Assets");
    }

    private void UpdateSuggestions()
    {
        suggestionList.Clear();

        // 1. Always add "Search for '[searchQuery]'"
        suggestionList.Add($"Search for '{searchQuery}'");

        // 2. Add Active/Inactive options
        if ("active".Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
        {
            suggestionList.Add("Active Objects");
        }
        if ("inactive".Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
        {
            suggestionList.Add("Inactive Objects");
        }

        // 3. Search for matching component types
        foreach (var componentName in componentDisplayNames)
        {
            if (componentName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
            {
                suggestionList.Add($"Component: {componentName}");
            }
        }

        // 4. Search for matching tags
        foreach (var tag in availableTags)
        {
            if (tag.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
            {
                suggestionList.Add($"Tag: {tag}");
            }
        }
    }

    private void AddSearchFilter(string filter)
    {
        if (!activeSearchFilters.Contains(filter))
        {
            activeSearchFilters.Add(filter);
        }
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
        List<GameObject> searchResults = new List<GameObject>();

        GameObject[] allObjects = limitToObject != null ? limitToObject.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject).ToArray() : GameObject.FindObjectsOfType<GameObject>(true);

        foreach (GameObject obj in allObjects)
        {
            bool matches = true;

            foreach (string filter in activeSearchFilters)
            {
                if (filter.StartsWith("Search for"))
                {
                    string objectName = filter.Substring(11).Trim('"');
                    if (!obj.name.Contains(objectName))
                    {
                        matches = false;
                        break;
                    }
                }
                else if (filter.StartsWith("Component:"))
                {
                    string componentName = filter.Substring(11).Trim();
                    Type componentType = componentTypes[componentDisplayNames.IndexOf(componentName)];
                    if (obj.GetComponent(componentType) == null)
                    {
                        matches = false;
                        break;
                    }
                }
                else if (filter.StartsWith("Tag:"))
                {
                    string tagName = filter.Substring(5).Trim();
                    if (!obj.CompareTag(tagName))
                    {
                        matches = false;
                        break;
                    }
                }
                else if (filter == "Active Objects")
                {
                    if (!obj.activeSelf)
                    {
                        matches = false;
                        break;
                    }
                }
                else if (filter == "Inactive Objects")
                {
                    if (obj.activeSelf)
                    {
                        matches = false;
                        break;
                    }
                }
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
