using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(SplineWalker))]
[System.Serializable]
public class SplineWalkerEditor : Editor {

    private SplineWalker m_Walker;


    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        GUILayout.Label("Woah, there.");

        m_Walker = target as SplineWalker;

        int splineCurves = m_Walker.m_Spline.CurveCount;
        if (m_Walker.m_size != splineCurves)
        {
            m_Walker.SizeWalker(splineCurves);
        }
        




    }

    private void OnSceneGUI()
    {
        
        
    }

}
