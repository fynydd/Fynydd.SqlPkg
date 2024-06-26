using System.Reflection;
using System.Text;
using Fynydd.SqlPkg.Extensions;
using CliWrap;
using Microsoft.Data.SqlClient;

namespace Fynydd.SqlPkg;

public class ApplicationState
{
    #region Properties
    
    public List<CliArgument> OriginalArguments { get; set; } = new();
    public List<CliArgument> WorkingArguments { get; set; } = new();
    
    public string Action { get; set; } = string.Empty;
    public string SqlPackageVersion { get; set; } = "0.0.0.0";

    public string SourceConnectionString { get; set; } = string.Empty;
    public string SourceServerName { get; set; } = string.Empty;
    public string SourceDatabaseName { get; set; } = string.Empty;
    public string SourceUserName { get; set; } = string.Empty;
    public string SourcePassword { get; set; } = string.Empty;
    public int SourceConnectionTimeout { get; set; } = 30;
    public int SourceCommandTimeout { get; set; } = 120;
    public bool SourceTrustServerCertificate { get; set; } = true;

    public string TargetConnectionString { get; set; } = string.Empty;
    public string TargetServerName { get; set; } = string.Empty;
    public string TargetDatabaseName { get; set; } = string.Empty;
    public string TargetUserName { get; set; } = string.Empty;
    public string TargetPassword { get; set; } = string.Empty;
    public int TargetConnectionTimeout { get; set; } = 30;
    public int TargetCommandTimeout { get; set; } = 120;
    public bool TargetTrustServerCertificate { get; set; } = true;
    
    public string SourceFile { get; set; } = string.Empty;
    public string TargetFile { get; set; } = string.Empty;
    public string LogFile { get; set; } = string.Empty;

    #endregion
    
    #region CLI Arguments
    
    public void ImportArguments(IEnumerable<string> args)
    {
        foreach (var arg in args)
        {
            if (string.IsNullOrEmpty(arg) || arg.Length < 2)
                continue;

            var delimiter = arg.StartsWith("/p:", StringComparison.CurrentCultureIgnoreCase) ? '=' : ':';
            var delimiterIndex = arg.IndexOf(delimiter);
            
            OriginalArguments.Add(new CliArgument
            {
	            Key = delimiterIndex > 0 ? $"{arg[..delimiterIndex]}{delimiter}" : arg,
	            Value = delimiterIndex > -1 ? arg[^(arg.Length - delimiterIndex - 1)..] : string.Empty
            });
        }

        if (OriginalArguments.HasArgument("/Action:", "/a:"))
	        Action = OriginalArguments.GetArgumentValue("/Action:", "/a:").ApTitleCase();

        SourceFile = OriginalArguments.GetArgumentValue("/SourceFile:", "/sf:").RemoveWrappedQuotes();
        TargetFile = OriginalArguments.GetArgumentValue("/TargetFile:", "/tf:").RemoveWrappedQuotes();
        LogFile = OriginalArguments.GetArgumentValue("/DiagnosticsFile:", "/df:").RemoveWrappedQuotes();
        
        NormalizeConnectionInfo();
    }
    
