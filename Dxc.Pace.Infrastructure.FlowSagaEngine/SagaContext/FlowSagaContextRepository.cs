using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.SagaContext
{
    public class FlowSagaContextRepository<T> : IFlowSagaContextRepository<T>, IFlowSagaContextValue<T>
    {
        private readonly string key;
        private readonly IFlowSagaContextRepository repository;

        public FlowSagaContextRepository(string key, IFlowSagaContextRepository repository)
        {
            this.key = key;
            this.repository = repository;
        }

        public Task<long> GetListLengthAsync()
        {
            return repository.GetListLengthAsync(key);
        }

        public Task<IReadOnlyCollection<T>> LoadAsync()
        {
            return repository.LoadAsync<T>(key);
        }

        public Task<IReadOnlyCollection<T>> LoadAsync(long index, long length)
        {
            return repository.LoadAsync<T>(key, index, length);
        }

        public Task RemoveAsync()
        {
            var keys = new List<string>() { key };
            return repository.RemoveAllAsync(keys);
        }

        public Task<long> AddAsync(IEnumerable<T> items, TimeSpan? expiresIn = null)
        {
            return repository.AddAsync(key, items, expiresIn.GetValueOrDefault(repository.DefaultTtl));
        }

        public Task<T> GetAsync()
        {
            return repository.GetAsync<T>(key);
        }

        public Task SetAsync(T value, TimeSpan? expiresIn = null)
        {
            return repository.SetAsync(key, value, expiresIn.GetValueOrDefault(repository.DefaultTtl));
        }

        public string GetKey() => key;

        async Task<bool> IFlowSagaContextRepository<T>.ExistsAsync()
        {
            return await repository.KeyExistsAsync(key) && await repository.GetListLengthAsync(key) > 0;
        }

        Task<bool> IFlowSagaContextValue<T>.ExistsAsync()
        {
            return repository.KeyExistsAsync(key);
        }
    }
}
