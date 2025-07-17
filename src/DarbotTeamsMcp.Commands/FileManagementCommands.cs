using System.Text.Json;
using Microsoft.Extensions.Logging;
using DarbotTeamsMcp.Core.Interfaces;
using DarbotTeamsMcp.Core.Models;
using DarbotTeamsMcp.Core.Services;

namespace DarbotTeamsMcp.Commands.FileManagement;

/// <summary>
/// Upload File Command - File sharing with metadata
/// </summary>
public class UploadFileCommand : TeamsToolBase
{
    public UploadFileCommand(ILogger<TeamsToolBase> logger, ITeamsGraphClient graphClient)
        : base(logger, graphClient)
    {
    }

    public override string Name => "teams-upload-file";
    public override string Description => "Upload a file to the team's SharePoint folder";
    public override TeamsToolCategory Category => TeamsToolCategory.Files;
    public override TeamsPermissionLevel RequiredPermission => TeamsPermissionLevel.Member;

    public override JsonElement InputSchema
    {
        get
        {
            var schema = new
            {
                type = "object",
                properties = new
                {
                    filePath = new
                    {
                        type = "string",
                        description = "Local path to the file to upload"
                    },
                    fileName = new
                    {
                        type = "string",
                        description = "Name for the uploaded file (optional, uses original name)"
                    },
                    channelId = new
                    {
                        type = "string",
                        description = "Channel to upload to (optional, uses current channel)"
                    },
                    description = new
                    {
                        type = "string",
                        description = "Optional file description",
                        maxLength = 500
                    },
                    overwrite = new
                    {
                        type = "boolean",
                        description = "Overwrite existing file with same name",
                        @default = false
                    }
                },
                required = new[] { "filePath" },
                additionalProperties = false
            };

            return JsonSerializer.SerializeToElement(schema);
        }
    }

    protected override async Task<McpResponse> ExecuteToolAsync(
        JsonElement parameters,
        TeamsContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filePath = parameters.GetProperty("filePath").GetString()!;
            var fileName = parameters.TryGetProperty("fileName", out var nameProp) ? 
                nameProp.GetString() : Path.GetFileName(filePath);
            var channelId = parameters.TryGetProperty("channelId", out var channelProp) ? 
                channelProp.GetString() : context.CurrentChannelId;
            var description = parameters.TryGetProperty("description", out var descProp) ? 
                descProp.GetString() : null;
            var overwrite = parameters.TryGetProperty("overwrite", out var overwriteProp) && 
                overwriteProp.GetBoolean();

            if (string.IsNullOrEmpty(channelId))
            {
                return CreateErrorResponse(context.CorrelationId, 400, 
                    "❌ No channel ID provided and no current channel selected");
            }

            // Check if file exists
            if (!File.Exists(filePath))
            {
                return CreateErrorResponse(context.CorrelationId, 404, 
                    $"❌ File not found: {filePath}");
            }

            var fileInfo = new FileInfo(filePath);
            var fileSizeMB = Math.Round(fileInfo.Length / 1024.0 / 1024.0, 2);
            
            // Mock implementation
            _logger.LogInformation("Would upload file {FilePath} as {FileName} to channel {ChannelId}", 
                filePath, fileName, channelId);

            var message = $"📎 **File Upload Started**\n\n" +
                         $"📄 **File:** {fileName}\n" +
                         $"📁 **Channel:** Development\n" +
                         $"📊 **Size:** {fileSizeMB} MB\n" +
                         $"🔄 **Overwrite:** {(overwrite ? "Yes" : "No")}\n" +
                         $"👤 **Uploaded By:** Current User\n" +
                         $"📅 **Started At:** {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC";

            if (!string.IsNullOrEmpty(description))
            {
                message += $"\n📝 **Description:** {description}";
            }

            message += "\n\n⏳ **Status:** Uploading... You will be notified when complete.";

            return CreateSuccessResponse(message, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file");
            return CreateErrorResponse(context.CorrelationId, 500, $"❌ Failed to upload file: {ex.Message}");
        }
    }
}

