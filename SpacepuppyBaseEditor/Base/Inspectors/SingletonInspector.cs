﻿using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

using com.spacepuppy;
using com.spacepuppy.Utils;

namespace com.spacepuppyeditor.Base
{

    [CustomEditor(typeof(SingletonManager))]
    public class SingletonInspector : SPEditor
    {

        private static System.Type[] IgnoredSpecialSingletonTypes;
        static SingletonInspector()
        {
            var lst = new List<System.Type>();
            foreach(var tp in TypeUtil.GetTypesAssignableFrom(typeof(ISingleton)))
            {
                var attribs = tp.GetCustomAttributes(typeof(Singleton.ConfigAttribute), false) as Singleton.ConfigAttribute[];
                if(attribs != null && attribs.Length > 0)
                {
                    if (attribs[0].ExcludeFromSingletonManager) lst.Add(tp);
                }
            }
            IgnoredSpecialSingletonTypes = lst.ToArray();
        }

        protected override void OnSPInspectorGUI()
        {
            this.DrawDefaultInspector();

            //var selectedTypes = (from c in (this.target as SingletonManager).GetComponents<Singleton>() select c.GetType()).ToArray();
            //var selectedTypes = (from c in Singleton.AllSingletons select c.GetType()).ToArray();

            var singletonType = typeof(Singleton);
            var types = (from t in TypeUtil.GetTypesAssignableFrom(singletonType)
                         where t != singletonType &&
                         !IgnoredSpecialSingletonTypes.Contains(t) && 
                         SingletonCount(t) == 0 //!selectedTypes.Contains(t)
                         select t
                        ).ToArray();
            var typeNames = (from t in types select t.Name).ToArray();

            int index = EditorGUILayout.Popup("Add Singleton", -1, typeNames);
            if (index >= 0)
            {
                var tp = types[index];
                (this.target as SingletonManager).gameObject.AddComponent(tp);
                EditorUtility.SetDirty(this.target);
            }

            if(this.target is SingletonManager)
            {
                var arr = (this.target as SingletonManager).GetComponents<Singleton>();
                for (int i = 0; i < arr.Length; i ++ )
                {
                    if(arr[i] != this.target && arr[i].MaintainOnLoad)
                    {
                        (this.target as SingletonManager).MaintainOnLoad = true;
                        EditorUtility.SetDirty(this.target);
                        break;
                    }
                }
            }
        }

        internal static int SingletonCount(System.Type tp)
        {
            return GameObject.FindObjectsOfType(tp).Length;
        }

    }

    [CustomPropertyDrawer(typeof(Singleton.Maintainer))]
    public class SingletonMaintainerPropertyDrawer : PropertyDrawer
    {

        private string _message;
        private MessageType _messageType;


        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.serializedObject.isEditingMultipleObjects) return 0f;

            if (property.serializedObject.targetObject is ISingleton && SingletonInspector.SingletonCount(property.serializedObject.targetObject.GetType()) > 1)
            {
                _message = "Multiple Singletons of this type exist, you should purge the scene of duplicates!";
                _messageType = MessageType.Error;
                return EditorGUIUtility.singleLineHeight * 2f;
            }

            var go = GameObjectUtil.GetGameObjectFromSource(property.serializedObject.targetObject);
            if (object.ReferenceEquals(go, null))
            {
                _message = "This Singleton appears to not be attached to a GameObject.";
                _messageType = MessageType.Error;
                return EditorGUIUtility.singleLineHeight * 2f;
            }

            if (go.HasComponent<SingletonManager>())
            {
                property.FindPropertyRelative("_maintainOnLoad").boolValue = false;
                _message = "This Singleton is managed by a SingletonManager.";
                _messageType = MessageType.Info;
                return EditorGUIUtility.singleLineHeight * 2f;
            }
            else if (go.GetLikeComponents<ISingleton>().Count() > 1)
            {
                _message = "A GameObject with multiple Singletons on it should have a SingletonManager attached!";
                _messageType = MessageType.Warning;
                return EditorGUIUtility.singleLineHeight * 2f;
            }
            else
            {
                _message = null;
                _messageType = MessageType.None;
                return EditorGUIUtility.singleLineHeight;
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.serializedObject.isEditingMultipleObjects) return;

            if(_message != null)
            {
                EditorGUI.HelpBox(position, _message, _messageType);
            }
            else
            {
                EditorGUI.PropertyField(position, property.FindPropertyRelative("_maintainOnLoad"));
            }
        }

    }
}
