namespace Pos.Application.Common;

/// <summary>
/// Serializa, dentro del proceso de la API, las peticiones que llegan con el mismo
/// identificador de operación (doble clic o dos pestañas, RNF04). Así la segunda petición
/// siempre concilia por uuid después de que termina la primera.
/// </summary>
public sealed class SaleOperationLock
{
    private readonly Dictionary<Guid, Entry> _entries = [];

    /// <summary>Espera el turno de la operación y devuelve el objeto que lo libera.</summary>
    public async Task<IDisposable> AcquireAsync(Guid operationId, CancellationToken cancellationToken)
    {
        Entry entry;
        lock (_entries)
        {
            if (!_entries.TryGetValue(operationId, out entry!))
            {
                entry = new Entry();
                _entries[operationId] = entry;
            }
            entry.References++;
        }

        try
        {
            await entry.Semaphore.WaitAsync(cancellationToken);
        }
        catch
        {
            ReleaseReference(operationId, entry);
            throw;
        }

        return new Releaser(this, operationId, entry);
    }

    private void Release(Guid operationId, Entry entry)
    {
        entry.Semaphore.Release();
        ReleaseReference(operationId, entry);
    }

    private void ReleaseReference(Guid operationId, Entry entry)
    {
        lock (_entries)
        {
            entry.References--;
            if (entry.References == 0)
            {
                _entries.Remove(operationId);
            }
        }
    }

    private sealed class Entry
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);

        public int References { get; set; }
    }

    private sealed class Releaser(SaleOperationLock owner, Guid operationId, Entry entry) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                owner.Release(operationId, entry);
            }
        }
    }
}
