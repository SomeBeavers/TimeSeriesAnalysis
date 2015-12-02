using System;
using System.Configuration;
using System.Data.SqlClient;
using Microsoft.SqlServer.Management.Smo;

namespace TimeSeriesAnalysis
{
  /// <summary>
  /// Class for providing helpers for restoring DB.
  /// </summary>
  class RestoreDBHelper
  {
    #region private methods

    /// <summary>
    /// Restore DB from backup.
    /// </summary>
    private static bool RestoreDB()
    {
      string connectionString =
        ConfigurationManager.ConnectionStrings["DeathsConnectionString"].ToString();
      var builder = new SqlConnectionStringBuilder(connectionString);
      var myServer = new Server(builder.DataSource);

      var res = new Restore();
      res.Database = "Deaths";

      res.Action = RestoreActionType.Database;
      res.Devices.AddDevice(
        Environment.CurrentDirectory + @"\Resources\DeathsFullBackup", DeviceType.File);
      res.ReplaceDatabase = true;
      res.SqlRestore(myServer);
      return true;
    }

    #endregion

    #region public methods

    /// <summary>
    /// Restore DB from backup if DB does not exist.
    /// </summary>
    public static bool dbTraining()
    {
      string query = "DECLARE @dbname nvarchar(128) " +
                     "SET @dbname = N'Deaths' " +
                     "IF NOT (EXISTS (SELECT name  " +
                     "FROM master.dbo.sysdatabases  " +
                     "WHERE ('[' + name + ']' = @dbname  " +
                     "OR name = @dbname))) " +
                     "select * from AllDeaths";
      try
      {
        PredictHelper.ExecuteSQLQueryData(query);
      }
      catch(Exception)
      {
        return RestoreDB();
      }
      return true;
    }

    #endregion
  }
}
