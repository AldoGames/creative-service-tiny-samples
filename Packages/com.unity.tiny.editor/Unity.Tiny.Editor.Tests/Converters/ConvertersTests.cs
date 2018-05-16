#if NET_4_6
using System;

using UnityEngine;
using NUnit.Framework;
using Unity.Tiny.Conversions;
using Unity.Tiny.Extensions;

namespace Unity.Tiny.Test
{
    public class ConvertersTests
    {
        IRegistry m_Registry;

        [OneTimeSetUp]
        public void Setup()
        {
            m_Registry = new UTinyRegistry();
            UTinyPersistence.LoadAllModules(m_Registry);
        }

        [Test]
        public void CannotConvertWithUnregisteredType()
        {
            var tinyMatrix = MakeObject(m_Registry.GetMatrix4x4Type());
            var identity = Matrix4x4.identity;
            Assert.Throws<NullReferenceException>(() => tinyMatrix.As<Matrix4x4>());
        }

        [Test]
        public void CannotConvertToBuiltinTypeFromUTinyObject()
        {
            var tinyVec2 = MakeObject(m_Registry.GetVector2Type());
            Assert.Throws<InvalidOperationException>(() => tinyVec2.As<float>());
            Assert.Throws<InvalidOperationException>(() => tinyVec2.AssignFrom(5.0f));
        }

        [Test]
        public void CannotUseAssignFromIfNotUTinyObject()
        {
            var value = 5.0f;
            Assert.Throws<InvalidOperationException>(() => value.AssignFrom(6.0f));
        }

        [Test]
        public void CanConvertToAndFromVector2()
        {
            var tinyVec2 = MakeObject(m_Registry.GetVector2Type());
            Assert.NotNull(tinyVec2);
            var vec2 = new Vector2(234.0f, 1.8f);
            tinyVec2.AssignFrom(vec2);
            Assert.AreEqual(vec2.x, tinyVec2["x"]);
            Assert.AreEqual(vec2.y, tinyVec2["y"]);
            Assert.AreEqual(vec2, tinyVec2.As<Vector2>());
        }

        [Test]
        public void CanConvertToAndFromVector3()
        {
            var tinyVec3 = MakeObject(m_Registry.GetVector3Type());
            Assert.NotNull(tinyVec3);
            var vec3 = new Vector3(234.0f, 1.8f, -15.0f);
            tinyVec3.AssignFrom(vec3);
            Assert.AreEqual(vec3.x, tinyVec3["x"]);
            Assert.AreEqual(vec3.y, tinyVec3["y"]);
            Assert.AreEqual(vec3.z, tinyVec3["z"]);
            Assert.AreEqual(vec3, tinyVec3.As<Vector3>());
        }

        [Test]
        public void CanConvertToAndFromVector4()
        {
            var tinyVec4 = MakeObject(m_Registry.GetVector4Type());
            Assert.NotNull(tinyVec4);
            var vec4 = new Vector4(234.0f, 1.8f, -15.0f, 1.0f);
            tinyVec4.AssignFrom(vec4);
            Assert.AreEqual(vec4.x, tinyVec4["x"]);
            Assert.AreEqual(vec4.y, tinyVec4["y"]);
            Assert.AreEqual(vec4.z, tinyVec4["z"]);
            Assert.AreEqual(vec4.w, tinyVec4["w"]);
            Assert.AreEqual(vec4, tinyVec4.As<Vector4>());
        }

        [Test]
        public void CanConvertToAndFromBuiltinTypes()
        {
            var tinyVec4 = MakeObject(m_Registry.GetVector4Type());
            Assert.NotNull(tinyVec4);

            var vec4 = new Vector4(234.0f, 1.8f, -15.0f, 1.0f);
            tinyVec4.AssignPropertyFrom("x", vec4.x);
            tinyVec4.AssignPropertyFrom("y", vec4.y);
            tinyVec4.AssignPropertyFrom("z", vec4.z);
            tinyVec4.AssignPropertyFrom("w", vec4.w);

            Assert.AreEqual(vec4.x, tinyVec4.GetProperty<float>("x"));
            Assert.AreEqual(vec4.y, tinyVec4.GetProperty<float>("y"));
            Assert.AreEqual(vec4.z, tinyVec4.GetProperty<float>("z"));
            Assert.AreEqual(vec4.w, tinyVec4.GetProperty<float>("w"));
        }

        [Test]
        public void CanConvertToAndFromNormalMappedEnum()
        {
            ObjectConverter<GradientMode>.TestRegistry = m_Registry;

            var tinyGradient = MakeObject(m_Registry.GetGradientType());
            var mode = GradientMode.Blend;
            tinyGradient.AssignPropertyFrom("mode", mode);
            Assert.AreEqual(mode, tinyGradient.GetProperty<GradientMode>("mode"));

            mode = GradientMode.Fixed;
            tinyGradient.AssignPropertyFrom("mode", mode);
            Assert.AreEqual(mode, tinyGradient.GetProperty<GradientMode>("mode"));

            ObjectConverter<GradientMode>.TestRegistry = null;
        }

        [Test]
        public void CanConvertToAndFromSpecialMappedEnum()
        {
            ObjectConverter<CameraClearFlags>.TestRegistry = m_Registry;

            var tinyCamera = MakeObject(m_Registry.GetCamera2DType());
            Assert.NotNull(tinyCamera);

            var flag = CameraClearFlags.Color;
            tinyCamera.AssignPropertyFrom("clearFlags", flag);
            Assert.AreEqual(flag, tinyCamera.GetProperty<CameraClearFlags>("clearFlags"));

            flag = CameraClearFlags.Nothing;
            tinyCamera.AssignPropertyFrom("clearFlags", flag);
            Assert.AreEqual(flag, tinyCamera.GetProperty<CameraClearFlags>("clearFlags"));

            flag = CameraClearFlags.Depth; // remapped as Nothing
            tinyCamera.AssignPropertyFrom("clearFlags", flag);
            Assert.AreEqual(CameraClearFlags.Nothing, tinyCamera.GetProperty<CameraClearFlags>("clearFlags"));

            flag = CameraClearFlags.Skybox; // remapped as Color
            tinyCamera.AssignPropertyFrom("clearFlags", flag);
            Assert.AreEqual(CameraClearFlags.Color, tinyCamera.GetProperty<CameraClearFlags>("clearFlags"));

            ObjectConverter<CameraClearFlags>.TestRegistry = null;
        }


        private UTinyObject MakeObject(UTinyType.Reference typeRef)
        {
            return new UTinyObject(m_Registry, typeRef);
        }
    }
}
#endif // NET_4_6
