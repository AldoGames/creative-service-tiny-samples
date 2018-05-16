#if NET_4_6
using System;

namespace Unity.Tiny
{
	public struct UTinyId : IEquatable<UTinyId>
	{
		private readonly Guid m_Guid;
		
		public static readonly UTinyId Empty = new UTinyId(Guid.Empty);
		
		public UTinyId(Guid guid)
		{
			m_Guid = guid;
		}
        
		public UTinyId(string guid)
		{
			m_Guid = string.IsNullOrEmpty(guid) ? Guid.Empty : new Guid(guid);
		}

		public static UTinyId New()
		{
			return new UTinyId(Guid.NewGuid());
		}

		public static UTinyId Generate(string name)
		{
			using (var provider = System.Security.Cryptography.MD5.Create())
			{
				var hash = provider.ComputeHash(System.Text.Encoding.Default.GetBytes(name));
				var guid = new Guid(hash);
				return new UTinyId(guid);
			}
		}

		public static bool operator ==(UTinyId a, UTinyId b)
		{
			return a.m_Guid.Equals(b.m_Guid);
		}

		public static bool operator !=(UTinyId a, UTinyId b)
		{
			return !(a == b);
		}

		public override bool Equals(object obj)
		{
			if (!(obj is UTinyId))
			{
				return false;
			}

			var typeReference = (UTinyId) obj;
			return (this == typeReference);
		}

		public override int GetHashCode()
		{
			return m_Guid.GetHashCode();
		}

		public Guid ToGuid()
		{
			return m_Guid;
		}

		public override string ToString()
		{
			return m_Guid.ToString("N");
		}

		public bool Equals(UTinyId other)
		{
			return m_Guid.Equals(other.m_Guid);
		}
	}
}
#endif // NET_4_6
