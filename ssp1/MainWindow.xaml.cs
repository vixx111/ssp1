using System;
using System.Collections.Generic;
using System.Management;
using System.Threading.Tasks;
using System.Windows;

namespace WmiNetworkProtocols
{
    public class NetworkProtocolItem
    {
        public string Name { get; set; }
        public string Status { get; set; }
        public bool IsEnabled { get; set; }
        public bool SupportsConnectionlessService { get; set; }
        public string Description { get; set; }
    }

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadProtocolsAsync();
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadProtocolsAsync();
        }

        private async Task LoadProtocolsAsync()
        {
            BtnRefresh.IsEnabled = false;
            TxtStatus.Text = "Считывание данных из WMI...";

            try
            {
                List<NetworkProtocolItem> protocols = await Task.Run(() => FetchNetworkProtocols());

                DgProtocols.ItemsSource = protocols;
                TxtCount.Text = $"Найдено протоколов: {protocols.Count}";
                TxtStatus.Text = "Данные успешно получены.";
            }
            catch (ManagementException mex)
            {
                MessageBox.Show($"Ошибка WMI: {mex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtStatus.Text = "Ошибка выполнения WMI-запроса.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtStatus.Text = "Ошибка загрузки.";
            }
            finally
            {
                BtnRefresh.IsEnabled = true;
            }
        }

        private List<NetworkProtocolItem> FetchNetworkProtocols()
        {
            var result = new List<NetworkProtocolItem>();

            string query = "SELECT * FROM Win32_NetworkProtocol";

            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
            using (ManagementObjectCollection collection = searcher.Get())
            {
                foreach (ManagementObject obj in collection)
                {
                    string name = obj["Name"]?.ToString() ?? obj["Caption"]?.ToString() ?? "Неизвестно";
                    string statusValue = obj["Status"]?.ToString() ?? "OK";
                    string description = obj["Description"]?.ToString() ?? "-";

                    bool supportsConnectionless = false;
                    try
                    {
                        if (obj["SupportsConnectionlessService"] != null)
                        {
                            bool.TryParse(obj["SupportsConnectionlessService"].ToString(), out supportsConnectionless);
                        }
                    }
                    catch
                    {
                    }

                    bool isEnabled = string.Equals(statusValue, "OK", StringComparison.OrdinalIgnoreCase);

                    result.Add(new NetworkProtocolItem
                    {
                        Name = name,
                        Status = statusValue,
                        IsEnabled = isEnabled,
                        SupportsConnectionlessService = supportsConnectionless,
                        Description = description
                    });
                }
            }

            return result;
        }
    }
}