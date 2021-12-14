using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading;
using System.Diagnostics;
using RaaiVan.Modules.GlobalUtilities;
using RaaiVan.Modules.Jobs;

namespace RaaiVan.Web.Ajax
{
    public class RVJob
    {
        public Thread ThreadObject;
        public int? Interval;
        public DateTime? StartTime;
        public DateTime? EndTime;
        public DateTime? LastActivityDate;
        public long? LastActivityDuration;
        public string ErrorMessage;
        public Guid? TenantID;

        public RVJob(Guid tenantId)
        {
            TenantID = tenantId;
        }

        public bool check_time()
        {
            if (!StartTime.HasValue || !EndTime.HasValue || !TenantID.HasValue) return false;

            DateTime now = DateTime.Now;
            now = new DateTime(2000, 1, 1, now.Hour, now.Minute, now.Second, now.Millisecond);
            StartTime = new DateTime(2000, 1, 1, StartTime.Value.Hour, StartTime.Value.Minute, 0);
            EndTime = new DateTime(2000, 1, 1, EndTime.Value.Hour, EndTime.Value.Minute, 59);

            if (!LastActivityDate.HasValue && now < StartTime) Thread.Sleep((StartTime.Value - now).Milliseconds + 1000);

            return EndTime > StartTime ? (now >= StartTime && now <= EndTime) : (now >= StartTime || now <= EndTime);
        }
    }

    class RVScheduler
    {
        private static Dictionary<string, RVJob> jobsDic = new Dictionary<string, RVJob>();

        private static bool _inited = false;

        private static void _initialize()
        {
            if (_inited) return;
            _inited = true;

            jobsDic["ImportStockData"] = new RVJob(Guid.NewGuid());
        }

        private static void _start_job(string jobName, RVJob jobObject)
        {
            while (true)
            {
                //sleep thread be madate Interval saniye
                if (jobObject.LastActivityDate.HasValue || !string.IsNullOrEmpty(jobObject.ErrorMessage))
                    Thread.Sleep(jobObject.Interval.Value);

                //agar dar saati hastim ke bayad update shavad edame midahim
                if (!jobObject.check_time()) continue;

                Stopwatch sw = Stopwatch.StartNew();
                sw.Start();

                try
                {
                    Jobs.run(jobObject.TenantID.Value, jobName);
                    jobObject.LastActivityDate = DateTime.Now;
                }
                catch (Exception ex) { }

                sw.Stop();
                jobObject.LastActivityDuration = sw.ElapsedMilliseconds;
            }
        }

        public static void start(string jobName, int? interval = null, DateTime? startTime = null, DateTime? endTime = null)
        {
            _initialize();

            RVJob trd = jobsDic.ContainsKey(jobName) && jobsDic[jobName] != null ? jobsDic[jobName] : null;
            if (trd == null) return;
            if (trd.ThreadObject != null) trd.ThreadObject.Abort();
            
            if (interval.HasValue) trd.Interval = interval;
            if (startTime.HasValue) trd.StartTime = startTime;
            if (endTime.HasValue) trd.EndTime = endTime;

            if (jobName.LastIndexOf('_') > 0) jobName = jobName.Substring(0, jobName.LastIndexOf('_'));

            PublicMethods.set_timeout(() => _start_job(jobName.Substring(1), trd), 0);

            jobsDic[jobName] = trd;
        }

        public static void run_jobs()
        {
            _initialize();

            List<string> keys = new List<string>(jobsDic.Keys);
            
            foreach (string jobName in keys) start(jobName);
        }
    }
}