/// <summary>
/// Download File Command - Secure file retrieval
/// </summary>
public class DownloadFileCommand : TeamsToolBase
{
    public DownloadFileCommand(ILogger<TeamsToolBase> logger, ITeamsGraphClient graphClient)
        : base(logger, graphClient)
    {
    }

    public override string Name => "teams-download-file";
    public override string Description => "Download a file from the team's SharePoint folder";
    public override TeamsToolCategory Category => TeamsToolCategory.Files;
    public override TeamsPermissionLevel RequiredPermission => TeamsPermissionLevel.Member;

    public override JsonElement InputSchema
    {
        get
        {
            var schema = new
            {
                type = "object",
                properties = new
                {
                    fileId = new
                    {
                        type = "string",
                        description = "ID of the file to download"
                    },
                    fileName = new
                    {
                        type = "string",
                        description = "Alternative: file name instead of ID"
                    },
                    downloadPath = new
                    {
                        type = "string",
                        description = "Local path to save the file (optional, uses current directory)"
                    },
                    channelId = new
                    {
                        type = "string",
                        description = "Channel to download from (optional, uses current channel)"
                    }
                },
                additionalProperties = false
            };

            return JsonSerializer.SerializeToElement(schema);
        }
    }

    protected override async Task<McpResponse> ExecuteToolAsync(
        JsonElement parameters,
        TeamsContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fileId = parameters.TryGetProperty("fileId", out var idProp) ? 
                idProp.GetString() : null;
            var fileName = parameters.TryGetProperty("fileName", out var nameProp) ? 
                nameProp.GetString() : null;
            var downloadPath = parameters.TryGetProperty("downloadPath", out var pathProp) ? 
                pathProp.GetString() : Environment.CurrentDirectory;
            var channelId = parameters.TryGetProperty("channelId", out var channelProp) ? 
                channelProp.GetString() : context.CurrentChannelId;

            if (string.IsNullOrEmpty(fileId) && string.IsNullOrEmpty(fileName))
            {
                return CreateErrorResponse(context.CorrelationId, 400, 
                    "❌ Either fileId or fileName must be provided");
            }

            if (string.IsNullOrEmpty(channelId))
            {
                return CreateErrorResponse(context.CorrelationId, 400, 
                    "❌ No channel ID provided and no current channel selected");
            }

            var fileIdentifier = fileId ?? fileName!;
            var mockFileName = fileName ?? "document.pdf";
            var fullDownloadPath = Path.Combine(downloadPath, mockFileName);
            
            // Mock implementation
            _logger.LogInformation("Would download file {FileId} to {DownloadPath}", 
                fileIdentifier, fullDownloadPath);

            var message = $"📥 **File Download Started**\n\n" +
                         $"📄 **File:** {mockFileName}\n" +
                         $"📁 **Channel:** Development\n" +
                         $"💾 **Download Path:** {fullDownloadPath}\n" +
                         $"📊 **Size:** 2.5 MB\n" +
                         $"👤 **Downloaded By:** Current User\n" +
                         $"📅 **Started At:** {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC\n\n" +
                         $"⏳ **Status:** Downloading... File will be saved to specified location.";

            return CreateSuccessResponse(message, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file");
            return CreateErrorResponse(context.CorrelationId, 500, $"❌ Failed to download file: {ex.Message}");
        }
    }
}

/// <summary>
/// Delete File Command - File removal with confirmations
/// </summary>
public class DeleteFileCommand : TeamsToolBase
{
    public DeleteFileCommand(ILogger<TeamsToolBase> logger, ITeamsGraphClient graphClient)
        : base(logger, graphClient)
    {
    }

    public override string Name => "teams-delete-file";
    public override string Description => "Delete a file from the team's SharePoint folder";
    public override TeamsToolCategory Category => TeamsToolCategory.Files;
    public override TeamsPermissionLevel RequiredPermission => TeamsPermissionLevel.Owner;

