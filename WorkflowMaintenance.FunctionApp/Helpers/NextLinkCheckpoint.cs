using Microsoft.Extensions.Logging;

namespace WorkflowMaintenance.FunctionApp.Helpers
{
    //TODO: Persist NextLink in Azure Storage (Blob/Table)
    public class NextLinkCheckpoint(ILogger logger)
    {
        private string FilePath = Path.Combine(Environment.GetEnvironmentVariable("HOME"), "site", "wwwroot", "nextlink.txt");

        /// <summary>
        /// Saves the nextLink URL to a file asynchronously.
        /// </summary>
        public async Task SaveNextLinkAsync(string nextLink)
        {
            try
            {
                // Ensure directory exists
                var dir = Path.GetDirectoryName(FilePath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                await File.WriteAllTextAsync(FilePath, nextLink ?? string.Empty);
                logger.LogInformation($"Saved nextLink to file: {nextLink}");
            }
            catch (Exception ex)
            {
                logger.LogInformation($"Error saving nextLink to file: {ex.Message}");
            }
        }

        /// <summary>
        /// Reads the saved nextLink from file asynchronously. Returns null if not found or empty.
        /// </summary>
        public async Task<string> ReadNextLinkAsync()
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    logger.LogInformation("Checkpoint file not found, starting from initial URL.");
                    return null;
                }

                var text = await File.ReadAllTextAsync(FilePath);
                if (string.IsNullOrWhiteSpace(text))
                {
                    logger.LogInformation("Checkpoint file is empty, starting from initial URL.");
                    return null;
                }

                logger.LogInformation($"Loaded nextLink from file: {text}");
                return text;
            }
            catch (Exception ex)
            {
                logger.LogInformation($"Error reading nextLink from file: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Deletes the checkpoint file.
        /// </summary>
        public void ClearCheckpoint()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    File.Delete(FilePath);
                    logger.LogInformation("Checkpoint file deleted.");
                }
            }
            catch (Exception ex)
            {
                logger.LogInformation($"Error deleting checkpoint file: {ex.Message}");
            }
        }
    }

}
