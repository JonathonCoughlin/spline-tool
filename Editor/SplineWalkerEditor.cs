﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

[CustomEditor(typeof(SplineWalker))]
[System.Serializable]
public class SplineWalkerEditor : Editor {

    private SplineWalker m_Walker;

    private SplineRenderer m_Renderer;
    private string testString;
    [SerializeField]
    private int currentCurve = 0;


    void OnEnable()
    {
        m_Walker = target as SplineWalker;
        
    }

 
    private void OnSceneGUI()
    {
        ShowHostSpline();
    }

    private void ShowHostSpline()
    {
        if (CheckRenderer())
        {
            m_Renderer.HighlightCurve(currentCurve);
            m_Renderer.DrawAndEdit(Event.current);
            SceneView.RepaintAll();
        }
    }

    private bool CheckRenderer()
    {
        bool rendererExists = false;

        if (m_Renderer != null)
        {
            rendererExists = true;
        } else if (m_Walker.m_Spline != null)
        {
            m_Renderer = (SplineRenderer)CreateInstance("SplineRenderer");
            m_Renderer.SetSpline(m_Walker.m_Spline);
            rendererExists = true;
        }

        return rendererExists;
    }

    public override void OnInspectorGUI()
    {

        m_Walker = target as SplineWalker;

        serializedObject.Update();
        
        m_Walker.m_Spline = (BezierSpline)EditorGUILayout.ObjectField(m_Walker.m_Spline, typeof(BezierSpline),true);

        if (!(m_Walker.m_Spline == null))
        {

            CheckRenderer();

            int splineCurves = m_Walker.m_Spline.CurveCount;
            if (m_Walker.m_WalkerSplinePoints.Count == 0)
            {
                m_Walker.AlignToNewSpline(m_Walker.m_Spline);
            } else if (m_Walker.m_WalkerSplinePoints.Count != m_Walker.m_Spline.CurveCount)
            {
                m_Walker.ResizeToSpline();
            }

            ////SplineWalkerTester
            //labelStyle.alignment = TextAnchor.UpperCenter;
            //labelStyle.fontStyle = FontStyle.Bold;
            //EditorGUILayout.LabelField("Walker Motion Tester", labelStyle);
            //if (GUILayout.Button(new GUIContent("Play/Pause"), EditorStyles.miniButton))
            //{
            //    if (!m_Walker.m_walking)
            //    {
            //        m_Walker.StartWalking();
            //    }
            //    else
            //    {
            //        m_Walker.PauseWalking();
            //    }
            //}

            //Set Up Styles
            var labelStyle = GUI.skin.GetStyle("Label");
            labelStyle.fontStyle = FontStyle.Normal;

            //Spline Summary
            EditorGUILayout.BeginHorizontal();

            labelStyle.fontStyle = FontStyle.Bold;
            EditorGUILayout.LabelField("Total Curves: ", GUILayout.Width(80f));
            EditorGUILayout.LabelField(splineCurves.ToString(), labelStyle, GUILayout.Width(30f));
            EditorGUILayout.LabelField("Total Time: ", GUILayout.Width(80f));
            EditorGUILayout.LabelField(m_Walker.TotalTime().ToString(), GUILayout.Width(30f));

            EditorGUILayout.EndHorizontal();

            //InitialPosition
            EditorGUILayout.LabelField("Initial Spline Position");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_initialSplinePos"), GUIContent.none);
            if (!Application.isPlaying)
            {
                m_Walker.SetPosToInitial();
            }

            //Auto functionality
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Auto Walk", GUILayout.Width(63f));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_autoWalk"), GUIContent.none, GUILayout.Width(15f));
            EditorGUILayout.LabelField("Auto Reset", GUILayout.Width(66f));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_autoReset"), GUIContent.none, GUILayout.Width(15f));
            EditorGUILayout.LabelField("Auto Kill", GUILayout.Width(55f));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_destroyAtEnd"), GUIContent.none, GUILayout.Width(15f));

            EditorGUILayout.EndHorizontal();

