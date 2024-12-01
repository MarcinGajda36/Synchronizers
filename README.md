This project is release as nuget package: https://www.nuget.org/packages/PerKeySynchronizers 

Main types: PerKeySynchronizer<TKey>, PerKeySynchronizer

example use case:
```C#
  private static readonly PerKeySynchronizer<Guid> synchronizer = new();
  
  await synchronizer.SynchronizeAsync(
    tenantId,
    tenantState,
    async (tenantState, cancellationToken) =>
    {
      // Operations here are going to happen one at a time for specific tenantId, but allows operations for different tenantIds to happen concurrently.
      tenantState.SomethingCount = tenantState.SomethingCount + 1;
    },
    cancellationToken);
```
