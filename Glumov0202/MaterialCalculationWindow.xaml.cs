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
using System.Windows.Shapes;

namespace Glumov0202
{
    /// <summary>
    /// Окно расчета материалов для выбранной продукции
    /// </summary>
    public partial class MaterialCalculationWindow : Window
    {

        private int _productId;
        private Products _product;

        public MaterialCalculationWindow(int productId)
        {
            InitializeComponent();
            _productId = productId;
            Loaded += MaterialCalculationWindow_Loaded;
        }

        /// <summary>
        /// Обработчик загрузки окна
        /// </summary>
        private void MaterialCalculationWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadProductInfo();
            }
            catch
            {
                MessageBox.Show("Ошибка при загрузке данных");
                Close();
            }
        }

        /// <summary>
        /// Загрузка информации о продукции из базы данных
        /// </summary>
        private void LoadProductInfo()
        {
            using (var context = new Entities())
            {
                _product = context.Products
                    .Include("Product_type")
                    .FirstOrDefault(p => p.ID == _productId);

                if (_product != null)
                {
                    ProductNameTextBlock.Text = _product.Name ?? "Неизвестно";
                }
            }
        }

        /// <summary>
        /// Ограничение ввода только цифрами (целые числа)
        /// </summary>
        private void NumberTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, 0))
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Ограничение ввода только цифрами и точкой (вещественные числа)
        /// </summary>
        private void DoubleTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Допускаем только цифры и точку как разделитель
            if (!char.IsDigit(e.Text, 0) && e.Text != ".")
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки расчета материалов
        /// </summary>
        private void CalculateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверка корректности требуемого количества
                if (!int.TryParse(RequiredCountTextBox.Text, out int requiredCount) || requiredCount <= 0)
                {
                    MessageBox.Show("Введите корректное требуемое количество!");
                    return;
                }

                // Проверка корректности параметра 1
                if (!double.TryParse(Param1TextBox.Text, out double param1) || param1 <= 0)
                {
                    MessageBox.Show("Введите корректное значение параметра 1!");
                    return;
                }

                // Проверка корректности параметра 2
                if (!double.TryParse(Param2TextBox.Text, out double param2) || param2 <= 0)
                {
                    MessageBox.Show("Введите корректное значение параметра 2!");
                    return;
                }

                int materialTypeId = 1;
                int stockCount = 100;

                int result = MaterialCalculator.CalculateRequiredMaterial(
                    _product.ID_Product_type ?? 0,
                    materialTypeId,
                    requiredCount,
                    stockCount,
                    param1,
                    param2
                );

                if (result == -1)
                {
                    ResultTextBlock.Text = "Неверные входные данные";
                }
                else
                {
                    ResultTextBlock.Text = $"Необходимо материалов: {result} ед.";
                }
            }
            catch
            {
                MessageBox.Show("Ошибка при расчете");
            }
        }

        /// <summary>
        /// Закрытие окна расчета
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
