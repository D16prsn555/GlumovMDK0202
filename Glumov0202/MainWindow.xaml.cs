using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Glumov0202
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Коллекция заявок партнеров, подготовленная для отображения в интерфейсе
        /// </summary>
        private List<PartnerRequestViewModel> _partnerRequests;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        /// <summary>
        /// Обработчик загрузки главного окна
        /// </summary>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadPartnerRequests();
            }
            catch
            {
                MessageBox.Show("Ошибка при загрузке данных");
            }
        }

        /// <summary>
        /// Загрузка заявок партнеров из базы данных и подготовка данных для отображения
        /// </summary>
        private void LoadPartnerRequests()
        {
            using (var context = new Entities())
            {
                // Получаем партнеров с типами и заявками на продукты
                var partnerRequests = context.Partners
                    .Include("Partner_type")
                    .Include("Partner_products_request.Products")
                    .Where(p => p.Partner_products_request.Any())
                    .ToList();

                _partnerRequests = new List<PartnerRequestViewModel>();

                foreach (var partner in partnerRequests)
                {
                    var requestViewModel = new PartnerRequestViewModel
                    {
                        PartnerId = partner.ID,
                        PartnerType = partner.Partner_type?.Name ?? "Тип не указан",
                        PartnerName = partner.Name ?? "Неизвестный партнер",
                        Address = partner.Address ?? "Юридический адрес не указан",
                        Phone = partner.Phone ?? "Телефон не указан",
                        Rating = partner.Rating ?? 0,
                        TotalCost = 0
                    };

                    // Расчет общей стоимости всех заявок партнера
                    foreach (var request in partner.Partner_products_request)
                    {
                        if (request.Products != null && request.Count.HasValue)
                        {
                            double unitCost = request.Products.Minimal_cost_for_partner ?? 0;
                            int count = request.Count.Value;
                            double productTotalCost = Math.Round(unitCost * count, 2);

                            // Защита от отрицательных значений
                            if (productTotalCost < 0)
                                productTotalCost = 0;

                            requestViewModel.TotalCost += productTotalCost;
                        }
                    }

                    // Округление общей суммы до 2 знаков после запятой
                    requestViewModel.TotalCost = Math.Round(requestViewModel.TotalCost, 2);

                    _partnerRequests.Add(requestViewModel);
                }

                // Привязка подготовленных данных к элементу интерфейса
                RequestsListBox.ItemsSource = _partnerRequests;
            }
        }

        /// <summary>
        /// Открытие окна редактирования партнера при клике по карточке заявки
        /// </summary>
        private void PartnerItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is PartnerRequestViewModel partner)
            {
                try
                {
                    var editWindow = new PartnerEditWindow(partner.PartnerId);
                    editWindow.Owner = this;

                    // После сохранения данных обновляем список заявок
                    editWindow.PartnerSaved += (s, args) => LoadPartnerRequests();

                    if (editWindow.ShowDialog() == true)
                    {
                        LoadPartnerRequests();
                    }
                }
                catch
                {
                    MessageBox.Show("Ошибка при открытии формы редактирования");
                }
            }
        }

        /// <summary>
        /// Открытие формы добавления нового партнера / заявки
        /// </summary>
        private void AddPartnerButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var editWindow = new PartnerEditWindow();
                editWindow.Owner = this;

                // После сохранения нового партнера обновляем список
                editWindow.PartnerSaved += (s, args) => LoadPartnerRequests();

                if (editWindow.ShowDialog() == true)
                {
                    LoadPartnerRequests();
                }
            }
            catch
            {
                MessageBox.Show("Ошибка при открытии формы добавления");
            }
        }

        /// <summary>
        /// Открытие окна с каталогом продукции
        /// </summary>
        private void ViewProductsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var productsWindow = new ProductsWindow();
                productsWindow.Owner = this;
                productsWindow.ShowDialog();
            }
            catch
            {
                MessageBox.Show("Ошибка при открытии каталога продукции");
            }
        }
    }

    /// <summary>
    /// Модель представления заявки партнера для отображения в интерфейсе
    /// </summary>
    public class PartnerRequestViewModel
    {
        public int PartnerId { get; set; }
        public string PartnerType { get; set; }
        public string PartnerName { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public int Rating { get; set; }
        public double TotalCost { get; set; }
        public string RatingText => $"Рейтинг: {Rating}";
    }
}
