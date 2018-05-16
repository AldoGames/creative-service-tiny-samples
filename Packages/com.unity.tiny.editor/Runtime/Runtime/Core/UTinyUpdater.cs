#if NET_4_6
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine.Assertions;
using Unity.Tiny.Filters;

namespace Unity.Tiny
{
    public interface ITypeUpdater
    {
        void UpdateObject(UTinyObject @object);
        UTinyType.Reference UpdateReference(UTinyType.Reference reference);
    }

    public interface IFieldUpdater
    {
        object UpdateValue(object value);
    }

    public interface ISystemUpdater
    {
        UTinySystem.Reference UpdateReference(UTinySystem.Reference reference);
    }

    /// <summary>
    /// Helper class to manage data migration and eventually versioning
    /// </summary>
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    public static class UTinyUpdater
    {
        private static readonly Dictionary<UTinyId, ITypeUpdater> s_TypeUpdaters = new Dictionary<UTinyId, ITypeUpdater>();
        private static readonly Dictionary<UTinyId, ISystemUpdater> s_SystemUpdaters = new Dictionary<UTinyId, ISystemUpdater>();
        private static readonly Dictionary<UTinyId, IFieldUpdater> s_FieldUpdaters = new Dictionary<UTinyId, IFieldUpdater>();

        static UTinyUpdater()
        {
            // @TODO Move registration to an external class

            // Move into Math module
            RegisterTypeIdChange("UTiny.Core.Vector2f", "UTiny.Math.Vector2");
            RegisterTypeIdChange("UTiny.Core.Vector3f", "UTiny.Math.Vector3");
            RegisterTypeIdChange("UTiny.Core.Vector4f", "UTiny.Math.Vector4");
            RegisterTypeIdChange("UTiny.Core.Matrix3x3f", "UTiny.Math.Matrix3x3");
            RegisterTypeIdChange("UTiny.Core.Matrix4x4f", "UTiny.Math.Matrix4x4");
            RegisterTypeIdChange("UTiny.Core.Quaternionf", "UTiny.Math.Quaternion");
            RegisterTypeIdChange("UTiny.Core.Rectf", "UTiny.Math.Rect");
            RegisterTypeIdChange("UTiny.Core.RectInt", "UTiny.Math.RectInt");

            // moves into Core2D module
            RegisterTypeIdChange("UTiny.Core.DisplayOrientation", "UTiny.Core2D.DisplayOrientation");
            RegisterTypeIdChange("UTiny.Core.DisplayInfo", "UTiny.Core2D.DisplayInfo");
            RegisterTypeIdChange("UTiny.Core.MouseState", "UTiny.Core2D.MouseState");
            RegisterTypeIdChange("UTiny.Core.Camera2D", "UTiny.Core2D.Camera2D");
            RegisterTypeIdChange("UTiny.Core.Image2D", "UTiny.Core2D.Image2D");
            RegisterTypeIdChange("UTiny.Core.Sprite2D", "UTiny.Core2D.Sprite2D");
            RegisterTypeIdChange("UTiny.Core.Sprite2DRenderer", "UTiny.Core2D.Sprite2DRenderer");
            RegisterTypeIdChange("UTiny.Core.Transform", "UTiny.Core2D.Transform");

            // System renames
            RegisterSystemIdChange("UTiny.HTML.HTMLService.InputHandler", "UTiny.HTML.InputHandler");
            RegisterSystemIdChange("UTiny.HTML.HTMLService.Renderer", "UTiny.HTML.Renderer");

            // ColorRGBA is migrated to Color
            Register(UTinyId.Generate("UTiny.Core.ColorRGBA"), new ColorRGBAUpdater());
        }

        public static void RegisterTypeIdChange(string srcTypeFullName, string dstTypeFullName)
        {
            var name = dstTypeFullName.Substring(dstTypeFullName.LastIndexOf(".", StringComparison.Ordinal) + 1);
            var type = new UTinyType.Reference(UTinyId.Generate(dstTypeFullName), name);
            Register(UTinyId.Generate(srcTypeFullName), new TypeIdChange(type));
        }

        public static void RegisterSystemIdChange(string srcSystemFullName, string dstSystemFullName)
        {
            var name = dstSystemFullName.Substring(dstSystemFullName.LastIndexOf(".", StringComparison.Ordinal) + 1);
            var type = new UTinySystem.Reference(UTinyId.Generate(dstSystemFullName), name);
            Register(UTinyId.Generate(srcSystemFullName), new SystemIdChange(type));
        }

        public static void Register(UTinyId id, ITypeUpdater updater)
        {
            Assert.IsFalse(s_TypeUpdaters.ContainsKey(id));
            s_TypeUpdaters.Add(id, updater);
        }

        public static void Register(UTinyId id, ISystemUpdater updater)
        {
            Assert.IsFalse(s_SystemUpdaters.ContainsKey(id));
            s_SystemUpdaters.Add(id, updater);
        }

        public static void Register(UTinyId id, IFieldUpdater updater)
        {
            Assert.IsFalse(s_FieldUpdaters.ContainsKey(id));
            s_FieldUpdaters.Add(id, updater);
        }

        public static UTinyType.Reference UpdateReference(UTinyType.Reference reference)
        {
            UTinyId id;

            do
            {
                var updater = GetTypeUpdater(reference.Id);

                if (null == updater)
                {
                    break;
                }

                id = reference.Id;
                reference = updater.UpdateReference(reference);
            } while (!id.Equals(reference.Id));

            return reference;
        }

        public static UTinySystem.Reference UpdateReference(UTinySystem.Reference reference)
        {
            UTinyId id;

            do
            {
                var updater = GetSystemUpdater(reference.Id);

                if (null == updater)
                {
                    break;
                }

                id = reference.Id;
                reference = updater.UpdateReference(reference);
            } while (!id.Equals(reference.Id));

            return reference;
        }

        public static void UpdateProject(UTinyProject project)
        {
            var registry = project.Registry;
            if (project.SerializedVersion < 1)
            {
                foreach(var entity in project.Module.Dereference(registry).EntityGroups.Deref(registry).Entities())
                {
                    entity.Enabled = true;
                }
            }

            project.SerializedVersion = UTinyProject.CurrentSerializedVersion;
        }

        public static void UpdateObject(UTinyObject @object)
        {
            UTinyId id;

            do
            {
                var type = @object.Type;
                if (type.Equals(UTinyType.Reference.None))
                {
                    return;
                }

                var updater = GetTypeUpdater(type.Id);
                if (null == updater)
                {
                    return;
                }

                id = @object.Type.Id;
                updater.UpdateObject(@object);
            } while (!id.Equals(@object.Type.Id));
        }

        public static object UpdateField(UTinyId id, object value)
        {
            if (!s_FieldUpdaters.ContainsKey(id))
            {
                return value;
            }

            return s_FieldUpdaters[id].UpdateValue(value);
        }

        private static ITypeUpdater GetTypeUpdater(UTinyId id)
        {
            ITypeUpdater updater;
            return !s_TypeUpdaters.TryGetValue(id, out updater) ? null : updater;
        }

        private static ISystemUpdater GetSystemUpdater(UTinyId id)
        {
            ISystemUpdater updater;
            return !s_SystemUpdaters.TryGetValue(id, out updater) ? null : updater;
        }
    }

