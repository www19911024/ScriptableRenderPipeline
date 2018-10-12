using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Linq.Expressions;

namespace UnityEngine.Experimental.Rendering
{
    public class HierarchicalBox
    {
        const float k_HandleSizeCoef = 0.05f;

        enum NamedFace { Right, Top, Front, Left, Bottom, Back, None }

        readonly Mesh m_Face;
        readonly Material m_Material;
        readonly Color[] m_PolychromeHandleColor;
        readonly Color m_MonochromeHandleColor;

        readonly HierarchicalBox m_container;

        private bool m_MonoHandle = true;
        public bool monoHandle
        {
            get
            {
                return m_MonoHandle;
            }
            set
            {
                m_MonoHandle = value;
            }
        }

        private int[] m_ControlIDs = new int[6] { 0, 0, 0, 0, 0, 0 };

        public Vector3 center { get; set; }

        public Vector3 size { get; set; }

        Func<int, Vector3, Vector3, Color, Vector3> LinearSlider;

        static HierarchicalBox()
        {
            var DotCapHandle = new Handles.CapFunction(Handles.DotHandleCap);
            Type SnapSettings = Type.GetType("UnityEditor.SnapSettings, UnityEditor");
            Type Slider1D = Type.GetType("UnityEditorInternal.Slider1D, UnityEditor");
            var controlIDParam = Expression.Parameter(typeof(int), "controlID");
            var positionParam = Expression.Parameter(typeof(Vector3), "handlePosition");
            var orientationParam = Expression.Parameter(typeof(Vector3), "handleOrientation");
            var colorParam = Expression.Parameter(typeof(Color), "color");
            var snapScaleVarialble = Expression.Variable(typeof(float), "snapScale");
            var refPositionVariable = Expression.Variable(typeof(float), "refPosition");
            var colorVariable = Expression.Variable(typeof(Color), "previousColor");
            var colorParam = Expression.Parameter(typeof(Color), "color");
            var scaleInfo = SnapSettings.GetProperty("scale");
            var getHandleSizeInfo = SnapSettings.GetMethod("GetHandleSize", BindingFlags.Static | BindingFlags.Public, null, CallingConventions.Any, new[] { typeof(Vector3) }, null);
            var slider1DInfo = Slider1D.GetMethod("Do", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public, null, CallingConventions.Any, new[] { typeof(int), typeof(Vector3), typeof(Vector3), typeof(float), typeof(Handles.CapFunction), typeof(float) }, null);
            var capFunctionInfo = typeof(Handles).GetMethod("CapFunction", BindingFlags.Static | BindingFlags.Public, null, CallingConventions.Any, new[] { typeof(Handles.CapFunction) }, null);
            var linearSliderBlock = Expression.Block(
                new[] { snapScaleVarialble, refPositionVariable, computedSizeVariable, colorVariable },
                Expression.Assign(refPositionVariable, positionParam),
                Expression.Assign(snapScaleVarialble, Expression.Call(scaleInfo.GetGetMethod())),
                Expression.Assign(computedSizeVariable, Expression.Multiply(Expression.Call(getHandleSizeInfo, positionParam), Expression.Constant(k_HandleSizeCoef))),
                Expression.Call(slider1DInfo, controlIDParam, refPositionVariable, orientationParam, computedSizeVariable, Expression.Constant(DotCapHandle.Target), snapScaleVarialble),
                refPositionVariable
                );
            var linearSliderLambda = Expression.Lambda<Func<int, Vector3, Vector3, Color, Vector3>>(linearSliderBlock, controlIDParam, positionParam, orientationParam, colorParam);
            BeginVerticalSplit = beginHorizontalSplitLambda.Compile();
        }

        //Note: Handles.Slider not allow to use a specific ControlID.
        //Thus Slider1D is used (with reflection)
        static PropertyInfo k_scale = Type.GetType("UnityEditor.SnapSettings, UnityEditor").GetProperty("scale");
        static Type k_Slider1D = Type.GetType("UnityEditorInternal.Slider1D, UnityEditor");
        static MethodInfo k_Slider1D_Do = k_Slider1D
                .GetMethod(
                    "Do",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
                    null,
                    CallingConventions.Any,
                    new[] { typeof(int), typeof(Vector3), typeof(Vector3), typeof(float), typeof(Handles.CapFunction), typeof(float) },
                    null);
        static void Slider1D(int controlID, ref Vector3 handlePosition, Vector3 handleOrientation, float snapScale, Color color)
        {
            using (new Handles.DrawingScope(color))
            {
                handlePosition = (Vector3)k_Slider1D_Do.Invoke(null, new object[]
                    {
                        controlID,
                        handlePosition,
                        handleOrientation,
                        HandleUtility.GetHandleSize(handlePosition) * k_HandleSizeCoef,
                        new Handles.CapFunction(Handles.DotHandleCap),
                        snapScale
                    });
            }
        }

