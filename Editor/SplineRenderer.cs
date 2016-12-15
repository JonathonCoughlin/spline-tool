using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;


class SplineRenderer : Editor
{


    private BezierSpline mySpline;
    private Quaternion splineQuaternion;

    //Interactivity Resolution
    private static int mousePixelRange = 10;
    private static int mouseSplineResolution = 1000;

    //Appearance
    private const float directionScale = 0.5f;
    private const int stepsPerCurve = 10;

    //handles
    private Transform handleTransform;
    private Quaternion handleRotation;

    //handle look/feel
    private const float dotCapSize = 0.04f;
    private const float sphereCapSize = 0.10f;
    private const float addPointCapScale = 1.5f;

    //GUI
    public static int hash = "BezierSplineRenderer".GetHashCode();

    //Curve coloring
    public Color specialCurveColor = Color.red;
    private int highlightedCurve = 0;

    private static Color[] modeColors =
    {
        Color.white,
        Color.yellow,
        Color.cyan
    };

    //Editing States
    //-Dragging modes
    private bool dragging = false;
    enum DragType { LeftMouse, RightMouse };
    private DragType m_dragType;
    private bool waitingForDrag = false;

    private bool inRangeOfSpline = false;
    private float mouseSplinePercentage;
    private bool inRangeOfControlPoint = false;
    private int selectedIndex = -1;
    private int nearestIndex = -1;


    public SplineRenderer (BezierSpline tgtSpline)
    {
        mySpline = tgtSpline;
    }

    public void DrawAndEdit()
    {

        handleTransform = mySpline.transform;
        handleRotation = Tools.pivotRotation == PivotRotation.Local ?
            handleTransform.rotation : Quaternion.identity;

        DrawSpline();

        EditSpline();

    }

    #region Editing Routines
    private void EditSpline()
    {
        StoreSplineStates();
        ManageClicking();
    }

    private void StoreSplineStates()
    {
        splineQuaternion = mySpline.transform.rotation;
    }

    private void ManageClicking()
    {
        int controlID = GUIUtility.GetControlID(hash, FocusType.Passive);
        Event currentEvent = Event.current;
        bool mouseDown = currentEvent.GetTypeForControl(controlID) == EventType.MouseDown;
        bool mouseUp = currentEvent.GetTypeForControl(controlID) == EventType.MouseUp;
        bool mouseDrag = currentEvent.GetTypeForControl(controlID) == EventType.MouseDrag;
        bool mouseEvent = (mouseDown || mouseUp || mouseDrag);

        //Type event
        bool control = currentEvent.control;
        bool clickL = currentEvent.button == 0;
        bool clickR = currentEvent.button == 1;
        bool controlLClick = control && clickL;
        bool controlRClick = control && clickR;

        if (mouseDown && (inRangeOfSpline || inRangeOfControlPoint))
        {
            if (controlLClick) // Add Point
            {
                Undo.RecordObject(mySpline, "Added Control Point");
                mySpline.AddCurveAtPosition(mouseSplinePercentage);
                //Stupid GUI stuff
                GUIUtility.hotControl = controlID;
                currentEvent.Use();
            }
            else if (controlRClick && inRangeOfControlPoint) // Remove Point
            {
                // select nearest point
                selectedIndex = nearestIndex;
                //Editor.Repaint();
                // delete point
                Undo.RecordObject(mySpline, "Removed Control Point");
                mySpline.RemovePoint(selectedIndex);
                GUIUtility.hotControl = controlID;
                currentEvent.Use();
            }
            else if (clickL || clickR) //Allow click on spline or points
            {
                //Stupid GUI stuff
                GUIUtility.hotControl = controlID;
                currentEvent.Use();
                if (inRangeOfControlPoint)
                {
                    selectedIndex = nearestIndex;
                    waitingForDrag = true;
                    //Refresh inspector
                    //Repaint();
                }
            }
        }
        else if (!dragging && mouseDrag && waitingForDrag && inRangeOfControlPoint && (clickL || clickR))
        {
            //Which type of drag?
            if (clickL && !clickR)
            {
                BeginDrag(DragType.LeftMouse);
            }
            else if (clickR && !clickL)
            {
                BeginDrag(DragType.RightMouse);
            }
        }
        else if (mouseUp)
        {
            bool timeToEndDrag = false;
            waitingForDrag = false;

            //check if correct button was raised
            if (dragging)
            {
                switch (m_dragType)
                {
                    case DragType.LeftMouse:
                        if (clickL) timeToEndDrag = true;
                        break;
                    case DragType.RightMouse:
                        if (clickR) timeToEndDrag = true;
                        break;
                }
                currentEvent.Use();
            }

            if (timeToEndDrag)
            {
                FinishDrag();
            }
            //Stupid GUI stuff
            if (GUIUtility.hotControl == controlID)
            {
                GUIUtility.hotControl = 0;
                Debug.Log("Releasing spline editor mouse control");
                currentEvent.Use();
            }

        }
        else if (dragging && mouseDrag)
        {
            //Continue Drag
            ManageDrag(selectedIndex);
            Undo.RecordObject(mySpline, "Move Point");
            currentEvent.Use();
        }
    }

