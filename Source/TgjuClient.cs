using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace ChronoDeskWidget
{
    internal sealed class MarketRate
    {
        public string Key;
        public string DisplayNameEnglish;
        public string DisplayNamePersian;
        public string Url;
        public string Value;
        public string Error;
    }

    internal static class TgjuClient
    {
        public static MarketRate[] Definitions()
        {
            return new MarketRate[]
            {
                new MarketRate { Key = "usd", DisplayNameEnglish = "US dollar", DisplayNamePersian = "دلار آزاد", Url = "https://www.tgju.org/profile/price_dollar_rl" },
                new MarketRate { Key = "eur", DisplayNameEnglish = "Euro", DisplayNamePersian = "یورو آزاد", Url = "https://www.tgju.org/profile/price_eur" },
                new MarketRate { Key = "gold18", DisplayNameEnglish = "18K gold / gram", DisplayNamePersian = "طلای ۱۸ عیار / گرم", Url = "https://www.tgju.org/profile/geram18" }
            };
        }

        public static MarketRate Fetch(MarketRate definition)
        {
            MarketRate result = new MarketRate
            {
                Key = definition.Key,
                DisplayNameEnglish = definition.DisplayNameEnglish,
                DisplayNamePersian = definition.DisplayNamePersian,
                Url = definition.Url
            };
            try
            {
                string html = Download(definition.Url);
                string text = Regex.Replace(html, "(?is)<(script|style|noscript)\\b[^>]*>.*?</\\1>", " ");
                text = Regex.Replace(text, "(?s)<[^>]+>", " ");
                text = WebUtility.HtmlDecode(text);
                text = Regex.Replace(text, "\\s+", " ");
                string number = FindCurrentValue(text);
                if (string.IsNullOrWhiteSpace(number))
                    throw new InvalidDataException("TGJU page format changed or did not contain a recognizable current price.");
                result.Value = NormalizeNumber(number);
                if (result.Value.Length < 4) throw new InvalidDataException("The extracted price was too short to be valid.");
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
            }
            return result;
        }

        private static string Download(string url)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/125.0 Safari/537.36 ChronoDeskWidget/1.0";
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            request.Headers[HttpRequestHeader.AcceptLanguage] = "fa-IR,fa;q=0.9,en;q=0.7";
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.Timeout = 12000;
            request.ReadWriteTimeout = 12000;
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode != HttpStatusCode.OK)
                    throw new WebException("TGJU returned HTTP " + (int)response.StatusCode + ".");
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, true))
                    return reader.ReadToEnd();
            }
        }

        private static string FindCurrentValue(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            // TGJU currently exposes the current quote as Persian text such as
            // "نرخ فعلی:: 1,854,000 0.27". Parse only the first formatted price,
            // avoiding the following daily-change value.
            const string price = @"(?<price>[0-9۰-۹٠-٩]+(?:[,٬][0-9۰-۹٠-٩]{3})+|[0-9۰-۹٠-٩]{4,})";
            Match match = Regex.Match(
                text,
                @"(?:نرخ\s*فعلی|قیمت\s*فعلی)\s*[:：|]*\s*" + price,
                RegexOptions.IgnoreCase);
            if (match.Success) return match.Groups["price"].Value;

            match = Regex.Match(
                text,
                @"(?:Last\s+price|Current\s+price)\s*[:：|]*\s*" + price,
                RegexOptions.IgnoreCase);
            if (match.Success) return match.Groups["price"].Value;

            return null;
        }

        private static string NormalizeNumber(string value)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in value)
            {
                if (c >= '0' && c <= '9') sb.Append(c);
                else if (c >= '۰' && c <= '۹') sb.Append((char)('0' + c - '۰'));
                else if (c >= '٠' && c <= '٩') sb.Append((char)('0' + c - '٠'));
                else if (c == ',' || c == '٬') { }
                else if (char.IsWhiteSpace(c)) { }
                else break;
            }
            return sb.ToString();
        }
    }
}