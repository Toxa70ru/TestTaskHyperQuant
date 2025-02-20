using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Connector.Clients;
using Connector.Services;

namespace TestTaskHyperQuant.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private readonly RestApiClient _restClient;
        private readonly PortfolioService _portfolioService;

        public ObservableCollection<PortfolioItem> Portfolio { get; set; } = new ObservableCollection<PortfolioItem>();

        public ICommand LoadCommand { get; }

        public MainViewModel()
        {
            _restClient = new RestApiClient();
            _portfolioService = new PortfolioService(_restClient);

            // Команда для загрузки данных портфеля
            LoadCommand = new RelayCommand(async () => await LoadPortfolioAsync());
        }

        private async Task LoadPortfolioAsync()
        {
            try
            {
                // Очищаем текущий список перед обновлением
                Portfolio.Clear();

                // Расчет портфеля
                var portfolioData = await _portfolioService.CalculatePortfolioAsync();

                // Добавляем данные в коллекцию для отображения в DataGrid
                foreach (var item in portfolioData)
                {
                    Portfolio.Add(new PortfolioItem
                    {
                        Currency = item.Key,
                        TotalValue = item.Value.ToString("F2") // Форматирование до 2 знаков после запятой
                    });
                }
            }
            catch (Exception ex)
            {
                // Обработка ошибок
                Console.WriteLine($"Error loading portfolio: {ex.Message}");
            }
        }
    }
    public class PortfolioItem
    {
        public string Currency { get; set; }
        public string TotalValue { get; set; }
    }
}
