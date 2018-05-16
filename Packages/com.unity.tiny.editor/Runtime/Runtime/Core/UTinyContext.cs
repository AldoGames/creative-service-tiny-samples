#if NET_4_6
namespace Unity.Tiny
{
	/// <summary>
	/// Helper class to build and maintain UTiny management objects
	/// </summary>
	public class UTinyContext
	{
		private readonly UTinyVersionStorage m_VersionStorage;

		public UTinyRegistry Registry { get; }
		public UTinyCaretaker Caretaker { get; }

		public UTinyContext()
		{
			m_VersionStorage = new UTinyVersionStorage();
			Registry = new UTinyRegistry(m_VersionStorage);
			Caretaker = new UTinyCaretaker(m_VersionStorage);
		}
	}
}
#endif // NET_4_6
