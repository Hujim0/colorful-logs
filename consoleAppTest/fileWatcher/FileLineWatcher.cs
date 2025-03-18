// using System;
// using System.IO;
// using System.Text;
// using System.Threading;
// using consoleAppTest.structs;

// namespace consoleAppTest.fileWatcher
// {
//     public class FileLineWatcher : IDisposable
//     {
//         private readonly FileWatcher _fileWatcher;
//         private readonly Encoding _encoding;

//         public delegate void NewLinesDetectedHandler(string filePath, string[] newLines);
//         public event NewLinesDetectedHandler? NewLinesDetected;

//         public FileLineWatcher(FileWatcher fileWatcher, Encoding? encoding = null)
//         {
//             _fileWatcher = fileWatcher;
//             _encoding = encoding ?? Encoding.UTF8;

//             _fileWatcher.FileCreated += HandleFileCreated;
//             _fileWatcher.FileAppended += HandleFileAppended;
//         }

//         private void HandleFileCreated(object sender, FileSystemEventArgs e)
//         {
//             ProcessFile(e.FullPath);
//         }

//         private void HandleFileAppended(object sender, FileSystemEventArgs e)
//         {
//             ProcessFile(e.FullPath);
//         }

//         private void ProcessFile(string filePath)
//         {
//             const int maxRetries = 3;
//             const int delayMs = 100;

//             for (int i = 0; i < maxRetries; i++)
//             {
//                 try
//                 {
//                     using var stream = new FileStream(
//                         filePath,
//                         FileMode.Open,
//                         FileAccess.Read,
//                         FileShare.ReadWrite | FileShare.Delete
//                     );

//                     if (!_fileWatcher._Files.TryGetValue(filePath, out LocalFile? localFile))
//                         return;

//                     var previousLength = localFile.LastLength;
//                     var currentLength = stream.Length;

//                     // Handle file truncation or reset
//                     if (currentLength < previousLength)
//                     {
//                         localFile.Buffer.Clear();
//                     }

//                     if (previousLength >= currentLength) return;

//                     stream.Seek(previousLength, SeekOrigin.Begin);
//                     using var reader = new StreamReader(stream, _encoding);

//                     var newContent = reader.ReadToEnd();
//                     localFile.Buffer.Append(newContent);

//                     var lines = newContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                    
//                     if (localFile.Buffer.Length > 0 && lines.Length > 0)
//                     {
//                         var completeLines = lines.AsSpan(0, lines.Length - 1);
//                         if (completeLines.Length > 0)
//                         {
//                             NewLinesDetected?.Invoke(filePath, completeLines.ToArray());
//                         }
//                         localFile.Buffer.Clear().Append(lines[^1]);
//                     }

//                     // Update LastLength to mark processed position
//                     localFile.LastLength = stream.Position;
//                     break;
//                 }
//                 catch (IOException) when (i < maxRetries - 1)
//                 {
//                     Thread.Sleep(delayMs);
//                 }
//                 catch
//                 {
//                     _fileWatcher._Files.TryRemove(filePath, out _);
//                     break;
//                 }
//             }
//         }

//         public void Dispose()
//         {
//             _fileWatcher.FileCreated -= HandleFileCreated;
//             _fileWatcher.FileAppended -= HandleFileAppended;
//             _fileWatcher.FileMoved -= HandleFileMoved;
//             _fileWatcher.FileDeleted -= HandleFileDeleted;
//             GC.SuppressFinalize(this);
//         }
//     }
// }