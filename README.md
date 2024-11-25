This project is release as nuget package: https://www.nuget.org/packages/PerKeySynchronizers 
Main types: BitMaskSemaphorePool, FibonacciSemaphorePool, ModuloSemaphorePool, DictionaryOfSemaphores, ConcurrentDictionaryOfSemaphores

example use case:
  private static readonly DictionaryOfSemaphores<Guid> semaphores = new();
  
  await semaphores.SynchronizeAsync(
    tenantId,
    tenantState,
    async (tenantState, cancellationToken) =>
    {
      // Operations here are going to happen one at a time for specific tenantId, but allows operations for different tenantIds to happen concurrently.
      tenantState.SomethingCount = tenantState.SomethingCount + 1;
    },
    cancellationToken);
