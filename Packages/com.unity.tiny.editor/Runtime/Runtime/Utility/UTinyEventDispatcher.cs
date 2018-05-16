#if NET_4_6
using System;
using System.Collections.Generic;

namespace Unity.Tiny
{
    public delegate void UTinyEventHandler<TEvent, in TValue>(TEvent @event, TValue @object)
        where TEvent : struct
        where TValue : class;

    public interface IEvent
    {
        void AddListener<TEvent, TValue>(UTinyEventHandler<TEvent, TValue> @delegate)
            where TEvent : struct
            where TValue : class;

        void RemoveListener<TEvent, TValue>(UTinyEventHandler<TEvent, TValue> @delegate)
            where TEvent : struct
            where TValue : class;
    }

    public class Event<TEvent, TValue> : IEvent
        where TEvent : struct
        where TValue : class
    {
        private UTinyEventHandler<TEvent, TValue> m_Delegate;

        public void AddListener<TEventType, TObject>(UTinyEventHandler<TEventType, TObject> @delegate)
            where TEventType : struct
            where TObject : class
        {
            m_Delegate += (UTinyEventHandler<TEvent, TValue>)(object)@delegate;
        }

        public void RemoveListener<TEventType, TObject>(UTinyEventHandler<TEventType, TObject> @delegate)
            where TEventType : struct
            where TObject : class
        {
            m_Delegate -= (UTinyEventHandler<TEvent, TValue>)(object)@delegate;
        }

        public void Dispatch(TEvent type, TValue value)
        {
            if (null != m_Delegate)
            {
                m_Delegate.Invoke(type, value);
            }
        }
    }

    public class UTinyEventDispatcher
    {
        public static void AddListener<TEvent, TValue>(TEvent type, UTinyEventHandler<TEvent, TValue> @event)
            where TEvent : struct
            where TValue : class
        {
            UTinyEventDispatcher<TEvent>.AddListener(type, @event);
        }

        public static void RemoveListener<TEvent, TValue>(TEvent type, UTinyEventHandler<TEvent, TValue> @event)
            where TEvent : struct
            where TValue : class
        {
            UTinyEventDispatcher<TEvent>.RemoveListener(type, @event);
        }

        public static void Dispatch<TEvent, TValue>(TEvent type, TValue @event)
            where TEvent : struct
            where TValue : class
        {
            UTinyEventDispatcher<TEvent>.Dispatch(type, @event);
        }
    }

    public class UTinyEventDispatcher<TEvent>
        where TEvent : struct
    {
        public class EventMap
        {
            private readonly Dictionary<Type, IEvent> m_Events = new Dictionary<Type, IEvent>();

            public void AddListener<TValue>(UTinyEventHandler<TEvent, TValue> @delegate) 
                where TValue : class
            {
                IEvent @event;

                if (!m_Events.TryGetValue(typeof(TValue), out @event))
                {
                    @event = new Event<TEvent, TValue>();
                    m_Events.Add(typeof(TValue), @event);
                }

                @event.AddListener(@delegate);
            }

            public void RemoveListener<TValue>(UTinyEventHandler<TEvent, TValue> @delegate)
                where TValue : class
            {
                IEvent @event;

                if (!m_Events.TryGetValue(typeof(TValue), out @event))
                {
                    return;
                }

                @event.RemoveListener(@delegate);
            }

            public void Dispatch<TValue>(TEvent type, TValue value, Type objectType) 
                where TValue : class
            {
                IEvent @event;

                if (!m_Events.TryGetValue(objectType, out @event))
                {
                    return;
                }
                var typeEvent = @event as Event<TEvent, TValue>;
                typeEvent.Dispatch(type, value);
            }
        }

        private static readonly Dictionary<TEvent, EventMap> Events = new Dictionary<TEvent, EventMap>();

        public static void AddListener<TValue>(TEvent type, UTinyEventHandler<TEvent, TValue> @delegate)
            
            where TValue : class
        {
            EventMap map = null;
            if (!Events.TryGetValue(type, out map))
            {
                map = Events[type] = new EventMap();
            }
            map.AddListener(@delegate);
        }

        public static void RemoveListener<TValue>(TEvent type, UTinyEventHandler<TEvent, TValue> @delegate)
            where TValue : class
        {
            EventMap map = null;
            if (!Events.TryGetValue(type, out map))
            {
                map = Events[type] = new EventMap();
            }
            map.RemoveListener(@delegate);
        }

        public static void Dispatch<T>(TEvent type, T value) where T : class
        {
            EventMap map = null;
            if (!Events.TryGetValue(type, out map))
            {
                return;
            }

            map.Dispatch(type, value, typeof(T));
        }
    }
}
#endif // NET_4_6
