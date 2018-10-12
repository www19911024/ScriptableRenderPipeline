using System;
using UnityEditor.AnimatedValues;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class InfluenceVolumeUI : BaseUI<SerializedInfluenceVolume>
    {
        const int k_AnimBoolFields = 2;
        static readonly int k_ShapeCount = Enum.GetValues(typeof(InfluenceShape)).Length;

        public HierarchicalBox boxBaseHandle;
        public Gizmo6FacesBoxContained boxInfluenceHandle;
        public Gizmo6FacesBoxContained boxInfluenceNormalHandle;

        public SphereBoundsHandle sphereBaseHandle = new SphereBoundsHandle();
        public SphereBoundsHandle sphereInfluenceHandle = new SphereBoundsHandle();
        public SphereBoundsHandle sphereInfluenceNormalHandle = new SphereBoundsHandle();

        public AnimBool isSectionExpandedShape { get { return m_AnimBools[k_ShapeCount]; } }
        public bool showInfluenceHandles { get; set; }

        public InfluenceVolumeUI()
            : base(k_ShapeCount + k_AnimBoolFields)
        {
            isSectionExpandedShape.value = true;

            Color[] handleColors = new Color[]
            {
                HDReflectionProbeEditor.k_handlesColor[0][0],
                HDReflectionProbeEditor.k_handlesColor[0][1],
                HDReflectionProbeEditor.k_handlesColor[0][2],
                HDReflectionProbeEditor.k_handlesColor[1][0],
                HDReflectionProbeEditor.k_handlesColor[1][1],
                HDReflectionProbeEditor.k_handlesColor[1][2]
            };
            boxBaseHandle = new HierarchicalBox(HDReflectionProbeEditor.k_GizmoThemeColorExtent, handleColors);
            boxInfluenceHandle = new Gizmo6FacesBoxContained(boxBaseHandle, HDReflectionProbeEditor.k_GizmoThemeColorInfluenceBlend, handleColors);
            boxInfluenceNormalHandle = new Gizmo6FacesBoxContained(boxBaseHandle, HDReflectionProbeEditor.k_GizmoThemeColorInfluenceNormalBlend, handleColors);
        }

        public void SetIsSectionExpanded_Shape(InfluenceShape shape)
        {
            SetIsSectionExpanded_Shape((int)shape);
        }

        public void SetIsSectionExpanded_Shape(int shape)
        {
            for (var i = 0; i < k_ShapeCount; i++)
                m_AnimBools[i].target = shape == i;
        }

        public AnimBool IsSectionExpanded_Shape(InfluenceShape shapeType)
        {
            return m_AnimBools[(int)shapeType];
        }
    }
}
