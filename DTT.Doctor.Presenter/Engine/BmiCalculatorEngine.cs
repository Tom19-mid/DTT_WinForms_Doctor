using System;

namespace DTT.Doctor.Presenter.Engine
{
    public class BmiResult
    {
        public decimal BmiValue { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Advice { get; set; } = string.Empty;
        public string StatusColorHex { get; set; } = "#10B981"; // Default Emerald Green
        public bool IsAlert => BmiValue < 18.5m || BmiValue >= 25.0m;
    }

    public static class BmiCalculatorEngine
    {
        public static BmiResult Calculate(decimal heightInCm, decimal weightInKg)
        {
            if (heightInCm <= 0 || weightInKg <= 0)
            {
                return new BmiResult { BmiValue = 0, Category = "Chưa nhập đủ thông tin", StatusColorHex = "#94A3B8", Advice = "Vui lòng nhập chiều cao và cân nặng hợp lệ." };
            }

            decimal heightInMeter = heightInCm / 100m;
            decimal bmi = Math.Round(weightInKg / (heightInMeter * heightInMeter), 1);

            string category;
            string colorHex;
            string advice;

            if (bmi < 18.5m)
            {
                category = "Nhẹ cân (Gầy)";
                colorHex = "#3B82F6"; // Blue
                advice = "Cần bổ sung dinh dưỡng, theo dõi tình trạng hấp thu và vi chất.";
            }
            else if (bmi < 23.0m) // Asian BMI Standard
            {
                category = "Bình thường";
                colorHex = "#10B981"; // Emerald Green
                advice = "Thể trạng chuẩn y tế. Duy trì chế độ vận động và ăn uống khoa học.";
            }
            else if (bmi < 25.0m)
            {
                category = "Tiền béo phì / Hơi thừa cân";
                colorHex = "#F59E0B"; // Amber / Warning
                advice = "Cảnh báo nguy cơ tăng cân. Khuyên bệnh nhân điều chỉnh calo tiêu thụ.";
            }
            else if (bmi < 30.0m)
            {
                category = "Béo phì độ I (Cảnh báo)";
                colorHex = "#EF4444"; // Red / Alert
                advice = "Khuyến nghị chỉ định thêm các xét nghiệm Lipid máu, Đường huyết đói và Tim mạch.";
            }
            else
            {
                category = "Béo phì độ II (Nguy cơ cao)";
                colorHex = "#991B1B"; // Deep Red
                advice = "Nguy cơ cao mắc các bệnh lý chuyển hóa và gan nhiễm mỡ. Cần lên phác đồ giảm cân chuyên sâu.";
            }

            return new BmiResult
            {
                BmiValue = bmi,
                Category = category,
                StatusColorHex = colorHex,
                Advice = advice
            };
        }
    }
}
