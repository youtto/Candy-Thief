using System.Collections.Generic;
using System.Text;

public static class CsvReader
{
    public static string DecodeUtf8(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
            return string.Empty;

        int start = 0;
        if (HasUtf8Bom(bytes))
            start = 3;

        return Encoding.UTF8.GetString(bytes, start, bytes.Length - start);
    }

    public static bool HasUtf8Bom(byte[] bytes)
    {
        return bytes != null
            && bytes.Length >= 3
            && bytes[0] == 0xEF
            && bytes[1] == 0xBB
            && bytes[2] == 0xBF;
    }

    public static string StripBom(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text[0] == '\uFEFF' ? text.Substring(1) : text;
    }

    public static List<string[]> ReadRecords(string csvText)
    {
        csvText = StripBom(csvText);
        var records = new List<string[]>();
        if (string.IsNullOrEmpty(csvText))
            return records;

        var field = new StringBuilder();
        var row = new List<string>();
        bool inQuotes = false;

        for (int i = 0; i < csvText.Length; i++)
        {
            char c = csvText[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < csvText.Length && csvText[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    field.Append(c);
                }

                continue;
            }

            if (c == '"')
            {
                inQuotes = true;
                continue;
            }

            if (c == ',')
            {
                row.Add(field.ToString());
                field.Length = 0;
                continue;
            }

            if (c == '\r')
            {
                if (i + 1 < csvText.Length && csvText[i + 1] == '\n')
                    i++;

                FinishRow(field, row, records);
                continue;
            }

            if (c == '\n')
            {
                FinishRow(field, row, records);
                continue;
            }

            field.Append(c);
        }

        if (inQuotes)
            FinishRow(field, row, records);
        else if (field.Length > 0 || row.Count > 0)
            FinishRow(field, row, records);

        return records;
    }

    static void FinishRow(StringBuilder field, List<string> row, List<string[]> records)
    {
        row.Add(field.ToString());
        field.Length = 0;

        if (!IsEmptyRow(row))
            records.Add(row.ToArray());

        row.Clear();
    }

    static bool IsEmptyRow(List<string> row)
    {
        for (int i = 0; i < row.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(row[i]))
                return false;
        }

        return true;
    }
}
