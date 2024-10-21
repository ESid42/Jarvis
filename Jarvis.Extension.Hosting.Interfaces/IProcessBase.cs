namespace Jarvis.Extension.Hosting.Interfaces
{
    public interface IProcessBase<TProcessSettings>
    {
        #region Events

        public Action<IEventLogInfo>? LogEvent { get; set; }

        #endregion Events

        #region Definitions

        public TProcessSettings Settings { get; }

        #endregion Definitions

        Task<bool> Start();
    }
}