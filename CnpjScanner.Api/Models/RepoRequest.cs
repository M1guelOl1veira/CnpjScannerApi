using System;

namespace CnpjScanner.Api.Models;

public class RepoRequest
{
    public required string Repo { get; set; }
    public string[]? Extensions { get; set; }
    public required string DirToClone { get; set; }

}
