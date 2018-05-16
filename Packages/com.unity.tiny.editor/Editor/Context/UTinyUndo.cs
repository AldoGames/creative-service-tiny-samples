#if NET_4_6
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace Unity.Tiny
{
    public delegate void UndoPerformed();
    public delegate void RedoPerformed();

    public class UTinyUndo
    {
        private class UTinyUndoObject : ScriptableObject
        {
            private int m_Current = 0;

            [SerializeField]
            private int m_Version = 0;

            public delegate void UndoHandler();
            public delegate void RedoHandler();

            public UndoHandler OnUndo;
            public UndoHandler OnRedo;
            
            public int Version => m_Version;

            public void IncrementVersion()
            {
                Undo.RecordObject(this, $"{UTinyConstants.ApplicationName} Operation");
                m_Version++;
                m_Current = m_Version;
                EditorUtility.SetDirty(this);
            }

            private void OnEnable()
            {
                Undo.undoRedoPerformed += HandleUndoRedoPerformed;
            }

            private void OnDisable()
            {
                Undo.undoRedoPerformed -= HandleUndoRedoPerformed;
            }
            
            public void Flush()
            {
                Undo.FlushUndoRecordObjects();
            }

            private void HandleUndoRedoPerformed()
            {
                if (m_Current != m_Version)
                {
                    if (m_Current > m_Version)
                    {
                        OnUndo?.Invoke();
                    }
                    else
                    {
                        OnRedo?.Invoke();
                    }

                    m_Current = m_Version;
                }
            }
        }

        public struct Change
        {
            public int Version { get; set; }
            public UTinyId Id { get; set; }
            public IRegistryObject RegistryObject { get; set; }
            public IMemento NextVersion { get; set; }
            public IMemento PreviousVersion { get; set; }
        }

        public class ChangeComparer : IEqualityComparer<Change>
        {
            public bool Equals(Change x, Change y)
            {
                return x.Id.Equals(y.Id);
            }

            public int GetHashCode(Change obj)
            {
                return obj.Id.GetHashCode();
            }
        }

        public event UndoPerformed OnUndoPerformed;
        public event RedoPerformed OnRedoPerformed;

        private readonly List<HashSet<Change>> m_UndoableChanges;
        private readonly List<HashSet<Change>> m_RedoableChanges;
        private readonly HashSet<Change> m_FrameChanges;

        private readonly UTinyUndoObject m_Undo;
        private readonly UTinyCaretaker m_Caretaker;
        private readonly IRegistry m_Registry;
        private int m_CurrentIndex;

        public UTinyUndo(IRegistry registry, UTinyCaretaker caretaker)
        {
            m_Registry = registry;
            caretaker.OnObjectChanged += HandleUndoableChange;
            m_Undo = ScriptableObject.CreateInstance<UTinyUndoObject>();
            m_Undo.hideFlags |= HideFlags.HideAndDontSave;
            AddCallbacks();
            m_UndoableChanges = new List<HashSet<Change>>();
            m_RedoableChanges = new List<HashSet<Change>>();
            m_Caretaker = caretaker;
            m_FrameChanges = new HashSet<Change>();
            EditorApplication.update += Update;
            AssemblyReloadEvents.beforeAssemblyReload += Unload;
        }

        private void AddCallbacks()
        {
            m_Undo.OnUndo += HandleUndoOperation;
            m_Undo.OnRedo += HandleRedoOperation;
        }

        private void RemoveCallbacks()
        {
            m_Undo.OnUndo -= HandleUndoOperation;
            m_Undo.OnRedo -= HandleRedoOperation;
        }

        private void HandleRedoOperation()
        {
            BindingsHelper.RemoveAllBindingsOnLoadedEntities();
            int version = int.MaxValue;
            try
            {
                do
                {
                    if (m_RedoableChanges.Count > 0)
                    {
                        //Debug.Log("Redo operation");
                        var changes = m_RedoableChanges.Last();
                        foreach (var change in changes)
                        {
                            var originator = m_Registry.FindById<IRegistryObject>(change.Id) as IOriginator;
                            version = change.Version;
                            if (null == originator)
                            {
                                originator = change.RegistryObject as IOriginator;
                            }

                            // We have no next version, which means that the originator must have been created.
                            // Knowing this, we shall remove it from the registry.
                            if (null == change.NextVersion)
                            {
                                m_Registry.Unregister(originator as IRegistryObject);
                            }
                            // Otherwise, restore the next version.
                            else
                            {
                                originator.Restore(change.NextVersion);
                            }
                        }
                        m_RedoableChanges.RemoveAt(m_RedoableChanges.Count - 1);
                        m_UndoableChanges.Add(changes);
                    }
                    else
                    {
                        break;
                    }
                }
                while (version < m_Undo.Version);
            }
            finally
            {
                RefreshAll();
                OnRedoPerformed?.Invoke();
                m_Caretaker.OnObjectChanged -= HandleUndoableChange;
                m_Caretaker.Update();
                m_Registry.ClearUnregisteredObjects();
                m_Caretaker.OnObjectChanged += HandleUndoableChange;
                BindingsHelper.RunAllBindingsOnLoadedEntities();
            }
        }

        private void HandleUndoOperation()
        {
            BindingsHelper.RemoveAllBindingsOnLoadedEntities();
            int version = -1;

            try
            {
                do
                {
                    // 0 is basically you loaded the project
                    if (m_UndoableChanges.Count > 1)
                    {
                        //Debug.Log("Undo operation");
                        var changes = m_UndoableChanges.Last();
                        foreach (var change in changes)
                        {
                            var originator = m_Registry.FindById<IRegistryObject>(change.Id) as IOriginator;
                            version = change.Version;
                            if (null == originator)
                            {
                                originator = change.RegistryObject as IOriginator;
                            }

                            // We have no previous version, which means that the originator must have been created.
                            // Knowing this, we shall remove it from the registry.
                            if (null == change.PreviousVersion)
                            {
                                m_Registry.Unregister(originator as IRegistryObject);
                            }
                            // Otherwise, restore the previous version.
                            else
                            {
                                originator.Restore(change.PreviousVersion);
                            }
                        }
                        m_UndoableChanges.RemoveAt(m_UndoableChanges.Count - 1);
                        m_RedoableChanges.Add(changes);
                    }
                    else
                    {
                        break;
                    }
                }
                while (version > m_Undo.Version);
            }
            finally
            {
                RefreshAll();
                OnUndoPerformed?.Invoke();
                m_Caretaker.OnObjectChanged -= HandleUndoableChange;
                m_Caretaker.Update();
                m_Registry.ClearUnregisteredObjects();
                m_Caretaker.OnObjectChanged += HandleUndoableChange;
                BindingsHelper.RunAllBindingsOnLoadedEntities();
            }
        }

        public void RefreshAll()
        {
            if (!m_Caretaker.HasChanges)
            {
                return;
            }

            foreach (var system in m_Registry.FindAllByType<Tiny.UTinySystem>())
            {
                system.Refresh();
            }

            foreach (var type in m_Registry.FindAllByType<Tiny.UTinyType>())
            {
                type.Refresh();
            }

            foreach (var entity in m_Registry.FindAllByType<Tiny.UTinyEntity>())
            {
                foreach (var component in entity.Components)
                {
                    component.Refresh();
                }
            }
        }

        public void Update()
        {
            if (!m_Undo || null == m_Undo)
            {
                return;
            }
            
            RefreshAll();

            m_FrameChanges.Clear();
            m_Caretaker.Update();

            // Handle deleted objects
            foreach(var unregistered in m_Registry.AllUnregistered())
            {
                m_FrameChanges.Add(new Change { Id = unregistered.Id, Version = m_Undo.Version, RegistryObject= unregistered, NextVersion = null, PreviousVersion = GetPreviousValue(unregistered) });
            }
            m_Registry.ClearUnregisteredObjects();

            if (m_FrameChanges.Count > 0)
            {
                if (TryMergeChanges())
                {
                    return;
                }

                //Debug.Log("UndoRedoable operation registered");
                // We didn't / couldn't merge the changes, push a new changeset.
                m_UndoableChanges.Add(new HashSet<Change>(m_FrameChanges, new ChangeComparer()));
                m_Undo.IncrementVersion();
                m_RedoableChanges.Clear();
                ++m_CurrentIndex;
            }
        }

        private bool TryMergeChanges()
        {
            // [MP] @TODO: If the target(s) of the current changeset is/are the same as the last changeset and we are inside a change delta time,
            //             merge the changes together instead of pushing a new changeset.

            return false;
        }

        public void HandleUndoableChange(IOriginator originator, IMemento memento)
        {
            var change = new Change { Id = originator.Id, Version = m_Undo.Version, RegistryObject = originator as IRegistryObject, NextVersion = memento, PreviousVersion = GetPreviousValue(originator) };
            m_FrameChanges.Remove(change);
            m_FrameChanges.Add(change);
        }

        /// <summary>
        /// When flushing an originator, we will push the current state at the index 0 (pretty much the initial state) and then remove it from
        /// the undo/redo stack.
        /// </summary>
        public void FlushChanges(params IOriginator[] originators)
        {
            FlushChanges((IEnumerable <IOriginator>) originators);
        }

        public void FlushChanges(IEnumerable<IOriginator> originators)
        {
            if (null == m_Undo)
            {
                return; 
            }
            
            foreach(var originator in originators)
            {
                m_UndoableChanges[0].Add(new Change { Id = originator.Id, Version = m_Undo.Version, RegistryObject = originator as IRegistryObject, NextVersion = originator.Save(), PreviousVersion = null });

                foreach(var changeSet in m_RedoableChanges)
                {
                    changeSet.RemoveWhere(change => change.Id.Equals(originator.Id));
                }

                for(int i = 1; i < m_UndoableChanges.Count; ++i)
                {
                    var changeSet = m_UndoableChanges[i];
                    changeSet.RemoveWhere(change => change.Id.Equals(originator.Id));
                }
            }
        }

        public void Unload()
        {
            EditorApplication.update -= Update;
            m_Undo.Flush();
            RemoveCallbacks();
            UnityEngine.Object.DestroyImmediate(m_Undo, false);
        }

        private IMemento GetPreviousValue<T>(IIdentifiable<T> originator)
        {
            // We skip the current changes
            for(int i = m_UndoableChanges.Count - 1; i >= 0; --i)
            {
                var changeset = m_UndoableChanges[i];
                var previous = changeset.FirstOrDefault(c => c.Id.Equals(originator.Id));
                if (previous.Equals(default(Change)))
                {
                    continue;
                }
                return previous.NextVersion;
            }
            return null;
        }
    }
}
#endif // NET_4_6
