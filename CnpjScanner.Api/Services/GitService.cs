using CnpjScanner.Api.Interfaces;
using CnpjScanner.Api.Models;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;

namespace CnpjScanner.Api.Services
{
    public class GitService(IConfiguration configuration) : IGitService
    {
        private readonly IConfiguration configuration = configuration;

        public async Task<string> CloneRepoAsync(RepoRequest request)
        {
            var repoName = Path.GetFileNameWithoutExtension(request.RepoUrl);
            var localPath = Path.Combine(request.DirToClone, repoName);
            if (!Directory.Exists(localPath))
            {
                var options = new CloneOptions();

                options.FetchOptions.CredentialsProvider = (_url, _user, _cred) =>
                    new UsernamePasswordCredentials
                    {
                        Username = "git",   
                        Password = configuration["Git:Token"]
                    };

                await Task.Run(() => Repository.Clone(request.RepoUrl, localPath, options));
            }
            return localPath;
        }
    }
}