using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace liXMaiLips.Utils;

public enum LogType { FILTER, TRANSFORMATION, SAVE, CLEAR }

public class Logger
{
    private static Logger _instance;
    private readonly string _logFilePath;

    private Logger()
    {
        _logFilePath = Path.Combine(FileSystem.AppDataDirectory, "events_log.txt");
    }

    public static Logger GetInstance() => _instance ??= new Logger();

    // simple logger
    // TODO 
    public void Log(LogType level, string message)
    {
        string logEntry = $"{DateTime.Now:dd.MM.yyyy HH:mm:ss} {level}. {message}";
        File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
        System.Diagnostics.Debug.WriteLine(logEntry);
    }
}