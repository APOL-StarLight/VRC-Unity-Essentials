#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

    private Vector2 scrollPosition; // For scrolling suggestion list

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

        // Add space before each major section
        EditorGUILayout.Space();

        // Limit Search To (Object Field)
        limitToObject = (GameObject)EditorGUILayout.ObjectField("Limit Search To", limitToObject, typeof(GameObject), true);

        EditorGUILayout.Space();

        // Search Bar
        EditorGUILayout.LabelField("Search");
        string newSearchQuery = EditorGUILayout.TextField(searchQuery);
        if (newSearchQuery != searchQuery)
        {
            searchQuery = newSearchQuery;
            UpdateSuggestions();
        }

        EditorGUILayout.Space();

        // Display suggestions dynamically in a scrollable view with a darker background
        if (suggestionList.Count > 0)
        {
            EditorGUILayout.BeginVertical("box", GUILayout.Height(150)); // Darker background for suggestions
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (var suggestion in suggestionList)
            {
                if (GUILayout.Button(suggestion, EditorStyles.label)) // Make suggestions look like selectable text
                {
                    AddSearchFilter(suggestion);
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space();

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

        EditorGUILayout.Space();

        // Search Button
        if (GUILayout.Button("Search"))
        {
            PerformSearch();
        }

        EditorGUILayout.Space();

        // Footer for credits
        APOLStar.VRCUE.Common.UI.Footer.Credits.DrawFooter("APOL Assets");
    }

    private void UpdateSuggestions()
    {
        suggestionList.Clear();

        // Only suggest if there's input in the search bar
        if (!string.IsNullOrEmpty(searchQuery))
        {
            // Always add "Search for '[searchQuery]'"
            if (!activeSearchFilters.Contains($"Search for '{searchQuery}'"))
            {
                suggestionList.Add($"Search for '{searchQuery}'");
            }

            // Add Active/Inactive options if not in active filters
            if ("active".Contains(searchQuery, StringComparison.OrdinalIgnoreCase) && !activeSearchFilters.Contains("Active Objects"))
            {
                suggestionList.Add("Active Objects");
            }
            if ("inactive".Contains(searchQuery, StringComparison.OrdinalIgnoreCase) && !activeSearchFilters.Contains("Inactive Objects"))
            {
                suggestionList.Add("Inactive Objects");
            }

            // Suggest all components if any part of "component" is typed
            if ("component".Contains(searchQuery, StringComparison.OrdinalIgnoreCase) || searchQuery.Contains("com", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var componentName in componentDisplayNames)
                {
                    if (!activeSearchFilters.Contains($"Component: {componentName}"))
                    {
                        suggestionList.Add($"Component: {componentName}");
                    }
                }
            }

            // Suggest all tags if any part of "tag" is typed
            if ("tag".Contains(searchQuery, StringComparison.OrdinalIgnoreCase) || searchQuery.Contains("tag", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var tag in availableTags)
                {
                    if (!activeSearchFilters.Contains($"Tag: {tag}"))
                    {
                        suggestionList.Add($"Tag: {tag}");
                    }
                }
            }

            // Suggest components and tags based on partial match
            else
            {
                // Suggest components containing the search term
                foreach (var componentName in componentDisplayNames)
                {
                    if (componentName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) && !activeSearchFilters.Contains($"Component: {componentName}"))
                    {
                        suggestionList.Add($"Component: {componentName}");
                    }
                }

                // Suggest tags containing the search term
                foreach (var tag in availableTags)
                {
                    if (tag.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) && !activeSearchFilters.Contains($"Tag: {tag}"))
                    {
                        suggestionList.Add($"Tag: {tag}");
                    }
                }
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

    // Load components from the CSV file
    private void LoadComponentsFromCsv()
    {
        componentDisplayNames.Clear();
        componentTypes.Clear();

        if (File.Exists(componentsCsvPath))
        {
            string[] lines = File.ReadAllLines(componentsCsvPath);
            foreach (string line in lines)
            {
                // Ignore comments and empty lines
                if (line.StartsWith("//") || string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                string[] values = line.Split(',');
                if (values.Length == 2)
                {
                    string displayName = values[0];
                    string typeName = values[1];

                    // Try to get the type from the name
                    Type type = Type.GetType(typeName);

                    // If not found, search all loaded assemblies for the type
                    if (type == null)
                    {
                        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            type = assembly.GetType(typeName);
                            if (type != null) break;
                        }
                    }

                    if (type != null)
                    {
                        componentDisplayNames.Add(displayName);
                        componentTypes.Add(type);
                    }
                    else
                    {
                        Debug.LogWarning($"Component type '{typeName}' not found.");
                    }
                }
                else
                {
                    Debug.LogError($"Invalid CSV line format: {line}");
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
                // Handle "Search for" filter: find objects whose names contain the search term
                if (filter.StartsWith("Search for"))
                {
                    string objectName = filter.Substring(11).Trim('\'');
                    if (!obj.name.Contains(objectName, StringComparison.OrdinalIgnoreCase))
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
