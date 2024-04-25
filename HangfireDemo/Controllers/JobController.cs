using Hangfire;
using HangfireDemo.Jobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HangfireDemo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobController : ControllerBase
    {
        // Background Job
        [HttpPost]
        [Route("CreateBackgroundJob")]
        public ActionResult CreateBackgroundJob()
        {
            // A background job can be used for performing resource-intensive tasks (e.g. email delivery, file transfer operations, data import/export processes, etc.)
            // .Enqueue returns a string that is the id of the job
            BackgroundJob.Enqueue(() => Console.WriteLine("Background Job Triggered"));
            return Ok();
        }

        // Scheduled Job
        [HttpPost]
        [Route("CreateScheduledJob")]
        public ActionResult CreateScheduledJob()
        {
            // Create a DateTime object to specify when we want to schedule this job (here, the job will execute five seconds after being triggered
            var scheduleDate = DateTime.UtcNow.AddSeconds(5);
            // We will create a DateTimeOffset object based on the schedule DateTime
            // DateTimeOffset functions similarly to DateTime, but it is aware of time zone offsets
            var dateTimeOffset = new DateTimeOffset(scheduleDate);
            // .Schedule returns a string that is the id of the job
            BackgroundJob.Schedule(() => Console.WriteLine("Scheduled Job Triggered"), dateTimeOffset);
            return Ok();
        }

        // Continuation Job
        [HttpPost]
        [Route("CreateContinuationJob")]
        public ActionResult CreateContinuationJob()
        {
            // A continuation job is a job that executes immediately after another job has completed
            // Here, we are going to schedule a new job, this time, storing its job id in a variable
            var scheduleDate = DateTime.UtcNow.AddSeconds(5);
            var dateTimeOffset = new DateTimeOffset(scheduleDate);
            var jobId = BackgroundJob.Schedule(() => Console.WriteLine("Scheduled Job Triggered"), dateTimeOffset);

            // Create a continuation job - .ContinueJobWith also returns a job id, meaning you can chain multiple jobs
            var job2Id = BackgroundJob.ContinueJobWith(jobId, () => Console.WriteLine("Continuation Job 1 Triggered"));
            var job3Id = BackgroundJob.ContinueJobWith(job2Id, () => Console.WriteLine("Continuation Job 2 Triggered"));
            var job4Id = BackgroundJob.ContinueJobWith(job2Id, () => Console.WriteLine("Continuation Job 3 Triggered"));

            return Ok();
        }

        // Recurring Jobs
        [HttpPost]
        [Route("CreateRecurringJob")]
        public ActionResult CreateRecurringJob()
        {
            // Recurring is the most commonly used Hangfire job
            // With recurring jobs, you can repeatedly run a job at specific time intervals
            // The first parameter is the id of the recurring job - if there is already a recurring job with this id, it will be updated
            // The last parameter is a cron expression - a cron expression is a string of five or six fields separated by white space, which represent a schedule for specifying when a task or job should be executed
            // In the example below, the job will be run every minute
            RecurringJob.AddOrUpdate("RecurringJob1", () => Console.WriteLine("Recurring Job Triggered"), "* * * * *");

            return Ok();
        }

        // Leverage Injected Dependencies
        [HttpPost]
        [Route("InjectedDependencies")]
        public ActionResult InjectedDependencies()
        {
            // Using the new TestJob class, we can inject it into our Hangfire methods, and use the WriteLog method declared in the TestJob class
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
    }
}
