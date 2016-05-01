# spline-tool
A tool for creating splines in the Unity editor window
Created by Jon Coughlin
Basic spline and spline inspector functionality inspired by Curves and Splines tutorial at catlikecoding.com

-Contents-
BezierSpline.cs
  A bezier spline class capable of outputting bezier curves integrated between user-defined control points at a user-defined resolution.
  Control points exist as:
    Anchor points - points through which the spline must intersect
    Stretch points - points that define the spline curve inbetween anchors. 

BezierSplineEditor.cs
  A custom Unity editor that allows click/drag manipulation of the spline in the Unity scene view

-Instructions-
1. Add the Spline Editor folder to your Unity project
2. Add the BezierSpline class to a Unity object
3. Manipulate your BezeirSpline in the Unity scene view with mouse and keyboard

-Controls-
--Select Points--
  * Select a control point for manipulation [Left Click OR Right Click]
--Move Points--
  * Move a control point in spline XZ plane [LeftClick+Drag on control point]
  * Move a contol point in the spline Y axis [RightClick+Drag on control point]
--Add/Remove Points--
  * Add new anchor point to spline at mouse location [Ctrl+LeftClick on spline]
  * Remove point from spline [Ctrl+RightClick on control point] !Note! When removing a control point, the Stretch and Anchor point(s) associated with the clicked point will also be removed.
  
