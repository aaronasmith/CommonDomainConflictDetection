using System;
using CommonDomain;
using CommonDomain.Core;
using CommonDomain.Persistence;
using CommonDomain.Persistence.EventStore;
using NEventStore;

namespace NoConflictDetection
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var store = GetInitializedEventStore();

            var repository1 = new EventStoreRepository(store, new AggregateConstructor(), new ConflictDetector());
            var repository2 = new EventStoreRepository(store, new AggregateConstructor(), new ConflictDetector());

            var id = Guid.NewGuid();
            var aggregate = new MyAggregate();
            aggregate.Create(id);
            repository1.Save(aggregate, Guid.NewGuid());

            var agg1 = repository1.GetById<MyAggregate>(id);
            var agg2 = repository2.GetById<MyAggregate>(id);
            agg1.DoSomething(42);
            agg2.DoSomething(13);

            repository1.Save(agg1, Guid.NewGuid());

            // The following should throw a concurrency exception.
            repository2.Save(agg2, Guid.NewGuid());
        }

        private static IStoreEvents GetInitializedEventStore()
        {
            return Wireup.Init()
                .LogToOutputWindow()
                .UsingInMemoryPersistence()
                .Build();
        }
    }

    public class AggregateConstructor : IConstructAggregates
    {
        public IAggregate Build(Type type, Guid id, IMemento snapshot)
        {
            return  new MyAggregate();
        }
    }

    public class MyAggregate : AggregateBase
    {
        public int TheValue { get; private set; }

        public void Apply(Created evt)
        {
            Id = evt.Id;
        }

        public void Apply(MyEvent evt)
        {
            TheValue = evt.MyValue;
        }

        public void Create(Guid id)
        {
            RaiseEvent(new Created(id));
        }

        public void DoSomething(int value)
        {
            RaiseEvent(new MyEvent(value));
        }
    }

    public class MyEvent
    {
        public MyEvent(int value)
        {
            MyValue = value;
        }

        public int MyValue { get; private set; }
    }

    public class Created
    {
        public Guid Id { get; private set; }

        public Created(Guid id)
        {
            Id = id;
        }
    }
}
