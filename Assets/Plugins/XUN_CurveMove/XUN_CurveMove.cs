using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class EditorHandlesColor {
    public Color pathColor = Color.green;
    public Color inactivePathColor = Color.gray;
    public Color frustrumColor = Color.white;
    public Color handleColor = Color.yellow;
}

public enum SelectTag {
    None=-1,
    Several=0,
    All =1,
   
}

public class XUN_CurveMove : MonoBehaviour
{    
    public bool pause;

    public bool loop;

    public float speed=1;
    /// <summary>
    /// 使用距离
    /// </summary>
    public float useDistance=10;
    public float ratio = 1;
    public bool IsPlaying { get; private set; } = false;

    public SelectTag UsePostion {
        get {
            int use = 0;
            for (int i = 0; i < curves.Count; i++)
            {
                if (curves[i].usePostion)
                    use++;                
            }
            if (use == curves.Count)
                return SelectTag.All;
            else if (use == 0)
                return SelectTag.None;
            else
                return SelectTag.Several;
        }
        set {
            switch (value)
            {
                case SelectTag.None:
                    for (int i = 0; i < curves.Count; i++)
                    {
                        curves[i].usePostion = false;                            
                    }
                    break;             
                case SelectTag.All:
                    for (int i = 0; i < curves.Count; i++)
                    {
                        curves[i].usePostion = true;
                    }
                    break;
                default:
                    break;
            }
        }
    }
    public SelectTag UseRotation
    {
        get
        {
            int use = 0;
            for (int i = 0; i < curves.Count; i++)
            {
                if (curves[i].useRotation)
                    use++;
            }
            if (use == curves.Count)
                return SelectTag.All;
            else if (use == 0)
                return SelectTag.None;
            else
                return SelectTag.Several;
        }
        set
        {
            switch (value)
            {
                case SelectTag.None:
                    for (int i = 0; i < curves.Count; i++)
                    {
                        curves[i].useRotation = false;
                    }
                    break;
                case SelectTag.All:
                    for (int i = 0; i < curves.Count; i++)
                    {
                        curves[i].useRotation = true;
                    }
                    break;
                default:
                    break;
            }
        }
    }

    Coroutine curveRun;

    public delegate void BackCallDelegate(MoveCurve curve,Transform target);

    public EditorHandlesColor editorColor = new EditorHandlesColor();
    /// <summary>
    /// 移动对象 不可为this.transform
    /// </summary>
    [SerializeField]
    Transform moveTarget;
    public Transform MoveTarget {
        get {
            if (moveTarget == transform)
                moveTarget = null;
            return moveTarget;
        }
        set => moveTarget = value;
    }
    /// <summary>
    /// 关键点
    /// </summary>
    public List<MovePoint> points=new List<MovePoint>();
    /// <summary>
    /// 点间过程
    /// </summary>
    public List<MoveCurve> curves=new List<MoveCurve>();

    public int currentCurve;

    public float currentCurveTime;

    public float TotalTime {get{
            float time = 0;
            for (int i = 0; i < curves.Count; i++)
            {
                time += curves[i].Time;
            }
            return time;
        }
    }
    /// <summary> 
    /// 依此添加点 和 曲线
    /// </summary>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    /// <returns></returns>
    public MovePoint AddPoint(Vector3 position, Quaternion rotation) {
        points = points ?? new List<MovePoint>();
        points.Add(new MovePoint(this, position, rotation));
        if (points.Count > 1) {
            AddCurve();
        }
        return points[points.Count - 1];
    }

    public void RemovePoint(MovePoint point)
    {
        if (!points.Contains(point))
            return;
        int index = points.IndexOf(point);
        RemovePoint( index);
    }

    public void RemovePoint(int index)
    {
        if (points.Count <= index)
            return;
        for (int i = curves.Count - 1; i >= 0; i--)
        {
            if (curves[i].StartPoint == points[index] || curves[i].NextPoint == points[index])
            {
                curves.RemoveAt(i);
                continue;
            }
            if (curves[i].StartPointIndex > index)
            {
                curves[i].StartPoint = points[curves[i].StartPointIndex - 1];
            }
            if (curves[i].NexPointIndex > index)
            {
                curves[i].NextPoint = points[curves[i].NexPointIndex - 1];
            }
        }
        points.RemoveAt(index);
    }

