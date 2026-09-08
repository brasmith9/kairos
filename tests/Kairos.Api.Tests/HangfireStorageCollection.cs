namespace Kairos.Api.Tests;

/// <summary>
/// Hangfire's storage is a process-wide static (<c>JobStorage.Current</c>), so any test
/// class that sets it must join this collection - xUnit runs classes in parallel by
/// default, and two of them swapping storage concurrently see each other's jobs.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class HangfireStorageCollection
{
    public const string Name = "Hangfire storage";
}
