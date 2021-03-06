﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


public enum WalkerSpeedType { PercentPerSecond, TimeInterval };
public enum WalkerRotationType { Angle, Velocity, Target, None };
public enum RotationAxis { X, Y, Z };

[Serializable]
public class WalkerSplinePoint
{
    public float m_speedPerSegment;
    public bool m_newRotationTarget;
    public GameObject m_rotationTarget; 
    public float m_pauseTimeReq;
    public bool m_pauseAtCurve;
    

    public WalkerSplinePoint(float spd, bool newTgt, GameObject tgt, bool pause, float pauseTime)
    {
        m_speedPerSegment = spd;
        m_newRotationTarget = newTgt;
        m_rotationTarget = tgt;
        m_pauseAtCurve = pause;
        m_pauseTimeReq = pauseTime;
    }

    public WalkerSplinePoint()
    {

    }

}

public class SplineWalker : MonoBehaviour {

    public BezierSpline m_Spline;

    [SerializeField]
    public int m_size { get; private set; }

    //Walking states
    public bool m_walking { get; private set; }
    public bool m_paused { get; private set; }
    private float m_splinePos;
    [Range(0f, 1f)]
    public float m_initialSplinePos;

    //Walking Parameters
    public bool m_startWithSpace = false;
    public bool m_autoWalk = false;
    public bool m_autoReset = false;
    public bool m_destroyAtEnd;
    public float m_walkSpeed; //Percent per second

    //Custom Speeds
    public bool m_variableSpeed;
    public List<WalkerSplinePoint> m_WalkerSplinePoints = new List<WalkerSplinePoint>();
    public WalkerSpeedType m_speedType;
    
    //Scheduled Pause members
    private int m_lastCurve = -1;
    private bool m_pausingOnSchedule = false;
    private float m_scheduledPauseClock = 0f;
    private float m_scheduledPauseLimit = 0f;

    //CustomRotation
    public WalkerRotationType m_rotationType;
    public RotationAxis m_rotationAxis;
    public float m_angleOffset = 0f;
    public GameObject m_lookTarget;

    //Duplication
    public int m_duplicationIdx = 0;

    // Use this for initialization
    void Start () {
        ReadySpline();
	}

    public float SplinePos()
    {
        return m_splinePos;
    }

    public void SetSpline(BezierSpline spline, SimpleSplineParameters parameters)
    {
        AlignToNewSpline(spline);

        m_speedType = parameters.m_speedType;
        m_walkSpeed = parameters.m_walkSpeed;
        m_rotationType = parameters.m_rotationType;
        m_rotationAxis = parameters.m_rotationAxis;
        m_lookTarget = parameters.m_lookTarget;
        m_angleOffset = parameters.m_offsetAngle;
        m_autoWalk = parameters.m_autoWalk;
        m_autoReset = parameters.m_autoReset;
        m_destroyAtEnd = parameters.m_destroyAtEnd;

        ResetSpline();
    }

    public void AlignToNewSpline(BezierSpline spline)
    {
        //make walker spline points good
        m_Spline = spline;
        m_size = m_Spline.CurveCount;
        m_WalkerSplinePoints = new List<WalkerSplinePoint>();
        for (int ii = 0; ii < m_size; ii++)
        {
            m_WalkerSplinePoints.Add(new WalkerSplinePoint());
        }
    }

    public void ResizeToSpline()
    {
        m_WalkerSplinePoints.Resize(m_Spline.CurveCount);
    }

    public void DuplicatePoint(int originalPoint, int pointToAdjust)
    {
        //check point boundaries
        int lastCurve = m_Spline.CurveCount - 1;
        pointToAdjust = pointToAdjust > lastCurve ? 0 : pointToAdjust;
        pointToAdjust = pointToAdjust < 0 ? lastCurve : pointToAdjust;
        //Modify point
        m_WalkerSplinePoints[pointToAdjust] = m_WalkerSplinePoints[originalPoint];
         
    }

    public void DuplicateAllPoints(int originalPoint)
    {
        for (int ii = 0; ii < m_Spline.CurveCount; ii++)
        {
            DuplicatePoint(originalPoint, ii);
        }
    }

    public int CurrentCurve()
    {
        int currentCurve = m_Spline.CurveIDatPercentage(m_splinePos);
        return currentCurve;
    }

    // Update is called once per frame
    void FixedUpdate () {

        
        if (!m_walking)
        {
            if (m_startWithSpace && (Input.GetKey(KeyCode.Space)) || m_autoWalk) { StartWalking(); }
        }
        else if (!m_paused)
        {
            ManageWalk(Time.fixedDeltaTime);
        }
        else
        {
            ManagePause();
        }
        ManageReset();
        
	}

