using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Domo
{
    public abstract class Repository<T> : IRepository<T>
        where T: new()
    {
        protected Repository(T value)
        {
            DefaultValue = value == null ? new T() : value;
        }

        public event EventHandler<RepositoryChangeArgs> RepositoryChanged;
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public Type ValueType
            => typeof(T);

        public T DefaultValue
        { get; }

        object IRepository.DefaultValue 
            => DefaultValue;

        private IDictionary<Guid, (T, Model<T>)> _dict = new Dictionary<Guid, (T, Model<T>)>();

        public void Dispose()
        {
            RepositoryChanged = null;
            CollectionChanged = null;
            Clear();
            _dict = null;
        }

        public void Clear()
        {
            foreach (var v in _dict.Keys.ToArray())
            {
                Delete(v);
            }

            _dict.Clear();
        }

        IModel IRepository.GetModel(Guid modelId)
            => GetModel(modelId);

        object IRepository.GetValue(Guid modelId)
            => GetModel(modelId);

        public void NotifyRepositoryChanged(RepositoryChangeType type, Guid modelId, object newValue, object oldValue)
        {
            var args = new RepositoryChangeArgs
            {
                ChangeType = type,
                ModelId = modelId,
                NewValue = newValue,
                OldValue = oldValue,
                Repository = this,
            };
            RepositoryChanged?.Invoke(this, args);
            switch (type)
            {
                case RepositoryChangeType.ModelAdded:
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, oldValue));
                    break;
                case RepositoryChangeType.ModelRemoved:
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldValue));
                    break;
                case RepositoryChangeType.ModelUpdated:
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newValue, oldValue));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public bool Update(Guid modelId, Func<T, T> updateFunc)
        {
            var model = GetModel(modelId);
            var oldValue = model.Value;
            var newValue = updateFunc(oldValue);
            if (oldValue.Equals(newValue))
            {
                // When there is no difference in the values there is no need to trigger a change
                return false;
            }
            if (!Validate(newValue))
            {
                // Value is invalid
                return false;
            }
            _dict[modelId] = (newValue, _dict[modelId].Item2);
            model.TriggerChangeNotification();
            NotifyRepositoryChanged(RepositoryChangeType.ModelUpdated, modelId, oldValue, newValue);
            return true;
        }

        public virtual T Create()
            => ForceValid(new T());

        public virtual T ForceValid(T state)
            => state == null ? Create() : state;

        public virtual bool Validate(T state)
            => state != null;

        public bool Validate(object state)
            => Validate((T)state);

        public IModel<T> Add(Guid id, T state = default)
        {
            state = ForceValid(state);
            if (IsSingleton && _dict.Count != 0)
                throw new Exception("Singleton repository cannot have more than one model");
            var model = new Model<T>(id, this);
            _dict.Add(id, (state, model));
            NotifyRepositoryChanged(RepositoryChangeType.ModelAdded, id, model.Value, null);
            return model;
        }

        public IReadOnlyList<IModel<T>> GetModels()
            => _dict.Values.Select(x => x.Item2).ToList();

        public IModel<T> GetModel(Guid modelId)
            => _dict[modelId].Item2;

        public T GetValue(Guid modelId)
            => _dict[modelId].Item1;

        public bool Update(Guid modelId, Func<object, object> updateFunc)
            => Update(modelId, x => (T)updateFunc(x));

        public IModel Add(Guid id, object state)
            => Add(id, (T)state);

        public virtual void Delete(Guid id)
        {
            var oldValue = _dict[id].Item1;
            _dict[id].Item2.Dispose();
            _dict.Remove(id);
            NotifyRepositoryChanged(RepositoryChangeType.ModelRemoved, id, null, oldValue);
        }

        public bool ModelExists(Guid id)
            => _dict.ContainsKey(id);

        IReadOnlyList<IModel> IRepository.GetModels()
            => GetModels();

        public abstract bool IsSingleton { get; }

        public int Count 
            => _dict.Count;
    }

    public class AggregateRepository<T> : Repository<T>, IAggregateRepository<T>
        where T : new()
    {
        public AggregateRepository(T value = default)
            : base(value)
        { }

        public override bool IsSingleton => false;
    }

    public class SingletonRepository<T> : Repository<T>, ISingletonRepository<T>
        where T : new()
    {
        public SingletonRepository(T value = default)
            : base(value)
            => Model = Add(Guid.NewGuid(), DefaultValue);
        
        public override bool IsSingleton => true;

        public IModel<T> Model { get; }

        public T Value
        {
            get => Model.Value;
            set => Model.Value = value;
        }
    } 
}