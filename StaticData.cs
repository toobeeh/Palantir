using System;
using System.IO;
using LibGit2Sharp;

namespace Palantir
{
    internal class StaticData
    {
        public static void AddFile(string filePath, string repoSavePath)
        {
            // Clone the repository
            var repoPath = Program.CacheDataPath + "/repo-cache/static-data" + DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();

            // Clone the repository
            var cloneOptions = new CloneOptions();
            cloneOptions.CredentialsProvider = (_url, _user, _cred) =>
                new UsernamePasswordCredentials { Username = Program.GithubToken, Password = string.Empty };
            Repository.Clone(Program.DataRepoUrl, repoPath, cloneOptions);

            // Open the repository
            using (var repo = new Repository(repoPath))
            {
                // Create a new file in the repository

                var fileName = Path.GetFileName(filePath);
                var repoFilePath = Path.Combine(repoSavePath, fileName);
                File.Copy(filePath, Path.Combine(repo.Info.WorkingDirectory, repoFilePath));

                // Stage the file
                LibGit2Sharp.Commands.Stage(repo, repoSavePath);

                // Create the commit
                var author = new Signature("Palantir Data Commit", "dev.tobeh@gmail.com", DateTimeOffset.Now);
                var committer = author;
                var commit = repo.Commit("Automated commit of data via Palantir command", author, committer);

                // Set the remote repository URL
                var remote = repo.Network.Remotes["origin"];
                var options = new PushOptions();
                options.CredentialsProvider = (_url, _user, _cred) =>
                    new UsernamePasswordCredentials { Username = Program.GithubToken, Password = string.Empty };

                // Push the commit to the remote repository
                repo.Network.Push(remote, $"refs/heads/{repo.Head.FriendlyName}", options);
            }

            // Clean up the temporary repository
            Directory.Delete(repoPath, true);
        }
    }
}