    private void ManageWalk(float timestep)
    {
        // check if on new curve
        int currentCurve = m_Spline.CurveIDatPercentage(m_splinePos);
        if (currentCurve > m_lastCurve)
        {
            if (m_WalkerSplinePoints[currentCurve].m_pauseAtCurve)
            {
                BeginScheduledPause(currentCurve);
            }
        }
        m_lastCurve = currentCurve;
        // pause at new curve if required
        if (!m_pausingOnSchedule)
        {
            if (m_variableSpeed)
            {
                WalkSplineAtVariableSpeed(timestep);
            }
            else
            {
                WalkSpline(timestep);
            }
        } 
        else
        {
            // increment scheduled pause counter
            if (m_scheduledPauseClock < m_scheduledPauseLimit)
            {
                m_scheduledPauseClock += timestep;
            } 
            else
            {
                EndScheduledPause();
            }
        }
    }

    //Scheduled Pause
    private void BeginScheduledPause(int currentCurve)
    {
        m_pausingOnSchedule = true;
        m_scheduledPauseClock = 0f;
        m_scheduledPauseLimit = m_WalkerSplinePoints[currentCurve].m_pauseTimeReq;
    }

    private void EndScheduledPause()
    {
        m_pausingOnSchedule = false;
        m_scheduledPauseClock = 0f;
        m_scheduledPauseLimit = 0f;
    }

    //Hard Pause
    private void ManagePause()
    {
        //if (Input.GetKeyDown(KeyCode.Space)) { PauseWalking(); }
    }

    private void ManageReset()
    {
        //if (Input.GetKeyDown(KeyCode.R)) { ResetSpline(); }
    }

    private void ResetSpline()
    {
        m_walking = false;
        m_paused = false;
        m_splinePos = 0f;
    }

    private void ReadySpline()
    {
        m_walking = false;
        m_paused = false;
        m_splinePos = m_initialSplinePos;
    }

    public void StartWalking()
    {
        m_walking = true;
    }

    public void PauseWalking()
    {
        m_paused = true;
    }

    public void ResumeWalking()
    {
        m_paused = false;
    }

    private void WalkSplineAtVariableSpeed(float deltaTime)
    {
        int currentCurveIndex = Mathf.Min(m_Spline.CurveIDatPercentage(m_splinePos),m_WalkerSplinePoints.Count-1);
        float currentSpeed = 0f;
        switch (m_speedType)
        {
            case WalkerSpeedType.PercentPerSecond:
                {
                    currentSpeed = m_WalkerSplinePoints[currentCurveIndex].m_speedPerSegment / 100f;
                    break;
                }
            case WalkerSpeedType.TimeInterval:
                {
                    float segmentPercentage = 1f / (m_Spline.CurveCount);
                    currentSpeed = segmentPercentage / m_WalkerSplinePoints[currentCurveIndex].m_speedPerSegment;
                    break;
                }
        }
        

        if (m_splinePos < 1.0f)
        {
            m_splinePos += deltaTime * currentSpeed;
            this.transform.position = m_Spline.GetPoint(m_splinePos, true);
            ManageWalkerAngle(currentCurveIndex);
        }
        else
        {
            if (m_destroyAtEnd)
            {
                Destroy(transform.gameObject);
            }
            else if (m_autoReset)
            {
                ResetSpline();
                StartWalking();
            }
        }
    }

    private void WalkSpline(float deltaTime)
    {
        float currentSpeed = 0f;
        switch (m_speedType)
        {
            case WalkerSpeedType.PercentPerSecond:
                {
                    currentSpeed = m_walkSpeed / 100f;
                    break;
                }
            case WalkerSpeedType.TimeInterval:
                {
                    currentSpeed = 1f / m_walkSpeed;
                    break;
                }
        }

        if (m_splinePos < 1.0f)
        {
            m_splinePos += deltaTime * currentSpeed;
            this.transform.position = m_Spline.GetPoint(m_splinePos, true);
            ManageWalkerAngle(0);
        } else
        {
            if (m_destroyAtEnd)
            {
                Destroy(transform.gameObject);
            }
            else if (m_autoReset)
            {
                ResetSpline();
                StartWalking();
            }
        }
        ManageWalkerAngle(0);
    }

    public void SetPosToInitial()
    {
        m_splinePos = m_initialSplinePos;
        this.transform.position = m_Spline.GetPoint(m_splinePos, true);
        int currentCurve = m_Spline.CurveIDatPercentage(m_splinePos);
        if (m_rotationType == WalkerRotationType.Target)
        {
            if (m_variableSpeed)
            {
                if (m_WalkerSplinePoints[currentCurve].m_rotationTarget != null)
                {
                    ManageWalkerAngle(currentCurve);
                }
            } else
            {
                if (m_lookTarget != null)
                {
                    ManageWalkerAngle(currentCurve);
                }
            }
        } else
        {
            ManageWalkerAngle(currentCurve);
        }        
    }

