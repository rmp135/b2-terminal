using B2Terminal.Commands;
using B2Net.Models;
using FakeItEasy;

namespace B2Terminal.Tests;

public class CDTests
{
    private IAPITasks apiTasks;
    private IConsoleProvider consoleProvider;
    private CD cd;
    private Client client;
    
    [SetUp]
    public void Setup()
    {
        apiTasks = A.Fake<IAPITasks>();
        consoleProvider = A.Fake<IConsoleProvider>();
        cd = new CD(apiTasks, consoleProvider);
        client = new Client(Enumerable.Empty<ICommand>(), consoleProvider, apiTasks);
    }
    
    [Test]
    public async Task FromRootNoBuckets()
    {
        var buckets = new List<B2Bucket>();
        A.CallTo(() => apiTasks.GetBucketsAsync()).Returns(buckets);
        
        await cd.Run(client, "bucket");
        
        A.CallTo(() => consoleProvider.WriteLine("Bucket bucket does not exist")).MustHaveHappened();
        Assert.That(client.CurrentBucket, Is.Null);
        Assert.That(client.CurrentPath, Is.Empty);
    }
    
    [Test]
    public async Task FromRootBucketFound()
    {
        var buckets = new List<B2Bucket>
        {
            new() { BucketName = "bucket" }
        };
        A.CallTo(() => apiTasks.GetBucketsAsync()).Returns(buckets);

        await cd.Run(client, "bucket");
        
        Assert.That(client.CurrentBucket.BucketName, Is.EqualTo("bucket"));
        Assert.That(client.CurrentPath, Is.Empty);
    }
    
    [Test]
    public async Task FromRootNavigateUpwards()
    {
        var buckets = new List<B2Bucket>
        {
            new() { BucketName = "bucket" }
        };
        A.CallTo(() => apiTasks.GetBucketsAsync()).Returns(buckets);

        await cd.Run(client, "..");
        
        Assert.That(client.CurrentBucket, Is.Null);
        Assert.That(client.CurrentPath, Is.Empty);
    }
    
    [Test]
    public async Task FromSingleDirectoryMoveUpwards()
    {
        var buckets = new List<B2Bucket>
        {
            new() { BucketName = "bucket" }
        };
        client.CurrentBucket = buckets.First();

        await cd.Run(client, "..");
        
        Assert.That(client.CurrentBucket, Is.Null);
        Assert.That(client.CurrentPath, Is.Empty);
    }
    
    [Test]
    public async Task FromNestedDirectoryMoveUpwards()
    {
        var buckets = new List<B2Bucket>
        {
            new() { BucketName = "bucket" }
        };
        client.CurrentBucket = buckets.First();
        client.CurrentPath = "one/two";

        await cd.Run(client, "..");
        
        Assert.That(client.CurrentBucket, Is.EqualTo(buckets.First()));
        Assert.That(client.CurrentPath, Is.EqualTo("one"));
    }
    
    [Test]
    public async Task DirectoryNotFound()
    {
        var buckets = new List<B2Bucket>
        {
            new() { BucketName = "bucket" }
        };
        client.CurrentBucket = buckets.First();
        client.CurrentPath = "one";
        
        var files = new List<B2File>();
        A.CallTo(() => apiTasks.GetFilesAsync(client.CurrentBucket.BucketId, client.CurrentPath)).Returns(files);

        await cd.Run(client, "two");
        
        A.CallTo(() => consoleProvider.WriteLine("Directory two does not exist")).MustHaveHappened();
        
        Assert.That(client.CurrentBucket, Is.EqualTo(buckets.First()));
        Assert.That(client.CurrentPath, Is.EqualTo("one"));
    }
    
    [Test]
    public async Task DirectoryNotFound_FileFoundInstead()
    {
        var buckets = new List<B2Bucket>
        {
            new() { BucketName = "bucket" }
        };
        client.CurrentBucket = buckets.First();
        client.CurrentPath = "one";
        
        var files = new List<B2File>
        {
            new() { FileName = "one/two.txt" }
        };
        A.CallTo(() => apiTasks.GetFilesAsync(client.CurrentBucket.BucketId, client.CurrentPath)).Returns(files);

        await cd.Run(client, "two");
        
        A.CallTo(() => consoleProvider.WriteLine("Directory two does not exist")).MustHaveHappened();
        
        Assert.That(client.CurrentBucket, Is.EqualTo(buckets.First()));
        Assert.That(client.CurrentPath, Is.EqualTo("one"));
    }
    
    [Test]
    public async Task DirectoryFound()
    {
        var buckets = new List<B2Bucket>
        {
            new() { BucketName = "bucket" }
        };
        client.CurrentBucket = buckets.First();
        client.CurrentPath = "one";
        
        var files = new List<B2File>
        {
            new() { FileName = "one/two/", Action = "folder" }
        };
        A.CallTo(() => apiTasks.GetFilesAsync(client.CurrentBucket.BucketId, "one/")).Returns(files);

        await cd.Run(client, "two");
        
        Assert.That(client.CurrentBucket, Is.EqualTo(buckets.First()));
        Assert.That(client.CurrentPath, Is.EqualTo("one/two"));
    }
}