using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avtoservis
{
    public static class RuDateAndMoneyConverter
    {
        private static readonly string[] units = { "", "один", "два", "три", "четыре", "пять", "шесть", "семь", "восемь", "девять" };
        private static readonly string[] teens = { "десять", "одиннадцать", "двенадцать", "тринадцать", "четырнадцать", "пятнадцать", "шестнадцать", "семнадцать", "восемнадцать", "девятнадцать" };
        private static readonly string[] tens = { "", "десять", "двадцать", "тридцать", "сорок", "пятьдесят", "шестьдесят", "семьдесят", "восемьдесят", "девяносто" };
        private static readonly string[] hundreds = { "", "сто", "двести", "триста", "четыреста", "пятьсот", "шестьсот", "семьсот", "восемьсот", "девятьсот" };
        private static readonly string[] thousands = { "", "тысяча", "тысячи", "тысяч" };
        private static readonly string[] millions = { "", "миллион", "миллиона", "миллионов" };
        private static readonly string[] billions = { "", "миллиард", "миллиарда", "миллиардов" };

        public static string CurrencyToTxt(decimal money, bool includeKopeeks)
        {
            money = decimal.Round(money, 2);
            long integerPart = (long)decimal.Truncate(money);
            int fractionalPart = (int)((money - integerPart) * 100);

            string result = ConvertNumberToText(integerPart) + " " + GetCurrencyName(integerPart);

            if (includeKopeeks)
            {
                result += " " + fractionalPart.ToString("00") + " " + GetKopeeksName(fractionalPart);
            }

            return result.Trim();
        }

        private static string ConvertNumberToText(long number)
        {
            if (number == 0) return "ноль";

            string result = "";

            if (number >= 1000000000)
            {
                long billionsCount = number / 1000000000;
                result += ConvertHundreds(billionsCount) + " " + GetWordForm(billionsCount, billions) + " ";
                number %= 1000000000;
            }

            if (number >= 1000000)
            {
                long millionsCount = number / 1000000;
                result += ConvertHundreds(millionsCount) + " " + GetWordForm(millionsCount, millions) + " ";
                number %= 1000000;
            }

            if (number >= 1000)
            {
                long thousandsCount = number / 1000;
                result += ConvertHundreds(thousandsCount, true) + " " + GetWordForm(thousandsCount, thousands) + " ";
                number %= 1000;
            }

            if (number > 0)
            {
                result += ConvertHundreds(number);
            }

            return result.Trim();
        }

        private static string ConvertHundreds(long number, bool isFemale = false)
        {
            string result = "";

            if (number >= 100)
            {
                result += hundreds[number / 100] + " ";
                number %= 100;
            }

            if (number >= 20)
            {
                result += tens[number / 10] + " ";
                number %= 10;
            }
            else if (number >= 10)
            {
                result += teens[number - 10] + " ";
                number = 0;
            }

            if (number > 0)
            {
                if (isFemale)
                {
                    result += (number == 1 ? "одна" : (number == 2 ? "две" : units[number])) + " ";
                }
                else
                {
                    result += units[number] + " ";
                }
            }

            return result.Trim();
        }

        private static string GetWordForm(long number, string[] forms)
        {
            number %= 100;
            if (number >= 11 && number <= 19) return forms[3];

            number %= 10;
            if (number == 1) return forms[1];
            if (number >= 2 && number <= 4) return forms[2];
            return forms[3];
        }

        private static string GetCurrencyName(long number)
        {
            return GetWordForm(number, new[] { "", "рубль", "рубля", "рублей" });
        }

        private static string GetKopeeksName(int number)
        {
            return GetWordForm(number, new[] { "", "копейка", "копейки", "копеек" });
        }
    }
}
