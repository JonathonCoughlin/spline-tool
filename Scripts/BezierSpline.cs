
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class BezierSpline : MonoBehaviour {

    [SerializeField]
    private Vector3[] m_points;

    [SerializeField]
    private BezierControlPointMode[] modes;

    [SerializeField]
    private bool amLoop;

    public bool Loop
    {
        get { return amLoop; }
        set
        {
            amLoop = value;
            if (value == true)
            {
                modes[modes.Length - 1] = modes[0];
                SetControlPoint(0, m_points[0]);
            }
        }
    }

    public void Reset()
    {
        m_points = new Vector3[]
        {
            new Vector3(2f,0f,-2f),
            new Vector3(3f,0f,-2f),
            new Vector3(3f,0f,0f),
            new Vector3(4f,0f,0f)
        };

        modes = new BezierControlPointMode[]
        {
            BezierControlPointMode.Free,
            BezierControlPointMode.Free
        };
    }

    public BezierControlPointMode GetControlPointMode(int index)
    {
        return modes[(index + 1) / 3];
    }

    public List<Vector3> GetFullSplineList(int splineResolution, bool pointsInWorldSpace)
    {
        List<Vector3> fullResolutionSpline = new List<Vector3>();
        if (splineResolution > 0)
        {
            //Step through resolution
            for (int resIdx = 0; resIdx <= splineResolution; resIdx++)
            {
                float splineTravelPosition = resIdx / (float)splineResolution;
                fullResolutionSpline.Add(GetPoint(splineTravelPosition,pointsInWorldSpace));
            }
        }

        return fullResolutionSpline;
    }

    public void SetControlPointMode(int index, BezierControlPointMode mode)
    {
        int modeIndex = (index + 1) / 3;
        modes[(index + 1) / 3] = mode;
        if (amLoop)
        {
            if (modeIndex == 0)
            {
                modes[modes.Length - 1] = mode;
            }
            else if (modeIndex == modes.Length - 1)
            {
                modes[0] = mode;
            }
        }
        EnforceMode(index);
    }

    private void EnforceMode(int index)
    {
        // modeIndex: the index of the 3 coupled points relating to the changed point
        int modeIndex = (index + 1) / 3;
        BezierControlPointMode mode = modes[modeIndex];
        if (mode == BezierControlPointMode.Free || !amLoop && (modeIndex == 0 || modeIndex == modes.Length - 1))
        {
            return;
        }
        // modeIndex: index of 3 coupled points
        // fixedIndex: 1st point - spline curve control
        // middleIndex: 2nd point - spline waypoint
        // enforcedIndex: 3rd point - spline curve control
        int middleIndex = modeIndex * 3;
        int fixedIndex, enforcedIndex;
        if (index <= middleIndex)
        {
            fixedIndex = middleIndex - 1;
            if (fixedIndex < 0) //loop from first point to last point
            {
                fixedIndex = m_points.Length - 2;
            }
            enforcedIndex = middleIndex + 1;
            if (enforcedIndex >= m_points.Length) //loop from last point to first
            {
                enforcedIndex = 1;
            }
        }
        else
        {
            fixedIndex = middleIndex + 1;
            if (fixedIndex >= m_points.Length) // loop to front
            {
                fixedIndex = 1;
            }
            enforcedIndex = middleIndex - 1;
            if (enforcedIndex < 0) // loop to back
            {
                enforcedIndex = m_points.Length - 2;
            }
        }

        Vector3 middle = m_points[middleIndex];
        Vector3 enforcedTangent = middle - m_points[fixedIndex];
        if (mode == BezierControlPointMode.Aligned)
        {
            enforcedTangent = enforcedTangent.normalized * Vector3.Distance(middle, m_points[enforcedIndex]);
        }
        m_points[enforcedIndex] = middle + enforcedTangent;
        
    }

    public int ControlPointCount
    {
        get
        {
            return m_points.Length;
        }
    }

    public Vector3 GetControlPoint (int index)
    {
        return m_points[index];
    }

    public void SetControlPoint (int index, Vector3 point)
    {
        if (index % 3 == 0)
        {
            Vector3 delta = point - m_points[index];
            if (amLoop)
            {
                if (index == 0)
                {
                    m_points[1] += delta;
                    m_points[m_points.Length - 2] += delta;
                    m_points[m_points.Length - 1] = point;
                }
                else if (index == m_points.Length - 1)
                {
                    m_points[0] = point;
                    m_points[1] += delta;
                    m_points[index - 1] += delta;
                }
                else
                {
                    m_points[index - 1] += delta;
                    m_points[index + 1] += delta;
                }
            }
            else
            {
                //Non-loop code
                if (index > 0)
                {
                    m_points[index - 1] += delta;
                }
                if (index + 1 < m_points.Length)
                {
                    m_points[index + 1] += delta;
                }
            }
        }

        m_points[index] = point;
        EnforceMode(index);
    }

    public void AddCurve ()
    {
        Vector3 point = m_points[m_points.Length - 1];
        Array.Resize(ref m_points, m_points.Length + 3);
        point.x += 1f;
        m_points[m_points.Length - 3] = point;
        point.x += 1f;
        m_points[m_points.Length - 2] = point;
        point.x += 1f;
        m_points[m_points.Length - 1] = point;

        Array.Resize(ref modes, modes.Length + 1);
        modes[modes.Length - 1] = modes[modes.Length - 2];

        EnforceMode(m_points.Length - 4);

        if (amLoop)
        {
            m_points[m_points.Length - 1] = m_points[0];
            modes[modes.Length - 1] = modes[0];
            EnforceMode(0);
        }
    }

    public void AddCurveAtPosition(float makePointAtPercent)
    {
        //mark curves surrounding new location
        int[] boundingIndices = BoundaryIndicesAtPercentage(makePointAtPercent);
        int previousCurveIndex = boundingIndices[0];
        int nextCurveIndex = boundingIndices[1];
        int modeIndex = boundingIndices[2];

        //store position/direction at point of new curve
        Vector3 curvePosition = GetPoint(makePointAtPercent, false);
        Vector3 curveDirection = GetVelocity(makePointAtPercent, false);

        //calculate points for new curve
        Vector3 rearAngle = curvePosition - curveDirection.normalized;
        Vector3 controlPoint = curvePosition;
        Vector3 forwardAngle = curvePosition + curveDirection.normalized;

        //add space to array for new curve
        int addedCurveIndex = previousCurveIndex;
        Array.Resize(ref m_points, m_points.Length + 3);
        Array.Resize(ref modes, modes.Length + 1);

        //shift rear curves, modes to end of arrays
        ShiftCurvesRight(nextCurveIndex);
        ShiftModesRight(modeIndex);

        //assign new point, mode values
        m_points[previousCurveIndex + 1] = rearAngle;
        m_points[previousCurveIndex + 2] = controlPoint;
        m_points[previousCurveIndex + 3] = forwardAngle;
        modes[modeIndex] = BezierControlPointMode.Mirrored;
        EnforceMode(modeIndex);

    }

    private int[] BoundaryIndicesAtPercentage(float splinePercentage)
    {
        int[] indices = new int[3];
        int previousIdx = 1; 
        int modeIdx = 1; // at a minimum, second point must be replaced
        int followingIdx = 2;

        for (int ii = 0; ii <= CurveCount; ii++)
        {
            float currentPercent = ii / (float)(CurveCount);
            modeIdx = ii; // new point will replace last anchor before breaking
            if (splinePercentage < currentPercent)
            {
                break;
            }
            previousIdx = ii * 3 + 1; // anchor point +1 = rear angle point
        }
        followingIdx = previousIdx + 1; // rear angle point + 1 = forward angle point
        indices[0] = previousIdx;
        indices[1] = followingIdx;
        indices[2] = modeIdx;
        return indices;
    }

    private void ShiftCurvesRight(int firstPointIdx)
    {
        for (int ii = m_points.Length-1; ii >= (firstPointIdx + 3); ii-=1)
        {
            m_points[ii] = m_points[ii - 3];
        }
    }

    private void ShiftModesRight(int modeIdx)
    {
        for (int ii = modes.Length-1; ii >= modeIdx; ii-=1)
        {
            modes[ii] = modes[ii - 1];
        }
    }

    private void EnforceLoop()
    {
        if (Loop)
        {
            Loop = true; // the set function of loop will make the final point equal to the first - just recall it
        }
    }

    public int CurveCount
    {
        get
        {
            return (m_points.Length - 1) / 3;
        }
    }

    public int AnchorCount
    {
        get
        {
            return CurveCount + 1;
        }
    }

    public void RemoveAnchor(int anchorIdx)
    {
        int[] badPoints;
        if (anchorIdx == 0)
        {
            badPoints = new int[] { 0, 1, 2 };
        }
        else if (anchorIdx == AnchorCount - 1)
        {
            badPoints = new int[] { m_points.Length - 3, m_points.Length - 2, m_points.Length - 1 };
        }
        else
        {
            int anchorPointIdx = anchorIdx * 3;
            badPoints = new int[] { anchorIdx - 1, anchorIdx, anchorIdx + 1};
        }
        RemovePoints(badPoints);
        RemoveMode(anchorIdx);
        EnforceLoop();
    }

    private void RemovePoints(int[] badPoints)
    {
        Vector3[] tempPoints = new Vector3[m_points.Length - 3];

        //fill temp array with useful points
        int tempArrayStep = 0;
        for (int ii = 0; ii < m_points.Length; ii++)
        {
            bool removeThisPoint = false;
            for (int jj = 0; jj < badPoints.Length; jj++)
            {
                if (ii == badPoints[jj])
                {
                    removeThisPoint = true;
                    break;
                }
            }
            if (!removeThisPoint)
            {
                tempPoints[tempArrayStep] = m_points[ii];
                tempArrayStep++;
            }
        }
        m_points = tempPoints;
    }

    private void RemoveMode(int badModeIdx)
    {
        BezierControlPointMode[] tempModes = new BezierControlPointMode[modes.Length - 1];
        int tempModeStep = 0;
        for (int ii = 0; ii < modes.Length; ii++)
        {
            if (ii != badModeIdx)
            {
                tempModes[tempModeStep] = modes[ii];
                tempModeStep++;
            }
        }
        modes = tempModes;
    }

    public Vector3 GetPoint(float t, bool pointInWorldSpace)
    {
        int ii;
        if (t >= 1f)
        {
            t = 1f;
            ii = m_points.Length - 4;
        }
        else
        {
            t = Mathf.Clamp01(t) * CurveCount;
            ii = (int)t;
            t -= ii;
            ii *= 3;
        }

        Vector3 point;

        if (pointInWorldSpace)
        {
            point = transform.TransformPoint(BezHelper.GetBezPoint(
            m_points[ii], m_points[ii + 1], m_points[ii + 2], m_points[ii + 3], t));
        }
        else
        {
            point = BezHelper.GetBezPoint(
            m_points[ii], m_points[ii + 1], m_points[ii + 2], m_points[ii + 3], t);
        }

        return point;
    }

    public Vector3 GetVelocity(float t, bool transformToWorldSpace)
    {
        int ii;
        if (t >= 1f)
        {
            t = 1f;
            ii = m_points.Length - 4;
        }
        else
        {
            t = Mathf.Clamp01(t) * CurveCount;
            ii = (int)t;
            t -= ii;
            ii *= 3;
        }

        Vector3 outputVelocity;
        if (transformToWorldSpace)
        {
            outputVelocity = transform.TransformPoint(BezHelper.GetFirstDerivative(
            m_points[ii], m_points[ii + 1], m_points[ii + 2], m_points[ii + 3], t))
            - transform.position;
        }
        else
        {
            outputVelocity = BezHelper.GetFirstDerivative(
            m_points[ii], m_points[ii + 1], m_points[ii + 2], m_points[ii + 3], t) 
            - transform.position;
        }
        return outputVelocity;
    }

    public Vector3 GetDirection(float t)
    {
        return GetVelocity(t,true).normalized;
    }


}

#region Helper Functions

public static class BezHelper
{
    public static Vector3 GetBezPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        t = Mathf.Clamp01(t);
        float oneMinusT = 1f - t;
        return
            oneMinusT * oneMinusT * oneMinusT * p0 +
            3f * oneMinusT * oneMinusT * t * p1 +
            3f * oneMinusT * t * t * p2 +
            t * t * t * p3;
    }

    public static Vector3 GetFirstDerivative(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        t = Mathf.Clamp01(t);
        float oneMinusT = 1f - t;
        return
            3f * oneMinusT * oneMinusT * (p1 - p0) +
            6f * oneMinusT * t * (p2 - p1) +
            3f * t * t * (p3 - p2);
    }
}
#endregion

