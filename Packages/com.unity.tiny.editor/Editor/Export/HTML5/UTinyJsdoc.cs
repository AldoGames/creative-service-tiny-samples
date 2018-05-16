#if NET_4_6
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
	internal static class UTinyJsdoc
	{
	    /// <inheritdoc />
	    /// <summary>
	    /// Helper class for comment scoping and jsdoc names
	    /// </summary>
	    internal class Writer : IDisposable
	    {
	        private readonly UTinyCodeWriter m_Writer;

	        public Writer(UTinyCodeWriter writer)
	        {
	            m_Writer = writer;
	            m_Writer.Line("/**");
	        }
        
	        public void Line(string content)
	        {
	            m_Writer.LineFormat(" * {0}", content);
	        }
		    
		    public void Namespace()
		    {
			    Line("@namespace");
		    }

	        public void Type(string name)
	        {
	            Line($"@type {{{name}}}");
	        }
	        
	        public void Enum(string name)
	        {
	            Line($"@enum {{{name}}}");
	        } 
		    
		    public void Class()
		    {
			    Line("@class");
		    }
		    
		    public void Method()
		    {
			    Line("@method");
		    }
		    
		    public void Extends(string name)
		    {
			    Line($"@extends {name}");
		    }
		    
		    public void Name(string name)
	        {
	            Line($"@name {name}");
	        }
	        
	        public void Desc(string desc)
	        {
	            if (string.IsNullOrEmpty(desc))
	            {
		            return;
	            }
		        
	            Line($"@desc {desc}");
	        }
		    
		    public void Classdesc(string desc)
		    {
			    if (string.IsNullOrEmpty(desc))
			    {
				    return;
			    }
		        
			    Line($"@classdesc {desc}");
		    }
	        
	        public void Readonly()
	        {
	            Line("@readonly");
	        }

		    public void Property(string type, string name, string desc)
		    {
			    Line($"@property {{{type}}} {name} {desc}");
		    }
		    
		    public void Returns(string type, string desc = "")
		    {
			    Line($"@returns {{{type}}} {desc}");
		    }
		    
		    public void Param(string type, string name, string desc = "")
		    {
			    Line($"@param {{{type}}} {name} {desc}");
		    }

	        public void Dispose()
	        {
	            m_Writer.Line(" */");
	        }
	    }
	    
		public static void WriteNamespace(UTinyCodeWriter writer, string desc = "")
		{
			using (var w = new Writer(writer))
			{
				w.Namespace();
				w.Desc(desc);
			}
		}
	    
		public static void WriteType(UTinyCodeWriter writer, string type, string desc = "")
		{
			using (var w = new Writer(writer))
			{
				w.Type(type);
				w.Desc(desc);
			}
		}

		public static void WriteSystem(UTinyCodeWriter writer, UTinySystem system)
		{
			using (var w = new Writer(writer))
			{
				w.Method();
				w.Desc($"System {system.Documentation.Summary}");
				
				if (system.Components.Count > 0)
				{
					var sb = new StringBuilder();
					sb.Append("Components [");
					for (var i = 0; i < system.Components.Count; i++)
					{
						var componentRef = system.Components[i];
						var component = componentRef.Dereference(system.Registry);
						if (null != component)
						{
							sb.AppendFormat(i == 0 ? "{{@link {0}}}" : ", {{@link {0}}}", UTinyBuildPipeline.GetJsTypeName(component));
						}
						else
						{
							throw new Exception($"System component is missing System=[{system.Name}] Component=[{componentRef.Name}]");
						}
					}
					sb.Append("]");
					w.Line(sb.ToString());
				}

				if (system.ExecuteAfter.Count > 0)
				{
					var sb = new StringBuilder();
					sb.Append("Execute After [");
					for (var i = 0; i < system.ExecuteAfter.Count; i++)
					{
						var executeAfterRef = system.ExecuteAfter[i];
						var executeAfter = executeAfterRef.Dereference(system.Registry);
						
						if (null != executeAfter)
						{
							sb.AppendFormat(i == 0 ? "{{@link {0}}}" : ", {{@link {0}}}", UTinyBuildPipeline.GetJsTypeName(executeAfter));
						}
						else
						{
							throw new Exception($"System reference is missing System=[{system.Name}] ExecuteAfter=[{executeAfterRef.Name}]");
						}
					}
					sb.Append("]");
					w.Line(sb.ToString());
				}

				if (system.ExecuteBefore.Count > 0)
				{
					var sb = new StringBuilder();
					sb.Append("Execute Before [");
					for (var i = 0; i < system.ExecuteBefore.Count; i++)
					{
						var executeBeforeRef = system.ExecuteBefore[i];
						var executeBefore = executeBeforeRef.Dereference(system.Registry);
						
						if (null != executeBefore)
						{
							sb.AppendFormat(i == 0 ? "{{@link {0}}}" : ", {{@link {0}}}", UTinyBuildPipeline.GetJsTypeName(executeBefore));
						}
						else
						{
							throw new Exception($"System reference is missing System=[{system.Name}] ExecuteBefore=[{executeBeforeRef.Name}]");
						}
					}
					sb.Append("]");
					w.Line(sb.ToString());
				}

				w.Param("ut.Scheduler", "sched");
				w.Param("ut.World", "world");
			}
		}
	}
}
#endif // NET_4_6
