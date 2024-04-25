# Hangfire Demo

## Hangfire Tables
* AggregatedCounter - tracks metrics related to background job processing (e.g. number enqueued, processed, succeeded, failed) and information such as counter name, value, and timestamp
* Counter - stores individual counter values for various events or metrics
* Hash - stores key value pairs associated with background jobs (usually to store job parameters or metadata)
* Job - stores information about background jobs (e.g. id, type/method name, arguments, queue, timestamp, etc.)
* JobParameter - stores additional usually custom parameters associated with background jobs
* JobQueue - maintains queue for background jobs, and stores information about the queues and the jobs enqueued in each queue
* List - stores collections of items in a specific order (e.g. background jobs waiting to be processed), each list belongs to a specific queue
* Schema - stores information about the database schema
* Server - stores information about Hangfire servers (e.g. id, name, timestamp, etc.)
* Set - data structures that store collections of unique items used for distributed locks or unique job ids
* State - stores the state of background jobs (i.e. enqueued, processing, succeeded, failed), the timestamp of when the state was created, and any additional data related to the state

## Program.cs
* Add Hangfire using AddHangfire method, then specify and use the database connection string for Hangfire
* Before starting the application, manually create the HangfireDemo.Dev database using SSMS - hangfire will automatically create the necessary tables within the database

```
builder.Services.AddHangfire((sp, config) =>
{
    var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("DbConnection");
    config.UseSqlServerStorage(connectionString);
});
```
* Ensure the Hangfire server is added to dependency services

```
builder.Services.AddHangfireServer();
```
* The Hangfire dashboard allows us to view the details of our jobs (/hangfire is the default location)
* In the dashboard, you can view the jobs being executed in real time in the graph on the home page, view jobs by their statuses, and view additional details about recurring jobs, such as its cron statement
* You can also customize the Hangfire dashboard by passing additional parameters (e.g. a custom path, setting a custom title, disable dark mode, hide the Database name in the footer, enabling authentication, etc.)
    * To enable authorization, install the Hangfire.Dashboard.Basic.Authentication library, which allows you to specify a username and password for your dashboard
```
app.UseHangfireDashboard("/test/job-dashboard", new DashboardOptions
{
    DashboardTitle = "Hangfire Job Demo Application",
    DarkModeEnabled = false,
    DisplayStorageConnectionString = false,
    Authorization = new[]
    {
        new HangfireCustomBasicAuthenticationFilter
        {
            User = "admin",
            Pass = "admin123"
        }
    }
});
```

## Controllers.cs
* All routes are 
* A background job can be used for performing resource-intensive tasks (e.g. email delivery, file transfer operations, data import/export processes, etc.)
```
[HttpPost]
[Route("CreateBackgroundJob")]
public ActionResult CreateBackgroundJob()
{
    BackgroundJob.Enqueue(() => Console.WriteLine("Background Job Triggered"));
    return Ok();
}
```
* Scheduled tasks will run at a specific time
* DateTimeOffset functions similarly to DateTime, but it is aware of time zone offsets
```
[HttpPost]
[Route("CreateScheduledJob")]
public ActionResult CreateScheduledJob()
{
    var scheduleDate = DateTime.UtcNow.AddSeconds(5);
    var dateTimeOffset = new DateTimeOffset(scheduleDate);
    BackgroundJob.Schedule(() => Console.WriteLine("Scheduled Job Triggered"), dateTimeOffset);
    return Ok();
}
```
* A continuation job is a job that executes immediately after another job has completed 
* You can store the id returned by a hangfire method in a variable, where the completion of that method will trigger the continuation job
* Create a continuation job - .ContinueJobWith also returns a job id, meaning you can chain multiple jobs
```
[HttpPost]
[Route("CreateContinuationJob")]
public ActionResult CreateContinuationJob()
{
    var scheduleDate = DateTime.UtcNow.AddSeconds(5);
    var dateTimeOffset = new DateTimeOffset(scheduleDate);
    var jobId = BackgroundJob.Schedule(() => Console.WriteLine("Scheduled Job Triggered"), dateTimeOffset);

    var job2Id = BackgroundJob.ContinueJobWith(jobId, () => Console.WriteLine("Continuation Job 1 Triggered"));
    var job3Id = BackgroundJob.ContinueJobWith(job2Id, () => Console.WriteLine("Continuation Job 2 Triggered"));
    var job4Id = BackgroundJob.ContinueJobWith(job2Id, () => Console.WriteLine("Continuation Job 3 Triggered"));

    return Ok();
}
```
* Recurring is the most commonly used Hangfire job, and they allow you to repeatedly run a job at specific time intervals
* The first parameter is the id of the recurring job - if there is already a recurring job with this id, it will be updated
* The last parameter is a cron expression - a cron expression is a string of five or six fields separated by white space, which represent a schedule for specifying when a task or job should be executed (e.g. below, the job will be run every minute)
``` 
[HttpPost]
[Route("CreateRecurringJob")]
public ActionResult CreateRecurringJob()
{
    RecurringJob.AddOrUpdate("RecurringJob1", () => Console.WriteLine("Recurring Job Triggered"), "* * * * *");

    return Ok();
}
```
* You can pass a custom class to hangfire methods to allow access to declared properties and methods within that class
```
public class TestJob
{
    private readonly ILogger _logger;

    public TestJob(ILogger<TestJob> logger) => _logger = logger;

    public void WriteLog(string logMessage)
    {
        _logger.LogInformation($"{DateTime.Now:yyyy-MM-dd hh:mm:ss tt} {logMessage}");
    }
}
```
```
[HttpPost]
[Route("InjectedDependencies")]
public ActionResult InjectedDependencies()
{
    BackgroundJob.Enqueue<TestJob>(x => x.WriteLog("Background Job Triggered"));

    var scheduleDate = DateTime.UtcNow.AddSeconds(5);
    var dateTimeOffset = new DateTimeOffset(scheduleDate);

    var jobId = BackgroundJob.Schedule<TestJob>(x => x.WriteLog("Scheduled Job Triggered"), dateTimeOffset);

    var job2Id = BackgroundJob.ContinueJobWith<TestJob>(jobId, x => x.WriteLog("Continuation Job 1 Triggered"));
    var job3Id = BackgroundJob.ContinueJobWith<TestJob>(job2Id, x => x.WriteLog("Continuation Job 2 Triggered"));
    var job4Id = BackgroundJob.ContinueJobWith<TestJob>(job2Id, x => x.WriteLog("Continuation Job 3 Triggered"));

    RecurringJob.AddOrUpdate<TestJob>("RecurringJob1", x => x.WriteLog("Recurring Job Triggered"), "* * * * *");

    return Ok();
}
```