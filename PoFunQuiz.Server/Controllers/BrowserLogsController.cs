using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.IO;
using System.Text.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PoFunQuiz.Server.Controllers
{
    [ApiController]
    [Route("api/browserlogs")]
    public class BrowserLogsController : ControllerBase
    {
        private static readonly string RootDirectory =
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\.."));

        private static readonly string DebugDirectory = Path.Combine(RootDirectory, "DEBUG");
        private static readonly string ConsoleLogFile = Path.Combine(DebugDirectory, "browser-console.txt");
        private static readonly string NetworkLogFile = Path.Combine(DebugDirectory, "network.txt");

        // semaphore to serialize file writes and avoid sharing violations
        private static readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);

        public BrowserLogsController()
        {
            try
            {
                if (!Directory.Exists(DebugDirectory))
                {
                    Directory.CreateDirectory(DebugDirectory);
                }
            }
            catch (Exception ex)
            {
                // ensure we don't throw during controller construction
                Console.WriteLine($"Warning: Could not create DEBUG directory: {ex.Message}");
            }
        }

        [HttpPost("console")]
        public async Task<IActionResult> PostConsoleLog([FromBody] JsonElement payload)
        {
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = false }) + Environment.NewLine;

            await _fileLock.WaitAsync();
            try
            {
                // Open file with FileShare.ReadWrite so other processes (or readers) do not lock us out.
                using var fs = new FileStream(ConsoleLogFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite, 4096, useAsync: true);
                fs.Seek(0, SeekOrigin.End);
                var bytes = Encoding.UTF8.GetBytes(json);
                await fs.WriteAsync(bytes, 0, bytes.Length);
                await fs.FlushAsync();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to write browser console log to file");
                return StatusCode(500);
            }
            finally
            {
                _fileLock.Release();
            }

            return Ok();
        }

        [HttpPost("network")]
        public async Task<IActionResult> PostNetworkLog([FromBody] JsonElement payload)
        {
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = false }) + Environment.NewLine;

            await _fileLock.WaitAsync();
            try
            {
                using var fs = new FileStream(NetworkLogFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite, 4096, useAsync: true);
                fs.Seek(0, SeekOrigin.End);
                var bytes = Encoding.UTF8.GetBytes(json);
                await fs.WriteAsync(bytes, 0, bytes.Length);
                await fs.FlushAsync();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to write browser network log to file");
                return StatusCode(500);
            }
            finally
            {
                _fileLock.Release();
            }

            return Ok();
        }
    }
}
