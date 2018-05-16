#if NET_4_6
using System;

using UnityEngine;

namespace Unity.Tiny
{
    /// <summary>
    /// UTinyEntityView acts as a proxy to the UTinyEntity to facilitate scene edit behaviour
    /// </summary>
    [DisallowMultipleComponent, ExecuteInEditMode]
    public sealed class UTinyEntityView : MonoBehaviour
    {
        #region Properties

        /// <summary>
        /// The underlying entity for this behaviour
        /// </summary>
        public UTinyEntity.Reference EntityRef { get; set; }
        public IRegistry Registry { get; set; }

        private static Action<UTinyTrackerRegistration, UTinyEntityView> Dispatch =>
            UTinyEventDispatcher<UTinyTrackerRegistration>.Dispatch;
        #endregion

        #region API
        public void RefreshName()
        {
            var entity = EntityRef.Dereference(Registry);
            gameObject.name = entity?.Name ?? gameObject.name;
        }
        #endregion

        #region Unity Event Handlers
        private void Awake()
        {
            Dispatch(UTinyTrackerRegistration.Register, this);
        }

        private void OnDestroy()
        {
            Dispatch(UTinyTrackerRegistration.Unregister, this);
        }

        private void Start()
        {
            DestroyIfUnlinked();
        }

        private void LateUpdate()
        {
            DestroyIfUnlinked();
        }

        public bool DestroyIfUnlinked()
        {
            if (null == Registry || null == EntityRef.Dereference(Registry))
            {
                DestroyImmediate(gameObject, false);
                return true;
            }

            // We got duplicated from Unity, kill self.
            return false;
        }
        #endregion
    }
}
#endif // NET_4_6
