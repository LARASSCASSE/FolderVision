using System;
using System.Collections.Generic;

namespace FolderVision.Core
{
    public class ErrorHandler
    {
        private readonly List<ScanError> _errors;

        public ErrorHandler()
        {
            _errors = new List<ScanError>();
        }

        public void LogError(string message, string? path = null, Exception? exception = null)
        {
            throw new NotImplementedException();
        }

        public void LogWarning(string message, string? path = null)
        {
            throw new NotImplementedException();
        }

        public void LogInfo(string message, string? path = null)
        {
            throw new NotImplementedException();
        }

        public void ClearErrors()
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<ScanError> GetErrors()
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<ScanError> GetWarnings()
        {
            throw new NotImplementedException();
        }

        public bool HasErrors => _errors.Count > 0;
        public int ErrorCount => _errors.Count;

        public event EventHandler<ErrorEventArgs>? ErrorLogged;
    }

    public class ScanError
    {
        public DateTime Timestamp { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Path { get; set; }
        public Exception? Exception { get; set; }
        public ErrorLevel Level { get; set; }
    }

    public enum ErrorLevel
    {
        Info,
        Warning,
        Error
    }

    public class ErrorEventArgs : EventArgs
    {
        public ScanError Error { get; set; } = new ScanError();
    }
}