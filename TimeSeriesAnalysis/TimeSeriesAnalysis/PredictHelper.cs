using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;

namespace TimeSeriesAnalysis
{
  /// <summary>
  /// Class for providing helpers for connection to Server.
  /// </summary>
  class PredictHelper
  {
    #region private fields

    /// <summary>
    /// Prediction number.
    /// </summary>
    public static int predictionNumber = 0;

    /// <summary>
    /// Query for deleting all data from t2 table.
    /// </summary>
    private static string deleteDataInt2 =  "DELETE FROM dbo.t2 ";

    /// <summary>
    /// Query for getting all column titles from Prediction table.
    /// </summary>
    private static string getColumnNamesFromPredictionTable = "SELECT COLUMN_NAME " +
                                                              "FROM INFORMATION_SCHEMA.COLUMNS " +
                                                              "WHERE TABLE_NAME = N'Prediction' " +
                                                              "ORDER BY COLUMN_NAME DESC";

    #endregion

    #region methods to execute SQL scripts

    /// <summary>
    /// Execute SQL script and return result data.
    /// </summary>
    public static DataTable ExecuteSQLQueryData(string query)
    {
      string connectionString =
        ConfigurationManager.ConnectionStrings["DeathsConnectionString"].ConnectionString;
      string cmdString = String.Empty;
      DataTable dataTable = new DataTable("Dates");
      using (SqlConnection con = new SqlConnection(connectionString))
      {
        con.Open();
        cmdString = query;
        SqlCommand cmd = new SqlCommand(cmdString, con);
        SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(cmd);
        sqlDataAdapter.Fill(dataTable);
        con.Close();
      }
      return dataTable;
    }

    /// <summary>
    /// Execute SQL script.
    /// </summary>
    public static void ExecuteSQLQuery(string query)
    {
      string connectionString =
        ConfigurationManager.ConnectionStrings["DeathsConnectionString"].ConnectionString;
      var myconnect = new SqlConnection(connectionString);
      var commandToExecute = new SqlCommand(query, myconnect);
      myconnect.Open();
      commandToExecute.ExecuteNonQuery();
      myconnect.Close();
    }

    #endregion

    #region public methods

    /// <summary>
    /// Update PredictionDeviation tabel with currently predicted data.
    /// </summary>
    public static void UpdateDeviations(int initialPeriod, int predictionPeriod,
      double deviation)
    {
      string insertDeviations = "insert into PredictionDeviation values(" +
                                predictionNumber + "," + initialPeriod + "," +
                                predictionPeriod + "," + deviation + ")";
      try
      {
        ExecuteSQLQuery(insertDeviations);
      }
      catch (Exception)
      {
        Console.WriteLine("Exception during inserting to MeanDeviation table.");
        var errorMessage = MessageBox.Show("Unable to insert mean deviation to DB.",
          "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        if (errorMessage == MessageBoxResult.OK)
        {
        }
        throw new Exception();
      }
    }

    /// <summary>
    /// Get prediction number.
    /// </summary>
    public static void SetPredictionNumber()
    {
      var dataTable = ExecuteSQLQueryData(getColumnNamesFromPredictionTable);
      var predictionNumbers = (from DataRow dr in dataTable.Rows
        where Convert.ToString(dr["COLUMN_NAME"]) != "id"
        select Convert.ToInt16(Convert.ToString(dr["COLUMN_NAME"]).Remove(0, 11))).Select(
          dummy => (int) dummy).ToList();
      if (predictionNumbers.Count == 0)
      {
        return;
      }
      predictionNumber = predictionNumbers.Max() + 1;
    }

    /// <summary>
    /// Move all data from start to end date to t2 table for prediction.
    /// </summary>
    /// <param name="startDate">Initial start date</param>
    /// <param name="endDate">Initial end date</param>
    public static void UpdatePredictionTable(DateTime startDate, DateTime endDate)
    {
      ExecuteSQLQuery(deleteDataInt2);
      string insertInitialDataTOt2 = "insert into dbo.t2 " +
                                     "SELECT * FROM dbo.AllDeaths " +
                                     "WHERE id BETWEEN CONVERT(date,'" +
                                     MainWindow.InitialStartDate +
                                     "') AND CONVERT(date,'" + MainWindow.InitialEndDate +
                                     "');";
      try
      {
        ExecuteSQLQuery(insertInitialDataTOt2);
      }
      catch (Exception)
      {
        Console.WriteLine("Exception during updating t2 table.");
        var errorMessage = MessageBox.Show("Data insertion to table failed. Please check date ranges.",
          "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        if (errorMessage == MessageBoxResult.OK)
        {
        }
        throw new Exception();
      }
    }

    /// <summary>
    /// Predict values from start to end date and put them to Prediction table.
    /// </summary>
    public static void PredictDeaths(int firstParameter, int lastParameter)
    {
      string miningModelTitle = "PredictionMiningModel_" + PredictHelper.predictionNumber;
      string addPredictedValuesToColumn = "UPDATE Prediction " +
                                          "SET Prediction_" + predictionNumber +
                                          " = (SELECT [Predicted_Quantity.Value]  FROM " +
                                          "OPENQUERY(DEATHSANALYSISSERVICES, " +
                                          "'SELECT FLATTENED " +
                                          "PredictTimeSeries([" + miningModelTitle +
                                          "].[Value]," + firstParameter + "," +
                                          lastParameter + ") as [Predicted_Quantity] " +
                                          "From " +
                                          "[" + miningModelTitle + "]') " +
                                          "WHERE [Predicted_Quantity.$TIME]=CONVERT(VARCHAR(24),Prediction.id,121)) ";

      string addPredictedColumn = "ALTER TABLE Prediction ADD Prediction_" +
                                  predictionNumber + " INT ";

      try
      {
        ExecuteSQLQuery(addPredictedColumn);
        ExecuteSQLQuery(addPredictedValuesToColumn);
      }
      catch (Exception)
      {
        Console.WriteLine("Exception during values prediction.");
        var errorMessage =
          MessageBox.Show(
            "Historic prediction is not available. Please change prediction or initial date range.",
            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        if (errorMessage == MessageBoxResult.OK)
        {
        }
        throw new Exception();
      }
    }

    #endregion
  }
}
