using System.Diagnostics;
using System.Globalization;
using System.Windows.Controls;

namespace DevKit.Utils
{
    public class NumberValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            Debug.Assert(value != null, nameof(value) + " != null");
            if (value.ToString().IsNumber())
            {
                return new ValidationResult(true, "");
            }

            return new ValidationResult(false, "不是有效的数字");
        }
    }
}