    private void BeginDrag(DragType myDragType)
    {
        dragging = true;
        m_dragType = myDragType;
        Debug.Log("Beginning Drag: " + myDragType.ToString());
        ManageDrag(selectedIndex);
    }

    private void ManageDrag(int dragPointIndex)
    {
        Vector3 pointNewPositionSplineSpace = new Vector3();
        Vector3 originalPoint = mySpline.GetControlPoint(dragPointIndex);
        switch (m_dragType)
        {
            case DragType.LeftMouse:
                pointNewPositionSplineSpace = MousePosInSplinePlane(originalPoint.y);
                break;
            case DragType.RightMouse:
                pointNewPositionSplineSpace = MousePosInSplineVertical(originalPoint.x, originalPoint.z);
                break;
        }

        Undo.RecordObject(mySpline, "Moved Point");
        mySpline.SetControlPoint(dragPointIndex, pointNewPositionSplineSpace);
        SceneView.RepaintAll();
    }

    private Vector3 MousePosInSplineVertical(float currentPointX, float currentPointZ)
    {
        //*Only move point in spline XZ space
        //!!Only works in overhead view, movement reverses in underneath view 
        //- something about the transformations is jacked up

        // Get mouse position on screen
        Vector2 mouseInScreenXY = Event.current.mousePosition;
        mouseInScreenXY.y = SceneView.lastActiveSceneView.camera.pixelHeight - mouseInScreenXY.y;
        Vector3 mouseInScreen = new Vector3(mouseInScreenXY.x, mouseInScreenXY.y, 0f);
        Ray mouseRay = SceneView.currentDrawingSceneView.camera.ScreenPointToRay(mouseInScreen);
        // Get distance from camera to spline XY plane
        //  -Create spline XY plane in world space
        //  --Make vector in worldSpace representing splineSpace Z vector
        Vector3 splineZvec = new Vector3(0f, 0f, 1f); // spline Z unit vector
        Vector3 splineZvecInWorld = handleTransform.TransformVector(splineZvec);
        //  --Make spline Z point, convert to world space
        Vector3 curPointZInSpline = new Vector3(0f, 0f, currentPointZ);
        Vector3 curPointZInWorld = handleTransform.TransformPoint(curPointZInSpline);
        //  --Make spline plane in world space
        Plane splineXYplaneInWorld = new Plane(splineZvecInWorld, curPointZInWorld);
        //convert mouse screen position to position in world space
        float cameraToSplineXY;
        splineXYplaneInWorld.Raycast(mouseRay, out cameraToSplineXY);
        Vector3 mouseInWorld = mouseRay.GetPoint(cameraToSplineXY);
        //convert mouse world point to spline space
        Vector3 mouseInSplineSpace = handleTransform.InverseTransformPoint(mouseInWorld);
        //reset X,Z to original values 
        mouseInSplineSpace.x = currentPointX;
        mouseInSplineSpace.z = currentPointZ;

        return mouseInSplineSpace;

    }

    private Vector3 MousePosInSplinePlane(float currentPointY)
    {
        //*Only move point in spline XZ space
        //!!Only works in overhead view, movement reverses in underneath view 
        //- something about the transformations is jacked up

        // Get mouse position on screen
        Vector2 mouseInScreenXY = Event.current.mousePosition;
        mouseInScreenXY.y = SceneView.lastActiveSceneView.camera.pixelHeight - mouseInScreenXY.y;
        Vector3 mouseInScreen = new Vector3(mouseInScreenXY.x, mouseInScreenXY.y, 0f);
        Ray mouseRay = SceneView.currentDrawingSceneView.camera.ScreenPointToRay(mouseInScreen);
        // Get distance from camera to spline XZ plane
        //  -Create spline XZ plane in world space
        //  --Make vector in worldSpace representing splineSpace Y vector
        Vector3 splineYvec = new Vector3(0f, 1f, 0f); // spline Y unit vector
        Vector3 splineYvecInWorld = handleTransform.TransformVector(splineYvec);
        //  --Make spline Y point, convert to world space
        Vector3 curPointYInSpline = new Vector3(0f, currentPointY, 0f);
        Vector3 curPointYInWorld = handleTransform.TransformPoint(curPointYInSpline);
        //  --Make spline plane in world space
        Plane splineXZplaneInWorld = new Plane(splineYvecInWorld, curPointYInWorld);
        //convert mouse screen position to position in world space
        float cameraToSplineXZ;
        splineXZplaneInWorld.Raycast(mouseRay, out cameraToSplineXZ);
        Vector3 mouseInWorld = mouseRay.GetPoint(cameraToSplineXZ);
        //convert mouse world point to spline space
        Vector3 mouseInSplineSpace = handleTransform.InverseTransformPoint(mouseInWorld);
        //reset Y to zero 
        mouseInSplineSpace.y = currentPointY;

        return mouseInSplineSpace;

    }

