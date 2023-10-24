using B2Terminal.Commands;
using B2Net.Models;
using FakeItEasy;
using Spectre.Console;

namespace B2Terminal.Tests;

public class LSTests
{
    private IAPITasks apiTasks;
    private IConsoleProvider consoleProvider;
    private LS ls;
    private Client client;
    
    [SetUp]
    public void Setup()
    {
        apiTasks = A.Fake<IAPITasks>();
        consoleProvider = A.Fake<IConsoleProvider>();
        ls = new LS(apiTasks, consoleProvider);
        client = new Client(Enumerable.Empty<ICommand>(), consoleProvider, apiTasks);
    }
    
    [Test]
    public async Task FromRootNoBuckets()
    {
        var buckets = new List<B2Bucket>();
        A.CallTo(() => apiTasks.GetBucketsAsync()).Returns(buckets);
        
        await ls.Run(client, "");
        
        A.CallTo(() => consoleProvider.WriteLine(A<string>.Ignored)).MustNotHaveHappened();
    }

    [Test]
    public async Task FromRootHasBuckets()
    {
        var buckets = new List<B2Bucket>
        {
            new() { BucketName = "bucket one" },
            new() { BucketName = "bucket two" }
        };
        A.CallTo(() => apiTasks.GetBucketsAsync()).Returns(buckets);
        
        await ls.Run(client, "");
        
        A.CallTo(() => consoleProvider.WriteLine("bucket one")).MustHaveHappened();
        A.CallTo(() => consoleProvider.WriteLine("bucket two")).MustHaveHappened();
    }

    [Test]
    public async Task FromBucketEmpty()
    {
        var buckets = new List<B2Bucket>
        {
            new() { BucketName = "bucket one" },
        };
        client.CurrentBucket = buckets.First();
        var files = new List<B2File>();
        A.CallTo(() => apiTasks.GetFilesAsync(client.CurrentBucket.BucketId, "")).Returns(files);
        
        await ls.Run(client, "");
        
        A.CallTo(() => consoleProvider.WriteLine(A<string>.Ignored)).MustNotHaveHappened();
    }

    [Test]
    public async Task FromDirectoryEmpty()
    {
        // Shouldn't technically be possible as B2 doesn't allow empty directories
        var buckets = new List<B2Bucket>
        {
            new() { BucketName = "bucket one" },
        };
        client.CurrentBucket = buckets.First();
        var files = new List<B2File>();
        client.CurrentPath = "one";
        A.CallTo(() => apiTasks.GetFilesAsync(client.CurrentBucket.BucketId, "one/")).Returns(files);
        
        var mockConsole = new TestConsole();
        A.CallTo(() => consoleProvider.WriteGrid(A<Grid>.Ignored)).Invokes((Grid g) => mockConsole.Write(g));

        await ls.Run(client, "");
        
        var expectedOutput = new[]
        {
            "Size  Name"
        };
        Assert.That(mockConsole.Lines, Is.EquivalentTo(expectedOutput));
    }

    [Test]
    public async Task FromDirectoryContainsFileWithSameName()
    {
        var buckets = new List<B2Bucket>
        {
            new() { BucketName = "bucket one" },
        };
        client.CurrentBucket = buckets.First();
        client.CurrentPath = "one";
        var files = new List<B2File>
        {
            new() { FileName = "one/one", Action = "upload", ContentLength = "123" },
        };
        A.CallTo(() => apiTasks.GetFilesAsync(client.CurrentBucket.BucketId, "one/")).Returns(files);
        
        var mockConsole = new TestConsole();
        A.CallTo(() => consoleProvider.WriteGrid(A<Grid>.Ignored)).Invokes((Grid g) => mockConsole.Write(g));

        await ls.Run(client, "");
        
        var expectedOutput = new[]
        {
            "Size   Name",
            "123 B  one "
        };
        Assert.That(mockConsole.Lines, Is.EquivalentTo(expectedOutput));
    }

    [Test]
    public async Task FromDirectoryOnlyFiles()
    {
        var buckets = new List<B2Bucket>
        {
            new() { BucketName = "bucket one" },
        };
        client.CurrentBucket = buckets.First();
        client.CurrentPath = "one";
        var files = new List<B2File>
        {
            new() { FileName = "one/file one", Action = "upload", ContentLength = "123" },
            new() { FileName = "one/file two", Action = "upload", ContentLength = "444" },
        };
        A.CallTo(() => apiTasks.GetFilesAsync(client.CurrentBucket.BucketId, "one/")).Returns(files);
        
        var mockConsole = new TestConsole();
        A.CallTo(() => consoleProvider.WriteGrid(A<Grid>.Ignored)).Invokes((Grid g) => mockConsole.Write(g));

        await ls.Run(client, "");
        
        var expectedOutput = new[]
        {
            "Size   Name    ",
            "123 B  file one",
            "444 B  file two"
        };
        Assert.That(mockConsole.Lines, Is.EquivalentTo(expectedOutput));
    }

    [Test]
    public async Task FromDirectoryOnlyDirectories()
    {
        var buckets = new List<B2Bucket>
        {
            new() { BucketName = "bucket one" },
        };
        client.CurrentBucket = buckets.First();
        client.CurrentPath = "one";
        var files = new List<B2File>
        {
            new() { FileName = "one/two/", Action = "folder" },
            new() { FileName = "one/three/", Action = "folder" },
        };
        A.CallTo(() => apiTasks.GetFilesAsync(client.CurrentBucket.BucketId, "one/")).Returns(files);
        
        var mockConsole = new TestConsole();
        A.CallTo(() => consoleProvider.WriteGrid(A<Grid>.Ignored)).Invokes((Grid g) => mockConsole.Write(g));

        await ls.Run(client, "");
        
        var expectedOutput = new[]
        {
            "Size  Name  ",
            "-     two/  ",
            "-     three/"
        };
        Assert.That(mockConsole.Lines, Is.EquivalentTo(expectedOutput));
    }
}