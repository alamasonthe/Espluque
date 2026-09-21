namespace Util
{
    public class File
    {
        public static bool isfilelocked(string filename)
        {
            Result<FileStream> fileStreamResult = OpenRead(filename);

            if (!fileStreamResult.IsSuccess)
                return true;

            using FileStream fileStream = fileStreamResult.Value!;
            return false;
        }

        public static Result<bool> CanOpenRead(string filepath)
        {
            Result<FileStream> fileStreamResult = OpenRead(filepath);

            if (!fileStreamResult.IsSuccess)
            {
                return Result<bool>.Failure( fileStreamResult.Error.Code, fileStreamResult.Error.Message);
            }

            using FileStream fileStream = fileStreamResult.Value;

            return Result<bool>.Success(true);
        }

        public static Result<FileStream> OpenRead(string filepath)
        {
            if (string.IsNullOrWhiteSpace(filepath))
            {
                return Result<FileStream>.Failure( "FILEPATH_EMPTY", "File path is empty.");
            }

            try
            {
                if (System.IO.Directory.Exists(filepath))
                {
                    return Result<FileStream>.Failure( "TARGET_IS_DIRECTORY", "Target path is a directory.");
                }

                System.IO.FileStream fileStream = new System.IO.FileStream(
                    filepath,
                    System.IO.FileMode.Open,
                    System.IO.FileAccess.Read,
                    System.IO.FileShare.ReadWrite | System.IO.FileShare.Delete);

                return Result<FileStream>.Success(fileStream);
            }
            catch (FileNotFoundException exception)
            {
                return Result<FileStream>.Failure( "FILE_NOT_FOUND", exception.Message);
            }
            catch (UnauthorizedAccessException exception)
            {
                return Result<FileStream>.Failure( "FILE_ACCESS_DENIED", exception.Message);
            }
            catch (IOException exception)
            {
                return Result<FileStream>.Failure( "FILE_IO_ERROR", exception.Message);
            }
            catch (Exception exception)
            {
                return Result<FileStream>.Failure( "FILE_OPEN_READ_ERROR", exception.Message);
            }
        }

        public static Result<long> GetLength(string  filepath)
        {
            FileInfo fileInfo = new(filepath);
            Result<long> lengthResult;

            if (!fileInfo.Exists)
            {
                lengthResult = Result<long>.Failure("FILE_NOT_FOUND", $"File NOT FOUND: {filepath}");
            }
            else
            {
                lengthResult = Result<long>.Success(fileInfo.Length);
            }

            return Result<long>.Success(fileInfo.Length);
        }

        public static DirectoryInfo CreateTempFolder(string? tempFolderTag)
        {
            if (tempFolderTag == null)
            {
                tempFolderTag = string.Empty;
            }

            string tempRootPath = Path.Combine(Path.GetTempPath(), tempFolderTag);
            Directory.CreateDirectory(tempRootPath);

            while (true)
            {
                string directoryName = $"analysis-{Path.GetRandomFileName()}";
                string directoryPath = Path.Combine(tempRootPath, directoryName);

                if (Directory.Exists(directoryPath))
                {
                    continue;
                }

                var directory = Directory.CreateDirectory(directoryPath);
                return directory;
            }
        }

        public static string CreateTempFilePath(string? extension = null)
        {
            string? appName = Path.GetFileNameWithoutExtension(Environment.ProcessPath);
            if (appName == null)
            {
                appName = string.Empty;
            }

            string tempRootPath = Path.Combine(Path.GetTempPath(), appName);
            Directory.CreateDirectory(tempRootPath);

            string normalizedExtension = string.IsNullOrWhiteSpace(extension)
                ? "tmp"
                : extension.TrimStart('.');

            while (true)
            {
                string fileName = $"{Path.GetFileNameWithoutExtension(Path.GetRandomFileName())}.{normalizedExtension}";
                string filePath = Path.Combine(tempRootPath, fileName);

                try
                {
                    using FileStream fileStream = new(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                    return filePath;
                }
                catch (IOException)
                {
                    continue;
                }
            }
        }

    }
}
