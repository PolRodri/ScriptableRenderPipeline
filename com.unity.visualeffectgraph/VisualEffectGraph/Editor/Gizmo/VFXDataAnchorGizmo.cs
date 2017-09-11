using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace UnityEditor.VFX.UI
{
    public class VFXDataAnchorGizmo
    {
        static Dictionary<System.Type, System.Action<VFXDataAnchorPresenter, VFXComponent>> s_DrawFunctions;

        static VFXDataAnchorGizmo()
        {
            s_DrawFunctions = new Dictionary<System.Type, System.Action<VFXDataAnchorPresenter, VFXComponent>>();

            s_DrawFunctions[typeof(Sphere)] = OnDrawSphereDataAnchorGizmo;
            s_DrawFunctions[typeof(Position)] = OnDrawPositionDataAnchorGizmo;
            s_DrawFunctions[typeof(AABox)] = OnDrawAABoxDataAnchorGizmo;
            s_DrawFunctions[typeof(OrientedBox)] = OnDrawOrientedBoxDataAnchorGizmo;
        }

        static internal void Draw(VFXDataAnchorPresenter anchor, VFXComponent component)
        {
            System.Action<VFXDataAnchorPresenter, VFXComponent> func;
            if (s_DrawFunctions.TryGetValue(anchor.anchorType, out func))
            {
                func(anchor, component);
            }
        }

        static bool PositionGizmo(VFXComponent component, CoordinateSpace space, ref Vector3 position)
        {
            EditorGUI.BeginChangeCheck();

            if (space == CoordinateSpace.Local)
            {
                position = component.transform.localToWorldMatrix.MultiplyPoint(position);
            }

            Vector3 modifiedPosition = Handles.PositionHandle(position, space == CoordinateSpace.Local ? component.transform.rotation : Quaternion.identity);
            if (space == CoordinateSpace.Local)
            {
                modifiedPosition = component.transform.worldToLocalMatrix.MultiplyPoint(modifiedPosition);
            }
            bool changed = GUI.changed;
            EditorGUI.EndChangeCheck();
            if (changed)
            {
                position = modifiedPosition;
                return true;
            }
            return false;
        }

        static void OnDrawPositionDataAnchorGizmo(VFXDataAnchorPresenter anchor, VFXComponent component)
        {
            Position pos = (Position)anchor.value;

            if (PositionGizmo(component, pos.space, ref pos.position))
            {
                anchor.value = pos;
            }
        }

        static void OnDrawSphereDataAnchorGizmo(VFXDataAnchorPresenter anchor, VFXComponent component)
        {
            Sphere sphere = (Sphere)anchor.value;

            Vector3 center = sphere.center;
            float radius = sphere.radius;
            if (sphere.space == CoordinateSpace.Local)
            {
                center = component.transform.localToWorldMatrix.MultiplyPoint(center);
            }

            Handles.DrawWireArc(center, Vector3.forward, Vector3.up, 360f, radius);
            Handles.DrawWireArc(center, Vector3.up, Vector3.right, 360f, radius);
            Handles.DrawWireArc(center, Vector3.right, Vector3.forward, 360f, radius);

            if (PositionGizmo(component, sphere.space, ref sphere.center))
            {
                anchor.value = sphere;
            }

            foreach (var dist in new Vector3[] { Vector3.left, Vector3.up, Vector3.forward })
            {
                EditorGUI.BeginChangeCheck();
                Vector3 sliderPos = center + dist * radius;
                Vector3 result = Handles.Slider(sliderPos, dist, 0.1f, Handles.CubeHandleCap, 0);

                if (GUI.changed)
                {
                    sphere.radius = (result - center).magnitude;

                    if (float.IsNaN(sphere.radius))
                    {
                        sphere.radius = 0;
                    }

                    anchor.value = sphere;
                }
                EditorGUI.EndChangeCheck();
            }
        }

        static void OnDrawAABoxDataAnchorGizmo(VFXDataAnchorPresenter anchor, VFXComponent component)
        {
            AABox box = (AABox)anchor.value;

            if (OnDrawBoxDataAnchorGizmo(anchor, component, box.space, ref box.center, ref box.size, Vector3.zero))
            {
                anchor.value = box;
            }
        }

        static void OnDrawOrientedBoxDataAnchorGizmo(VFXDataAnchorPresenter anchor, VFXComponent component)
        {
            OrientedBox box = (OrientedBox)anchor.value;

            if (OnDrawBoxDataAnchorGizmo(anchor, component, box.space, ref box.center, ref box.size, box.angles))
            {
                anchor.value = box;
            }
        }

        static bool OnDrawBoxDataAnchorGizmo(VFXDataAnchorPresenter anchor, VFXComponent component, CoordinateSpace space, ref Vector3 center, ref Vector3 size, Vector3 additionnalRotation)
        {
            Vector3 worldCenter = center;
            if (space == CoordinateSpace.Local)
            {
                worldCenter = component.transform.localToWorldMatrix.MultiplyPoint(center);
            }
            Vector3[] points = new Vector3[8];

            points[0] = center + new Vector3(size.x * 0.5f, size.y * 0.5f, size.z * 0.5f);
            points[1] = center + new Vector3(size.x * 0.5f, -size.y * 0.5f, size.z * 0.5f);

            points[2] = center + new Vector3(-size.x * 0.5f, size.y * 0.5f, size.z * 0.5f);
            points[3] = center + new Vector3(-size.x * 0.5f, -size.y * 0.5f, size.z * 0.5f);

            points[4] = center + new Vector3(size.x * 0.5f, size.y * 0.5f, -size.z * 0.5f);
            points[5] = center + new Vector3(size.x * 0.5f, -size.y * 0.5f, -size.z * 0.5f);

            points[6] = center + new Vector3(-size.x * 0.5f, size.y * 0.5f, -size.z * 0.5f);
            points[7] = center + new Vector3(-size.x * 0.5f, -size.y * 0.5f, -size.z * 0.5f);


            Matrix4x4 mat = Matrix4x4.identity;

            if (space == CoordinateSpace.Local)
            {
                mat = component.transform.localToWorldMatrix;
            }
            mat *= Matrix4x4.Rotate(Quaternion.Euler(additionnalRotation));

            for (int i = 0; i < points.Length; ++i)
            {
                points[i] = mat.MultiplyPoint(points[i]);
            }

            Handles.DrawLine(points[0], points[1]);
            Handles.DrawLine(points[2], points[3]);
            Handles.DrawLine(points[4], points[5]);
            Handles.DrawLine(points[6], points[7]);

            Handles.DrawLine(points[0], points[2]);
            Handles.DrawLine(points[0], points[4]);
            Handles.DrawLine(points[1], points[3]);
            Handles.DrawLine(points[1], points[5]);

            Handles.DrawLine(points[2], points[6]);
            Handles.DrawLine(points[3], points[7]);
            Handles.DrawLine(points[4], points[6]);
            Handles.DrawLine(points[5], points[7]);

            bool changed = false;

            EditorGUI.BeginChangeCheck();

            {
                // axis +Z
                Vector3 middle = (points[0] + points[1] + points[2] + points[3]) * 0.25f;
                Vector3 middleResult = Handles.Slider(middle, (middle - center), 0.1f, Handles.CubeHandleCap, 0);

                if (GUI.changed)
                {
                    size.z = (middleResult - worldCenter).magnitude * 2;
                    changed = true;
                }
            }
            EditorGUI.EndChangeCheck();
            EditorGUI.BeginChangeCheck();
            {
                // axis -Z
                Vector3 middle = (points[4] + points[5] + points[6] + points[7]) * 0.25f;
                Vector3 middleResult = Handles.Slider(middle, (middle - center), 0.1f, Handles.CubeHandleCap, 0);

                if (GUI.changed)
                {
                    size.z = (middleResult - worldCenter).magnitude * 2;
                    changed = true;
                }
            }
            EditorGUI.EndChangeCheck();
            EditorGUI.BeginChangeCheck();
            {
                // axis +X
                Vector3 middle = (points[0] + points[1] + points[4] + points[5]) * 0.25f;
                Vector3 middleResult = Handles.Slider(middle, (middle - center), 0.1f, Handles.CubeHandleCap, 0);

                if (GUI.changed)
                {
                    size.x = (middleResult - worldCenter).magnitude * 2;
                    changed = true;
                }
            }
            EditorGUI.EndChangeCheck();
            EditorGUI.BeginChangeCheck();
            {
                // axis -X
                Vector3 middle = (points[2] + points[3] + points[6] + points[7]) * 0.25f;
                Vector3 middleResult = Handles.Slider(middle, (middle - center), 0.1f, Handles.CubeHandleCap, 0);

                if (GUI.changed)
                {
                    size.x = (middleResult - worldCenter).magnitude * 2;
                    changed = true;
                }
            }
            EditorGUI.EndChangeCheck();
            EditorGUI.BeginChangeCheck();
            {
                // axis +Y
                Vector3 middle = (points[0] + points[2] + points[4] + points[6]) * 0.25f;
                Vector3 middleResult = Handles.Slider(middle, (middle - center), 0.1f, Handles.CubeHandleCap, 0);

                if (GUI.changed)
                {
                    size.y = (middleResult - worldCenter).magnitude * 2;
                    changed = true;
                }
            }
            EditorGUI.EndChangeCheck();
            EditorGUI.BeginChangeCheck();
            {
                // axis -Y
                Vector3 middle = (points[1] + points[3] + points[5] + points[7]) * 0.25f;
                Vector3 middleResult = Handles.Slider(middle, (middle - center), 0.1f, Handles.CubeHandleCap, 0);

                if (GUI.changed)
                {
                    size.y = (middleResult - worldCenter).magnitude * 2;
                    changed = true;
                }
            }
            EditorGUI.EndChangeCheck();


            if (PositionGizmo(component, space, ref center))
            {
                changed = true;
            }

            return changed;
        }
    }
}