    /// <summary>
    /// Process server/database info to ensure connection settings/strings exist.
    /// </summary>
    public void NormalizeConnectionInfo()
    {
	    SourceConnectionString = OriginalArguments.GetArgumentValue("/SourceConnectionString:", "/scs:").RemoveWrappedQuotes();
	    TargetConnectionString = OriginalArguments.GetArgumentValue("/TargetConnectionString:", "/tcs:").RemoveWrappedQuotes();

	    #region Source
	    
	    if (string.IsNullOrEmpty(SourceConnectionString) == false)
	    {
		    var builder = new SqlConnectionStringBuilder(SourceConnectionString);

		    builder.TrustServerCertificate =
			    OriginalArguments.HasArgument("/SourceTrustServerCertificate:", "/stsc:")
				    ? OriginalArguments.GetArgumentValue("/SourceTrustServerCertificate:", "/stsc:", "true").Equals("true", StringComparison.CurrentCultureIgnoreCase)
				    : builder.TrustServerCertificate;

		    builder.ConnectTimeout =
			    OriginalArguments.HasArgument("/SourceTimeout:", "/st:")
				    ? int.Parse(OriginalArguments.GetArgumentValue("/SourceTimeout:", "/st:", "30"))
				    : builder.ConnectTimeout;
		    
		    builder.CommandTimeout =
			    OriginalArguments.HasArgument("/p:CommandTimeout=")
				    ? int.Parse(OriginalArguments.GetArgumentValue("/p:CommandTimeout=", string.Empty, "120"))
				    : builder.CommandTimeout;

		    SourceServerName = builder.DataSource;
		    SourceUserName = builder.UserID;
		    SourcePassword = builder.Password;
		    SourceDatabaseName = builder.InitialCatalog;
		    SourceConnectionTimeout = builder.ConnectTimeout;
		    SourceCommandTimeout = builder.CommandTimeout;
		    SourceTrustServerCertificate = builder.TrustServerCertificate;
		    SourceConnectionString = builder.ToString();
	    }

	    else
	    {
		    SourceServerName = OriginalArguments.GetArgumentValue("/SourceServerName:", "/ssn:").RemoveWrappedQuotes();
		    SourceDatabaseName = OriginalArguments.GetArgumentValue("/SourceDatabaseName:", "/sdn:").RemoveWrappedQuotes();
		    SourceUserName = OriginalArguments.GetArgumentValue("/SourceUser:", "/su:").RemoveWrappedQuotes();
		    SourcePassword = OriginalArguments.GetArgumentValue("/SourcePassword:", "/sp:").RemoveWrappedQuotes();
		    SourceConnectionTimeout = int.Parse(OriginalArguments.GetArgumentValue("/SourceTimeout:", "/st:", "30"));
		    SourceTrustServerCertificate = OriginalArguments.GetArgumentValue("/SourceTrustServerCertificate:", "/stsc:", "true").Equals("true", StringComparison.CurrentCultureIgnoreCase);
		    SourceCommandTimeout = int.Parse(OriginalArguments.GetArgumentValue("/p:CommandTimeout=", string.Empty, "120"));

		    var builder = new SqlConnectionStringBuilder
		    {
			    DataSource = SourceServerName,
			    InitialCatalog = SourceDatabaseName,
			    UserID = SourceUserName,
			    Password = SourcePassword,
			    TrustServerCertificate = SourceTrustServerCertificate,
			    ConnectTimeout = SourceConnectionTimeout,
			    CommandTimeout = SourceCommandTimeout
		    };

		    SourceConnectionString = builder.ToString();
	    }

	    #endregion
	    
	    #region Target
	    
	    if (string.IsNullOrEmpty(TargetConnectionString) == false)
	    {
		    var builder = new SqlConnectionStringBuilder(TargetConnectionString);

		    builder.TrustServerCertificate =
			    OriginalArguments.HasArgument("/TargetTrustServerCertificate:", "/ttsc:")
				    ? OriginalArguments.GetArgumentValue("/TargetTrustServerCertificate:", "/ttsc:", "true").Equals("true", StringComparison.CurrentCultureIgnoreCase)
				    : builder.TrustServerCertificate;

		    builder.ConnectTimeout =
			    OriginalArguments.HasArgument("/TargetTimeout:", "/tt:")
				    ? int.Parse(OriginalArguments.GetArgumentValue("/TargetTimeout:", "/tt:", "30"))
				    : builder.ConnectTimeout;
		    
		    builder.CommandTimeout =
			    OriginalArguments.HasArgument("/p:CommandTimeout=")
				    ? int.Parse(OriginalArguments.GetArgumentValue("/p:CommandTimeout=", string.Empty, "120"))
				    : builder.CommandTimeout;

		    TargetServerName = builder.DataSource;
		    TargetUserName = builder.UserID;
		    TargetPassword = builder.Password;
		    TargetDatabaseName = builder.InitialCatalog;
		    TargetConnectionTimeout = builder.ConnectTimeout;
		    TargetCommandTimeout = builder.CommandTimeout;
		    TargetTrustServerCertificate = builder.TrustServerCertificate;
		    TargetConnectionString = builder.ToString();
	    }

	    else
	    {
		    TargetServerName = OriginalArguments.GetArgumentValue("/TargetServerName:", "/tsn:").RemoveWrappedQuotes();
		    TargetDatabaseName = OriginalArguments.GetArgumentValue("/TargetDatabaseName:", "/tdn:").RemoveWrappedQuotes();
		    TargetUserName = OriginalArguments.GetArgumentValue("/TargetUser:", "/tu:").RemoveWrappedQuotes();
		    TargetPassword = OriginalArguments.GetArgumentValue("/TargetPassword:", "/tp:").RemoveWrappedQuotes();
		    TargetConnectionTimeout = int.Parse(OriginalArguments.GetArgumentValue("/TargetTimeout:", "/tt:", "30"));
		    TargetTrustServerCertificate = OriginalArguments.GetArgumentValue("/TargetTrustServerCertificate:", "/ttsc:", "true").Equals("true", StringComparison.CurrentCultureIgnoreCase);
		    TargetCommandTimeout = int.Parse(OriginalArguments.GetArgumentValue("/p:CommandTimeout=", string.Empty, "120"));

		    var builder = new SqlConnectionStringBuilder
		    {
			    DataSource = TargetServerName,
			    InitialCatalog = TargetDatabaseName,
			    UserID = TargetUserName,
			    Password = TargetPassword,
			    TrustServerCertificate = TargetTrustServerCertificate,
			    ConnectTimeout = TargetConnectionTimeout,
			    CommandTimeout = TargetCommandTimeout
		    };

		    TargetConnectionString = builder.ToString();
	    }

	    #endregion
    }
    
