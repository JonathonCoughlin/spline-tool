using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

//*Refactor Spline Inspector
//  -Add custom point movement routines (stop relying on position handles)
//  -Separate Edit() from Draw() routines
//  -Make Edit() 1st, Draw() 2nd
//*Edit()
//  -by default, only move in local X,Z plane
//  -Ctrl+Lclick AddPoint()
//  -Ctrl+Rclick DeletePoint()
//*Github
//  -Create a repo on github for spline work
//  -Open empty unity project, init inside a spline folder
//  -Learn how to add spline work as a shared library
//  -Start contributing to EVERYONE'S repos!!!!!

[CustomEditor(typeof(BezierSpline))]

public class BezierSplineInspector : Editor {

    private BezierSpline mySpline;
    private Transform handleTransform;
    private Quaternion handleRotation;

    private const float directionScale = 0.5f;
    private const int stepsPerCurve = 10;

    //Adding Points
    [SerializeField]
    static private bool addPointBySlider;
    [SerializeField]
    static private float pointSliderValue;
    [SerializeField]
    static private bool addPointWithMouse;
    private float mouseSplinePercentage;
    [SerializeField]
    private int mousePixelRange = 10;
    [SerializeField]
    private int mouseSplineResolution = 300;


    //handle look/feel
    private const float handleSize = 0.04f;
    private const float pickSize = 0.06f;
    private const float dotScale = 0.07f;

    private static Color[] modeColors =
    {
        Color.white,
        Color.yellow,
        Color.cyan
    };

    private int selectedIndex = -1;

