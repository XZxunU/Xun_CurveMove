using System;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;

[CustomEditor(typeof(XUN_CurveMove))]
public class XUN_CurveMoveDraw : Editor
{
    XUN_CurveMove t;
    SerializedObject serializedObjectTarget;
    float CurveTime {
        get {
            float time = 0;
            for (int i = 1; i <= t.currentCurve; i++)
            {
                time += t.curves[i-1].Time;
            }
            time += t.currentCurveTime;
            return time;
        }
        set {
            float time = Mathf.Clamp(value,0, SumTime);
            int index = 0;
            for (int i = 0; i < t.curves.Count; i++)
            {
                if (time < t.curves[i].Time)
                {
                    t.currentCurveTime =Mathf.Clamp( time,0, t.curves[i].Time);
                    break;
                }
                
                time -= t.curves[i].Time;
                index++;
            }
            t.currentCurve = Mathf.Clamp(index,0, t.curves.Count-1);
        }
    }
    float SumTime { get {
            float sumTime = 0;
            for (int i = 0; i < t.curves.Count; i++)
            {
                sumTime += t.curves[i].Time;
            }
            return sumTime;
        }
    }
    float time;
    float time2;
    SelectTag usePosition;
    SelectTag useRotation;
    private ReorderableList pointReorderableList;
    private ReorderableList curvesReorderableList;

    private int selectedIndex = -1;

    private bool showAllPoint = false;
    bool color = false;
    #region Property
    SerializedProperty loopProperty;
    SerializedProperty speedProperty;
    SerializedProperty useDistanceProperty;
    SerializedProperty moveTargetProperty;
    SerializedProperty color_PathProperty;
    SerializedProperty color_InactiveProperty;
    SerializedProperty color_FrustumProperty;
    SerializedProperty color_HandleProperty;

    #endregion

    #region GUIContent
    private GUIContent chainedContent = new GUIContent("o───o", "曲线手柄对称");
    private GUIContent unchainedContent = new GUIContent("o— —o", "曲线手柄不对称");
    private GUIContent gotoPointContent = new GUIContent("跟踪", "跟踪到这个点");
    private GUIContent relocateContent = new GUIContent("读入", "将当前目标对象的位置结构读入");
    private GUIContent deletePointContent = new GUIContent("X", "删除");
    private GUIContent testButtonContent = new GUIContent("运行测试");
    private GUIContent pauseButtonContent = new GUIContent("暂停");
    private GUIContent continueButtonContent = new GUIContent("继续");
    private GUIContent stopButtonContent = new GUIContent("停止");
    #endregion


    private void OnEnable()
    {
        EditorApplication.update += Update;

       t = target as XUN_CurveMove;
        InitProperty();
        SetupReorderableList();
        time = PlayerPrefs.GetFloat("Curve_Time", 10); ;
    }
    void Update()
    {
        if (t == null) return;
        time = CurveTime;
        if (Math.Abs(time - time2) > 0.0001f)
        {
            Repaint();
            time2 = time;
        }       
    }
    void OnDisable()
    {
        EditorApplication.update -= Update;
    }

    private void InitProperty()
    {
        serializedObjectTarget = new SerializedObject(t);
        loopProperty = serializedObjectTarget.FindProperty("loop");
        useDistanceProperty= serializedObjectTarget.FindProperty("useDistance");
        speedProperty = serializedObjectTarget.FindProperty("speed");
        moveTargetProperty = serializedObjectTarget.FindProperty("moveTarget");
        color_PathProperty = serializedObjectTarget.FindProperty("editorColor.pathColor");
        color_InactiveProperty = serializedObjectTarget.FindProperty("editorColor.inactivePathColor");
        color_FrustumProperty = serializedObjectTarget.FindProperty("editorColor.frustrumColor");
        color_HandleProperty = serializedObjectTarget.FindProperty("editorColor.handleColor");
    }