    /// <summary>
    /// 单独添加曲线
    /// </summary>
    /// <returns></returns>
    public MoveCurve AddCurve() {
        curves = curves ?? new List<MoveCurve>();
        MoveCurve curve = new MoveCurve(this, points.Count - 2);
        curves.Add(curve);
        curve.Time = 1;
        curve.positionCurve = AnimationCurve.Linear(0, 0.01f, 1, 1f);
        curve.rotationCurve = AnimationCurve.Linear(0, 0.01f, 1, 1f);

        return curves[curves.Count - 1];
    }

    public IEnumerator MoveByPath(int start=0,int loopStart=0) {
        currentCurve = start;
        while (speed > 0 ? currentCurve <curves.Count:currentCurve>-1)
        {
            if (speed > 0)
                curves[currentCurve].startCall?.Invoke(curves[currentCurve],MoveTarget);

            currentCurveTime = speed > 0 ? 0: curves[currentCurve].Time;
            while (speed > 0 ? currentCurveTime <curves[currentCurve].Time:currentCurveTime>0)
            {
                if (!pause)
                {
                    currentCurveTime += Time.deltaTime * speed;
                    currentCurveTime = Mathf.Clamp(currentCurveTime, 0, curves[currentCurve].Time);
                    TargetMove();
                }
                yield return new WaitForFixedUpdate();
            }

            if (speed > 0)
                curves[currentCurve].overCall?.Invoke(curves[currentCurve], MoveTarget);

            currentCurve +=speed >0?1:-1;
            if (currentCurve == curves.Count && loop)
                currentCurve = loopStart;
            if (currentCurve == -1 && loop)
                currentCurve = curves.Count - 1;
        }
        IsPlaying = false;
        curveRun = null;
    }

    public void TargetMove()
    {
        if(curves[currentCurve].usePostion)
            MoveTarget.position = curves[currentCurve].GetBezierPosition(currentCurveTime);
        if (curves[currentCurve].useRotation)
            MoveTarget.rotation = curves[currentCurve].GetLerpRotation(currentCurveTime);
    }

    public void PlayCurve(int start = 0, int loopStart = 0)
    {
        if (IsPlaying) {
            StopCurve();
        }
        else
        {
            IsPlaying = true;
            curveRun= StartCoroutine(MoveByPath(start, loopStart));
        }
       
    }

    public void StopCurve() {
        if (curveRun == null)
            return;
        StopCoroutine(curveRun);
        curveRun = null;
        IsPlaying = false;
        pause = false;
        
    }

    public void SettingDistance(float distance ) {
        ratio = distance / useDistance;
        transform.localScale = Vector3.one * ratio;
    }
    private void OnDisable()
    {
        StopCurve();        
    }

#if UNITY_EDITOR
    public void OnDrawGizmos()
    {
        if (curves != null)
        {
            if (points.Count >= 2)
            {
                for (int i = 0; i < curves.Count; i++)
                {
                    var index = curves[i].StartPoint;
                    var indexNext = curves[i].NextPoint;
                    UnityEditor.Handles.DrawBezier(index.Position, indexNext.Position, index.Position + index.Handlenext,
                        indexNext.Position + indexNext.Handleprev, ((UnityEditor.Selection.activeGameObject == gameObject) ? editorColor.pathColor : editorColor.inactivePathColor), null, 5);
                }
            }

            for (int i = 0; i < points.Count; i++)
            {
                var index = points[i];
                Gizmos.matrix = Matrix4x4.TRS(index.Position, index.rotation, Vector3.one);
                Gizmos.color = editorColor.frustrumColor;
                Gizmos.DrawFrustum(Vector3.zero, 90f, 0.25f, 0.01f, 1.78f);
                Gizmos.matrix = Matrix4x4.identity;
            }
        } 



    }
    
   
#endif


    [Serializable]
public class MovePoint {
    public XUN_CurveMove curve;
    public Transform BaseTransform => curve.transform;

    /// <summary>
    /// 比例锁
    /// </summary>
    public RatioBool ratioBool;
    public bool handleChained=true;

    [SerializeField]
    public Vector3 position;
    public Vector3 Position { get => RatioVector3(position); set => position = RatioVector3Back(value); }

