namespace HangfireDemo.Jobs
{
    public class TestJob
    {
        // ILogger is a default injected dependency in dotnet core
        private readonly ILogger _logger;

        // to access this dependency, retrieve it from the class Constructor and assign it to a read-only object
        public TestJob(ILogger<TestJob> logger) => _logger = logger;

        public void WriteLog(string logMessage)
        {
            _logger.LogInformation($"{DateTime.Now:yyyy-MM-dd hh:mm:ss tt} {logMessage}");
        }
    }
}
