using REBUSS.Pure.Cli;

namespace REBUSS.Pure.Tests.Cli;

public class InitCommandTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsError_WhenNotInGitRepository()
    {
        var output = new StringWriter();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var command = new InitCommand(output, tempDir, "rebuss-pure.exe");

            var exitCode = await command.ExecuteAsync();

            Assert.Equal(1, exitCode);
            Assert.Contains("Not inside a Git repository", output.ToString());
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_CreatesVsCodeMcpJson_InGitRepositoryRoot()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var gitDir = Path.Combine(tempDir, ".git");
        Directory.CreateDirectory(gitDir);

        try
        {
            var output = new StringWriter();
            var command = new InitCommand(output, tempDir, @"C:\tools\REBUSS.Pure.exe");

            var exitCode = await command.ExecuteAsync();

            Assert.Equal(0, exitCode);

            var mcpConfigPath = Path.Combine(tempDir, ".vscode", "mcp.json");
            Assert.True(File.Exists(mcpConfigPath));

            var content = await File.ReadAllTextAsync(mcpConfigPath);
            Assert.Contains("REBUSS.Pure", content);
            Assert.Contains("--repo", content);
            Assert.Contains("${workspaceFolder}", content);
            Assert.Contains(@"C:\\tools\\REBUSS.Pure.exe", content);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsError_WhenConfigAlreadyExists()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var gitDir = Path.Combine(tempDir, ".git");
        var vsCodeDir = Path.Combine(tempDir, ".vscode");
        Directory.CreateDirectory(gitDir);
        Directory.CreateDirectory(vsCodeDir);
        await File.WriteAllTextAsync(Path.Combine(vsCodeDir, "mcp.json"), "{}");

        try
        {
            var output = new StringWriter();
            var command = new InitCommand(output, tempDir, "rebuss-pure.exe");

            var exitCode = await command.ExecuteAsync();

            Assert.Equal(1, exitCode);
            Assert.Contains("already exists", output.ToString());
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WorksFromSubdirectory_FindsGitRoot()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var gitDir = Path.Combine(tempDir, ".git");
        var subDir = Path.Combine(tempDir, "src", "app");
        Directory.CreateDirectory(gitDir);
        Directory.CreateDirectory(subDir);

        try
        {
            var output = new StringWriter();
            var command = new InitCommand(output, subDir, "rebuss-pure.exe");

            var exitCode = await command.ExecuteAsync();

            Assert.Equal(0, exitCode);

            var mcpConfigPath = Path.Combine(tempDir, ".vscode", "mcp.json");
            Assert.True(File.Exists(mcpConfigPath));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void BuildConfigContent_ProducesValidJsonStructure()
    {
        var content = InitCommand.BuildConfigContent("C:\\\\tools\\\\REBUSS.Pure.exe");

        Assert.Contains("\"REBUSS.Pure\"", content);
        Assert.Contains("\"stdio\"", content);
        Assert.Contains("\"--repo\"", content);
        Assert.Contains("\"${workspaceFolder}\"", content);
        Assert.DoesNotContain("--pat", content);
    }

    [Fact]
    public void BuildConfigContent_IncludesPat_WhenProvided()
    {
        var content = InitCommand.BuildConfigContent("C:\\\\tools\\\\REBUSS.Pure.exe", "my-secret-pat");

        Assert.Contains("\"--pat\"", content);
        Assert.Contains("\"my-secret-pat\"", content);
    }

    [Fact]
    public void BuildConfigContent_OmitsPat_WhenNullOrEmpty()
    {
        var contentNull  = InitCommand.BuildConfigContent("exe", null);
        var contentEmpty = InitCommand.BuildConfigContent("exe", "");
        var contentWhite = InitCommand.BuildConfigContent("exe", "   ");

        Assert.DoesNotContain("--pat", contentNull);
        Assert.DoesNotContain("--pat", contentEmpty);
        Assert.DoesNotContain("--pat", contentWhite);
    }

    [Fact]
    public async Task ExecuteAsync_WithPat_IncludesPatInConfig()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var gitDir = Path.Combine(tempDir, ".git");
        Directory.CreateDirectory(gitDir);

        try
        {
            var output = new StringWriter();
            var command = new InitCommand(output, tempDir, "rebuss-pure.exe", "my-pat-value");

            var exitCode = await command.ExecuteAsync();

            Assert.Equal(0, exitCode);

            var content = await File.ReadAllTextAsync(Path.Combine(tempDir, ".vscode", "mcp.json"));
            Assert.Contains("\"--pat\"", content);
            Assert.Contains("\"my-pat-value\"", content);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithoutPat_OmitsPatFromConfig()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var gitDir = Path.Combine(tempDir, ".git");
        Directory.CreateDirectory(gitDir);

        try
        {
            var output = new StringWriter();
            var command = new InitCommand(output, tempDir, "rebuss-pure.exe");

            var exitCode = await command.ExecuteAsync();

            Assert.Equal(0, exitCode);

            var content = await File.ReadAllTextAsync(Path.Combine(tempDir, ".vscode", "mcp.json"));
            Assert.DoesNotContain("--pat", content);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_CopiesPromptFiles_ToGitHubPromptsDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var gitDir = Path.Combine(tempDir, ".git");
        Directory.CreateDirectory(gitDir);

        try
        {
            var output = new StringWriter();
            var command = new InitCommand(output, tempDir, "rebuss-pure.exe");

            var exitCode = await command.ExecuteAsync();

            Assert.Equal(0, exitCode);

            var reviewPrPath = Path.Combine(tempDir, ".github", "prompts", "review-pr.prompt.md");
            var selfReviewPath = Path.Combine(tempDir, ".github", "prompts", "self-review.prompt.md");

            Assert.True(File.Exists(reviewPrPath), $"Expected prompt file at {reviewPrPath}");
            Assert.True(File.Exists(selfReviewPath), $"Expected prompt file at {selfReviewPath}");

            var reviewPrContent = await File.ReadAllTextAsync(reviewPrPath);
            Assert.Contains("Pull Request Code Review", reviewPrContent);
            Assert.Contains("REBUSS.Pure", reviewPrContent);

            var selfReviewContent = await File.ReadAllTextAsync(selfReviewPath);
            Assert.Contains("Self-Review", selfReviewContent);
            Assert.Contains("get_local_files", selfReviewContent);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_SkipsPromptFiles_WhenAlreadyExist()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var gitDir = Path.Combine(tempDir, ".git");
        var promptsDir = Path.Combine(tempDir, ".github", "prompts");
        Directory.CreateDirectory(gitDir);
        Directory.CreateDirectory(promptsDir);

        var existingContent = "# My custom review prompt";
        await File.WriteAllTextAsync(Path.Combine(promptsDir, "review-pr.prompt.md"), existingContent);

        try
        {
            var output = new StringWriter();
            var command = new InitCommand(output, tempDir, "rebuss-pure.exe");

            var exitCode = await command.ExecuteAsync();

            Assert.Equal(0, exitCode);

            var reviewPrContent = await File.ReadAllTextAsync(Path.Combine(promptsDir, "review-pr.prompt.md"));
            Assert.Equal(existingContent, reviewPrContent);

            var selfReviewPath = Path.Combine(promptsDir, "self-review.prompt.md");
            Assert.True(File.Exists(selfReviewPath));

            Assert.Contains("already exists, skipping", output.ToString());
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_OutputMentionsPromptCopy()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var gitDir = Path.Combine(tempDir, ".git");
        Directory.CreateDirectory(gitDir);

        try
        {
            var output = new StringWriter();
            var command = new InitCommand(output, tempDir, "rebuss-pure.exe");

            await command.ExecuteAsync();

            var outputText = output.ToString();
            Assert.Contains("Copied", outputText);
            Assert.Contains("prompt file(s)", outputText);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_CopiesPrompts_FromSubdirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var gitDir = Path.Combine(tempDir, ".git");
        var subDir = Path.Combine(tempDir, "src", "app");
        Directory.CreateDirectory(gitDir);
        Directory.CreateDirectory(subDir);

        try
        {
            var output = new StringWriter();
            var command = new InitCommand(output, subDir, "rebuss-pure.exe");

            var exitCode = await command.ExecuteAsync();

            Assert.Equal(0, exitCode);

            var reviewPrPath = Path.Combine(tempDir, ".github", "prompts", "review-pr.prompt.md");
            Assert.True(File.Exists(reviewPrPath));
            Assert.False(File.Exists(Path.Combine(subDir, ".github", "prompts", "review-pr.prompt.md")));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
