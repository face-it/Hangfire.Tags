using Hangfire.Storage;
using Hangfire.Tags.Dashboard;
using Hangfire.Tags.Storage;
using Moq;
using Xunit;

namespace Hangfire.Tags.Tests.Extensions
{
    public class HangfireExtensionsFacts
    {
        private Mock<IJobCancellationToken> _cancellationToken;
        private Mock<JobStorageConnection> _connection;
        private Mock<JobStorage> _jobStorage;
        private readonly Mock<JobStorageTransaction> _transaction;
        private readonly Mock<ITagsServiceStorage> _tagsServiceStorage;


        public HangfireExtensionsFacts()
        {
            _jobStorage = new Mock<JobStorage>(); 
            _cancellationToken = new Mock<IJobCancellationToken>();
            _connection = new Mock<JobStorageConnection>();
            _transaction = new Mock<JobStorageTransaction>();
            _tagsServiceStorage = new Mock<ITagsServiceStorage>();

            _connection.Setup(x => x.CreateWriteTransaction()).Returns(_transaction.Object);
            _jobStorage.Setup(x => x.GetConnection()).Returns(() => _connection.Object);

            JobStorage.Current = _jobStorage.Object;
            JobStorage.Current.Register(new TagsOptions(), _tagsServiceStorage.Object);
        }

        [Fact]
        public void AddTags_Commit_AfterAddingTags()
        {
            "234".AddTags("Tag 1", "Tag 2");
            _transaction.Verify(x => x.Commit(), Times.Once);
        }
    }
}
