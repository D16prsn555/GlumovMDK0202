using System;
using System.Linq;

namespace Glumov0202
{
    public static class MaterialCalculator
    {
        /// <summary>
        /// Вычисляет количество материалов, необходимое для изготовления заданного количества продукции.
        /// Возвращает -1 при ошибке входных данных или отсутствии необходимых справочников.
        /// </summary>
        public static int CalculateRequiredMaterial(
            int productTypeId,
            int materialTypeId,
            int requiredProductCount,
            int productStockCount,
            double productParam1,
            double productParam2)
        {
            try
            {
                // Проверка корректности числовых параметров
                if (requiredProductCount <= 0 ||
                    productStockCount < 0 ||
                    productParam1 <= 0 ||
                    productParam2 <= 0)
                {
                    return -1;
                }

                using (var context = new Entities())
                {
                    // Получаем тип продукции
                    var productType = context.Product_type
                        .FirstOrDefault(pt => pt.ID == productTypeId);

                    // Получаем тип материала
                    var materialType = context.Material_type
                        .FirstOrDefault(mt => mt.ID == materialTypeId);

                    // Проверяем, что оба типа существуют
                    if (productType == null || materialType == null)
                    {
                        return -1;
                    }

                    // Сколько продукции нужно произвести, учитывая остаток
                    int productionCount = Math.Max(0, requiredProductCount - productStockCount);

                    // Если склад полностью покрывает потребность
                    if (productionCount == 0)
                    {
                        return 0;
                    }

                    // Коэффициент, зависящий от типа продукции
                    double productTypeFactor = productType.Product_Type_Factor ?? 1.0;

                    // Учитываем процент брака материала
                    double defectPercentage = materialType.Percentage_of_defective_materials ?? 0.0;
                    double defectFactor = 1.0 + (defectPercentage / 100.0);

                    // Расход материала на одну единицу продукции
                    double materialPerUnit = productParam1 * productParam2 * productTypeFactor;

                    // Итоговое количество материала с учетом брака
                    double totalMaterial = materialPerUnit * productionCount * defectFactor;

                    // Округляем в большую сторону
                    return (int)Math.Ceiling(totalMaterial);
                }
            }
            catch
            {
                // В случае исключения возвращаем ошибку
                return -1;
            }
        }
    }
}
