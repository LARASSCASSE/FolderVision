using System;
using System.Text;
using FolderVision.Core;

namespace FolderVision.Ui
{
    public class ProgressDisplay
    {
        private readonly int _progressBarWidth;
        private int _lastProgressLength;
        private bool _isDisplaying;

        public ProgressDisplay(int progressBarWidth = 50)
        {
            _progressBarWidth = progressBarWidth;
            _lastProgressLength = 0;
            _isDisplaying = false;
        }

        public void ShowProgress(double percentComplete, string currentPath = "", long processedItems = 0, long totalItems = 0)
        {
            throw new NotImplementedException();
        }

        public void ShowProgress(ProgressChangedEventArgs args)
        {
            throw new NotImplementedException();
        }

        public void UpdateProgress(int percentComplete, string status = "")
        {
            throw new NotImplementedException();
        }

        public void StartProgress(string initialMessage = "Starting...")
        {
            throw new NotImplementedException();
        }

        public void CompleteProgress(string completionMessage = "Completed!")
        {
            throw new NotImplementedException();
        }

        public void HideProgress()
        {
            throw new NotImplementedException();
        }

        private string CreateProgressBar(double percentComplete)
        {
            throw new NotImplementedException();
        }

        private void ClearCurrentLine()
        {
            Console.Write("\r" + new string(' ', _lastProgressLength) + "\r");
        }

        private string FormatFileSize(long bytes)
        {
            throw new NotImplementedException();
        }

        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            throw new NotImplementedException();
        }

        private string TruncatePath(string path, int maxLength)
        {
            throw new NotImplementedException();
        }

        public bool IsDisplaying => _isDisplaying;
    }
}