using System;
using CnpjScanner.Api.Models;

namespace CnpjScanner.Api.Interfaces;

public interface IGitService
{
    Task<string> CloneRepoAsync(RepoRequest request);
}
