#if NET_4_6
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    public static class GameViewUtility
    {
        #region static
        private static readonly MethodInfo s_AddCustomSizeMethod;
        private static readonly MethodInfo s_RemoveCustomSizeMethod;
        private static readonly MethodInfo s_GetDisplayTestMethod;
        private static readonly ConstructorInfo s_GameViewSizeConstructor;
        private static readonly PropertyInfo s_SelectedSizeIndexProp;
        private static readonly Type s_GameViewType;
        private static readonly object s_StandaloneGroup;

        private static readonly bool s_ConfiguredProperly;
        private static bool s_Changed;
        private static int s_SelectedSizeIndex;

        private const string KTinyDisplay = UTinyConstants.ApplicationName;
        private const string KFreeAspect = "Free Aspect";

        static GameViewUtility()
        {
            // Verify that we can find everything we are looking for and cache it.
            // If we didn't get everything, we disable the feature altogether.
            try
            {
                var editorAssembly = typeof(Editor).Assembly;
                s_GameViewType = editorAssembly.GetType("UnityEditor.GameView");
                var sizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
                var gameViewSizeType = editorAssembly.GetType("UnityEditor.GameViewSizeType");
                
                var getGroupMethod = sizesType.GetMethod("GetGroup");
                s_AddCustomSizeMethod = getGroupMethod.ReturnType.GetMethod("AddCustomSize");
                s_RemoveCustomSizeMethod = getGroupMethod.ReturnType.GetMethod("RemoveCustomSize");
                var gameViewSizesInstance = typeof(ScriptableSingleton<>)
                                                .MakeGenericType(sizesType)
                                                .GetProperty("instance")
                                               ?.GetValue(null, null);



                s_StandaloneGroup = getGroupMethod.Invoke(gameViewSizesInstance, new object[] { (int)GameViewSizeGroupType.Standalone });
                var gameViewSize = editorAssembly.GetType("UnityEditor.GameViewSize");
                s_GameViewSizeConstructor = gameViewSize.GetConstructor(new Type[] { gameViewSizeType, typeof(int), typeof(int), typeof(string) });
                s_SelectedSizeIndexProp = s_GameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                s_GetDisplayTestMethod = s_StandaloneGroup.GetType()
                                                          .GetMethod("GetDisplayTexts");

                s_ConfiguredProperly = true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                ShowErrorMessage();
            }
            
            EditorApplication.update += Update;
        }
        #endregion

        #region API
        public static void SetSize(int width, int height)
        {
            if (!s_ConfiguredProperly)
            {
                ShowErrorMessage();
                return;
            }

            RemoveUtinySize();

            if (width <= 0)
            {
                width = 1;
            }

            if (height <= 0)
            {
                height = 1;
            }

            if (width < height && (width / (float) height) < 0.01)
            {
                width = (int) (height * 0.01);
            }
            else if (height < width && (height / (float) width) < 0.01)
            {
                height = (int) (width * 0.01);
            }
            
            AddCustomSize(width, height, KTinyDisplay);
            SetSize(KTinyDisplay);
        }

        public static void SetFreeAspect()
        {
            if (!s_ConfiguredProperly)
            {
                ShowErrorMessage();
                return;
            }
            
            RemoveUtinySize();
            SetSize(KFreeAspect);
        }
        #endregion

        #region Implementation
        private static void Update()
        {
            if (UTinyEditorApplication.EditorContext?.ContextType == EditorContextType.None)
            {
                return;
            }
            
            // Any time the user focuses the GameView window. Apply the size change
            if (EditorWindow.focusedWindow?.GetType() == s_GameViewType)
            {
                if (s_Changed)
                {
                    SetSize(s_SelectedSizeIndex);
                }
            }
            else
            {
                s_Changed = true;
            }
        }
        
        private static bool IsWindowOpen()
        {
            var windows = Resources.FindObjectsOfTypeAll(s_GameViewType);
            return windows.Length > 0;
        }

        private static void RemoveUtinySize()
        {
            RemoveCustomSize(KTinyDisplay);
        }

        private static void SetSize(string name)
        {
            SetSize(FindSize(name));
        }
        
        private static void SetSize(int index)
        {
            s_SelectedSizeIndex = index;
            
            if (!IsWindowOpen())
            {
                return;
            }
            
            var focusedWindow = EditorWindow.focusedWindow;
            var window = EditorWindow.GetWindow(s_GameViewType);
            s_SelectedSizeIndexProp.SetValue(window, index, null);
            focusedWindow?.Focus();
            s_Changed = false;
        }

        private static void AddCustomSize(int width, int height, string text)
        {
            var newSize = s_GameViewSizeConstructor.Invoke(new object[] { 0, width, height, text });
            s_AddCustomSizeMethod.Invoke(s_StandaloneGroup, new [] { newSize });
        }

        private static void RemoveCustomSize(string text)
        {
            var index = FindSize(text);
            if (index == -1)
            {
                return;
            }

            s_RemoveCustomSizeMethod.Invoke(s_StandaloneGroup, new object[] { index });
        }

        private static int FindSize(string text)
        {
            var displayTexts = s_GetDisplayTestMethod.Invoke(s_StandaloneGroup, null) as string[];
            for (var i = 0; i < displayTexts.Length; i++)
            {
                var display = displayTexts[i];
                var pren = display.IndexOf('(');
                if (pren != -1)
                    display = display.Substring(0, pren - 1);
                if (display == text)
                    return i;
            }
            return -1;
        }

        private static void ShowErrorMessage()
        {
            Debug.Log($"{UTinyConstants.ApplicationName}: GameViewUtility has not been configured properly. {UTinyConstants.ApplicationName} cannot resize the GameView");
        }
        #endregion
    }
}
#endif // NET_4_6