    public override JsonElement InputSchema
    {
        get
        {
            var schema = new
            {
                type = "object",
                properties = new
                {
                    fileId = new
                    {
                        type = "string",
                        description = "ID of the file to delete"
                    },
                    fileName = new
                    {
                        type = "string",
                        description = "Alternative: file name instead of ID"
                    },
                    channelId = new
                    {
                        type = "string",
                        description = "Channel containing the file (optional, uses current channel)"
                    },
                    reason = new
                    {
                        type = "string",
                        description = "Reason for deletion (for audit log)",
                        maxLength = 500
                    },
                    permanentDelete = new
                    {
                        type = "boolean",
                        description = "Permanently delete (bypass recycle bin)",
                        @default = false
                    }
                },
                additionalProperties = false
            };

            return JsonSerializer.SerializeToElement(schema);
        }
    }

    protected override async Task<McpResponse> ExecuteToolAsync(
        JsonElement parameters,
        TeamsContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fileId = parameters.TryGetProperty("fileId", out var idProp) ? 
                idProp.GetString() : null;
            var fileName = parameters.TryGetProperty("fileName", out var nameProp) ? 
                nameProp.GetString() : null;
            var channelId = parameters.TryGetProperty("channelId", out var channelProp) ? 
                channelProp.GetString() : context.CurrentChannelId;
            var reason = parameters.TryGetProperty("reason", out var reasonProp) ? 
                reasonProp.GetString() : "Administrative deletion";
            var permanentDelete = parameters.TryGetProperty("permanentDelete", out var permProp) && 
                permProp.GetBoolean();

            if (string.IsNullOrEmpty(fileId) && string.IsNullOrEmpty(fileName))
            {
                return CreateErrorResponse(context.CorrelationId, 400, 
                    "❌ Either fileId or fileName must be provided");
            }

            if (string.IsNullOrEmpty(channelId))
            {
                return CreateErrorResponse(context.CorrelationId, 400, 
                    "❌ No channel ID provided and no current channel selected");
            }

            var fileIdentifier = fileId ?? fileName!;
            var mockFileName = fileName ?? "document.pdf";
            
            // Mock implementation
            _logger.LogInformation("Would delete file {FileId} from channel {ChannelId} (permanent: {Permanent})", 
                fileIdentifier, channelId, permanentDelete);

            var message = $"🗑️ **File Deleted**\n\n" +
                         $"📄 **File:** {mockFileName}\n" +
                         $"📁 **Channel:** Development\n" +
                         $"📝 **Reason:** {reason}\n" +
                         $"🗑️ **Deletion Type:** {(permanentDelete ? "Permanent" : "Moved to Recycle Bin")}\n" +
                         $"👤 **Deleted By:** Current User\n" +
                         $"📅 **Deleted At:** {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC";

            if (!permanentDelete)
            {
                message += "\n\n♻️ **Note:** File can be restored from SharePoint Recycle Bin within 93 days.";
            }

            return CreateSuccessResponse(message, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file");
            return CreateErrorResponse(context.CorrelationId, 500, $"❌ Failed to delete file: {ex.Message}");
        }
    }
}

/// <summary>
/// List Files Command - File discovery and organization
/// </summary>
public class ListFilesCommand : TeamsToolBase
{
    public ListFilesCommand(ILogger<TeamsToolBase> logger, ITeamsGraphClient graphClient)
        : base(logger, graphClient)
    {
    }

    public override string Name => "teams-list-files";
    public override string Description => "List files in the team's SharePoint folder";
    public override TeamsToolCategory Category => TeamsToolCategory.Files;
    public override TeamsPermissionLevel RequiredPermission => TeamsPermissionLevel.Member;

    public override JsonElement InputSchema
    {
        get
        {
            var schema = new
            {
                type = "object",
                properties = new
                {
                    channelId = new
                    {
                        type = "string",
                        description = "Channel to list files from (optional, uses current channel)"
                    },
                    folderPath = new
                    {
                        type = "string",
                        description = "Specific folder path to list (optional, lists root)"
                    },
                    fileType = new
                    {
                        type = "string",
                        description = "Filter by file type extension (e.g., 'pdf', 'docx')"
                    },
                    sortBy = new
                    {
                        type = "string",
                        description = "Sort files by criteria",
                        @enum = new[] { "name", "size", "modified", "created" },
                        @default = "modified"
                    },
                    sortOrder = new
                    {
                        type = "string",
                        description = "Sort order",
                        @enum = new[] { "asc", "desc" },
                        @default = "desc"
                    },
                    maxResults = new
                    {
                        type = "integer",
                        description = "Maximum number of files to return",
                        @default = 50,
                        minimum = 1,
                        maximum = 200
                    }
                },
                additionalProperties = false
            };

            return JsonSerializer.SerializeToElement(schema);
        }
    }

