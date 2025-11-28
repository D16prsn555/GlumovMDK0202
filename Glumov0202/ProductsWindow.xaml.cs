using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Glumov0202
{
    public partial class ProductsWindow : Window
    {
        private List<Products> _products;
        public ProductsWindow()
        {
            InitializeComponent();
            Loaded += ProductsWindow_Loaded;
        }

        /// <summary>
        /// Обработка загрузки окна — загружаем все данные по продукции.
        /// </summary>
        private void ProductsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при загрузке данных: {ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Close();
            }
        }

        /// <summary>
        /// Загружает список продукции из базы данных и преобразует его в модель отображения.
        /// </summary>
        private void LoadProducts()
        {
            using (var context = new Entities())
            {
                // Получаем продукцию вместе с типом продукции
                _products = context.Products
                    .Include("Product_type")
                    .ToList();

                // Преобразуем в удобную для таблицы модель
                var productViewModels = _products.Select(p => new ProductViewModel
                {
                    Id = p.ID,
                    Name = p.Name ?? "Неизвестно",
                    Article = p.Article ?? 0,
                    StockCount = CalculateProductStockCount(p.ID),
                    MinimalCost = p.Minimal_cost_for_partner ?? 0,
                    ProductTypeName = p.Product_type?.Product_type_name ?? "Не указан",
                    ProductTypeFactor = p.Product_type?.Product_Type_Factor ?? 1.0
                }).ToList();

                ProductsDataGrid.ItemsSource = productViewModels;
            }
        }

        /// <summary>
        /// Расчет количества товара на складе.
        /// </summary>
        private int CalculateProductStockCount(int productId)
        {
            var random = new Random(productId);
            return random.Next(0, 1000);
        }

        /// <summary>
        /// Кнопка "Рассчитать материалы".
        /// </summary>
        private void CalculateMaterialsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedProduct = ProductsDataGrid.SelectedItem as ProductViewModel;

                if (selectedProduct == null)
                {
                    MessageBox.Show(
                        "Выберите продукт для расчета материалов!",
                        "Предупреждение",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                // Открываем окно расчета
                var calcWindow = new MaterialCalculationWindow(selectedProduct.Id);
                calcWindow.Owner = this;
                calcWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при расчете материалов: {ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Кнопка "Закрыть".
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    /// <summary>
    /// Модель для отображения продукции в таблице.
    /// </summary>
    public class ProductViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Article { get; set; }
        public int StockCount { get; set; }
        public double MinimalCost { get; set; }
        public string ProductTypeName { get; set; }
        public double ProductTypeFactor { get; set; }
    }
}
