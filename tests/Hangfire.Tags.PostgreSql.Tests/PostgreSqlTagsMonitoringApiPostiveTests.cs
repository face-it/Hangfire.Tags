using FluentAssertions;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Npgsql;
using System;
using System.Collections.Generic;
using Xunit;

namespace Hangfire.Tags.PostgreSql.Tests
{
    public class PostgreSqlTagsMonitoringApiPostiveTests
    {
        [Fact]
        public void WhenTypeContainsUseConnection_ThenNotThrow()
        {
            // Arrange
            var fakeImplementation = new PostgreSqlMonitoringApi();

            // Act
            Action act = () => new PostgreSqlTagsMonitoringApi(fakeImplementation);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void WhenTypeContainsUseConnection_ThenNotThrow1()
        {
            // Arrange
            var fakeImplementation = new PostgreSqlMonitoringApi();

            // Act
            var api = new PostgreSqlTagsMonitoringApi(fakeImplementation);
            api.UseConnection((con) => con.CreateCommand());

            // Assert
            fakeImplementation.NumberOfCalls.Should().Be(1);
        }

        private class PostgreSqlMonitoringApi : IMonitoringApi
        {
            public int NumberOfCalls = 0;

            private T UseConnection<T>(Func<NpgsqlConnection, T> action)
            {
                NumberOfCalls++;
                return action(new NpgsqlConnection());
            }

            public JobList<DeletedJobDto> DeletedJobs(int from, int count)
            {
                throw new NotImplementedException();
            }

            public long DeletedListCount()
            {
                throw new NotImplementedException();
            }

            public long EnqueuedCount(string queue)
            {
                throw new NotImplementedException();
            }

            public JobList<EnqueuedJobDto> EnqueuedJobs(string queue, int from, int perPage)
            {
                throw new NotImplementedException();
            }

            public IDictionary<DateTime, long> FailedByDatesCount()
            {
                throw new NotImplementedException();
            }

            public long FailedCount()
            {
                throw new NotImplementedException();
            }

            public JobList<FailedJobDto> FailedJobs(int from, int count)
            {
                throw new NotImplementedException();
            }

            public long FetchedCount(string queue)
            {
                throw new NotImplementedException();
            }

            public JobList<FetchedJobDto> FetchedJobs(string queue, int from, int perPage)
            {
                throw new NotImplementedException();
            }

            public StatisticsDto GetStatistics()
            {
                throw new NotImplementedException();
            }

            public IDictionary<DateTime, long> HourlyFailedJobs()
            {
                throw new NotImplementedException();
            }

            public IDictionary<DateTime, long> HourlySucceededJobs()
            {
                throw new NotImplementedException();
            }

            public JobDetailsDto JobDetails(string jobId)
            {
                throw new NotImplementedException();
            }

            public long ProcessingCount()
            {
                throw new NotImplementedException();
            }

            public JobList<ProcessingJobDto> ProcessingJobs(int from, int count)
            {
                throw new NotImplementedException();
            }

            public IList<QueueWithTopEnqueuedJobsDto> Queues()
            {
                throw new NotImplementedException();
            }

            public long ScheduledCount()
            {
                throw new NotImplementedException();
            }

            public JobList<ScheduledJobDto> ScheduledJobs(int from, int count)
            {
                throw new NotImplementedException();
            }

            public IList<ServerDto> Servers()
            {
                throw new NotImplementedException();
            }

            public IDictionary<DateTime, long> SucceededByDatesCount()
            {
                throw new NotImplementedException();
            }

            public JobList<SucceededJobDto> SucceededJobs(int from, int count)
            {
                throw new NotImplementedException();
            }

            public long SucceededListCount()
            {
                throw new NotImplementedException();
            }
        }
    }
}
