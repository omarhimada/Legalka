namespace Eloi.Services {
    using System.Diagnostics;

    public sealed class EnsureOllamaBuildsEloiService : IHostedService {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<EnsureOllamaBuildsEloiService> _log;

        public EnsureOllamaBuildsEloiService(IWebHostEnvironment env, ILogger<EnsureOllamaBuildsEloiService> log) {
            _env = env;
            _log = log;
        }

        /// <summary>
        /// Starts the model build process asynchronously if the application is running in the development environment.
        /// </summary>
        /// <remarks>This method only performs the model build operation when the application is running
        /// in the development environment. If the model file has not changed since the last build, the operation is
        /// skipped. Logging information is provided for build events and completion status.</remarks>
        /// <param name="ct">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="Exception">Thrown if the required model file is not found in the content root path.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the model build process fails to complete successfully.</exception>
        public async Task StartAsync(CancellationToken ct) {
            if (!_env.IsDevelopment())
                return;

            string modelfile = Path.Combine(_env.ContentRootPath, Constants._eloiModelfile);
            if (!File.Exists(modelfile)) {
                throw new Exception($"{Constants._eloiModelfile} not found.");
            }

            // Hash guard file stored alongside the modelfile (project root)
            string hashPath = Path.Combine(_env.ContentRootPath, Constants._hashedModelfile);

            string currentHash = Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(modelfile))
            );

            string? lastHash = File.Exists(hashPath) ? File.ReadAllText(hashPath) : null;

            if (string.Equals(currentHash, lastHash, StringComparison.OrdinalIgnoreCase)) {
                _log.LogInformation($"{Constants._eloiModelfile} changed.");
                return;
            }

            _log.LogInformation($"{Constants._eloiModelfile} changed. Rebuilding '{Constants._eloi}' from {modelfile}.");

            ProcessStartInfo psi = new() {
                FileName = Constants._ollamaExecutableName,
                Arguments = $"{Constants._ollamaCreateArgument} \"{modelfile}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process process = Process.Start(psi)!;
            string stdout = await process.StandardOutput.ReadToEndAsync(ct);
            string stderr = await process.StandardError.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);

            if (process.ExitCode != 0) {
                throw new InvalidOperationException($"Failure: {stderr}");
            }

            _log.LogInformation($"Completed. {stdout}");
        }

        public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
