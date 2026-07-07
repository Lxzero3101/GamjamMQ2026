#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Inspector for SpellData that adds a "capture points by clicking in the Scene view"
/// workflow, so you don't have to hand-type Vector2 coordinates into the template points list.
///
/// Usage:
/// 1. Select a SpellData asset in the Project window.
/// 2. In the Inspector, click "Start Capturing Points".
/// 3. Click in the Scene view, in order, to trace the shape. Each click drops a point;
///    a yellow line preview connects them so you can see the shape as you go.
/// 4. Press Enter (or click "Finish & Save") to write the captured points into the asset's
///    _templatePoints list. Backspace undoes the last point. Escape cancels the whole capture.
///
/// This only touches the editor-time serialized data via SerializedProperty — it doesn't
/// need any public setter on SpellData, and writing through SerializedObject triggers
/// SpellData's own OnValidate (which clears the normalization cache), so you never end up
/// comparing against stale cached data after a re-capture.
///
/// IMPORTANT: this file must live inside a folder named "Editor" anywhere in your Assets
/// (e.g. Assets/Editor/SpellDataEditor.cs) so Unity excludes it from player builds.
/// </summary>
[CustomEditor(typeof(SpellData))]
public class SpellDataEditor : Editor
{
    private bool _isCapturing;
    private readonly List<Vector2> _capturedPoints = new List<Vector2>();

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    public override void OnInspectorGUI()
    {
        // Draw all the normal SpellData fields (name, guide image, template points list) first.
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Template Point Capture", EditorStyles.boldLabel);

        if (!_isCapturing)
        {
            if (GUILayout.Button("Start Capturing Points (Scene View)"))
            {
                _isCapturing = true;
                _capturedPoints.Clear();
                SceneView.RepaintAll();
            }
        }
        else
        {
            EditorGUILayout.HelpBox(
                $"Capturing... {_capturedPoints.Count} point(s).\n" +
                "Click in the Scene view to add points, in order, tracing the shape.\n" +
                "Enter = finish & save   |   Backspace = undo last point   |   Escape = cancel",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Undo Last Point"))
                {
                    UndoLastPoint();
                }

                if (GUILayout.Button("Cancel"))
                {
                    CancelCapture();
                }
            }

            GUI.enabled = _capturedPoints.Count >= 2;
            if (GUILayout.Button("Finish & Save to Template Points"))
            {
                FinishCapture();
            }
            GUI.enabled = true;
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!_isCapturing) return;

        Event e = Event.current;

        // Left click: add a point at the mouse position projected onto the z=0 plane.
        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Plane drawPlane = new Plane(Vector3.forward, Vector3.zero);

            if (drawPlane.Raycast(ray, out float distance))
            {
                Vector3 worldPoint = ray.GetPoint(distance);
                _capturedPoints.Add(new Vector2(worldPoint.x, worldPoint.y));
                Repaint();
            }

            e.Use();
        }

        // Keyboard shortcuts while capturing.
        if (e.type == EventType.KeyDown)
        {
            if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
            {
                FinishCapture();
                e.Use();
            }
            else if (e.keyCode == KeyCode.Backspace)
            {
                UndoLastPoint();
                e.Use();
            }
            else if (e.keyCode == KeyCode.Escape)
            {
                CancelCapture();
                e.Use();
            }
        }

        DrawPreview();

        // Keep repainting while capturing so the preview line follows clicks immediately.
        HandleUtility.Repaint();
    }

    private void DrawPreview()
    {
        if (_capturedPoints.Count == 0) return;

        Handles.color = Color.yellow;

        for (int i = 0; i < _capturedPoints.Count; i++)
        {
            Vector3 point = new Vector3(_capturedPoints[i].x, _capturedPoints[i].y, 0f);
            Handles.SphereHandleCap(0, point, Quaternion.identity, 0.08f, EventType.Repaint);

            if (i > 0)
            {
                Vector3 previous = new Vector3(_capturedPoints[i - 1].x, _capturedPoints[i - 1].y, 0f);
                Handles.DrawLine(previous, point);
            }
        }
    }

    private void UndoLastPoint()
    {
        if (_capturedPoints.Count == 0) return;
        _capturedPoints.RemoveAt(_capturedPoints.Count - 1);
        Repaint();
        SceneView.RepaintAll();
    }

    private void CancelCapture()
    {
        _isCapturing = false;
        _capturedPoints.Clear();
        Repaint();
        SceneView.RepaintAll();
    }

    private void FinishCapture()
    {
        if (_capturedPoints.Count < 2)
        {
            Debug.LogWarning("SpellDataEditor: need at least 2 points to save a shape.");
            return;
        }

        serializedObject.Update();

        SerializedProperty templatePointsProp = serializedObject.FindProperty("_templatePoints");
        templatePointsProp.ClearArray();

        for (int i = 0; i < _capturedPoints.Count; i++)
        {
            templatePointsProp.InsertArrayElementAtIndex(i);
            templatePointsProp.GetArrayElementAtIndex(i).vector2Value = _capturedPoints[i];
        }

        // ApplyModifiedProperties triggers SpellData's OnValidate, which clears the
        // normalization cache — so ShapeComparer will re-normalize fresh points next use.
        serializedObject.ApplyModifiedProperties();

        EditorUtility.SetDirty(target);
        AssetDatabase.SaveAssets();

        _isCapturing = false;
        _capturedPoints.Clear();
        Repaint();
        SceneView.RepaintAll();

        Debug.Log($"SpellDataEditor: saved {templatePointsProp.arraySize} template points to '{target.name}'.");
    }
}
#endif