        public HierarchicalBox(Color faceColor, Color[] polychromeHandleColors = null, HierarchicalBox container = null)
        {
            m_container = container;
            m_Material = new Material(Shader.Find("Hidden/UnlitTransparentColored"));
            m_Material.color = faceColor.gamma;
            m_Face = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            if(polychromeHandleColors != null && polychromeHandleColors.Length != 6)
            {
                throw new System.ArgumentException("polychromeHandleColors must be null or have a size of 6.");
            }
            m_PolychromeHandleColor = polychromeHandleColors ?? new Color[]
            {
                new Color(1f, 0f, 0f, 1f),
                new Color(0f, 1f, 0f, 1f),
                new Color(0f, 0f, 1f, 1f),
                new Color(1f, 0f, 0f, 1f),
                new Color(0f, 1f, 0f, 1f),
                new Color(0f, 0f, 1f, 1f)
            };
            faceColor.a = 0.7f;
            m_MonochromeHandleColor = faceColor;
        }

        Color GetHandleColor(NamedFace name)
        {
            return monoHandle ? m_MonochromeHandleColor : m_PolychromeHandleColor[(int)name];
        }

        public void DrawHull(bool selected)
        {
            if (selected)
            {
                Vector3 xSize = new Vector3(size.z, size.y, 1f);
                m_Material.SetPass(0);
                Graphics.DrawMeshNow(m_Face, Handles.matrix * Matrix4x4.TRS(center + size.x * .5f * Vector3.left, Quaternion.FromToRotation(Vector3.forward, Vector3.left), xSize));
                Graphics.DrawMeshNow(m_Face, Handles.matrix * Matrix4x4.TRS(center + size.x * .5f * Vector3.right, Quaternion.FromToRotation(Vector3.forward, Vector3.right), xSize));
                
                Vector3 ySize = new Vector3(size.x, size.z, 1f);
                Graphics.DrawMeshNow(m_Face, Handles.matrix * Matrix4x4.TRS(center + size.y * .5f * Vector3.up, Quaternion.FromToRotation(Vector3.forward, Vector3.up), ySize));
                Graphics.DrawMeshNow(m_Face, Handles.matrix * Matrix4x4.TRS(center + size.y * .5f * Vector3.down, Quaternion.FromToRotation(Vector3.forward, Vector3.down), ySize));

                Vector3 zSize = new Vector3(size.x, size.y, 1f);
                Graphics.DrawMeshNow(m_Face, Handles.matrix * Matrix4x4.TRS(center + size.z * .5f * Vector3.forward, Quaternion.identity, zSize));
                Graphics.DrawMeshNow(m_Face, Handles.matrix * Matrix4x4.TRS(center + size.z * .5f * Vector3.back, Quaternion.FromToRotation(Vector3.forward, Vector3.back), zSize));

                //if contained, also draw handle distance to container here
                if (m_container != null)
                {
                    Vector3 centerDiff = center - m_container.center;
                    Vector3 xRecal = centerDiff;
                    Vector3 yRecal = centerDiff;
                    Vector3 zRecal = centerDiff;
                    xRecal.x = 0;
                    yRecal.y = 0;
                    zRecal.z = 0;

                    Color previousColor = Handles.color;
                    Handles.color = m_container.GetHandleColor(NamedFace.Left);
                    Debug.Log(Handles.color);
                    Handles.DrawLine(m_container.center + xRecal + m_container.size.x * .5f * Vector3.left, center + size.x * .5f * Vector3.left);

                    Handles.color = m_container.GetHandleColor(NamedFace.Right);
                    Handles.DrawLine(m_container.center + xRecal + m_container.size.x * .5f * Vector3.right, center + size.x * .5f * Vector3.right);

                    Handles.color = m_container.GetHandleColor(NamedFace.Top);
                    Handles.DrawLine(m_container.center + yRecal + m_container.size.y * .5f * Vector3.up, center + size.y * .5f * Vector3.up);

                    Handles.color = m_container.GetHandleColor(NamedFace.Bottom);
                    Handles.DrawLine(m_container.center + yRecal + m_container.size.y * .5f * Vector3.down, center + size.y * .5f * Vector3.down);

                    Handles.color = m_container.GetHandleColor(NamedFace.Front);
                    Handles.DrawLine(m_container.center + zRecal + m_container.size.z * .5f * Vector3.forward, center + size.z * .5f * Vector3.forward);

                    Handles.color = m_container.GetHandleColor(NamedFace.Back);
                    Handles.DrawLine(m_container.center + zRecal + m_container.size.z * .5f * Vector3.back, center + size.z * .5f * Vector3.back);

                    Handles.color = previousColor;
                }
            }

            Handles.DrawWireCube(center, size);
        }