            //Speed functionality
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Vary Speed", GUILayout.Width(70f));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_variableSpeed"), GUIContent.none, GUILayout.Width(15f));
            EditorGUILayout.LabelField("Spd Type", GUILayout.Width(60f));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_speedType"), GUIContent.none, GUILayout.Width(100f));

            EditorGUILayout.EndHorizontal();


            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Rotation", GUILayout.Width(60f));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_rotationType"), GUIContent.none, GUILayout.Width(60f));
            if (m_Walker.m_rotationType == WalkerRotationType.Angle || m_Walker.m_rotationType == WalkerRotationType.Velocity)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_rotationAxis"), GUIContent.none, GUILayout.Width(60f));
                if (m_Walker.m_rotationType == WalkerRotationType.Angle)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_angleOffset"), GUIContent.none, GUILayout.Width(60f));
                }
            } else if (m_Walker.m_rotationType == WalkerRotationType.Target && !m_Walker.m_variableSpeed)
            {
                m_Walker.m_lookTarget = (GameObject)EditorGUILayout.ObjectField(m_Walker.m_lookTarget, (typeof(GameObject)), true);
            }
            
            EditorGUILayout.EndHorizontal();


            if (!m_Walker.m_variableSpeed)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_walkSpeed"));
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                //Current Curve Editor
                labelStyle.alignment = TextAnchor.UpperCenter;
                labelStyle.fontStyle = FontStyle.Bold;
                EditorGUILayout.LabelField("Curve Editor", labelStyle);
                currentCurve = EditorGUILayout.IntSlider(currentCurve, 0, splineCurves - 1);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent("<"), EditorStyles.miniButtonLeft))
                {
                    currentCurve -= 1;
                    currentCurve = currentCurve < 0 ? splineCurves - 1 : currentCurve;
                }
                labelStyle = GUI.skin.GetStyle("Label");
                labelStyle.alignment = TextAnchor.UpperCenter;
                EditorGUILayout.LabelField(currentCurve.ToString(), labelStyle, GUILayout.Width(30f));
                if (GUILayout.Button(new GUIContent(">"), EditorStyles.miniButtonRight))
                {
                    currentCurve += 1;
                    currentCurve = currentCurve > splineCurves - 1 ? 0 : currentCurve;
                }
                EditorGUILayout.EndHorizontal();

                //Current point to edit
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("m_WalkerSplinePoints").GetArrayElementAtIndex(currentCurve),
                    GUIContent.none);

                //Duplicate point
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent("Duplicate Left"), EditorStyles.miniButton))
                {
                    m_Walker.DuplicatePoint(currentCurve, currentCurve - 1);
                }
                if (GUILayout.Button(new GUIContent("Duplicate Right"), EditorStyles.miniButton))
                {
                    m_Walker.DuplicatePoint(currentCurve, currentCurve + 1);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent("Duplicate All"), EditorStyles.miniButton))
                {
                    m_Walker.DuplicateAllPoints(currentCurve);
                }
                if (GUILayout.Button(new GUIContent("Duplicate To Idx:"), EditorStyles.miniButton, GUILayout.Width(120f)))
                {
                    m_Walker.DuplicatePoint(currentCurve, m_Walker.m_duplicationIdx);
                }
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_duplicationIdx"), GUIContent.none);
                EditorGUILayout.EndHorizontal();

                //All Points
                ShowArrayProperty(serializedObject.FindProperty("m_WalkerSplinePoints"),
                    "Curve ");
                //EditorGUILayout.PropertyField(serializedObject.FindProperty("m_WalkerSplinePoints"),true);
            }
        }
        serializedObject.ApplyModifiedProperties();
        
    }

    public void ShowArrayProperty(SerializedProperty list, string elementLabel)
    {
        EditorGUILayout.PropertyField(list);

        if (list.isExpanded)
        {
            for (int i = 0; i < list.arraySize; i++)
            {
                EditorGUIUtility.labelWidth = 60f;
                EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i),
                new GUIContent(elementLabel + (i).ToString()));
            }
        }
    }

}