    /// <summary>
    /// Process CLI arguments into WorkingArguments.
    /// </summary>
    /// <returns></returns>
    public void BuildBackupArguments()
    {
	    WorkingArguments.Clear();
	    
	    foreach (var arg in OriginalArguments)
	    {
		    if (ArgumentHelpers.ExportSkippedArguments.Any(a => a.Equals(arg.Key, StringComparison.CurrentCultureIgnoreCase)) == false)
			    WorkingArguments.Add(arg);
	    }

	    WorkingArguments.Insert(0, new CliArgument
	    {
		    Key = "/a:",
		    Value = "Export"
	    });

	    WorkingArguments.Insert(1, new CliArgument
	    {
		    Key = "/SourceConnectionString:",
		    Value = SourceConnectionString.RemoveWrappedQuotes()
	    });

	    WorkingArguments.Insert(2, new CliArgument
	    {
		    Key = "/TargetFile:",
		    Value = TargetFile
	    });

	    if (string.IsNullOrEmpty(LogFile) == false)
		    WorkingArguments.Insert(3, new CliArgument
		    {
			    Key = "/DiagnosticsFile:",
			    Value = LogFile
		    });
	    
	    TargetFile.EnsureDirectoryExists();
	    LogFile.EnsureDirectoryExists();

	    WorkingArguments.SetDefault("/p:VerifyExtraction=", "false");
    }
    
    /// <summary>
    /// Process CLI arguments into WorkingArguments.
    /// </summary>
    /// <returns></returns>
    public void BuildRestoreArguments()
    {
	    WorkingArguments.Clear();

	    foreach (var arg in OriginalArguments)
	    {
		    if (ArgumentHelpers.ImportSkippedArguments.Any(a => a.Equals(arg.Key, StringComparison.CurrentCultureIgnoreCase)) == false)
			    WorkingArguments.Add(arg);
	    }

	    WorkingArguments.Insert(0, new CliArgument
	    {
		    Key = "/a:",
		    Value = "Import"
	    });

	    WorkingArguments.Insert(1, new CliArgument
	    {
		    Key = "/TargetConnectionString:",
		    Value = TargetConnectionString.RemoveWrappedQuotes()
	    });

	    WorkingArguments.Insert(2, new CliArgument
	    {
		    Key = "/SourceFile:",
		    Value = SourceFile
	    });

	    if (string.IsNullOrEmpty(LogFile) == false)
		    WorkingArguments.Insert(3, new CliArgument
		    {
			    Key = "/DiagnosticsFile:",
			    Value = LogFile
		    });
	    
	    LogFile.EnsureDirectoryExists();
    }
    
