using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using Microsoft.AnalysisServices.AdomdClient;
using Microsoft.AnalysisServices.Xmla;

namespace TimeSeriesAnalysis
{
  /// <summary>
  /// Class for providing helpers for connection to Microsoft Analysis Services.
  /// </summary>
  internal class AnalysisServicesHelper
  {
    #region constants

    /// <summary>
    /// Mining structure title.
    /// </summary>
    public static string miningStructureTitle = "PredictionMiningStructure1";

    /// <summary>
    /// Table name where initial data for prediction is keeping.
    /// </summary>
    public static string tableNameForPrediction = "dbo.t2";

    #endregion

    #region private fields

    /// <summary>
    /// XML query for creating mining structure.
    /// </summary>
    private static string createMiningStructure = "CREATE MINING STRUCTURE [" +
                                                  miningStructureTitle + "] " +
                                                  "([Id] DATE KEY TIME, [Value] LONG CONTINUOUS)";

    /// <summary>
    /// XML query for clearing mining structure.
    /// </summary>
    private static string clearMiningStructure = "delete from mining structure [" +
                                                 miningStructureTitle + "]";

    /// <summary>
    /// XML query for training mining model.
    /// </summary>
    private static string trainMiningModel = "INSERT INTO MINING STRUCTURE [" +
                                             miningStructureTitle + "] " +
                                             "([Id],[Value] ) " +
                                             "OPENQUERY([Deaths],'SELECT [id], [value] FROM " +
                                             tableNameForPrediction + "')";

    #endregion

    #region public methods for executing queries

    /// <summary>
    /// Execute XMLA script for creating multidimensional project.
    /// </summary>
    public static void ExecuteXMLAScript()
    {
      string connectionString =
        ConfigurationManager.ConnectionStrings["ConnectionStringAnalysisServices"].ToString();
      var client = new XmlaClient();
      var builder = new SqlConnectionStringBuilder(connectionString);
      client.Connect(builder.DataSource);
      string xmla = File.ReadAllText(Environment.CurrentDirectory + @"\Resources\XMLAQuery2.xmla");
      client.Send(xmla, null);
      client.Disconnect();
    }

    /// <summary>
    /// Conect to Analysis Services and execute MDX query.
    /// </summary>
    /// <param name="query">MDX query to execute (Analysis Services)</param>
    public static void ExecuteMDXQuery(string query)
    {
      string connectionString =
        ConfigurationManager.ConnectionStrings["ConnectionStringAnalysisServices"].ConnectionString;
      var myconnect = new AdomdConnection(connectionString);
      var commandToExecute = new AdomdCommand(query, myconnect);
      myconnect.Open();
      commandToExecute.Execute();
      myconnect.Close();
    }

    #endregion

    #region public methods

    /// <summary>
    /// Create mining project.
    /// </summary>
    public static void CreateMultidimensionalProject()
    {
      ExecuteXMLAScript();
    }

    /// <summary>
    /// Create and traing mining model.
    /// </summary>
    public static void ConnectToAnalysisServices()
    {
      string miningModelTitle = "PredictionMiningModel_" + PredictHelper.predictionNumber;

      string createMiningModel = "ALTER MINING STRUCTURE [" + miningStructureTitle + "] " +
                                 "ADD MINING MODEL [" + miningModelTitle + "] " +
                                 " ([Id], [Value] predict) " +
                                 "USING Microsoft_Time_Series (PERIODICITY_HINT = '{365,31,30}', FORECAST_METHOD = 'MIXED')";


      if (PredictHelper.predictionNumber == 0)
      {
        CreateMultidimensionalProject();
        ExecuteMDXQuery(createMiningStructure);
      }
      ExecuteMDXQuery(clearMiningStructure);
      ExecuteMDXQuery(createMiningModel);
      ExecuteMDXQuery(trainMiningModel);
    }

    #endregion
  }
}