    private void FinishDrag()
    {
        ManageDrag(selectedIndex);
        dragging = false;
    }

    #endregion

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

        //Vector2 nearestSplinePos = new Vector2();
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
                //nearestSplinePos = splinePos;
            }
        }

        return mouseInRange;
    }

    private bool MouseInRangeOfControlPoint()
    {
        bool mouseInRange = false;
        //Reset nearestIndex
        nearestIndex = -1;
        float minMouseDistance = -1f;
        //Get mouse position
        Vector2 mousePosition = Event.current.mousePosition;
        //Stupid scene view coordinate adjustment for mouse position
        mousePosition.y = SceneView.lastActiveSceneView.camera.pixelHeight - mousePosition.y;
        //Get points
        List<Vector3> pointPositionsWorld = mySpline.GetControlPointList(true);
        //convert positions to screen space
        List<Vector3> pointPositionsScreen = new List<Vector3>();
        foreach (Vector3 worldPos in pointPositionsWorld)
        {
            pointPositionsScreen.Add(SceneView.lastActiveSceneView.camera.WorldToScreenPoint(worldPos));
        }

        //Vector2 nearestPointPos = new Vector2();
        for (int ii = 0; ii < mySpline.ControlPointCount; ii++)
        {
            //if mouse in range of point, try to store it as minDistance
            Vector2 pointPos = new Vector2(pointPositionsScreen[ii].x, pointPositionsScreen[ii].y);
            float mouseToPointDistance = Vector2.Distance(pointPos, mousePosition);
            if (mouseToPointDistance <= (float)mousePixelRange)
            {
                mouseInRange = true;
                if (minMouseDistance == -1f) // first recorded min distance
                {
                    minMouseDistance = mouseToPointDistance;
                    nearestIndex = ii;
                }
                else if (mouseToPointDistance < minMouseDistance)
                {
                    minMouseDistance = mouseToPointDistance;
                    nearestIndex = ii;
                }
                //nearestPointPos = pointPos;
            }
        }

        return mouseInRange;
    }

    #region Draw Routines

    public void HighlightCurve(int curveIDX)
    {
        highlightedCurve = curveIDX;
    }

    private void DrawSpline()
    {
        //Where is mouse relative to spline?
        if (!dragging)
        {
            inRangeOfSpline = MouseInRangeOfSpline();
            inRangeOfControlPoint = MouseInRangeOfControlPoint();
        }

        Vector3 p0 = ShowPoint(0, true); // anchor point
        for (int ii = 1; ii < mySpline.ControlPointCount; ii += 3)
        {
            Vector3 p1 = ShowPoint(ii, false); //bezier point
            Vector3 p2 = ShowPoint(ii + 1, false); //bezier point
            Vector3 p3 = ShowPoint(ii + 2, true); //anchor point

            Handles.color = Color.grey;
            Handles.DrawLine(p0, p1);
            Handles.DrawLine(p2, p3);

            Color bezierColor;
            bezierColor = (ii/3) == highlightedCurve ? specialCurveColor : Color.white;

            Handles.DrawBezier(p0, p3, p1, p2, bezierColor, null, 2f);
            p0 = p3;
        }

        if (inRangeOfSpline && !dragging)
        {
            ShowPercentagePoint(mouseSplinePercentage, Color.green);
        }
    }

    private Vector3 ShowPoint(int pointIndex, bool pointIsAnchor)
    {
        Vector3 point = handleTransform.TransformPoint(mySpline.GetControlPoint(pointIndex));
        //Vector3 point = mySpline.GetControlPoint(pointIndex);

        float size = HandleUtility.GetHandleSize(point);
        if (pointIndex == 0)
        {
            size *= addPointCapScale;
        }
        Handles.color = modeColors[(int)mySpline.GetControlPointMode(pointIndex)];
        if (selectedIndex == pointIndex)
        {
            Handles.color = Color.magenta;
        }

        //Choose cap type
        if (pointIsAnchor)
        {
            Handles.SphereCap(pointIndex, point, handleRotation, size * sphereCapSize);
        }
        else
        {
            Handles.DotCap(pointIndex, point, handleRotation, size * dotCapSize);
        }

        return point;
    }

    private void ShowPercentagePoint(float percentOnSpline, Color dotColor)
    {
        Vector3 pointInWorldSpace = mySpline.GetPoint(percentOnSpline, true);
        float size = HandleUtility.GetHandleSize(pointInWorldSpace) * sphereCapSize * addPointCapScale;
        Handles.color = dotColor;
        Handles.SphereCap(0, pointInWorldSpace, handleRotation, size);
        SceneView.RepaintAll();
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

    #endregion
}

