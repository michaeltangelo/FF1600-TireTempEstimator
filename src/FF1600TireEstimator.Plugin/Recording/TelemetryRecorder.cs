using GameReaderCommon;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace FF1600TireEstimator.Plugin.Recording
{
    internal sealed class TelemetryRecorder : IDisposable
    {
        private const int QueueCapacity = 4096;
        private const double SampleIntervalSeconds = 0.1;
        private readonly RawIRacingReader rawReader = new RawIRacingReader();
        private readonly BlockingCollection<WriterMessage> queue =
            new BlockingCollection<WriterMessage>(new ConcurrentQueue<WriterMessage>(), QueueCapacity);
        private readonly Thread writerThread;
        private volatile bool captureRequested;
        private volatile bool faulted;
        private string faultMessage;
        private Guid activeSessionId;
        private Guid captureRunId;
        private long sequence;
        private long acceptedRecords;
        private long rejectedRecords;
        private long nextSampleAt;
        private double[] previousTemperatures;
        private Guid resumeSessionId;
        private Guid resumePreviousRunId;
        private double[] resumeTemperatures;
        private RecorderStatusSnapshot status = RecorderStatusSnapshot.Stopped;

        internal static string EvidenceRoot =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FF1600TireEstimator",
                "evidence");

        public TelemetryRecorder()
        {
            writerThread = new Thread(WriterLoop)
            {
                IsBackground = true,
                Name = "FF1600 telemetry writer"
            };
            writerThread.Start();
        }

        public RecorderStatusSnapshot Status => Volatile.Read(ref status);

        public void StartCapture()
        {
            captureRequested = true;
            PublishStatus("Armed");
        }

        public void StopCapture()
        {
            captureRequested = false;
            PublishStatus("Stopped");
        }

        public void ToggleCapture()
        {
            if (captureRequested)
            {
                StopCapture();
            }
            else
            {
                StartCapture();
            }
        }

        public void Update(GameData data)
        {
            if (faulted)
            {
                PublishStatus("Faulted");
                return;
            }

            var normalized = data.NewData;
            var eligible =
                captureRequested &&
                data.GameRunning &&
                !data.GameReplay &&
                normalized != null &&
                !normalized.Spectating &&
                string.Equals(normalized.CarId, "raygr22", StringComparison.Ordinal);

            if (!eligible)
            {
                if (!captureRequested)
                {
                    ClearResumeCandidate();
                }

                if (captureRunId != Guid.Empty)
                {
                    if (captureRequested)
                    {
                        PreserveResumeCandidate();
                    }
                    else
                    {
                        ClearResumeCandidate();
                    }

                    CloseRun("Eligibility lost");
                }

                PublishStatus(captureRequested ? "Armed" : "Stopped");
                return;
            }

            RawIRacingFrame raw;
            try
            {
                if (!rawReader.TryRead(normalized, out raw) || raw.IsReplayPlaying)
                {
                    PublishStatus("Armed; raw telemetry unavailable");
                    return;
                }
            }
            catch (Exception ex)
            {
                Fail("Raw telemetry error: " + ex.Message);
                return;
            }

            if (captureRunId == Guid.Empty || activeSessionId != data.SessionId)
            {
                if (captureRunId != Guid.Empty)
                {
                    CloseRun("Session changed");
                }

                if (resumeSessionId != Guid.Empty && resumeSessionId != data.SessionId)
                {
                    ClearResumeCandidate();
                }

                activeSessionId = data.SessionId;
                captureRunId = Guid.NewGuid();
                sequence = 0;
                previousTemperatures = null;
                nextSampleAt = 0;
                TryQueue(WriterMessage.Open(activeSessionId, captureRunId));
            }

            DetectResumeCheckpoint(normalized, raw);
            DetectCheckpoint(data, normalized, raw);

            var now = Stopwatch.GetTimestamp();
            if (nextSampleAt == 0 || now >= nextSampleAt)
            {
                var intervalTicks = (long)(SampleIntervalSeconds * Stopwatch.Frequency);
                nextSampleAt = nextSampleAt == 0
                    ? now + intervalTicks
                    : nextSampleAt + intervalTicks;
                if (nextSampleAt <= now)
                {
                    nextSampleAt = now + intervalTicks;
                }

                TryQueue(
                    WriterMessage.Record(
                        new TelemetryRecord(
                            activeSessionId,
                            captureRunId,
                            ++sequence,
                            DateTime.UtcNow,
                            normalized,
                            raw)));
            }

            PublishStatus("Recording");
        }

        public void Dispose()
        {
            captureRequested = false;
            ClearResumeCandidate();
            CloseRun("Plugin shutdown");
            queue.CompleteAdding();
            if (!writerThread.Join(TimeSpan.FromSeconds(5)))
            {
                SimHub.Logging.Current.Warn("FF1600 telemetry writer did not stop within five seconds");
            }

            queue.Dispose();
        }

        private void DetectCheckpoint(GameData data, StatusDataBase normalized, RawIRacingFrame raw)
        {
            if (previousTemperatures != null &&
                raw.CarcassTemperatures.Where((value, index) => Math.Abs(value - previousTemperatures[index]) > 0.000001).Any())
            {
                TryQueue(
                    WriterMessage.Record(
                        new CheckpointRecord(
                            activeSessionId,
                            captureRunId,
                            ++sequence,
                            DateTime.UtcNow,
                            raw,
                            (double[])previousTemperatures.Clone(),
                            normalized.IsInPit,
                            normalized.IsInPitLane,
                            "temperature_change",
                            null)));
            }

            previousTemperatures = (double[])raw.CarcassTemperatures.Clone();
        }

        private void DetectResumeCheckpoint(StatusDataBase normalized, RawIRacingFrame raw)
        {
            if (resumeSessionId == Guid.Empty || resumeSessionId != activeSessionId || resumeTemperatures == null)
            {
                return;
            }

            if (TemperaturesChanged(resumeTemperatures, raw.CarcassTemperatures))
            {
                TryQueue(
                    WriterMessage.Record(
                        new CheckpointRecord(
                            activeSessionId,
                            captureRunId,
                            ++sequence,
                            DateTime.UtcNow,
                            raw,
                            (double[])resumeTemperatures.Clone(),
                            normalized.IsInPit,
                            normalized.IsInPitLane,
                            "telemetry_resume",
                            resumePreviousRunId)));
            }

            ClearResumeCandidate();
        }

        private void PreserveResumeCandidate()
        {
            if (activeSessionId == Guid.Empty || captureRunId == Guid.Empty || previousTemperatures == null)
            {
                return;
            }

            resumeSessionId = activeSessionId;
            resumePreviousRunId = captureRunId;
            resumeTemperatures = (double[])previousTemperatures.Clone();
        }

        private void ClearResumeCandidate()
        {
            resumeSessionId = Guid.Empty;
            resumePreviousRunId = Guid.Empty;
            resumeTemperatures = null;
        }

        private static bool TemperaturesChanged(double[] previous, double[] current)
        {
            return current.Where((value, index) => Math.Abs(value - previous[index]) > 0.000001).Any();
        }

        private void CloseRun(string reason)
        {
            if (captureRunId == Guid.Empty)
            {
                return;
            }

            TryQueue(WriterMessage.Close(activeSessionId, captureRunId, reason));
            activeSessionId = Guid.Empty;
            captureRunId = Guid.Empty;
            previousTemperatures = null;
            nextSampleAt = 0;
        }

        private bool TryQueue(WriterMessage message)
        {
            if (queue.IsAddingCompleted || !queue.TryAdd(message))
            {
                Interlocked.Increment(ref rejectedRecords);
                Fail("Writer queue full; capture stopped");
                return false;
            }

            if (message.RecordValue != null)
            {
                Interlocked.Increment(ref acceptedRecords);
            }

            return true;
        }

        private void WriterLoop()
        {
            StreamWriter telemetryWriter = null;
            StreamWriter checkpointWriter = null;
            try
            {
                foreach (var message in queue.GetConsumingEnumerable())
                {
                    if (message.Kind == WriterMessageKind.Open)
                    {
                        CloseWriters(ref telemetryWriter, ref checkpointWriter);
                        var runDirectory = GetRunDirectory(message.SessionId, message.CaptureRunId);
                        Directory.CreateDirectory(runDirectory);
                        telemetryWriter = OpenNew(Path.Combine(runDirectory, "telemetry.jsonl"));
                        checkpointWriter = OpenNew(Path.Combine(runDirectory, "checkpoints.jsonl"));
                    }
                    else if (message.Kind == WriterMessageKind.Close)
                    {
                        CloseWriters(ref telemetryWriter, ref checkpointWriter);
                    }
                    else if (message.RecordValue != null)
                    {
                        var writer = message.RecordValue is CheckpointRecord ? checkpointWriter : telemetryWriter;
                        if (writer == null)
                        {
                            throw new InvalidOperationException("Recorder record received without an open capture run.");
                        }

                        writer.WriteLine(JsonLinesEncoder.Encode(message.RecordValue));
                    }
                }
            }
            catch (Exception ex)
            {
                Fail("Writer error: " + ex.Message);
                SimHub.Logging.Current.Error("FF1600 telemetry writer failed", ex);
            }
            finally
            {
                CloseWriters(ref telemetryWriter, ref checkpointWriter);
            }
        }

        private static StreamWriter OpenNew(string path)
        {
            return new StreamWriter(
                new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read),
                new UTF8Encoding(false))
            {
                AutoFlush = true
            };
        }

        private static string GetRunDirectory(Guid sessionId, Guid runId)
        {
            return Path.Combine(
                EvidenceRoot,
                "sessions",
                sessionId.ToString("D"),
                "runs",
                runId.ToString("D"));
        }

        private static void CloseWriters(ref StreamWriter telemetryWriter, ref StreamWriter checkpointWriter)
        {
            telemetryWriter?.Dispose();
            checkpointWriter?.Dispose();
            telemetryWriter = null;
            checkpointWriter = null;
        }

        private void Fail(string message)
        {
            faultMessage = message;
            faulted = true;
            captureRequested = false;
            PublishStatus("Faulted");
        }

        private void PublishStatus(string state)
        {
            Volatile.Write(
                ref status,
                new RecorderStatusSnapshot(
                    state,
                    captureRequested,
                    captureRunId,
                    Interlocked.Read(ref acceptedRecords),
                    Interlocked.Read(ref rejectedRecords),
                    faultMessage));
        }
    }

    internal sealed class RecorderStatusSnapshot
    {
        public static readonly RecorderStatusSnapshot Stopped =
            new RecorderStatusSnapshot("Stopped", false, Guid.Empty, 0, 0, null);

        public RecorderStatusSnapshot(string state, bool requested, Guid runId, long accepted, long rejected, string fault)
        {
            State = state;
            Requested = requested;
            RunId = runId;
            Accepted = accepted;
            Rejected = rejected;
            Fault = fault;
        }

        public string State { get; }
        public bool Requested { get; }
        public Guid RunId { get; }
        public long Accepted { get; }
        public long Rejected { get; }
        public string Fault { get; }

        public string GetSummary()
        {
            return string.Join(
                Environment.NewLine,
                "--- Raw telemetry recorder (10 Hz) ---",
                "State: " + State,
                "CaptureRequested: " + Requested,
                "RunId: " + (RunId == Guid.Empty ? "<none>" : RunId.ToString("D")),
                "EvidenceRoot: " + TelemetryRecorder.EvidenceRoot,
                "AcceptedRecords: " + Accepted.ToString(CultureInfo.InvariantCulture),
                "RejectedRecords: " + Rejected.ToString(CultureInfo.InvariantCulture),
                "Fault: " + (string.IsNullOrEmpty(Fault) ? "<none>" : Fault));
        }
    }

    internal enum WriterMessageKind
    {
        Open,
        Record,
        Close
    }

    internal sealed class WriterMessage
    {
        private WriterMessage(WriterMessageKind kind, Guid sessionId, Guid captureRunId, RecorderRecord record, string reason)
        {
            Kind = kind;
            SessionId = sessionId;
            CaptureRunId = captureRunId;
            RecordValue = record;
            Reason = reason;
        }

        public WriterMessageKind Kind { get; }
        public Guid SessionId { get; }
        public Guid CaptureRunId { get; }
        public RecorderRecord RecordValue { get; }
        public string Reason { get; }

        public static WriterMessage Open(Guid sessionId, Guid runId)
        {
            return new WriterMessage(WriterMessageKind.Open, sessionId, runId, null, null);
        }

        public static WriterMessage Record(RecorderRecord record)
        {
            return new WriterMessage(WriterMessageKind.Record, record.SessionId, record.CaptureRunId, record, null);
        }

        public static WriterMessage Close(Guid sessionId, Guid runId, string reason)
        {
            return new WriterMessage(WriterMessageKind.Close, sessionId, runId, null, reason);
        }
    }
}
