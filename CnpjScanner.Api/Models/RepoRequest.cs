using System;

namespace CnpjScanner.Api.Models;

public class RepoRequest : PagingParams
{
    public required string RepoUrl { get; set; }
    public string[]? Extensions { get; set; }
    public string? Username { get; set; }
    public string? Token { get; set; }

}
