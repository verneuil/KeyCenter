using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

namespace KeyCenter
{
    /// <summary>轻量统一动画器：全程序共用一个 15ms 定时器，只在有动画时运行；按真实时间计算进度。</summary>
    internal static class Motion
    {
        private static Timer timer;
        private static readonly List<Job> jobs = new List<Job>();
        private const int Interval = 15;

        private class Job
        {
            public object Owner;
            public float From;
            public float To;
            public int Duration;
            public long StartedAt;
            public Action<float> Apply;
            public Action Done;
        }

        private static void EnsureTimer()
        {
            if (timer != null) return;
            timer = new Timer();
            timer.Interval = Interval;
            timer.Tick += delegate { Tick(); };
        }

        public static void Animate(object owner, float from, float to, int durationMs, Action<float> apply)
        {
            Animate(owner, from, to, durationMs, apply, null);
        }

        public static void Animate(object owner, float from, float to, int durationMs, Action<float> apply, Action done)
        {
            if (apply == null) return;
            Stop(owner);
            EnsureTimer();
            Job job = new Job();
            job.Owner = owner;
            job.From = from;
            job.To = to;
            job.Duration = Math.Max(1, durationMs);
            job.StartedAt = Stopwatch.GetTimestamp();
            job.Apply = apply;
            job.Done = done;
            jobs.Add(job);
            timer.Start();
        }

        public static void Stop(object owner)
        {
            for (int i = jobs.Count - 1; i >= 0; i--)
            {
                if (jobs[i].Owner == owner) jobs.RemoveAt(i);
            }
            if (jobs.Count == 0 && timer != null) timer.Stop();
        }

        public static void StopAll()
        {
            jobs.Clear();
            if (timer != null) timer.Stop();
        }

        public static void Shutdown()
        {
            jobs.Clear();
            if (timer != null)
            {
                timer.Stop();
                timer.Dispose();
                timer = null;
            }
        }

        private static void Tick()
        {
            long now = Stopwatch.GetTimestamp();
            for (int i = jobs.Count - 1; i >= 0; i--)
            {
                if (i >= jobs.Count) continue;
                Job job = jobs[i];
                Control ctl = job.Owner as Control;
                if (ctl != null && (ctl.IsDisposed || ctl.Disposing))
                {
                    jobs.RemoveAt(i);
                    continue;
                }
                double elapsedMs = (now - job.StartedAt) * 1000.0 / Stopwatch.Frequency;
                float t = (float)(elapsedMs / job.Duration);
                if (t > 1f) t = 1f;
                float eased = 1f - (1f - t) * (1f - t) * (1f - t);
                float value = job.From + (job.To - job.From) * eased;
                try { job.Apply(value); }
                catch (Exception)
                {
                    // 回调异常时移除该 Job，避免每 15ms 无限重试
                    if (i < jobs.Count && ReferenceEquals(jobs[i], job)) jobs.RemoveAt(i);
                    continue;
                }
                if (t >= 1f)
                {
                    Action done = job.Done;
                    job.Done = null;
                    if (done != null)
                    {
                        try { done(); }
                        catch (Exception) { }
                    }
                }
                // 回调里可能 Stop/StopAll，删除前必须确认索引仍指向同一个 Job
                if (t >= 1f && i < jobs.Count && ReferenceEquals(jobs[i], job)) jobs.RemoveAt(i);
            }
            if (jobs.Count == 0 && timer != null) timer.Stop();
        }
    }
}
