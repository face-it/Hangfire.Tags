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
        public void WhenMonitorTypeContainsUseConnection_ThenNotThrow()
        {
            // Arrange
            var fakeImplementation = new WithoutConnection.PostgreSqlJobStorage();

            // Act
            Action act = () => new PostgreSqlTagsMonitoringApi(fakeImplementation);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void WhenStorageTypeContainsUseConnection_ThenNotThrow()
        {
            // Arrange
            var fakeImplementation = new WithConnection.PostgreSqlJobStorage();

            // Act
            Action act = () => new PostgreSqlTagsMonitoringApi(fakeImplementation);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void WhenMonitorTypeContainsUseConnection_ThenNotThrow1()
        {
            // Arrange
            var fakeImplementation = new WithoutConnection.PostgreSqlJobStorage();

            // Act
            var api = new PostgreSqlTagsMonitoringApi(fakeImplementation);
            api.UseConnection((con) => con.CreateCommand());

            // Assert
            fakeImplementation.NumberOfCalls.Should().Be(1);
        }

        [Fact]
        public void WhenStorageTypeContainsUseConnection_ThenNotThrow1()
        {
            // Arrange
            var fakeImplementation = new WithConnection.PostgreSqlJobStorage();

            // Act
            var api = new PostgreSqlTagsMonitoringApi(fakeImplementation);
            api.UseConnection((con) => con.CreateCommand());

            // Assert
            fakeImplementation.NumberOfCalls.Should().Be(1);
        }
    }
}

namespace Hangfire.Tags.PostgreSql.Tests.WithConnection
{
    internal class PostgreSqlJobStorage : WithoutConnection.PostgreSqlJobStorage
    {
        private T UseConnection<T>(Func<NpgsqlConnection, T> action)
        {
            NumberOfCalls++;
            return action(new NpgsqlConnection());
        }
        public override IMonitoringApi GetMonitoringApi()
        {
            return new WithoutConnection.PostgreSqlMonitoringApi();
        }
    }

    internal class PostgreSqlMonitoringApi : WithoutConnection.PostgreSqlMonitoringApi
    {
        private readonly WithoutConnection.PostgreSqlJobStorage _postgreSqlJobStorage;

        public PostgreSqlMonitoringApi(WithoutConnection.PostgreSqlJobStorage postgreSqlJobStorage)
        {
            _postgreSqlJobStorage = postgreSqlJobStorage;
        }

        private T UseConnection<T>(Func<NpgsqlConnection, T> action)
        {
            _postgreSqlJobStorage.NumberOfCalls++;
            return action(new NpgsqlConnection());
        }
    }
}

namespace Hangfire.Tags.PostgreSql.Tests.WithoutConnection
{
    internal class PostgreSqlJobStorage : JobStorage
    {
        public int NumberOfCalls = 0;

        public override IMonitoringApi GetMonitoringApi()
        {
            return new WithConnection.PostgreSqlMonitoringApi(this);
        }

        public override IStorageConnection GetConnection()
        {
            return null;
        }
    }

    internal class PostgreSqlMonitoringApi : IMonitoringApi
    {
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