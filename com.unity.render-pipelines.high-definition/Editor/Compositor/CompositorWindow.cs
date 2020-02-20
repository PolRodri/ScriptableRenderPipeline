using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.HighDefinition.Compositor;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition.Compositor
{
    internal class CompositorWindow : EditorWindow
    {
        static partial class TextUI
        {
            static public readonly GUIContent EnableCompositor = EditorGUIUtility.TrTextContent("Enable Compositor", "Enabled the compositor and creates a default composition profile.");
            static public readonly GUIContent RemoveCompositor = EditorGUIUtility.TrTextContent("Remove compositor from scene", "Removes the compositor and any composition settings from the scene.");
        }

        static CompositorWindow s_Window;
        CompositionManagerEditor m_Editor;
        Vector2 m_ScrollPosition = Vector2.zero;
        bool m_RequiresRedraw = false;

        [MenuItem("Window/Render Pipeline/HD Render Pipeline Compositor", false, 10400)]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            s_Window = (CompositorWindow)EditorWindow.GetWindow(typeof(CompositorWindow));
            s_Window.titleContent = new GUIContent("HDRP Compositor (Preview)");
            s_Window.Show();
        }

        void Update()
        {
            // This ensures that layer thumbnails are updated every frame (for video layers)
            Repaint();
        }

        void OnGUI()
        {
            CompositionManager compositor = CompositionManager.GetInstance();
            bool enableCompositor = false;
            if (compositor)
            {
                enableCompositor = compositor.enabled;
            }
            enableCompositor = EditorGUILayout.Toggle(TextUI.EnableCompositor, enableCompositor);

            if (compositor == null && enableCompositor)
            {
                Debug.Log("The scene does not have a compositor. Creating a new one with the default configuration.");
                GameObject go = new GameObject("HDRP Compositor") { hideFlags = HideFlags.HideInHierarchy };
                compositor = go.AddComponent<CompositionManager>();

                // Mark as dirty, so if the user closes the scene right away, the change will be saved.
                EditorUtility.SetDirty(compositor);

                // Now add the default configuration
                CompositionUtils.LoadDefaultCompositionGraph(compositor);
                CompositionUtils.LoadOrCreateCompositionProfileAsset(compositor);
                CompositionUtils.SetDefaultCamera(compositor);
            }

            if (compositor)
            {
                compositor.enabled = enableCompositor;
            }
            else
            {
                return;
            }

            if (compositor && compositor.enabled == false)
            {
                if (GUILayout.Button(new GUIContent("Remove compositor from scene")))
                {
                    if (compositor.outputCamera)
                    {
                        if(compositor.outputCamera.name == CompositionUtils.k_DefaultCameraName)
                        {
                            var cameraData = compositor.outputCamera.GetComponent<HDAdditionalCameraData>();
                            if (cameraData != null)
                            {
                                CoreUtils.Destroy(cameraData);
                            }
                            CoreUtils.Destroy(compositor.outputCamera.gameObject);
                            CoreUtils.Destroy(compositor.outputCamera);
                        }
                    }
                    
                    CoreUtils.Destroy(compositor);
                    return;
                }
            }

            if (compositor.profile == null)
            {
                // The compositor was loaded, but there was no profile (someone deleted the asset from disk?), so create a new one
                CompositionUtils.LoadOrCreateCompositionProfileAsset(compositor);
                compositor.SetupCompositionMaterial();
                return;
            }

            if (compositor.shader != null)
            {
                // keep track of shader graph changes: when the user saves a graph, we should load/reflect any new shader properties
                GraphData.onSaveGraph += MarkShaderAsDirty;
            }

            if (m_Editor == null || m_Editor.target == null || m_Editor.isDirty || m_RequiresRedraw)
            {
                m_Editor = (CompositionManagerEditor)Editor.CreateEditor(compositor);
                m_RequiresRedraw = false;
            }

            m_ScrollPosition = GUILayout.BeginScrollView(m_ScrollPosition);
            using (new EditorGUI.DisabledScope(compositor.enabled == false))
            {
                if (m_Editor)
                {
                    m_Editor.OnInspectorGUI();
                }
            }
            GUILayout.EndScrollView();
        }

        void MarkShaderAsDirty(Shader shader, object context)
        {
            CompositionManager compositor = CompositionManager.GetInstance();
            compositor.shaderPropertiesAreDirty = true;
            m_RequiresRedraw = true;

            EditorUtility.SetDirty(compositor);
            EditorUtility.SetDirty(compositor.profile);
        }

        private void OnDestroy()
        {
            CompositionManager compositor = CompositionManager.GetInstance();
            if (compositor.shader != null)
            {
                GraphData.onSaveGraph -= MarkShaderAsDirty;
            }
        }
    }
}
#endif