        public void DrawHandle()
        {
            for (int i = 0, count = m_ControlIDs.Length; i < count; ++i)
                m_ControlIDs[i] = GUIUtility.GetControlID(GetHashCode(), FocusType.Passive);

            EditorGUI.BeginChangeCheck();

            Vector3 leftPosition = center + size.x * .5f * Vector3.left;
            Vector3 rightPosition = center + size.x * .5f * Vector3.right;
            Vector3 topPosition = center + size.y * .5f * Vector3.up;
            Vector3 bottomPosition = center + size.y * .5f * Vector3.down;
            Vector3 frontPosition = center + size.z * .5f * Vector3.forward;
            Vector3 backPosition = center + size.z * .5f * Vector3.back;

            float snapScale = (float)k_scale.GetValue(null, null);
            NamedFace theChangedFace = NamedFace.None;

            EditorGUI.BeginChangeCheck();
            SliderLineHandle()
            Slider1D(m_ControlIDs[(int)NamedFace.Left], ref leftPosition, Vector3.left, snapScale, GetHandleColor(NamedFace.Left));
            if (EditorGUI.EndChangeCheck() && monoHandle)
                theChangedFace = NamedFace.Left;

            EditorGUI.BeginChangeCheck();
            Slider1D(m_ControlIDs[(int)NamedFace.Right], ref rightPosition, Vector3.right, snapScale, GetHandleColor(NamedFace.Right));
            if (EditorGUI.EndChangeCheck() && monoHandle)
                theChangedFace = NamedFace.Right;

            EditorGUI.BeginChangeCheck();
            Slider1D(m_ControlIDs[(int)NamedFace.Top], ref topPosition, Vector3.up, snapScale, GetHandleColor(NamedFace.Top));
            if (EditorGUI.EndChangeCheck() && monoHandle)
                theChangedFace = NamedFace.Top;

            EditorGUI.BeginChangeCheck();
            Slider1D(m_ControlIDs[(int)NamedFace.Bottom], ref bottomPosition, Vector3.down, snapScale, GetHandleColor(NamedFace.Bottom));
            if (EditorGUI.EndChangeCheck() && monoHandle)
                theChangedFace = NamedFace.Bottom;

            EditorGUI.BeginChangeCheck();
            Slider1D(m_ControlIDs[(int)NamedFace.Front], ref frontPosition, Vector3.forward, snapScale, GetHandleColor(NamedFace.Front));
            if (EditorGUI.EndChangeCheck() && monoHandle)
                theChangedFace = NamedFace.Front;

            EditorGUI.BeginChangeCheck();
            Slider1D(m_ControlIDs[(int)NamedFace.Back], ref backPosition, Vector3.back, snapScale, GetHandleColor(NamedFace.Back));
            if (EditorGUI.EndChangeCheck() && monoHandle)
                theChangedFace = NamedFace.Back;

            if (EditorGUI.EndChangeCheck())
            {
                if (monoHandle)
                {
                    float decal = 0f;
                    switch (theChangedFace)
                    {
                        case NamedFace.Left:
                            decal = (leftPosition - center - size.x * .5f * Vector3.left).x;
                            break;
                        case NamedFace.Right:
                            decal = -(rightPosition - center - size.x * .5f * Vector3.right).x;
                            break;
                        case NamedFace.Top:
                            decal = -(topPosition - center - size.y * .5f * Vector3.up).y;
                            break;
                        case NamedFace.Bottom:
                            decal = (bottomPosition - center - size.y * .5f * Vector3.down).y;
                            break;
                        case NamedFace.Front:
                            decal = -(frontPosition - center - size.z * .5f * Vector3.forward).z;
                            break;
                        case NamedFace.Back:
                            decal = (backPosition - center - size.z * .5f * Vector3.back).z;
                            break;
                    }

                    Vector3 tempSize = size - Vector3.one * decal;
                    for (int axis = 0; axis < 3; ++axis)
                    {
                        if (tempSize[axis] < 0)
                        {
                            decal += tempSize[axis];
                            tempSize = size - Vector3.one * decal;
                        }
                    }

                    size = tempSize;
                }
                else
                {
                    Vector3 max = new Vector3(rightPosition.x, topPosition.y, frontPosition.z);
                    Vector3 min = new Vector3(leftPosition.x, bottomPosition.y, backPosition.z);

                    //ensure that the box face are still facing outside
                    for (int axis = 0; axis < 3; ++axis)
                    {
                        if (min[axis] > max[axis])
                        {
                            if (GUIUtility.hotControl == m_ControlIDs[axis])
                            {
                                max[axis] = min[axis];
                            }
                            else
                            {
                                min[axis] = max[axis];
                            }
                        }
                    }

                    center = (max + min) * .5f;
                    size = max - min;
                }
            }
        }
    }
}