    protected override async Task<McpResponse> ExecuteToolAsync(
        JsonElement parameters,
        TeamsContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var channelId = parameters.TryGetProperty("channelId", out var channelProp) ? 
                channelProp.GetString() : context.CurrentChannelId;
            var folderPath = parameters.TryGetProperty("folderPath", out var folderProp) ? 
                folderProp.GetString() : null;
            var fileType = parameters.TryGetProperty("fileType", out var typeProp) ? 
                typeProp.GetString() : null;
            var sortBy = parameters.TryGetProperty("sortBy", out var sortProp) ? 
                sortProp.GetString() ?? "modified" : "modified";
            var sortOrder = parameters.TryGetProperty("sortOrder", out var orderProp) ? 
                orderProp.GetString() ?? "desc" : "desc";
            var maxResults = parameters.TryGetProperty("maxResults", out var maxProp) ? 
                Math.Min(maxProp.GetInt32(), 200) : 50;

            if (string.IsNullOrEmpty(channelId))
            {
                return CreateErrorResponse(context.CorrelationId, 400, 
                    "❌ No channel ID provided and no current channel selected");
            }

            // Mock file data
            var mockFiles = new[]
            {
                new { name = "Project_Proposal.docx", size = "1.2 MB", type = "docx", 
                      modified = "2024-01-15 14:30", author = "John Doe", 
                      url = "https://company.sharepoint.com/sites/team/file1" },
                new { name = "Budget_Analysis.xlsx", size = "856 KB", type = "xlsx", 
                      modified = "2024-01-14 16:45", author = "Jane Smith", 
                      url = "https://company.sharepoint.com/sites/team/file2" },
                new { name = "Presentation.pptx", size = "5.3 MB", type = "pptx", 
                      modified = "2024-01-13 09:15", author = "Bob Wilson", 
                      url = "https://company.sharepoint.com/sites/team/file3" },
                new { name = "Technical_Spec.pdf", size = "2.1 MB", type = "pdf", 
                      modified = "2024-01-12 11:20", author = "Alice Johnson", 
                      url = "https://company.sharepoint.com/sites/team/file4" }
            };

            // Apply filters
            var filteredFiles = mockFiles.AsEnumerable();
            if (!string.IsNullOrEmpty(fileType))
            {
                filteredFiles = filteredFiles.Where(f => f.type.Equals(fileType, StringComparison.OrdinalIgnoreCase));
            }

            var limitedFiles = filteredFiles.Take(maxResults).ToList();

            var filesList = string.Join("\n\n", limitedFiles.Select((file, i) =>
                $"{i + 1}. 📄 **{file.name}**\n" +
                $"   📊 {file.size} • 🏷️ {file.type.ToUpper()}\n" +
                $"   👤 {file.author} • 📅 {file.modified}\n" +
                $"   🔗 {file.url}"));

            var locationInfo = string.IsNullOrEmpty(folderPath) ? "Root folder" : folderPath;
            var filterInfo = !string.IsNullOrEmpty(fileType) ? $" (filtered by .{fileType})" : "";

            var message = $"📁 **Team Files** ({limitedFiles.Count} files{filterInfo})\n\n" +
                         $"📂 **Location:** {locationInfo}\n" +
                         $"📊 **Sorted By:** {sortBy} ({sortOrder})\n" +
                         $"📁 **Channel:** Development\n\n{filesList}";

            return CreateSuccessResponse(message, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list files");
            return CreateErrorResponse(context.CorrelationId, 500, $"❌ Failed to list files: {ex.Message}");
        }
    }
}
