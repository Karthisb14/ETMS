using Xunit;

// EtmsWebApplicationFactory sets process-wide environment variables to select the
// EF Core InMemory provider for the test host (see EtmsWebApplicationFactory) —
// running test classes in parallel would race on those env vars, so parallelization
// is disabled for this assembly.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
