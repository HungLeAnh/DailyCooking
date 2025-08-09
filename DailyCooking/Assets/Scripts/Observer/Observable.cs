using System;
using System.Collections.Generic;
using UnityEngine;

namespace Observer
{
    public enum EObserverEvent
    {
        Default,
        ModelChange,
    }
    public interface IObserver
    {
        public void OnNotify();
    }

    public abstract class Observable
    {
        private readonly SortedDictionary<EObserverEvent, List<IObserver>> SubscribersDict = new SortedDictionary<EObserverEvent, List<IObserver>>();
        public Observable()
        {
            SubscribersDict = new SortedDictionary<EObserverEvent, List<IObserver>>();
            Array.ForEach<EObserverEvent>((EObserverEvent[])Enum.GetValues(typeof(EObserverEvent)),
                EventType => SubscribersDict.Add(EventType, new List<IObserver>()));
        }

        public void Subscribe(EObserverEvent eventType, IObserver subscriber)
        {
            SubscribersDict.GetValueOrDefault(eventType).Add(subscriber);
        }
        public void Unsubscribe(EObserverEvent eventType, IObserver subscriber)
        {
            SubscribersDict.GetValueOrDefault(eventType).Remove(subscriber);
        }
        public void NotifySubscribers(EObserverEvent eventType)
        {
            SubscribersDict.GetValueOrDefault(eventType).ForEach(subscriber => subscriber.OnNotify());
        }

    }

}