    /// <summary>
    /// Simple class to handle migrating a system id
    /// </summary>
    public class SystemIdChange : ISystemUpdater
    {
        private readonly UTinySystem.Reference m_System;

        public SystemIdChange(UTinySystem.Reference system)
        {
            m_System = system;
        }

        public UTinySystem.Reference UpdateReference(UTinySystem.Reference reference)
        {
            return m_System;
        }
    }

    /// <summary>
    /// Simple class to handle migrating a type id
    /// </summary>
    public class TypeIdChange : ITypeUpdater
    {
        private readonly UTinyType.Reference m_Type;

        public TypeIdChange(UTinyType.Reference type)
        {
            m_Type = type;
        }

        public UTinyType.Reference UpdateReference(UTinyType.Reference reference)
        {
            return m_Type;
        }

        public void UpdateObject(UTinyObject @object)
        {
            @object.Type = UpdateReference(@object.Type);

            // no migration to perform
        }
    }

    /// <summary>
    /// Migrates ColorRGBA to Color
    /// </summary>
    public class ColorRGBAUpdater : ITypeUpdater
    {
        private static readonly UTinyType.Reference s_ColorType = new UTinyType.Reference(UTinyId.Generate("UTiny.Core2D.Color"), "Color");

        public UTinyType.Reference UpdateReference(UTinyType.Reference reference)
        {
            return s_ColorType;
        }

        public void UpdateObject(UTinyObject @object)
        {
            // `ColorRGBA` was deprecated and replace with `Color`
            @object.Type = s_ColorType;

            // Convert each property from byte to float
            ConvertPropertyFromByteToFloat(@object, "r");
            ConvertPropertyFromByteToFloat(@object, "g");
            ConvertPropertyFromByteToFloat(@object, "b");
            ConvertPropertyFromByteToFloat(@object, "a");
        }

        private static void ConvertPropertyFromByteToFloat(UTinyObject @object, string property)
        {
            if (@object.HasProperty(property))
            {
                var obj = @object[property];
                var value = 1f;

                var convertible = obj as IConvertible;
                if (null != convertible)
                {
                    value = convertible.ToSingle(CultureInfo.CurrentCulture);
                }

                @object[property] = value / 255;
            }
        }
    }
}
#endif // NET_4_6
