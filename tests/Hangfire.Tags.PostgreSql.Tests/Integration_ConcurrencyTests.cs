using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Hangfire.PostgreSql;
using Hangfire.States;
using Hangfire.Tags.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Hangfire.Tags.PostgreSql.Tests
{
    namespace Hangfire.Tags.PostgreSql.Tests
    {
        public class Integration_ConcurrencyTests : IntegrationTestBase
        {
            private readonly ITestOutputHelper _testOutputHelper;

            public Integration_ConcurrencyTests(ITestOutputHelper testOutputHelper)
            {
                _testOutputHelper = testOutputHelper;
            }

            public class ConcurrencyErrorJob
            {
                public enum JobType
                {
                    Type1,
                    Type2
                }

                [Tag("GlobalTag", "JobType-{0}", "JobRelatedObjectId-{1}")]
                public void MethodWithSharedTags([UsedImplicitly] JobType jobType, [UsedImplicitly] int jobRelatedObjectId)
                {
                }
            }

            [Fact]
            public async Task AddTags_ShouldNotFail_WithConcurrencyError()
            {
                if (Environment.ProcessorCount == 1) throw new InvalidOperationException("At least 2 CPU cores are required for this multi-threaded test");

                var testDuration = TimeSpan.FromSeconds(30);

                Services.AddHangfire(configuration => configuration
                    .UsePostgreSqlStorage(Configuration.GetConnectionString("HangfirePostgres"))
                    .UseTagsWithPostgreSql()
                );

                var provider = Services.BuildServiceProvider();

                var client = provider.GetRequiredService<IBackgroundJobClient>();

                int createdJobsCount = 0;

                void CreateJobs(ConcurrencyErrorJob.JobType jobType, CancellationToken token)
                {
                    for (int i = 0; i < int.MaxValue; i++)
                    {
                        if (token.IsCancellationRequested) return;

                        client.Create<ConcurrencyErrorJob>(x => x.MethodWithSharedTags(jobType, i % 100), new EnqueuedState {Queue = "not-processed-queue"});

                        Interlocked.Increment(ref createdJobsCount);
                    }
                }

                var cts = new CancellationTokenSource();
                cts.CancelAfter(testDuration);

                var workers = Enumerable.Range(1, Environment.ProcessorCount - 1)
                    .Select(i => Task.Run(() => CreateJobs(i % 2 == 0 ? ConcurrencyErrorJob.JobType.Type1 : ConcurrencyErrorJob.JobType.Type2, cts.Token), cts.Token))
                    .ToArray();

                try
                {
                    await Task.WhenAll(workers);
                    _testOutputHelper.WriteLine("Test completed after have created {0} jobs", createdJobsCount);
                }
                catch
                {
                    _testOutputHelper.WriteLine("Test failed after have created {0} jobs", createdJobsCount);
                    throw;
                }
            }
        }
    }
}