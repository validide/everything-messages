using System.Threading;
using System.Threading.Tasks;

namespace EverythingMessages.Api.Infrastructure.DocumentStore
{
    public interface IDocumentStore
    {
        public Task<string> StoreAsync(byte[] document, CancellationToken cancellationToken);
        public Task<byte[]> GetAsync(string id, CancellationToken cancellationToken);
        public Task RemoveAsync(string id, CancellationToken cancellationToken);
        public Task<string[]> ListAsync(CancellationToken cancellationToken);
    }
}
