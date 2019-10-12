namespace MovieList.Preferences
{
    public class LoggingPreferences
    {
        public string LogPath { get; set; }
        public int MinLogLevel { get; set; }

        public LoggingPreferences(string logPath, int minLogLevel)
        {
            this.LogPath = logPath;
            this.MinLogLevel = minLogLevel;
        }
    }
}