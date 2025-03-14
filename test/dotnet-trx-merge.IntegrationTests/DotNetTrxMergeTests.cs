using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO.Abstractions;
using dotnet_test_rerun.IntegrationTests.Utilities;
using dotnet_trx_merge.Commands;
using dotnet_trx_merge.Commands.Configurations;
using dotnet_trx_merge.Logging;
using dotnet_trx_merge.Services;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace dotnet_test_rerun.IntegrationTests;

public class DotNetTrxMergeTests
{
    private readonly ITestOutputHelper TestOutputHelper;
    private static string _dir = TestUtilities.GetTmpDirectory();
    private static readonly IFileSystem FileSystem = new FileSystem();

    public DotNetTrxMergeTests(ITestOutputHelper testOutputHelper)
    {
        TestOutputHelper = testOutputHelper;
        TestUtilities.CopyFixture(string.Empty, new DirectoryInfo(_dir));
    }

    [Fact]
    public void DotnetTrxMerge_WithSelectingFiles_Success()
    {
        // Arrange
        Environment.ExitCode = 0;

        // Act
        var output = RunDotNetTestRerunAndCollectOutputMessage("MergeWithTwoFilesAllPass",
            $"-f {_dir}/MergeOnePassOneFailed/TrxAllPass.trx -f {_dir}/MergeOnePassOneFailed/TrxWithFailures.trx");

        // Assert
        Environment.ExitCode.Should().Be(0);
        output.Should().Contain("Found 2 files to merge");
        output.Should().Contain("Found 1 tests in file");
        output.Should().Contain("Found 2 tests in file");
        output.Should().Contain("New result of test 3dacbae9-707e-1881-d63c-3573123dffc6 was found in file ");
    }

    [Fact]
    public void DotnetTrxMerge_WithSelectingDir_Success()
    {
        // Arrange
        Environment.ExitCode = 0;

        // Act
        var output = RunDotNetTestRerunAndCollectOutputMessage("MergeWithTwoFilesAllPass",
            $"-d {_dir}/MergeWithTwoFilesAllPass/");

        // Assert
        Environment.ExitCode.Should().Be(0);
        output.Should().Contain("Found 2 files to merge");
        output.Should().Contain("Found 1 tests in file");
        output.Should().Contain("Found 1 tests in file");
    }

    [Fact]
    public void DotnetTrxMerge_WithNoFileFound_Success()
    {
        // Arrange
        Environment.ExitCode = 0;

        // Act
        var output = RunDotNetTestRerunAndCollectOutputMessage("MergeWithRecursiveOption",
            $"-d {_dir}/MergeWithRecursiveOption/");

        // Assert
        Environment.ExitCode.Should().Be(0);
        output.Should().Contain("Found 0 files to merge");
        output.Should().NotContain(".trx was saved");
    }

    [Fact]
    public void DotnetTrxMerge_WithRecursiveOption_Success()
    {
        // Arrange
        Environment.ExitCode = 0;

        // Act
        var output = RunDotNetTestRerunAndCollectOutputMessage("MergeWithRecursiveOption",
            $"-d {_dir}/MergeWithRecursiveOption/ -r");

        // Assert
        Environment.ExitCode.Should().Be(0);
        output.Should().Contain("Found 2 files to merge");
        output.Should().Contain("Found 1 tests in file");
        output.Should().Contain("Found 1 tests in file");
    }
    
    [Fact]
    public async Task DotnetTrxMerge_WithOutputFolder_Success()
    {
        // Arrange
        Environment.ExitCode = 0;
        var outputFile = $"{_dir}/MergeWithRecursiveOption/mergeDocument.trx";

        // Act
        var _ = RunDotNetTestRerunAndCollectOutputMessage("MergeWithRecursiveOption",
            $"-d {_dir}/MergeWithRecursiveOption/ -r -o {outputFile}");

        // Assert
        Environment.ExitCode.Should().Be(0);
        File.Exists(outputFile).Should().BeTrue();
        var text = await File.ReadAllTextAsync(outputFile);
        text.Should().Contain("<Counters total=\"1\" passed=\"1\" failed=\"0\" />");
        text.Should().Contain("testId=\"86e2b6e4-df7a-e4fa-006e-c056c908e219\"");
        text.Should().Contain("testName=\"SecondSimpleNumberCompare\"");
    }
    
    
    private string RunDotNetTestRerunAndCollectOutputMessage(string proj, string args = "")
    {
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        var logger = new Logger();
        logger.SetLogLevel(LogLevel.Debug);

        Command command = new Command("trx-merge");
        MergeCommandConfiguration mergeCommandConfiguration = new MergeCommandConfiguration();
        mergeCommandConfiguration.Set(command);

        ParseResult result =
            new Parser(command).Parse($"{args}");
        InvocationContext context = new(result);

        mergeCommandConfiguration.GetValues(context);

        MergeCommand mergeCommand = new MergeCommand(logger,
            mergeCommandConfiguration,
            new TrxFetcher(logger));

        mergeCommand.Run();

        return stringWriter.ToString().Trim();
    }

}