    [SerializeField]
    Vector3 handleprev;
    public Vector3 Handleprev { get => BaseTransform.TransformPoint(ScaleOfVector3(handleprev)) - BaseTransform.position; set => handleprev = BaseTransform.InverseTransformPoint(ScaleOfVector3Back(value )+BaseTransform.position); }
    [SerializeField]
    Vector3 handlenext;
    public Vector3 Handlenext { get => BaseTransform.TransformPoint(ScaleOfVector3(handlenext )) - BaseTransform.position; set => handlenext = BaseTransform.InverseTransformPoint(ScaleOfVector3Back(value )+ BaseTransform.position); }

    [SerializeField]
    public Quaternion rotation;
    public Quaternion Rotation { get => BaseTransform .rotation* rotation; set => rotation =value*Quaternion.Inverse(BaseTransform.rotation) /*Quaternion.RotateTowards(Rotation, value,360)*/; }

    public MovePoint(XUN_CurveMove curve, Vector3 position, Quaternion rotation) {
        this.curve = curve;
        this.position = position;
        Handleprev = Vector3.back;
        Handlenext = Vector3.forward;
        this.rotation = rotation;
    }
    private Vector3 RatioVector3(Vector3 vect)
    {
        if (ratioBool.world)
            return vect;
        else
            return BaseTransform.TransformPoint(ScaleOfVector3(vect));
    }
    private Vector3 ScaleOfVector3(Vector3 vect)
    {      
        return new Vector3(vect.x * (ratioBool.x ? 1 / BaseTransform.localScale.x : 1),
                           vect.y * (ratioBool.y ? 1 / BaseTransform.localScale.y : 1),
                           vect.z * (ratioBool.z ? 1 / BaseTransform.localScale.z : 1));
    }
    private Vector3 RatioVector3Back(Vector3 vect)
    {
        if (ratioBool.world)
            return vect;
        else
            return BaseTransform.InverseTransformPoint(ScaleOfVector3Back(vect));
    }
    private Vector3 ScaleOfVector3Back(Vector3 vect)
    {        
        return new Vector3(vect.x * (ratioBool.x ? BaseTransform.localScale.x : 1),
                                    vect.y * (ratioBool.y ? BaseTransform.localScale.y : 1),
                                    vect.z * (ratioBool.z ? BaseTransform.localScale.z : 1));
    }
}
[Serializable]
public struct RatioBool {
    public bool world;
    public bool x;
    public bool y;
    public bool z;
}

[Serializable ]
public class MoveCurve {
    [SerializeField]
    XUN_CurveMove curveMove;
    public float time;
    public float Time { get => time == 0 ? 0.001f :Mathf.Clamp( time *Mathf.Abs( curveMove.ratio),0.01f,float.MaxValue ); set => time = value; }
    [SerializeField]
    private int startPointIndex;
    public int StartPointIndex => startPointIndex;
    public MovePoint StartPoint {
        get => curveMove.points [startPointIndex];
        set => startPointIndex = curveMove.points.IndexOf( value);
    }
    [SerializeField]
    private int nexPointIndex;
    public int NexPointIndex=> nexPointIndex;

    public MovePoint NextPoint {
        get => curveMove.points [nexPointIndex];
        set => nexPointIndex = curveMove.points.IndexOf(value);
    }

    public bool usePostion=true;
    public AnimationCurve positionCurve;
    public bool useRotation=true;
    public AnimationCurve rotationCurve;

    public BackCallDelegate startCall;
    public BackCallDelegate overCall;

    public MoveCurve(XUN_CurveMove curve,int index) {
        this.curveMove = curve;
        startPointIndex = index;
        nexPointIndex =index>=curve.points.Count? 0:index + 1;
    }

    public Vector3 GetBezierPosition(float time)
    {
        float t = positionCurve.Evaluate(time/this.Time);        
        return
           Vector3.Lerp(
                Vector3.Lerp(
                    Vector3.Lerp(StartPoint.Position, StartPoint.Position  +StartPoint.Handlenext, t),
                    Vector3.Lerp(StartPoint.Position + StartPoint.Handlenext, NextPoint.Position + NextPoint.Handleprev, t),
                    t),
                Vector3.Lerp(
                    Vector3.Lerp(StartPoint.Position + StartPoint.Handlenext, NextPoint.Position + NextPoint.Handleprev, t),
                    Vector3.Lerp(NextPoint.Position + NextPoint.Handleprev, NextPoint.Position, t),
                    t),
                t);
    }
    
    public Quaternion GetLerpRotation( float time)
    {
        return Quaternion.LerpUnclamped(StartPoint.Rotation, NextPoint.Rotation, rotationCurve.Evaluate(time/this.Time));
    }
}

}