    private Vector3 CalculateNewEulerAngles()
    {
        //get current euler angles
        Vector3 oldEuler = this.transform.rotation.eulerAngles;
        Vector3 newEuler = oldEuler;

        //Get Velocity
        Vector3 splineVelocity = m_Spline.GetVelocity(m_splinePos, true);

        //Calculate Rotation
        switch (m_rotationAxis)
        {
            case RotationAxis.X:
                {
                    float newXangle = -Mathf.Rad2Deg * Mathf.Atan2(splineVelocity.y, splineVelocity.z);
                    //combine angles
                    newEuler.x = newXangle;

                    break;
                }
            case RotationAxis.Y:
                {
                    float newYangle = -Mathf.Rad2Deg * Mathf.Atan2(splineVelocity.z, splineVelocity.x);
                    //combine angles
                    newEuler.y = newYangle;

                    break;
                }
            case RotationAxis.Z:
                {
                    float newZangle = -Mathf.Rad2Deg * Mathf.Atan2(splineVelocity.y, splineVelocity.x);
                    //combine angles
                    newEuler.z = newZangle;

                    break;
                }

        }

        return newEuler;
    }

    private void ManageWalkerAngle(int currentCurveIndex)
    {
        switch (m_rotationType)
        {
            case WalkerRotationType.Velocity:
                {
                    this.transform.eulerAngles = CalculateNewEulerAngles();
                    break;
                }
            case WalkerRotationType.Angle:
                {
                    Vector3 newEuler = CalculateNewEulerAngles();

                    switch (m_rotationAxis)
                    {
                        case RotationAxis.X:
                            {
                                newEuler.x += m_angleOffset;
                                break;
                            }
                        case RotationAxis.Y:
                            {
                                newEuler.y += m_angleOffset;
                                break;
                            }
                        case RotationAxis.Z:
                            {
                                newEuler.z += m_angleOffset;
                                break;
                            }
                    }
                    
                    this.transform.eulerAngles = newEuler;
                    break;
                }
            case WalkerRotationType.Target:
                {
                    if (m_variableSpeed)
                    {
                        if (m_WalkerSplinePoints[currentCurveIndex].m_newRotationTarget)
                        {
                            transform.LookAt(m_WalkerSplinePoints[currentCurveIndex].m_rotationTarget.transform);
                            
                        }
                    } else
                    {
                        transform.LookAt(m_lookTarget.transform);
                    }
                    break;
                }
        }


    }

    //Helpers
    public float TotalTime()
    {
        float totalTime = 0f;
        totalTime = m_variableSpeed ? sumVariableSpeedTime() : sumConstantSpeedTime();
        return totalTime;
    }

    private float sumConstantSpeedTime()
    {
        float constantSpeedTime = 0f;
        switch (m_speedType)
        {
            case WalkerSpeedType.PercentPerSecond:
                {
                    constantSpeedTime = 100f / m_walkSpeed;
                    break;
                }
            case WalkerSpeedType.TimeInterval:
                {
                    constantSpeedTime = m_walkSpeed;
                    break;
                }
        }
        return constantSpeedTime;
    }

    private float sumVariableSpeedTime()
    {
        float variableSpeedTime = 0f;
        switch (m_speedType)
        {
            case WalkerSpeedType.PercentPerSecond:
                {
                    foreach(WalkerSplinePoint point in m_WalkerSplinePoints)
                    {
                        variableSpeedTime += (100f / m_Spline.CurveCount) / point.m_speedPerSegment;
                    }
                    break;
                }
            case WalkerSpeedType.TimeInterval:
                {
                    foreach (WalkerSplinePoint point in m_WalkerSplinePoints)
                    {
                        variableSpeedTime += point.m_speedPerSegment;
                    }
                    break;
                }
        }
        variableSpeedTime += sumPauseTime();
        return variableSpeedTime;
    }

    private float sumPauseTime()
    {
        float pauseTimeTotal = 0f;
        foreach(WalkerSplinePoint point in m_WalkerSplinePoints)
        {
            if (point.m_pauseAtCurve)
            {
                pauseTimeTotal += point.m_pauseTimeReq;
            }
        }
        return pauseTimeTotal;
    }
}


public static class ListExtra
{
    public static void Resize<T>(this List<T> list, int sz, T c)
    {
        int cur = list.Count;
        if (sz < cur)
            list.RemoveRange(sz, cur - sz);
        else if (sz > cur)
        {
            if (sz > list.Capacity)//this bit is purely an optimisation, to avoid multiple automatic capacity changes.
                list.Capacity = sz;
            list.AddRange(System.Linq.Enumerable.Repeat(c, sz - cur));
        }
    }
    public static void Resize<T>(this List<T> list, int sz) where T : new()
    {
        Resize(list, sz, new T());
    }
}