    private void SetupReorderableList()
    {
        pointReorderableList = new ReorderableList(serializedObject, serializedObject.FindProperty("points"), true, true, false, false);
        curvesReorderableList = new ReorderableList(serializedObject, serializedObject.FindProperty("curves"), true, true, false, false);
        pointReorderableList.elementHeight *= 2f;        
        pointReorderableList.drawElementCallback = (rect, index, active, focused) =>
        {
            if (index > t.points.Count - 1) return;
            float startRectY = rect.y;
            rect.height -= 2;
            float fullWidth = rect.width ;
            rect.width = 40;
            fullWidth -= 60;
            rect.height /= 2;
            GUI.Label(rect, "#" + (index + 1));
            rect.y += rect.height - 3;
            rect.x -= 14;
            rect.width += 12;
            if (GUI.Button(rect, t.points[index].handleChained ? chainedContent : unchainedContent))
            {
                Undo.RecordObject(t, "Changed handleChained type");
                t.points[index].handleChained = !t.points[index].handleChained;
            }
            rect.x += rect.width +2;
            rect.y = startRectY;
            rect.width = 60;
            EditorGUI.BeginChangeCheck();
            {
                GUI.Label(rect, "世界坐标");
                float worldX = rect.x;
                rect.x += rect.width + 2;
                rect.width = fullWidth / 2;
                t.points[index].ratioBool.world = EditorGUI.Toggle(rect, t.points[index].ratioBool.world);

                GUI.enabled = !t.points[index].ratioBool.world&&t.MoveTarget!=null;
                rect.x = worldX;
                rect.y += rect.height;
                rect.width /= 6;

                GUI.Label(rect, "X");
                rect.x += rect.width;
                t.points[index].ratioBool.x = EditorGUI.Toggle(rect, t.points[index].ratioBool.x);
                rect.x += rect.width;
                GUI.Label(rect, "Y");
                rect.x += rect.width;
                t.points[index].ratioBool.y = EditorGUI.Toggle(rect, t.points[index].ratioBool.y);
                rect.x += rect.width;
                GUI.Label(rect, "Z");
                rect.x += rect.width;
                t.points[index].ratioBool.z = EditorGUI.Toggle(rect, t.points[index].ratioBool.z);
                GUI.enabled = true && t.MoveTarget != null;
                
            }
            if (EditorGUI.EndChangeCheck())
                {
                    SceneView.RepaintAll();

                }
            fullWidth -= rect.width * 6;
            rect.y = startRectY;
            rect.x += rect.width;
           rect.height *= 2;           
            rect.width = fullWidth;
            rect.height = rect.height / 2 - 1;
            if (GUI.Button(rect, gotoPointContent))
            {
                pointReorderableList.index = index;
                selectedIndex = index;
                SceneView.lastActiveSceneView.pivot = t.points[pointReorderableList.index].Position;
                SceneView.lastActiveSceneView.size = 3;
                SceneView.lastActiveSceneView.Repaint();
            }
            rect.y += rect.height + 2;
            if (GUI.Button(rect, relocateContent))
            {
                if (t.MoveTarget == null)
                    Debug.LogError("目标为NULL");
                else
                {
                    Undo.RecordObject(t, "Relocated waypoint");
                    pointReorderableList.index = index;
                    selectedIndex = index;
                    t.points[pointReorderableList.index].Position = t.MoveTarget.transform.position;
                    t.points[pointReorderableList.index].Rotation = t.MoveTarget.rotation ;
                    SceneView.lastActiveSceneView.Repaint();
                }
            }
            rect.height = (rect.height + 1) * 2;
            rect.y = startRectY;
            rect.x += rect.width + 2;
            rect.width = 20;

            if (GUI.Button(rect, deletePointContent))
            {
                Undo.RecordObject(t, "Deleted a waypoint");
                t.RemovePoint(index);
                SceneView.RepaintAll();
            }
        };
        pointReorderableList.drawHeaderCallback = rect =>{ GUI.Label(rect, "标识点总数：" + t.points.Count);};
        pointReorderableList.onSelectCallback = list => selectedIndex = list.index;

        curvesReorderableList.elementHeight *= 3f;
        curvesReorderableList.drawElementCallback = (rect, index, active, focused) =>
        {
            if (index > t.curves.Count - 1) return;
            float startRectY = rect.y;
            rect.height -= 2;
            float fullWidth = rect.width;
            rect.width = 60;
            fullWidth -= 60;
            rect.height /= 3;
            GUI.Label(rect, "#" + (index + 1));
            rect.y += rect.height;
            t.curves[index].StartPoint= t.points[EditorGUI.IntPopup(rect,t.points.IndexOf(t.curves[index].StartPoint), EnumByList(t.points,"起点"), IntByList(t.points))];
            rect.y += rect.height;
            t.curves[index].NextPoint = t.points[EditorGUI.IntPopup(rect, t.points.IndexOf(t.curves[index].NextPoint), EnumByList(t.points, "终点"), IntByList(t.points))];



            rect.y = startRectY;
            rect.x += rect.width+2;
            rect.width = fullWidth - 22;
            EditorGUI.BeginChangeCheck();
            float time=Mathf.Clamp( EditorGUI.FloatField(rect,"运行时间",t.curves[index].Time),0,float .MaxValue);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(t, "Changed time");
                t.curves[index].Time = time;
            }

            rect.width = 12;
            rect.y +=  rect.height;
            t.curves[index].usePostion = EditorGUI.Toggle(rect, t.curves[index].usePostion);
            GUI.enabled = t.curves[index].usePostion && t.MoveTarget != null;
            rect.x += 14;
            rect.width = (fullWidth-50) / 2;
           
            rect.height *= 2;

            EditorGUI.BeginChangeCheck();
            AnimationCurve posAC = EditorGUI.CurveField(rect, t.curves[index].positionCurve );
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(t, "Changed curve");
                t.curves[index].positionCurve = posAC;
            }
            GUI.enabled = true && t.MoveTarget != null;
            rect.x += rect.width+2;
            rect.width = 12;
            t.curves[index].useRotation = EditorGUI.Toggle(rect, t.curves[index].useRotation);
            GUI.enabled = t.curves[index].useRotation && t.MoveTarget != null;
            rect.x += rect.width + 2;
            rect .width = (fullWidth - 50) / 2;
            EditorGUI.BeginChangeCheck();
            AnimationCurve rotAC = EditorGUI.CurveField(rect, t.curves[index].rotationCurve);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(t, "Changed curve");
                t.curves[index].rotationCurve = rotAC;
            }
            GUI.enabled = true && t.MoveTarget != null;
            rect.x += rect.width + 2;
            rect.width = 16;
            rect.height *= 1.5f;
            rect.y = startRectY;
            if (GUI.Button(rect, deletePointContent)){
                Undo.RecordObject(t, "Deleted a curve");
                t.curves.Remove(t.curves[index]);
                SceneView.RepaintAll();
            }

        };
        curvesReorderableList.drawHeaderCallback = rect => {
            float fullWidth = rect.width;
            rect.width = 80;
            GUI.Label(rect, "总动画数: " + t.curves.Count);
            rect.x += rect.width;
            rect.width = 60;
            EditorGUI.BeginChangeCheck();
            usePosition = (SelectTag)EditorGUI.EnumPopup(rect ,t.UsePostion);
            if (EditorGUI.EndChangeCheck())
            {
                t.UsePostion = usePosition;
            }
            rect.x += rect.width+2;
            rect.width = (fullWidth - 220) /2;            
            GUI.Label(rect, "Position");
            rect.x += rect.width;
            rect.width = 60;
            EditorGUI.BeginChangeCheck();
            useRotation = (SelectTag)EditorGUI.EnumPopup(rect, t.UseRotation );
            if (EditorGUI.EndChangeCheck())
            {
                t.UseRotation = useRotation;
            }
            rect.x += rect.width;
            rect .width = (fullWidth - 180) / 2;
            GUI.Label(rect, "Rotation");
        };
       
    }
    string[] EnumByList<T>(List<T> list,string value) {
        if (list != null) {
            string[] strs = new string[list.Count];
            for (int i = 0; i < strs.Length ; i++)
            {
                strs[i] = value+(i+1).ToString();
            }
            return strs;
        }
        return new string[0];
    }
    int[] IntByList<T>(List<T> list)
    {
        if (list != null)
        {
            int[] strs = new int[list.Count];
            for (int i = 0; i < strs.Length; i++)
            {
                strs[i] = i ;
            }
            return strs;
        }
        return new int[0];
    }
    public override void OnInspectorGUI()
    {
        serializedObjectTarget.Update();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(!color ? "改变编辑颜色" : "关闭")) {
            color = !color;
        }
        if (color)
        {
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.PropertyField(color_PathProperty);
            EditorGUILayout.PropertyField(color_InactiveProperty);
            EditorGUILayout.PropertyField(color_FrustumProperty);
            EditorGUILayout.PropertyField(color_HandleProperty);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndHorizontal();

        TestWindow();
        EditorGUILayout.PropertyField(moveTargetProperty);
        EditorGUILayout.PropertyField(useDistanceProperty); 
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(loopProperty);
        EditorGUILayout.PropertyField(speedProperty); 
        EditorGUILayout.EndHorizontal();

       


        GUI.enabled = t.MoveTarget != null;

        pointReorderableList.DoLayoutList();
        if (GUILayout.Button("添加点"))
        {
            t.AddPoint(Vector3.zero, t.transform.rotation);
        }
        if (selectedIndex != -1) {
            DrawPointData(t.points[selectedIndex]);
        }



        curvesReorderableList.DoLayoutList();
        if (GUILayout.Button("添加运动曲线"))
        {
            if (t.points.Count < 2)
                Debug.LogError("缺少可用关键点！");
            else
                t.AddCurve();
        }
        if (GUILayout.Button(showAllPoint?"关闭":"编辑所有点")) {
            showAllPoint = !showAllPoint;            
        }
        if (showAllPoint) {
            for (int i = 0; i < t.points.Count ; i++)
            {
                int num = i;
                DrawPointData(t.points[num]);
            }
        }
        serializedObjectTarget.ApplyModifiedProperties();

    }

   
    private void OnSceneGUI()
    {
        if (t.points.Count > 1)
        {
            for (int i = 0; i < t.points.Count; i++)
            {
                int index = i;
                HandlesDraw(index);
                Handles.color = Color.white;
            }
        }
      
    }

    
    

    /// <summary>
    /// 测试窗口
    /// </summary>
    private void TestWindow()
    {
        GUI.enabled = Application.isPlaying&&!t.IsPlaying;
        GUILayout.BeginVertical("Box");
        GUILayout.BeginHorizontal();
        
        if (GUILayout.Button(testButtonContent))
        {
            t.PlayCurve();
        }
        GUI.enabled = Application.isPlaying;
        if (!t.pause)
        {
            if (Application.isPlaying && !t.IsPlaying) GUI.enabled = false;
            if (GUILayout.Button(pauseButtonContent))
            {
                t.pause=true;
            }
        }
        else if (GUILayout.Button(continueButtonContent))
        {
            t.pause = false;
        }

        if (GUILayout.Button(stopButtonContent))
        {
            t.StopCurve();
        }
        GUI.enabled = true;
        EditorGUI.BeginChangeCheck();
        GUILayout.Label("时间进度（秒）");
        time = EditorGUILayout.FloatField(time, GUILayout.MinWidth(5), GUILayout.MaxWidth(50));
        if (EditorGUI.EndChangeCheck())
        {           
            PlayerPrefs.SetFloat("Curve_Time", time);
            CurveTime = time;
            t.TargetMove();
        }
        GUILayout.EndHorizontal();
        GUI.enabled =true;
        EditorGUI.BeginChangeCheck();
        time = EditorGUILayout.Slider(time, 0, SumTime) ;
        if (EditorGUI.EndChangeCheck())
        {
            CurveTime = time;
            t.TargetMove();
        }
        GUI.enabled = false;
        Rect rr = GUILayoutUtility.GetRect(4, 8);
        float endWidth = rr.width-60;
        rr.y -= 4;
        rr.x += 4;
        int c = t.curves.Count;
        for (int i = 0; i < c; ++i)
        {
            rr.width = endWidth * t.curves[i].Time/SumTime;
            GUI.Box(rr, "");
            rr.x += rr.width;
        }
        GUILayout.EndVertical();
        GUI.enabled = true;
    }

   

    void HandlesDraw(int i)
    {
        HandleLinesDraw(i);
        Handles.color = t.editorColor.handleColor;
        NextHandleDraw(i);
        PrevHandleDraw(i);
        DrawWaypointHandles(i);      
        DrawSelectionHandles(i);
    }

    private void NextHandleDraw(int i)
    {
        if (i < t.points.Count  )
        {
            EditorGUI.BeginChangeCheck();
            Vector3 posNext = Vector3.zero;
            float size = HandleUtility.GetHandleSize(t.points[i].Position + t.points[i].Handlenext) * 0.11f;
            Handles.SphereHandleCap(0, t.points[i].Position + t.points[i].Handlenext, Quaternion.identity, size, EventType.Repaint);
            posNext = Handles.FreeMoveHandle(t.points[i].Position + t.points[i].Handlenext, Quaternion.identity, size, Vector3.zero, Handles.SphereHandleCap);

            if (selectedIndex == i&&(Tools.current == Tool.Move || Tools.current == Tool.Transform))
            {
                Handles.SphereHandleCap(0, t.points[i].Position + t.points[i].Handlenext, Quaternion.identity, size, EventType.Repaint);
                posNext = Handles.PositionHandle(t.points[i].Position + t.points[i].Handlenext, Quaternion.identity);
            }
            else if (Event.current.button != 1)
            {
                if (Handles.Button(t.points[i].Position + t.points[i].Handlenext, Quaternion.identity, size, size, Handles.SphereHandleCap))
                {
                    SelectIndex(i);
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Changed Handle Position");
                t.points[i].Handlenext = posNext - t.points[i].Position ;
                if (t.points[i].handleChained)
                    t.points[i].Handleprev = -1 * t.points[i].Handlenext;
            }
        }
    }
    private void PrevHandleDraw(int i)
    {
        if (i < t.points.Count)
        {
            EditorGUI.BeginChangeCheck();
            Vector3 posNext = Vector3.zero;
            float size = HandleUtility.GetHandleSize(t.points[i].Position + t.points[i].Handleprev) * 0.11f;
            Handles.SphereHandleCap(0, t.points[i].Position + t.points[i].Handleprev, Quaternion.identity, size, EventType.Repaint);
            posNext = Handles.FreeMoveHandle(t.points[i].Position + t.points[i].Handleprev, Quaternion.identity, size, Vector3.zero, Handles.SphereHandleCap);

            if (selectedIndex == i&& (Tools.current == Tool.Move || Tools.current == Tool.Transform))
            {
                Handles.SphereHandleCap(0, t.points[i].Position + t.points[i].Handleprev, Quaternion.identity, size, EventType.Repaint);
                posNext = Handles.PositionHandle(t.points[i].Position + t.points[i].Handleprev, Quaternion.identity);
            }
            else if (Event.current.button != 1)
            {
                if (Handles.Button(t.points[i].Position + t.points[i].Handleprev, Quaternion.identity, size, size, Handles.SphereHandleCap))
                {
                    SelectIndex(i);
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Changed Handle Position");
                t.points[i].Handleprev = posNext - t.points[i].Position;
                if (t.points[i].handleChained)
                    t.points[i].Handlenext = -1 * t.points[i].Handleprev;
            }
        }
    }

    void SelectIndex(int index)
    {
        selectedIndex = index;
        pointReorderableList.index = index;
        Repaint();
    }


    private void HandleLinesDraw(int i)
    {
        Handles.color = t.editorColor.handleColor;
       
        Handles.DrawLine(t.points[i].Position, t.points[i].Position+ t.points[i].Handlenext);
        
        Handles.DrawLine(t.points[i].Position, t.points[i].Position+t.points[i].Handleprev);
        Handles.color = Color.white;
    }

    void DrawWaypointHandles(int i)
    {
        if (Tools.current == Tool.Move || Tools.current == Tool.Transform)
        {
            EditorGUI.BeginChangeCheck();
            Vector3 pos = Vector3.zero;
            pos = Handles.FreeMoveHandle(t.points[i].Position , (Tools.pivotRotation == PivotRotation.Local) ? t.points[i].Rotation : Quaternion.identity, HandleUtility.GetHandleSize(t.points[i].Position) * 0.2f, Vector3.zero, Handles.RectangleHandleCap);
            if (selectedIndex == i)
            {
                pos = Handles.PositionHandle(t.points[i].Position, (Tools.pivotRotation == PivotRotation.Local) ? t.points[i].Rotation : Quaternion.identity);
            }
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Moved Waypoint");
                t.points[i].Position = pos;
            }
        }
        if (Tools.current == Tool.Rotate|| Tools.current==Tool.Transform)
        {
            EditorGUI.BeginChangeCheck();
            Quaternion rot = Quaternion.identity;
            rot = Handles.FreeRotateHandle(t.points[i].Rotation, t.points[i].Position, HandleUtility.GetHandleSize(t.points[i].Position) * 0.2f);

            if (selectedIndex == i)
            {
                rot = Handles.RotationHandle(t.points[i].Rotation, t.points[i].Position);
            }
           
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Rotated Waypoint");
                t.points[i].Rotation = rot;
            }
        }
    }

    void DrawSelectionHandles(int i)
    {
        if (Event.current.button != 1 && selectedIndex != i)
        {
            if ( Tools.current == Tool.Move|| Tools.current == Tool.Rotate|| Tools.current == Tool.Transform )
            {
                float size = HandleUtility.GetHandleSize(t.points[i].Position) * 0.2f;
                if (Handles.Button(t.points[i].Position, t.points[i].Rotation, size, size, Handles.CubeHandleCap))
                {
                    SelectIndex(i);
                }

            }
        }
    }


    public void DrawPointData(XUN_CurveMove.MovePoint point) {
        EditorGUI.BeginChangeCheck();
        GUILayout.BeginVertical("Box");
        GUILayout.Label("Point:#"+(t.points.IndexOf (point) +1));        
        Vector3 Position= EditorGUILayout.Vector3Field("Postion:",point .Position );
        Quaternion Rotation = Quaternion.Euler(EditorGUILayout.Vector3Field("Rotation", point.Rotation.eulerAngles));
        Vector3 Handlenext = EditorGUILayout.Vector3Field("Handlenext:", point.Handlenext);
        Vector3 Handleprev = EditorGUILayout.Vector3Field("Handleprev:", point.Handleprev);
        GUILayout.EndVertical();
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(target, "PointData");
            point.Position = Position;
            point.Rotation =Rotation;
            point.Handlenext = Handlenext ;
            point.Handleprev = Handleprev ;
            SceneView.lastActiveSceneView.Repaint();
        }
    }
}
