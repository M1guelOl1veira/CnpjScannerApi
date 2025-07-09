using CnpjScanner.Api.Interfaces;
using CnpjScanner.Api.Models;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;

namespace CnpjScanner.Api.Services
{
    public class GitService : IGitService
    {
        public async Task<string> CloneRepoAsync(RepoRequest request)
        {
            var repoName = Path.GetFileNameWithoutExtension(request.RepoUrl);
            var localPath = Path.Combine(Path.GetTempPath(), "CnpjRepoCache", repoName + "_" + Guid.NewGuid());

            var options = new CloneOptions();

            if (!string.IsNullOrEmpty(request.Token))
            {
                options.FetchOptions.CredentialsProvider = (_url, _user, _cred) =>
                    new UsernamePasswordCredentials
                    {
                        Username = request.Username ?? "git",
                        Password = request.Token
                    };
            }

            await Task.Run(() => Repository.Clone(request.RepoUrl, localPath, options));

            return localPath;
        }
    }
}