    /// <summary>
    /// Process table names and remove excluded tables from arguments.
    /// </summary>
    public async Task ProcessTableDataArguments()
    {
	    var tableDataList = new List<CliArgument>();
	    
        if (OriginalArguments.HasArgument("/p:TableData="))
        {
            foreach (var argument in OriginalArguments.Where(a => a.Key.Equals("/p:TableData=", StringComparison.CurrentCultureIgnoreCase)))
            {
                tableDataList.Add(new CliArgument
                {
	                Key = "/p:TableData=",
	                Value = argument.Value.RemoveWrappedQuotes().NormalizeTableName() 
                });
            }
        }

        else
        {
	        foreach (var tableName in await SqlTools.LoadUserTableNamesAsync(SourceConnectionString))
	        {
		        tableDataList.Add(new CliArgument
		        {
			        Key = "/p:TableData=",
			        Value = tableName.RemoveWrappedQuotes().NormalizeTableName()
		        });
	        }
	        
            #region Handle Table Data Exclusions

            if (OriginalArguments.HasArgument("/p:ExcludeTableData="))
            {
                foreach (var exclusion in OriginalArguments.Where(a => a.Key.Equals("/p:ExcludeTableData=", StringComparison.CurrentCultureIgnoreCase)))
                {
                    var excludedTableName = exclusion.Value.RemoveWrappedQuotes().NormalizeTableName();

                    if (string.IsNullOrEmpty(excludedTableName))
                        continue;

                    if (excludedTableName.Contains('*'))
                    {
                        if (excludedTableName.EndsWith("*]"))
                        {
	                        var wildcard = excludedTableName.Left("*");

	                        tableDataList.RemoveAll(t => t.Value.StartsWith(wildcard, StringComparison.CurrentCultureIgnoreCase));
                        }

                        else
                        {
	                        var wildcard = excludedTableName.Right("].[*");

							tableDataList.RemoveAll(t => t.Value.EndsWith(wildcard, StringComparison.CurrentCultureIgnoreCase));
                        }
                    }

                    else
                    {
                        tableDataList.RemoveAll(t => t.Value.Equals(excludedTableName, StringComparison.CurrentCultureIgnoreCase));
                    }
                }

                var filteredArguments = new List<CliArgument>();
                
                foreach (var arg in WorkingArguments)
                {
                    if (arg.Key.Equals("/p:TableData=", StringComparison.CurrentCultureIgnoreCase))
                        continue;

                    filteredArguments.Add(arg);
                }

                WorkingArguments.Clear();
                WorkingArguments.AddRange(filteredArguments);
                WorkingArguments.AddRange(tableDataList);
            }

            #endregion
        }
    }
    
    #endregion
    
    #region Output Settings

    /// <summary>
    /// Column width of the console output for items that require cropping.
    /// </summary>
    public static int ColumnWidth => 78;
    public static int FullColumnWidth => Console.WindowWidth - 1;

    #endregion
    
    #region App Info

    /// <summary>
    /// Get the full path to the blank.dacpac file.
    /// </summary>
    /// <returns></returns>
    public static string GetBlankDacPacPath()
    {
	    var resourcePath = Assembly.GetExecutingAssembly().Location;

	    while (resourcePath.LastIndexOf(Path.DirectorySeparatorChar) > -1)
	    {
		    resourcePath = resourcePath[..resourcePath.LastIndexOf(Path.DirectorySeparatorChar)];

		    if (Directory.Exists(Path.Combine(resourcePath, "contentFiles")) == false)
			    continue;

		    resourcePath = Path.Combine(resourcePath, "contentFiles", "any", "any", "blank.dacpac");

		    if (File.Exists(resourcePath))
			    break;
	    }

	    return resourcePath;
    }
    
