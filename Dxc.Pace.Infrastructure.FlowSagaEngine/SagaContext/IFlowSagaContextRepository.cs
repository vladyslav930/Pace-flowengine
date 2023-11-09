using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.SagaContext
{
    public interface IFlowSagaContextRepository
    {
        public TimeSpan DefaultTtl { get; }

        Task<IReadOnlyCollection<T>> LoadAsync<T>(string key);
        Task<IReadOnlyCollection<T>> LoadAsync<T>(string key, long index, long length);
        Task<long> AddAsync<T>(string key, IEnumerable<T> items, TimeSpan? expiresIn = null);
        Task<long> GetListLengthAsync(string key);
        Task RemoveAllAsync(IEnumerable<string> keys);
        Task<T> GetAsync<T>(string key);
        Task<object> GetAsync(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiresIn = null);
        Task RenameAsync(string key, string newKey);
        Task<bool> KeyExistsAsync(string key);
    }

    public interface IFlowSagaContextRepository<T>
    {
        Task<IReadOnlyCollection<T>> LoadAsync();
        Task<IReadOnlyCollection<T>> LoadAsync(long index, long length);
        Task<long> AddAsync(IEnumerable<T> items, TimeSpan? expiresIn = null);
        Task<long> GetListLengthAsync();
        string GetKey();
        Task RemoveAsync();
        Task<bool> ExistsAsync();
    }
}
