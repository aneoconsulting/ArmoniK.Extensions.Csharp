using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Threading;

using ArmoniK.DevelopmentKit.WorkerApi.Common.Exceptions;


namespace ArmoniK.DevelopmentKit.WorkerApi.Common
{
  public class ZipArchiver
  {
    private static string rootAppPath = "/tmp/packages";

    /// <summary>
    /// 
    /// </summary>
    /// <param name="assemblyNameFilePath"></param>
    /// <returns></returns>
    public static bool IsZipFile(string assemblyNameFilePath)
    {
      //ATm ONLY Check the extensions 

      string extension = Path.GetExtension(assemblyNameFilePath);
      if (extension?.ToLower() == ".zip")
        return true;

      return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="assemblyNameFilePath"></param>
    /// <returns></returns>
    /// <exception cref="WorkerApiException"></exception>
    public static IEnumerable<string> ExtractNameAndVersion(string assemblyNameFilePath)
    {
      string filePathNoExt;
      string appName;
      string versionName;

      try
      {
        filePathNoExt = Path.GetFileNameWithoutExtension(assemblyNameFilePath);
      }
      catch (ArgumentException e)
      {
        throw new WorkerApiException(e);
      }

      // Instantiate the regular expression object.
      string pat = @"(.*)-v([\d\w]+\.[\d\w]+\.[\d\w]+)";

      Regex r = new Regex(pat,
                          RegexOptions.IgnoreCase);

      Match m          = r.Match(filePathNoExt);
      
      if (m.Success)
      {
        appName     = m.Groups[1].Value;
        versionName = m.Groups[2].Value;
      }
      else
      {
        throw new WorkerApiException("File name format doesn't match");
      }

      return new[] { appName, versionName };
    }

    public static string GetLocalPathToAssembly(string pathToZip)
    {
      string filePathNoExt;
      //Remove directory from path
      try
      {
        filePathNoExt = Path.GetFileNameWithoutExtension(pathToZip);
      }
      catch (ArgumentException e)
      {
        throw new WorkerApiException(e);
      }

      var assemblyInfo    = ExtractNameAndVersion(pathToZip);
      var assemblyName    = assemblyInfo.ElementAt(0);
      var assemblyVersion = assemblyInfo.ElementAt(1);
      var basePath        = $"{rootAppPath}/{assemblyName}/{assemblyVersion}";

      return $"{basePath}/{assemblyName}.dll";
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="appVolume"></param>
    /// <param name="assemblyNameFilePath"></param>
    /// <param name="waitForArchiver"></param>
    /// <returns></returns>
    /// <exception cref="WorkerApiException"></exception>
    public static bool ArchiveAlreadyExtracted(string assemblyNameFilePath, int waitForArchiver = 300)
    {
      string filePathNoExt;
      //Remove directory from path
      try
      {
        filePathNoExt = Path.GetFileNameWithoutExtension(assemblyNameFilePath);
      }
      catch (ArgumentException e)
      {
        throw new WorkerApiException(e);
      }

      var assemblyInfo    = ExtractNameAndVersion(assemblyNameFilePath);
      var assemblyName    = assemblyInfo.ElementAt(0);
      var assemblyVersion = assemblyInfo.ElementAt(1);
      var basePath        = $"{rootAppPath}/{assemblyName}/{assemblyVersion}";

      if (Directory.Exists($"{rootAppPath}/{assemblyName}/{assemblyVersion}"))
      {
        //Now at least if dll exist or if a lock file exists and wait for unlock
        if (File.Exists($"{basePath}/{assemblyName}.dll"))
        {
          return true;
        }

        if (File.Exists($"{basePath}/{assemblyName}.lock"))
        {
          int retry       = 0;
          int loopingWait = 2; // 2 secs

          if (waitForArchiver == 0) return true;

          while (!File.Exists($"{basePath}/{assemblyName}.lock"))
          {
            Thread.Sleep(loopingWait * 1000);
            retry++;
            if (retry > (waitForArchiver >> 2))
            {
              throw new WorkerApiException($"Wait for unlock unzip was timeout after {waitForArchiver * 2} seconds");
            }
          }
        }
      }

      return false;
    }

    /// <summary>
    /// Unzip Archive if the temporary folder doesn't contain the
    /// foler convention path should exist in /tmp/{AppName}/{AppVersion/AppName.dll
    /// </summary>
    /// <param name="assemblyNameFilePath">The path to the zip file
    /// Pattern for zip file has to be {AppName}-v{AppVersion}.zip
    /// </param>
    /// <returns>return string containing the path to the client assembly (.dll) </returns>
    public static string UnzipArchive(string assemblyNameFilePath)
    {
      if (!IsZipFile(assemblyNameFilePath))
        throw new WorkerApiException("Cannot yet extract or manage raw data other than zip archive");

      var assemblyInfo    = ExtractNameAndVersion(assemblyNameFilePath);
      var assemblyVersion = assemblyInfo.ElementAt(1);
      var assemblyName    = assemblyInfo.ElementAt(0);


      string pathToAssembly    = $"{rootAppPath}/{assemblyName}/{assemblyVersion}/{assemblyName}.dll";
      string pathToAssemblyDir = $"{rootAppPath}/{assemblyName}/{assemblyVersion}";

      if (ArchiveAlreadyExtracted(assemblyNameFilePath,
                                  0))
      {
        return pathToAssembly;
      }

      if (!Directory.Exists(pathToAssemblyDir))
        Directory.CreateDirectory(pathToAssemblyDir);

      string lockFileName = $"{pathToAssemblyDir}/{assemblyName}.lock";


      using (FileStream fileStream = new FileStream(
               lockFileName,
               FileMode.OpenOrCreate,
               FileAccess.ReadWrite,
               FileShare.ReadWrite))
      {
        var lockfileForExtractionString = "Lockfile for extraction";

        UnicodeEncoding unicodeEncoding = new UnicodeEncoding();
        int             textLength      = unicodeEncoding.GetByteCount(lockfileForExtractionString);

        if (fileStream.Length == 0)
        {
          //Try to lock file to protect extraction
          fileStream.Write(new UnicodeEncoding().GetBytes(lockfileForExtractionString),
                           0,
                           unicodeEncoding.GetByteCount(lockfileForExtractionString));
        }

        try
        {
          fileStream.Lock(0,
                          textLength);
        }
        catch (IOException e)
        {
          return pathToAssembly;
        }
        catch (Exception e)
        {
          throw new WorkerApiException(e);
        }


        try
        {
          ZipFile.ExtractToDirectory(assemblyNameFilePath,
                                     rootAppPath);
        }
        catch (Exception e)
        {
          throw new WorkerApiException(e);
        }
        finally
        {
          fileStream.Unlock(0,
                            textLength);
        }
      }

      //Check now if the assembly is present
      if (!File.Exists(pathToAssembly))
      {
        throw new WorkerApiException($"Fail to find assembly {pathToAssembly}. Something went wrong during the extraction. " +
                                     $"Please sure that tree folder inside is {assemblyName}/{assemblyVersion}/*.dll");
      }

      return pathToAssembly;
    }
  }
}