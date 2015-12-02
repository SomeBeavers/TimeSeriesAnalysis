using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.DataVisualization.Charting;

namespace TimeSeriesAnalysis
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : INotifyPropertyChanged
  {
    #region textBoxes display values methods

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string property)
    {
      PropertyChangedEventHandler handler = PropertyChanged;
      if (handler != null)
      {
        handler(this, new PropertyChangedEventArgs(property));
      }
    }

    /// <summary>
    /// Initial date period in days.
    /// </summary>
    public string InitialDatePeriod
    {
      get { return InitialDatePeriodTextBox.Text; }
      set
      {
        InitialDatePeriodTextBox.Text = value;
        OnPropertyChanged("InitialDatePeriod");
      }
    }

    /// <summary>
    /// Initial date period for maximum deviation textBox.
    /// </summary>
    public string InitialDatePeriodMaximumDeviation
    {
      get { return InitialDatePeriodMaximumDeviationTextBox.Text; }
      set
      {
        InitialDatePeriodMaximumDeviationTextBox.Text = value;
        OnPropertyChanged("InitialDatePeriodMaximumDeviation");
      }
    }

    /// <summary>
    /// Initial date period for minimum deviation textBox.
    /// </summary>
    public string InitialDatePeriodMinimumDeviation
    {
      get { return InitialDatePeriodMinimumDeviationTextBox.Text; }
      set
      {
        InitialDatePeriodMinimumDeviationTextBox.Text = value;
        OnPropertyChanged("InitialDatePeriodMinimumDeviation");
      }
    }

    /// <summary>
    /// Prediction date period for maximum deviation textBox.
    /// </summary>
    public string PredictionDatePeriodMaximumDeviation
    {
      get { return PredictionDatePeriodMaximumDeviationTextBox.Text; }
      set
      {
        PredictionDatePeriodMaximumDeviationTextBox.Text = value;
        OnPropertyChanged("PredictionDatePeriodMaximumDeviation");
      }
    }

    /// <summary>
    /// Prediction date period for maximum deviation textBox.
    /// </summary>
    public string PredictionDatePeriodMinimumDeviation
    {
      get { return PredictionDatePeriodMinimumDeviationTextBox.Text; }
      set
      {
        PredictionDatePeriodMinimumDeviationTextBox.Text = value;
        OnPropertyChanged("PredictionDatePeriodMinimumDeviation");
      }
    }

    /// <summary>
    /// Prediction date period for maximum deviation textBox.
    /// </summary>
    public string MaximumDeviation
    {
      get { return MaximumDeviationTextBox.Text; }
      set
      {
        MaximumDeviationTextBox.Text = value;
        OnPropertyChanged("MaximumDeviation");
      }
    }

    /// <summary>
    /// Prediction date period for maximum deviation textBox.
    /// </summary>
    public string MinimumDeviation
    {
      get { return MinimumDeviationTextBox.Text; }
      set
      {
        MinimumDeviationTextBox.Text = value;
        OnPropertyChanged("MinimumDeviation");
      }
    }

    /// <summary>
    /// Prediction date period in days.
    /// </summary>
    public string PredictionDatePeriod
    {
      get { return PredictionDatePeriodTextBox.Text; }
      set
      {
        PredictionDatePeriodTextBox.Text = value;
        OnPropertyChanged("PredictionDatePeriod");
      }
    }

    /// <summary>
    /// Mean deviation for current prediction.
    /// </summary>
    public string MeanDeviationPeriod
    {
      get { return MeanDeviationTextBox.Text; }
      set
      {
        MeanDeviationTextBox.Text = value;
        OnPropertyChanged("MeanDeviationPeriod");
      }
    }

    #endregion

    #region comboBoxes change values methods

    private void ComboBoxInitialData_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      var comboBox = sender as ComboBox;
      InitialStartDate = Convert.ToDateTime(comboBox.SelectedItem);
    }

    private void ComboBoxInitialDataEnd_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      var comboBox = sender as ComboBox;
      InitialEndDate = Convert.ToDateTime(comboBox.SelectedItem);
    }

    private void ComboBoxPredictionStartDate_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      var comboBox = sender as ComboBox;
      PredictionStartDate = Convert.ToDateTime(comboBox.SelectedItem);
    }

    private void ComboBoxPredictionEndDate_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      var comboBox = sender as ComboBox;
      PredictionEndDate = Convert.ToDateTime(comboBox.SelectedItem);
    }

    #endregion

    #region private fields

    public static DateTime InitialStartDate = new DateTime(0);
    public static DateTime InitialEndDate;
    public static DateTime PredictionStartDate;
    public static DateTime PredictionEndDate;
    public static string initialDate = "0";
    public static string predictionDate = "0";
    public static string meanDeviation = "0";
    private static string getAllDeaths = "select * from AllDeaths";

    private static string selectAllPredictionDeviation = "SELECT * " +
                                                               "FROM PredictionDeviation ";

    private static string maximumDeviationQuery = "SELECT * " +
                                                               "FROM PredictionDeviation " +
                                                               "INNER JOIN " +
                                                               "(SELECT MAX(MeanDeviation) AS MaxDeviation " +
                                                               "FROM PredictionDeviation) groupedtt " +
                                                               "ON PredictionDeviation.MeanDeviation = groupedtt.MaxDeviation ";

    private static string minimumDeviationQuery = "SELECT * " +
                                                           "FROM PredictionDeviation " +
                                                           "INNER JOIN " +
                                                           "(SELECT MIN(MeanDeviation) AS MaxDeviation " +
                                                           "FROM PredictionDeviation) groupedtt " +
                                                           "ON PredictionDeviation.MeanDeviation = groupedtt.MaxDeviation ";

    public static List<double> deviations = new List<double>();

    private bool isRestoreCompleted = false;

    private static List<DateTime> comboBoxSource = new List<DateTime>();

    private List<List<KeyValuePair<DateTime, int>>> dataSourceList = new List<List<KeyValuePair<DateTime, int>>>();
    private List<List<KeyValuePair<int, int>>> dataSourceListAnalysisInitial = new List<List<KeyValuePair<int, int>>>();
    private List<List<KeyValuePair<int, int>>> dataSourceListAnalysisPrediction = new List<List<KeyValuePair<int, int>>>();


    #endregion

    #region private methods

    /// <summary>
    /// Update values on Analysis Tab.
    /// </summary>
    private void UpdateAnalysisTab()
    {
      DataTable maximumDev =
        PredictHelper.ExecuteSQLQueryData(maximumDeviationQuery);

      foreach (DataRow dr in maximumDev.Rows)
      {
        InitialDatePeriodMaximumDeviation = Convert.ToString(dr["InitialPeriod"]);
        PredictionDatePeriodMaximumDeviation = Convert.ToString(dr["PredictionPeriod"]);
        MaximumDeviation = Convert.ToString(dr["MeanDeviation"]);
      }

      DataTable minimumDev =
        PredictHelper.ExecuteSQLQueryData(minimumDeviationQuery);

      foreach (DataRow dr in minimumDev.Rows)
      {
        InitialDatePeriodMinimumDeviation = Convert.ToString(dr["InitialPeriod"]);
        PredictionDatePeriodMinimumDeviation = Convert.ToString(dr["PredictionPeriod"]);
        MinimumDeviation = Convert.ToString(dr["MeanDeviation"]);
      }
      ShowDataOnAnalysisTab();
      (LineChartAnalysis.Series[0] as DataPointSeries).ItemsSource = dataSourceListAnalysisInitial[0];
      (LineChartAnalysis_Copy.Series[0] as DataPointSeries).ItemsSource = dataSourceListAnalysisPrediction[0];
    }

    /// <summary>
    /// Fill grid on Analysis Tab.
    /// </summary>
    private void FillDataGrid()
    {
      string fillQuery = "select * from PredictionDeviation";
      DeviationsDataGrid.ItemsSource =
        PredictHelper.ExecuteSQLQueryData(fillQuery).DefaultView;
    }

    /// <summary>
    /// Predict.
    /// </summary>
    private void BuisenessLogic()
    {
      InitialDatePeriod = "0";
      PredictHelper.SetPredictionNumber();
      PredictHelper.UpdatePredictionTable(InitialStartDate, InitialEndDate);
      AnalysisServicesHelper.ConnectToAnalysisServices();
      var firstParameter = (int) (PredictionStartDate - InitialEndDate).TotalDays;

      InitialDatePeriod =
        Convert.ToString((int) (InitialEndDate - InitialStartDate).TotalDays);

      PredictionDatePeriod =
        Convert.ToString((int) (PredictionEndDate - PredictionStartDate).TotalDays);

      var lastParameter =
        Math.Abs((int) (PredictionEndDate - PredictionStartDate).TotalDays) +
        Math.Abs(firstParameter);
      PredictHelper.PredictDeaths(firstParameter, lastParameter);
    }

    /// <summary>
    /// Click on Predict button.
    /// </summary>
    private void Predict_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        BuisenessLogic();
      }
      catch (Exception)
      {
        Console.WriteLine("Error during prediction occured.");
        var errorMessage = MessageBox.Show("Error during prediction occured.",
          "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        if (errorMessage == MessageBoxResult.OK)
        {

        }
        return;
      }
      
      ShowDataOnGraph();
      (LineChart.Series[0] as DataPointSeries).ItemsSource = dataSourceList[0];
      (LineChart.Series[1] as DataPointSeries).ItemsSource = dataSourceList[1];
      deviations = new List<double>();
      for (int predictionNumber = 0;
        predictionNumber < dataSourceList[0].Count;
        predictionNumber++)
      {
        deviations.Add(
          Math.Abs(dataSourceList[0][predictionNumber].Value -
                   dataSourceList[1][predictionNumber].Value));
      }
      MeanDeviationPeriod = Convert.ToString(deviations.Sum()/deviations.Count);
      PredictHelper.UpdateDeviations(Convert.ToInt16(InitialDatePeriod),
        Convert.ToInt16(PredictionDatePeriod), Convert.ToDouble(MeanDeviationPeriod));
      FillDataGrid();
      UpdateAnalysisTab();
    }

    /// <summary>
    /// Show data on graph on Analysis Tab.
    /// </summary>
    private void ShowDataOnAnalysisTab()
    {
      dataSourceListAnalysisInitial = new List<List<KeyValuePair<int, int>>>();
      dataSourceListAnalysisPrediction = new List<List<KeyValuePair<int, int>>>();
      DataTable dataTable = PredictHelper.ExecuteSQLQueryData(selectAllPredictionDeviation);

      var test = (from DataRow dr in dataTable.Rows
                  select
                    new KeyValuePair<int, int>(Convert.ToInt16(dr["InitialPeriod"]),
                      Convert.ToInt16(dr["MeanDeviation"]))).ToList();
      var test2 = (from DataRow dr in dataTable.Rows
                  select
                    new KeyValuePair<int, int>(Convert.ToInt16(dr["PredictionPeriod"]),
                      Convert.ToInt16(dr["MeanDeviation"]))).ToList();
      dataSourceListAnalysisInitial.Add(test);
      dataSourceListAnalysisPrediction.Add(test2);
    }

    /// <summary>
    /// Display data on chart.
    /// </summary>
    private void ShowDataOnGraph()
    {
      dataSourceList = new List<List<KeyValuePair<DateTime, int>>>();
      string getDeathsBetween = "SELECT * FROM dbo.AllDeaths " +
                                "WHERE id BETWEEN CONVERT(date,'" + PredictionStartDate +
                                "') AND CONVERT(date,'" + PredictionEndDate + "');";

      DataTable dataTable = PredictHelper.ExecuteSQLQueryData(getDeathsBetween);

      var test = (from DataRow dr in dataTable.Rows
        select
          new KeyValuePair<DateTime, int>(Convert.ToDateTime(dr["id"]),
            Convert.ToInt16(dr["Value"]))).ToList();
      string predictionColumnName = "Prediction_" + PredictHelper.predictionNumber;
      string getDeathsBetweenPrediction = "select id, " + predictionColumnName +
                                          " from Prediction where " + predictionColumnName +
                                          ">0";

      DataTable dataTablePrediction =
        PredictHelper.ExecuteSQLQueryData(getDeathsBetweenPrediction);
      var llistaPreu = (from DataRow dr in dataTablePrediction.Rows
        select
          new KeyValuePair<DateTime, int>(Convert.ToDateTime(dr["id"]),
            Convert.ToInt16(dr[predictionColumnName]))).ToList();
      dataSourceList.Add(test);
      dataSourceList.Add(llistaPreu);
    }

    /// <summary>
    /// Fill comboBoxes.
    /// </summary>
    private List<DateTime> FillComboBox()
    {
      Thread.Sleep(5000);
      string connectionString =
        ConfigurationManager.ConnectionStrings["DeathsConnectionString"].ConnectionString;
      string cmdString = String.Empty;
      var dataTable = new DataTable("Dates");
      using (var con = new SqlConnection(connectionString))
      {
        bool connecting = true;
        while (connecting)
        {
          try
          {
            con.Open();
          }
          catch (Exception)
          {
            Thread.Sleep(1000);
          }
          connecting = false;
        }
        cmdString = "select id from AllDeaths";
        var cmd = new SqlCommand(cmdString, con);
        var sqlDataAdapter = new SqlDataAdapter(cmd);
        sqlDataAdapter.Fill(dataTable);
        con.Close();
      }
      return (from row in dataTable.AsEnumerable()
        select row.Field<DateTime>("id")).ToList<DateTime>();
    }

    #endregion

    #region private methods load UI

    /// <summary>
    /// Check if DB restore is needed and restore.
    /// </summary>
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      isRestoreCompleted = RestoreDBHelper.dbTraining();
      comboBoxSource = FillComboBox();
    }

    /// <summary>
    /// Fill comboBoxes.
    /// </summary>
    private void ComboBox_Loaded(object sender, RoutedEventArgs e)
    {
      while (!isRestoreCompleted)
      {
        Thread.Sleep(1000);
      }
      var comboBox = sender as ComboBox;

      comboBox.ItemsSource = comboBoxSource;
    }

    #endregion

    #region public methods

    public MainWindow()
    {
      InitializeComponent();
    }

    #endregion
  }
}
