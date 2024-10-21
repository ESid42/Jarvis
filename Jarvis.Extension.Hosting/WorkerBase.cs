using Jarvis.Common;
using Jarvis.Extension.Hosting.Interfaces;

namespace Jarvis.Extension.Hosting
{
    public class WorkerBase<TProcess, TProcessSettings> : BackgroundService where TProcess : IProcessBase<TProcessSettings>
    {
        #region Events

        public Action<string>? ErrorOccurred;

        #endregion Events

        #region Definitions

        private readonly IContainer _container;
        private readonly ILogger<WorkerBase<TProcess, TProcessSettings>> _logger;
        private TProcess? _process;

        #endregion Definitions

        #region Constructor

        public WorkerBase(IContainer container, ILogger<WorkerBase<TProcess, TProcessSettings>> logger)
        {
            _container = container;
            _logger = logger;

            Ini();
        }

        private void Ini()
        {
            try
            {
                TProcessSettings? processSettings = _container.Get<TProcessSettings>();
                if (processSettings is TProcessSettings processSet)
                {
                    object? obj = Activator.CreateInstance(typeof(TProcess), processSet);

                    if (obj is TProcess process)
                    {
                        _process = process;
                    }
                }

                if (_process is null)
                {
                    throw new Exception("Process creation failed.");
                }

                _process.LogEvent = Process_LogEventRaised;
            }
            catch (Exception ex)
            {
                _logger.LogError(new EventId(22), ex, "");
            }
        }

        private void Process_LogEventRaised(IEventLogInfo e)
        {
#pragma warning disable CA2254 // Template should be a static expression
            switch (e.Level)
            {
                case EventLogLevel.Trace:
                    _logger.LogTrace(new EventId(e.EventId), e.Message);
                    break;

                case EventLogLevel.Information:
                    _logger.LogInformation(new EventId(e.EventId), e.Message);
                    break;

                case EventLogLevel.Warning:
                    _logger.LogWarning(new EventId(e.EventId), e.Message);
                    break;

                case EventLogLevel.Error:
                    _logger.LogError(new EventId(e.EventId), e.Message);
                    break;

                case EventLogLevel.Critical:
                    _logger.LogCritical(new EventId(e.EventId), e.Message);
                    break;

                default:
                    break;
            }
#pragma warning restore CA2254 // Template should be a static expression
        }

        #endregion Constructor

        #region Methods

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_process == null)
            {
                _logger.LogWarning($"{nameof(_process)} is null.", new EventId(21));
                return;
            }
            try
            {
                bool res = await _process.Start();
                while (res != true)
                {
                    _logger.LogWarning("Process start sequence FAILED.", new EventId(25));
                    await Task.Delay(1000);
                    res = await _process.Start();
                }
                _logger.LogInformation("Process start sequence SUCCESUFUL.", new EventId(25));
            }
            catch (Exception ex)
            {
                _logger.LogError(new EventId(0), ex, "");
            }

            _logger.LogInformation(new EventId(1), "Service started.");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(200, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                }
            }
        }

        #endregion Methods
    }
}