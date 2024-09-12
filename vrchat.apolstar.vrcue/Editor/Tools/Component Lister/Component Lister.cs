#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using APOLStar.VRCUE.Common.UI.Footer;

public class ComponentLister : EditorWindow
{
    private GameObject selectedObject = null;
    private List<string> componentNames = new List<string>();

    [MenuItem("Tools/VRC Unity Essentials/Advanced/Component Lister")]
    public static void ShowWindow()
    {
        GetWindow<ComponentLister>("Component Lister");
    }

    private void OnEnable()
    {
        Selection.selectionChanged += OnSelectionChange; // Listen for selection changes
        UpdateComponentList();
    }

    private void OnDisable()
    {
        Selection.selectionChanged -= OnSelectionChange; // Stop listening when window is closed
    }

    // Update the component list when a new object is selected
    private void OnSelectionChange()
    {
        UpdateComponentList();
        Repaint(); // Force the window to repaint to show the updated components
    }

    // Update the list of components on the currently selected object
    private void UpdateComponentList()
    {
        componentNames.Clear();
        selectedObject = Selection.activeGameObject;

        if (selectedObject != null)
        {
            Component[] components = selectedObject.GetComponents<Component>();
            foreach (Component component in components)
            {
                componentNames.Add(component.GetType().FullName); // Add the full component name (namespace + class name)
            }
        }
    }

    private void OnGUI()
    {
        // Display helpbox explaining the tool
        EditorGUILayout.HelpBox("This tool displays the list of components on the selected GameObject. You can copy component names by clicking on them.", MessageType.Info);

        // Show the selected object's name
        if (selectedObject != null)
        {
            EditorGUILayout.LabelField("Selected Object:", selectedObject.name);

            EditorGUILayout.Space();

            // Display the list of components and allow copying the names
            EditorGUILayout.LabelField("Components on Selected Object:");
            foreach (string componentName in componentNames)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.TextField(componentName); // Make the component name copyable
                if (GUILayout.Button("Copy", GUILayout.Width(50)))
                {
                    EditorGUIUtility.systemCopyBuffer = componentName; // Copy the component name to the clipboard
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        else
        {
            EditorGUILayout.LabelField("No object selected.");
        }

        // Footer for credits
        EditorGUILayout.Space();
        APOLStar.VRCUE.Common.UI.Footer.Credits.DrawFooter("APOL Assets");
    }
}
#endif
