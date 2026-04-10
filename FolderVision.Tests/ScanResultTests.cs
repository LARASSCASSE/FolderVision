using FolderVision.Models;

namespace FolderVision.Tests
{
    public class ScanResultTests
    {
        // ─────────────────────────────────────────────────────────────────────
        //  AddRootFolder
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void AddRootFolder_AddsFolder()
        {
            var result = new ScanResult();
            var folder = new FolderInfo("C:\\Test");

            result.AddRootFolder(folder);

            Assert.Single(result.RootFolders);
            Assert.Equal("C:\\Test", result.RootFolders[0].FullPath);
        }

        [Fact]
        public void AddRootFolder_IgnoresDuplicates()
        {
            var result = new ScanResult();
            var folder = new FolderInfo("C:\\Test");

            result.AddRootFolder(folder);
            result.AddRootFolder(folder); // same instance

            Assert.Single(result.RootFolders);
        }

        [Fact]
        public void AddRootFolder_IgnoresNull()
        {
            var result = new ScanResult();

            result.AddRootFolder(null!);

            Assert.Empty(result.RootFolders);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  AddScannedPath
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void AddScannedPath_IgnoresDuplicates()
        {
            var result = new ScanResult();

            result.AddScannedPath("C:\\Test");
            result.AddScannedPath("C:\\Test");

            Assert.Single(result.ScannedPaths);
        }

        [Fact]
        public void AddScannedPath_IgnoresNullOrEmpty()
        {
            var result = new ScanResult();

            result.AddScannedPath(null!);
            result.AddScannedPath(string.Empty);

            Assert.Empty(result.ScannedPaths);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  FindFolder
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void FindFolder_FindsRootByPath()
        {
            var result = new ScanResult();
            var folder = new FolderInfo("C:\\Root");
            result.AddRootFolder(folder);

            var found = result.FindFolder("C:\\Root");

            Assert.NotNull(found);
            Assert.Equal("C:\\Root", found!.FullPath);
        }

        [Fact]
        public void FindFolder_FindsNestedFolder()
        {
            var result = new ScanResult();
            var root = new FolderInfo("C:\\Root");
            var child = new FolderInfo("C:\\Root\\Child");
            root.AddSubFolder(child);
            result.AddRootFolder(root);

            var found = result.FindFolder("C:\\Root\\Child");

            Assert.NotNull(found);
            Assert.Equal("C:\\Root\\Child", found!.FullPath);
        }

        [Fact]
        public void FindFolder_IsCaseInsensitive()
        {
            var result = new ScanResult();
            result.AddRootFolder(new FolderInfo("C:\\Root"));

            var found = result.FindFolder("c:\\root");

            Assert.NotNull(found);
        }

        [Fact]
        public void FindFolder_ReturnsNullForMissingPath()
        {
            var result = new ScanResult();
            result.AddRootFolder(new FolderInfo("C:\\Root"));

            var found = result.FindFolder("C:\\DoesNotExist");

            Assert.Null(found);
        }

        [Fact]
        public void FindFolder_ReturnsNullForNullInput()
        {
            var result = new ScanResult();

            var found = result.FindFolder(null!);

            Assert.Null(found);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  GetAllFolders
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void GetAllFolders_ReturnsAllIncludingNested()
        {
            var result = new ScanResult();
            var root = new FolderInfo("C:\\Root");
            var child = new FolderInfo("C:\\Root\\Child");
            var grandchild = new FolderInfo("C:\\Root\\Child\\GrandChild");
            child.AddSubFolder(grandchild);
            root.AddSubFolder(child);
            result.AddRootFolder(root);

            var all = result.GetAllFolders().ToList();

            Assert.Equal(3, all.Count);
            Assert.Contains(all, f => f.FullPath == "C:\\Root");
            Assert.Contains(all, f => f.FullPath == "C:\\Root\\Child");
            Assert.Contains(all, f => f.FullPath == "C:\\Root\\Child\\GrandChild");
        }

        [Fact]
        public void GetAllFolders_EmptyResultYieldsNothing()
        {
            var result = new ScanResult();

            Assert.Empty(result.GetAllFolders());
        }

        // ─────────────────────────────────────────────────────────────────────
        //  UpdateTotals
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void UpdateTotals_ComputesCorrectCounts()
        {
            var result = new ScanResult();
            var root = new FolderInfo("C:\\Root");
            var child = new FolderInfo("C:\\Root\\Child");
            root.AddSubFolder(child);
            root.SetFileCount(3);
            child.SetFileCount(7);
            result.AddRootFolder(root); // AddRootFolder calls UpdateTotals internally

            result.UpdateTotals();

            // 1 root + 1 child = 2 folders
            Assert.Equal(2, result.TotalFolders);
            // 3 + 7 = 10 files
            Assert.Equal(10, result.TotalFiles);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Thread safety
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task ConcurrentAddAndFind_NoException()
        {
            var result = new ScanResult();
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

            var writers = Enumerable.Range(0, 20).Select(i => Task.Run(() =>
            {
                try { result.AddRootFolder(new FolderInfo($"C:\\Folder{i}")); }
                catch (Exception ex) { exceptions.Add(ex); }
            }));

            var readers = Enumerable.Range(0, 20).Select(i => Task.Run(() =>
            {
                try { result.FindFolder($"C:\\Folder{i}"); }
                catch (Exception ex) { exceptions.Add(ex); }
            }));

            await Task.WhenAll(writers.Concat(readers));

            Assert.Empty(exceptions);
        }

        [Fact]
        public async Task ConcurrentGetAllFolders_NoException()
        {
            var result = new ScanResult();
            for (int i = 0; i < 10; i++)
                result.AddRootFolder(new FolderInfo($"C:\\Folder{i}"));

            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

            var tasks = Enumerable.Range(0, 10).Select(_i => Task.Run(() =>
            {
                try { result.GetAllFolders().ToList(); }
                catch (Exception ex) { exceptions.Add(ex); }
            })).Concat(Enumerable.Range(10, 10).Select(i => Task.Run(() =>
            {
                try { result.AddRootFolder(new FolderInfo($"C:\\FolderNew{i}")); }
                catch (Exception ex) { exceptions.Add(ex); }
            })));

            await Task.WhenAll(tasks);

            Assert.Empty(exceptions);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  ToString / SetScanDuration
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void SetScanDuration_ComputesDurationFromStartTime()
        {
            var start = DateTime.Now.AddSeconds(-5);
            var result = new ScanResult { ScanStartTime = start };

            result.SetScanDuration(DateTime.Now);

            Assert.True(result.ScanDuration.TotalSeconds >= 4.9);
        }

        [Fact]
        public void ToString_ContainsFolderAndFileCount()
        {
            var result = new ScanResult();
            var root = new FolderInfo("C:\\Root");
            root.SetFileCount(5);
            result.AddRootFolder(root);
            result.UpdateTotals();

            var str = result.ToString();

            Assert.Contains("folder", str, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("file", str, StringComparison.OrdinalIgnoreCase);
        }
    }
}
