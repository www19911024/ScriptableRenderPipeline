using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class InfluenceVolumeUI
    {
        [Flags]
        public enum HandleType
        {
            None = 0,
            Base = 1,
            Influence = 1 << 1,
            InfluenceNormal = 1 << 2,

            All = ~0
        }

        public static void DrawGizmos(InfluenceVolumeUI s, InfluenceVolume d, Matrix4x4 matrix, HandleType editedHandle, HandleType showedHandle)
        {
            if ((showedHandle & HandleType.Base) != 0)
                DrawGizmos_BaseHandle(s, d, matrix, (editedHandle & HandleType.Base) != 0, k_GizmoThemeColorBase);

            if ((showedHandle & HandleType.Influence) != 0)
                DrawGizmos_FadeHandle(
                    s, d, matrix,
                    d.boxBlendOffset, d.boxBlendSize,
                    -d.sphereBlendDistance,
                    (editedHandle & HandleType.Influence) != 0,
                    k_GizmoThemeColorInfluence,
                    false);

            if ((showedHandle & HandleType.InfluenceNormal) != 0)
                DrawGizmos_FadeHandle(
                    s, d, matrix,
                    d.boxBlendNormalOffset, d.boxBlendNormalSize,
                    -d.sphereBlendNormalDistance,
                    (editedHandle & HandleType.InfluenceNormal) != 0,
                    k_GizmoThemeColorInfluenceNormal,
                    true);
        }

        static void DrawGizmos_BaseHandle(
            InfluenceVolumeUI s, InfluenceVolume d, Matrix4x4 matrix,
            bool isSolid, Color color)
        {
            using (new Handles.DrawingScope(color, matrix))
            {
                switch (d.shape)
                {
                    case InfluenceShape.Box:
                        {
                            s.boxBaseHandle.center = d.offset;
                            s.boxBaseHandle.size = d.boxSize;
                            s.boxBaseHandle.DrawHull(isSolid);
                            break;
                        }
                    case InfluenceShape.Sphere:
                        {
                            if (isSolid)
                                Gizmos.DrawSphere(d.offset, d.sphereRadius);
                            else
                                Gizmos.DrawWireSphere(d.offset, d.sphereRadius);
                            break;
                        }
                }
            }
        }

        static void DrawGizmos_FadeHandle(
            InfluenceVolumeUI s, InfluenceVolume d, Matrix4x4 matrix,
            Vector3 boxOffset, Vector3 boxSizeOffset,
            float sphereOffset,
            bool isSolid, Color color, bool isNormal)
        {
            using (new Handles.DrawingScope(color, matrix))
            {
                switch (d.shape)
                {
                    case InfluenceShape.Box:
                        {
                            HierarchicalBox refBox = isNormal ? s.boxInfluenceNormalHandle : s.boxInfluenceHandle;
                            refBox.center = d.offset + boxOffset;
                            refBox.size = d.boxSize + boxSizeOffset;
                            refBox.DrawHull(isSolid);
                            break;
                        }
                    case InfluenceShape.Sphere:
                        {
                            if (isSolid)
                                Gizmos.DrawSphere(d.offset, d.sphereRadius + sphereOffset);
                            else
                                Gizmos.DrawWireSphere(d.offset, d.sphereRadius + sphereOffset);
                            break;
                        }
                }
            }
        }
    }
}
