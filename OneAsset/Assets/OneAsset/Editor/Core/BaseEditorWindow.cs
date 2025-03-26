using System;
using UnityEditor;
using UnityEngine;

namespace OneAsset.Editor.Core
{
    public class BaseEditorWindow : EditorWindow
    {
        private class FakeClass
        {
        }

        private FakeClass _fakeClass;

        private void OnGUI()
        {
            if (_fakeClass == null)
            {
                _fakeClass = new FakeClass();
                OnInit();
            }

            EditorGUI.BeginDisabledGroup(EditorApplication.isCompiling);
            {
                OnUpdate();
            }
            EditorGUI.EndDisabledGroup();
        }

        protected virtual void OnInit()
        {
        }

        protected virtual void OnUpdate()
        {
        }
 
        protected void DrawHorizontalLine()
        {
            var rect = EditorGUILayout.GetControlRect(false, GUILayout.ExpandWidth(true), GUILayout.Height(1));
            EditorGUI.DrawRect(rect, Color.black);
        }

        protected void DrawVerticalLine()
        {
            var rect = EditorGUILayout.GetControlRect(false, GUILayout.ExpandHeight(true), GUILayout.Width(1));
            EditorGUI.DrawRect(rect, Color.black);
        }
    }
}