    /// <summary>
    /// Determine if SqlPackage is installed.
    /// </summary>
    /// <returns></returns>
    public async Task<bool> SqlPackageIsInstalled()
    {
	    var stdOut = new StringBuilder();

	    try
	    {
		    var arguments = new List<CliArgument>
		    {
			    new ()
			    {
				    Key = "/version:",
				    Value = "true"
			    }
		    };

		    var cmd = Cli.Wrap("sqlpackage")
			    .WithArguments(arguments.GetArgumentsForCli())
			    .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOut))
			    .WithStandardErrorPipe(PipeTarget.Null);
		    
		    _ = await cmd.ExecuteAsync();
		    
		    SqlPackageVersion = stdOut.ToString().Trim();

		    try
		    {
			    // Update to latest
			    
			    cmd = Cli.Wrap("dotnet")
				    .WithArguments(new [] { "tool", "update", "--global", "microsoft.sqlpackage" })
				    .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOut))
				    .WithStandardErrorPipe(PipeTarget.Null);
		    
			    _ = await cmd.ExecuteAsync();
		    }

		    catch
		    {
			    // ignored
		    }

		    return true;
	    }

	    catch
	    {
		    try
		    {
			    var cmd = Cli.Wrap("dotnet")
				    .WithArguments(new [] { "tool", "install", "--global", "microsoft.sqlpackage" })
				    .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOut))
				    .WithStandardErrorPipe(PipeTarget.Null);
		    
			    _ = await cmd.ExecuteAsync();

			    return true;
		    }

		    catch
		    {
			    Console.Write("ERROR");
			    CliHelpers.WriteArrow(true);
			    Console.WriteLine("Could not install Microsoft's 'SqlPackage' command.");
			    Console.WriteLine("Try manually installing it in your command line interface/terminal using \"dotnet tool install --global microsoft.sqlpackage\".");
            
			    return false;
		    }
	    }
    }
    
    public static string AppMajorVersion
    {
        get
        {
            var result = string.Empty;

            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                result = assembly.GetName().Version?.Major.ToString();
            }

            catch (Exception e)
            {
                Console.WriteLine($"Settings.AppMajorVersion Exception: {e.Message}");
            }

            return result ?? string.Empty;
        }
    }
    
    public static string AppMinorVersion
    {
        get
        {
            var result = string.Empty;

            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                result = assembly.GetName().Version?.Minor.ToString();
            }

            catch (Exception e)
            {
                Console.WriteLine($"Settings.AppMinorVersion Exception: {e.Message}");
            }

            return result ?? string.Empty;
        }
    }
    
    public static string AppBuildVersion
    {
        get
        {
            var result = string.Empty;

            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                result = assembly.GetName().Version?.Build.ToString();
            }

            catch (Exception e)
            {
                Console.WriteLine($"Settings.AppBuildVersion Exception: {e.Message}");
            }

            return result ?? string.Empty;
        }
    }

    public static string AppRevisionVersion
    {
        get
        {
            var result = string.Empty;

            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                result = assembly.GetName().Version?.Revision.ToString();
            }

            catch (Exception e)
            {
                Console.WriteLine($"Settings.AppRevisionVersion Exception: {e.Message}");
            }

            return result ?? string.Empty;
        }
    }
    
    public static string Version
    {
        get
        {
            var result = string.Empty;

            try
            {
                result = AppMajorVersion + "." + AppMinorVersion + "." + AppBuildVersion;
            }

            catch (Exception e)
            {
                Console.WriteLine($"Settings.Version Exception: {e.Message}");
            }

            return result;
        }
    }
    
    #endregion
}