    public override void OnInspectorGUI()
    {
        mySpline = target as BezierSpline;
        
        // Loop checkbox
        EditorGUI.BeginChangeCheck();
        bool loop = EditorGUILayout.Toggle("Loop", mySpline.Loop);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(mySpline, "Toggle Loop");
            EditorUtility.SetDirty(mySpline);
            mySpline.Loop = loop;
        }
        // show single point coords
        if (selectedIndex >= 0 && selectedIndex < mySpline.ControlPointCount)
        {
            DrawSelectedPointInspector();
        }
        //Mouse Code
        addPointWithMouse = EditorGUILayout.Toggle("Add anchor with Mouse", addPointWithMouse);
        if (addPointWithMouse)
        {
            // Set mouse pixel range, traveler resolution
            mousePixelRange = EditorGUILayout.IntField("Mouse pixel range", mousePixelRange);
            mouseSplineResolution = EditorGUILayout.IntField("Mouse spline rez", mouseSplineResolution);
        }
        //Slider code
        addPointBySlider = EditorGUILayout.Toggle("Add anchor with slider",addPointBySlider);
        if (addPointBySlider)
        {
            if (float.IsNaN(pointSliderValue)) { pointSliderValue = 0.5f; }
            EditorGUI.BeginChangeCheck();
            pointSliderValue = EditorGUILayout.Slider(pointSliderValue, 0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(mySpline, "Move slider");
                EditorUtility.SetDirty(mySpline);
                SceneView.RepaintAll();
            }
        }
        // New curve button
        if (GUILayout.Button("Add Anchor Point"))
        {
            Undo.RecordObject(mySpline, "Add Anchor Point");
            if (addPointBySlider)
            {
                mySpline.AddCurveAtPosition(pointSliderValue);
            }
            else { mySpline.AddCurve(); }
            EditorUtility.SetDirty(mySpline);
        }
    }

    private void OnSceneGUI()
    {
        mySpline = target as BezierSpline;
        handleTransform = mySpline.transform;
        handleRotation = Tools.pivotRotation == PivotRotation.Local ?
            handleTransform.rotation : Quaternion.identity;

        DrawSpline();

        if (addPointBySlider)
        {
            ShowPercentagePoint(pointSliderValue, Color.red);
        }
        if (addPointWithMouse && MouseInRangeOfSpline())
        {
            ShowPercentagePoint(mouseSplinePercentage, Color.green);
            ManageAnchorPoints(mouseSplinePercentage);
        }

        ShowDirections();
    }

    private void ManageAnchorPoints(float splinePercentage)
    {
        //Add point logic
        Event currentEvent = Event.current;
        bool control = currentEvent.control;
        bool click = currentEvent.type == EventType.mouseDown && currentEvent.button == 0;
        bool controlClick = control && click;
        
        if (controlClick)
        {
            mySpline.AddCurveAtPosition(splinePercentage);
        }
        Selection.activeObject = target;
    }

    private bool MouseInRangeOfSpline()
    {
        bool mouseInRange = false;
        float minMouseDistance = -1f;
        //Get mouse position
        Vector2 mousePosition = Event.current.mousePosition;
        //Stupid scene view coordinate adjustment for mouse position
        mousePosition.y = SceneView.lastActiveSceneView.camera.pixelHeight - mousePosition.y;
        //Get spline positions in world space
        List<Vector3> splinePositionsWorld = mySpline.GetFullSplineList(mouseSplineResolution, true);
        //convert positions to screen space
        List<Vector3> splinePositionsScreen = new List<Vector3>();
        foreach (Vector3 worldPos in splinePositionsWorld)
        {
            splinePositionsScreen.Add(SceneView.lastActiveSceneView.camera.WorldToScreenPoint(worldPos));
        }

        Vector2 nearestSplinePos = new Vector2();
        for (int ii = 0; ii < splinePositionsScreen.Count; ii++)
        {
            //if mouse in range of point, try to store it as minDistance
            Vector2 splinePos = new Vector2(splinePositionsScreen[ii].x, splinePositionsScreen[ii].y);
            float mouseToPointDistance = Vector2.Distance(splinePos, mousePosition);
            if (mouseToPointDistance <= (float)mousePixelRange)
            {
                mouseInRange = true;
                if (minMouseDistance == -1f) // first recorded min distance
                {
                    minMouseDistance = mouseToPointDistance;
                    mouseSplinePercentage = ii / (float)mouseSplineResolution;
                }
                else if (mouseToPointDistance < minMouseDistance)
                {
                    minMouseDistance = mouseToPointDistance;
                    mouseSplinePercentage = ii / (float)mouseSplineResolution;
                }
                nearestSplinePos = splinePos;
            }
        }
        
        return mouseInRange;
    }

    private void ShowPercentagePoint(float percentOnSpline, Color dotColor)
    {
        Vector3 pointInWorldSpace = mySpline.GetPoint(percentOnSpline, true);
        float size = HandleUtility.GetHandleSize(pointInWorldSpace)*dotScale;
        Handles.color = dotColor;
        Handles.CircleCap(0, pointInWorldSpace, handleRotation, size);
        SceneView.RepaintAll();
    }

    private void DrawSpline()
    {
        Vector3 p0 = ShowPoint(0, true); // anchor point
        for (int ii = 1; ii < mySpline.ControlPointCount; ii += 3)
        {
            Vector3 p1 = ShowPoint(ii, false); //bezier point
            Vector3 p2 = ShowPoint(ii + 1, false); //bezier point
            Vector3 p3 = ShowPoint(ii + 2, true); //anchor point

            Handles.color = Color.grey;
            Handles.DrawLine(p0, p1);
            Handles.DrawLine(p2, p3);

            Handles.DrawBezier(p0, p3, p1, p2, Color.white, null, 2f);
            p0 = p3;
        }
    }

    private Vector3 ShowPoint(int pointIndex, bool pointIsAnchor)
    {
        Vector3 point = handleTransform.TransformPoint(mySpline.GetControlPoint(pointIndex));
        //Vector3 point = mySpline.GetControlPoint(pointIndex);

        float size = HandleUtility.GetHandleSize(point);
        if (pointIndex == 0)
        {
            size *= 2f;
        }
        Handles.color = modeColors[(int)mySpline.GetControlPointMode(pointIndex)];
        //Choose cap type
        Handles.DrawCapFunction pointCapType;
        if (pointIsAnchor)
        {
            pointCapType = Handles.CircleCap;
        }
        else
        {
            pointCapType = Handles.DotCap;
        }

        //Draw point
        if (Handles.Button(point, handleRotation, size * handleSize, size * pickSize, pointCapType))
        {
            selectedIndex = pointIndex;
            Repaint();
        }
        
        if (selectedIndex == pointIndex)
        {
            // Show handle, allow movement of point
            EditorGUI.BeginChangeCheck();
            point = Handles.DoPositionHandle(point, handleRotation);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(mySpline, "Move Point");
                EditorUtility.SetDirty(mySpline);
                mySpline.SetControlPoint(pointIndex,handleTransform.InverseTransformPoint(point));
            }
        }
        return point;
    }

    private void ShowDirections()
    {
        Handles.color = Color.green;
        Vector3 point = mySpline.GetPoint(0f, true);
        Handles.DrawLine(point, point + mySpline.GetDirection(0f) * directionScale);
        int steps = stepsPerCurve * mySpline.CurveCount;
        for (int ii = 1; ii <= steps; ii++)
        {
            point = mySpline.GetPoint(ii / (float)steps, true);
            Handles.DrawLine(point, point + mySpline.GetDirection(ii / (float)steps) * directionScale);
        }
    }

    private void DrawSelectedPointInspector()
    {
        GUILayout.Label("Selected Point");
        EditorGUI.BeginChangeCheck();
        Vector3 point = EditorGUILayout.Vector3Field("Position", mySpline.GetControlPoint(selectedIndex));
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(mySpline, "Move Point");
            EditorUtility.SetDirty(mySpline);
            mySpline.SetControlPoint(selectedIndex, point);
        }
        EditorGUI.BeginChangeCheck();
        BezierControlPointMode mode = (BezierControlPointMode)
            EditorGUILayout.EnumPopup("Mode", mySpline.GetControlPointMode(selectedIndex));
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(mySpline, "Change Point Mode");
            mySpline.SetControlPointMode(selectedIndex, mode);
            EditorUtility.SetDirty(mySpline);
        }
    }

}
