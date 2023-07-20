using FluentAssertions;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;

namespace Hangfire.Tags.MySql.Tests
{
    public class MySqlTagsMonitoringApiNegativeTests
    {
        [Fact]
        public void WhenTypeDoesNotMatch_ThenThrow()
        {
            // Arrange
            var monitoringApiMock = Mock.Of<IMonitoringApi>();

            // Act
            Action act = () => new MySqlTagsMonitoringApi(monitoringApiMock);

            // Assert
            act.Should().Throw<Exception>("Monitoring api is not MySqlMonitoringApi type").WithMessage("The monitor API is not implemented using MySql*");
        }

        [Fact]
        public void WhenUseConnectionMethodIsNotInTheInstance_ThenThrow()
        {
            // Arrange
            var fakeImplementation = new MySqlMonitoringApi();

            // Act
            Action act = () => new MySqlTagsMonitoringApi(fakeImplementation);

            // Assert
            act.Should().Throw<ArgumentException>("Api doesn't have UseConnection method").WithMessage("The function UseConnection cannot be found.");
        }

        private class MySqlMonitoringApi : IMonitoringApi
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
}