[CustomPropertyDrawer(typeof(WalkerSplinePoint))]
public class WalkerSplinePointPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        float rowHeight =       16f;
        float rowMargin =       2f;
        float checkWidthRatio = 1/8f;
        float labelWidthRatio = 3/8f;
        float fieldWidthRatio = 4/8f;

        label = EditorGUI.BeginProperty(position, label, property);
        Rect contentPosition = EditorGUI.PrefixLabel(position, label);
        EditorGUI.indentLevel = 0;
        // Speed - Expands across the whole width
        Rect speedPosition = contentPosition;
        speedPosition.height = rowHeight; // default label height
        EditorGUIUtility.labelWidth = speedPosition.width * labelWidthRatio;
        EditorGUI.PropertyField(speedPosition, property.FindPropertyRelative("m_speedPerSegment"), new GUIContent("Speed"));
        
        
        // Look at Target Options
        SerializedProperty tgtCheck = property.FindPropertyRelative("m_newRotationTarget");
        Rect tgtCheckPosition = contentPosition;
        tgtCheckPosition.height =   rowHeight;
        tgtCheckPosition.width *=   checkWidthRatio;
        tgtCheckPosition.y +=       rowHeight + rowMargin;
        EditorGUI.PropertyField(tgtCheckPosition, tgtCheck, GUIContent.none);

        Rect tgtLabelPosition = tgtCheckPosition;
        tgtLabelPosition.x +=       tgtCheckPosition.width;
        tgtLabelPosition.width =    tgtCheckPosition.width * labelWidthRatio / checkWidthRatio;
        EditorGUI.LabelField(tgtLabelPosition,"Look Tgt:");

        
        Rect tgtFieldPosition = tgtLabelPosition;
        tgtFieldPosition.x +=       tgtLabelPosition.width;
        tgtFieldPosition.width =    tgtLabelPosition.width * fieldWidthRatio / labelWidthRatio;
        if (tgtCheck.boolValue)
        {
            EditorGUI.ObjectField(tgtFieldPosition, property.FindPropertyRelative("m_rotationTarget"), GUIContent.none);
        } else
        {
            EditorGUI.LabelField(tgtFieldPosition, "| enable to edit |");
        }

        // Pause at curve beginning
        SerializedProperty pauseCheck = property.FindPropertyRelative("m_pauseAtCurve");
        Rect pauseCheckPosition = contentPosition;
        pauseCheckPosition.height = rowHeight;
        pauseCheckPosition.width *= checkWidthRatio;
        pauseCheckPosition.y += (rowHeight + rowMargin) * 2f;
        EditorGUI.PropertyField(pauseCheckPosition, pauseCheck, GUIContent.none);

        Rect pauseLabelPosition = pauseCheckPosition;
        pauseLabelPosition.x += pauseCheckPosition.width;
        pauseLabelPosition.width = pauseCheckPosition.width * labelWidthRatio / checkWidthRatio;
        EditorGUI.LabelField(pauseLabelPosition, "Pause Time: ");


        Rect pauseFieldPosition = pauseLabelPosition;
        pauseFieldPosition.x += pauseLabelPosition.width;
        pauseFieldPosition.width = pauseLabelPosition.width * fieldWidthRatio / labelWidthRatio;
        if (pauseCheck.boolValue)
        {
            EditorGUI.PropertyField(pauseFieldPosition, property.FindPropertyRelative("m_pauseTimeReq"), GUIContent.none);
        }
        else
        {
            EditorGUI.LabelField(pauseFieldPosition, "| enable to edit |");
        }

        //EditorGUI.PropertyField(contentPosition, property.FindPropertyRelative("m_pauseAtCurve"), new GUIContent("Pause?"));
        //EditorGUI.PropertyField(contentPosition, property.FindPropertyRelative("m_pauseTimeReq"), new GUIContent("PauseTime"));

        EditorGUI.EndProperty();

    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return 16f + 18f * 2f;
    }

}