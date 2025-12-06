using InventorySystem.Pages;
using System.Text.RegularExpressions;
using System.Linq;
using Newtonsoft.Json;

namespace InventorySystem.GlobalVariables
{
    public class HelperSql
    {
        private readonly Random _random = new Random();
    
        // Template pools untuk variasi response
        private readonly Dictionary<string, List<string>> _templates = new Dictionary<string, List<string>>
        {
            ["insert_intro"] = new List<string> 
            { 
                "Apakah Anda yakin ingin menambahkan",
                "Anda akan menambah",
                "Konfirmasi penambahan",
                "Ingin menambahkan",
                "Siap menambah"
            },
            ["insert_ending"] = new List<string>
            {
                "ke dalam sistem?",
                "ke database?",
                "sekarang?",
                "ke tabel {0}?",
                "?"
            },
            ["update_intro"] = new List<string>
            {
                "Apakah Anda ingin memperbarui",
                "Anda akan mengubah",
                "Konfirmasi perubahan",
                "Ingin mengupdate",
                "Siap memperbarui"
            },
            ["delete_intro"] = new List<string>
            {
                "Apakah Anda yakin ingin menghapus",
                "Anda akan menghapus",
                "Konfirmasi penghapusan",
                "⚠️ Ingin menghapus",
                "⚠️ Siap menghapus"
            },
            ["delete_warning"] = new List<string>
            {
                "Data yang dihapus tidak dapat dikembalikan!",
                "Tindakan ini bersifat permanen!",
                "Data akan hilang secara permanen!",
                "Ini adalah tindakan yang tidak bisa dibatalkan!"
            },
            ["select_intro"] = new List<string>
            {
                "Anda akan melihat",
                "Menampilkan",
                "Mengambil data",
                "Melihat informasi",
                "Mencari data"
            }
        };

        // Mapping nama kolom ke bahasa natural
        private readonly Dictionary<string, string> _columnNames = new Dictionary<string, string>
        {
            ["name"] = "nama",
            ["stock"] = "stok",
            ["price"] = "harga",
            ["quantity"] = "jumlah",
            ["description"] = "deskripsi",
            ["category"] = "kategori",
            ["created_by"] = "dibuat oleh",
            ["updated_by"] = "diperbarui oleh",
            ["created_at"] = "tanggal dibuat",
            ["updated_at"] = "tanggal diperbarui",
            ["status"] = "status",
            ["code"] = "kode",
            ["barcode"] = "barcode",
            ["unit"] = "satuan",
            ["supplier"] = "supplier",
            ["location"] = "lokasi"
        };

        // Mapping nama tabel ke bahasa natural
        private readonly Dictionary<string, string> _tableNames = new Dictionary<string, string>
        {
            ["products"] = "produk",
            ["product"] = "produk",
            ["items"] = "item",
            ["item"] = "item",
            ["inventory"] = "inventori",
            ["stock"] = "stok",
            ["users"] = "pengguna",
            ["categories"] = "kategori",
            ["suppliers"] = "supplier",
            ["transactions"] = "transaksi"
        };
        
        private static readonly string[] SqlPrefixes = new[]
        {
            "select ",
            "update ",
            "insert into ",
            "delete from ",
            "show tables"
        };
        private static string currentQuery = "";

        public string wrapSqlScript(string currentResponse)
        {
            if (string.IsNullOrWhiteSpace(currentResponse))
                return currentResponse;

            string trimmed = currentResponse.Trim().ToLowerInvariant();

            foreach (var prefix in SqlPrefixes)
            {
                if (trimmed.StartsWith(prefix))
                {
                    // misalnya kita bungkus biar aman
                    return $"[SQL Script Detected] {currentResponse}";
                }
            }

            // kalau bukan SQL script, return apa adanya
            return currentResponse;
        }

        public string ConvertSqlScriptToNaturalLanguage(string sqlScript)
        {
            if (string.IsNullOrWhiteSpace(sqlScript))
                return "❌ Tidak ada perintah SQL yang ditemukan.";

            string trimmed = sqlScript.Trim();
            string lowerScript = trimmed.ToLowerInvariant();

            try
            {
                if (lowerScript.StartsWith("insert into"))
                    return ParseInsert(trimmed);
                
                if (lowerScript.StartsWith("update"))
                    return ParseUpdate(trimmed);
                
                if (lowerScript.StartsWith("delete from"))
                    return ParseDelete(trimmed);
                
                if (lowerScript.StartsWith("select"))
                    return ParseSelect(trimmed);
                
                if (lowerScript.StartsWith("show tables"))
                    return GetRandomTemplate("select_intro") + " daftar semua tabel di database?";
                
                if (lowerScript.StartsWith("drop table"))
                    return "⚠️⚠️ PERINGATAN: Anda akan menghapus seluruh tabel! Ini akan menghilangkan semua data secara permanen!";

                // Fallback untuk SQL yang tidak dikenali
                return $"Apakah Anda yakin ingin menjalankan operasi database ini?\n\n📝 Perintah: {TruncateSQL(sqlScript)}";
            }
            catch (Exception)
            {
                return $"Apakah Anda ingin menjalankan perintah SQL ini?\n\n{TruncateSQL(sqlScript)}";
            }
        }

        private string ParseInsert(string sql)
        {
            try
            {
                // Extract table name
                var tableMatch = Regex.Match(sql, @"insert\s+into\s+`?(\w+)`?", RegexOptions.IgnoreCase);
                if (!tableMatch.Success) return FallbackMessage(sql);
                
                string tableName = GetNaturalTableName(tableMatch.Groups[1].Value);

                // Extract columns and values
                var columnsMatch = Regex.Match(sql, @"\(([^)]+)\)\s*values", RegexOptions.IgnoreCase);
                var valuesMatch = Regex.Match(sql, @"values\s*\(([^)]+)\)", RegexOptions.IgnoreCase);

                if (!columnsMatch.Success || !valuesMatch.Success)
                    return $"{GetRandomTemplate("insert_intro")} data baru ke tabel {tableName}?";

                var columns = columnsMatch.Groups[1].Value.Split(',')
                    .Select(c => c.Trim().Trim('`', '"', '\'', '[', ']')).ToArray();
                var values = ParseValues(valuesMatch.Groups[1].Value);

                // Build natural description
                var details = BuildDetailedDescription(columns, values);
                
                string intro = GetRandomTemplate("insert_intro");
                string ending = GetRandomTemplate("insert_ending");
                ending = ending.Replace("{0}", tableName);

                if (!string.IsNullOrEmpty(details))
                {
                    // Variasi format
                    int format = _random.Next(3);
                    switch (format)
                    {
                        case 0:
                            return $"{intro} data berikut {ending}\n\n{details}";
                        case 1:
                            return $"📝 {intro}:\n{details}\n\nKe tabel {tableName}?";
                        case 2:
                            return $"{intro} {details} {ending}";
                        default:
                            return $"{intro} data baru {ending}\n\n{details}";
                    }
                }

                return $"{intro} data baru ke tabel {tableName}?";
            }
            catch
            {
                return FallbackMessage(sql);
            }
        }

        private string ParseUpdate(string sql)
        {
            try
            {
                var tableMatch = Regex.Match(sql, @"update\s+`?(\w+)`?", RegexOptions.IgnoreCase);
                if (!tableMatch.Success) return FallbackMessage(sql);
                
                string tableName = GetNaturalTableName(tableMatch.Groups[1].Value);

                // Extract SET clause
                var setMatch = Regex.Match(sql, @"set\s+(.+?)(?:where|$)", RegexOptions.IgnoreCase);
                var whereMatch = Regex.Match(sql, @"where\s+(.+?)(?:;|$)", RegexOptions.IgnoreCase);

                string intro = GetRandomTemplate("update_intro");
                
                if (setMatch.Success)
                {
                    var updates = ParseSetClause(setMatch.Groups[1].Value);
                    string condition = whereMatch.Success ? ParseWhereClause(whereMatch.Groups[1].Value) : "semua data";

                    string updateDesc = string.Join(", ", updates.Select(kvp => 
                        $"**{GetNaturalColumnName(kvp.Key)}** menjadi '{kvp.Value}'"));

                    int format = _random.Next(3);
                    switch (format)
                    {
                        case 0:
                            return $"{intro} data di tabel {tableName}?\n\n📝 Perubahan:\n{updateDesc}\n\n🎯 Untuk: {condition}";
                        case 1:
                            return $"✏️ {intro} {updateDesc} pada {condition} di tabel {tableName}?";
                        case 2:
                            return $"{intro}:\n• {updateDesc}\n• Target: {condition}\n• Tabel: {tableName}";
                        default:
                            return $"{intro} {updateDesc} untuk {condition}?";
                    }
                }

                return $"{intro} data di tabel {tableName}?";
            }
            catch
            {
                return FallbackMessage(sql);
            }
        }

        private string ParseDelete(string sql)
        {
            try
            {
                var tableMatch = Regex.Match(sql, @"delete\s+from\s+`?(\w+)`?", RegexOptions.IgnoreCase);
                if (!tableMatch.Success) return FallbackMessage(sql);
                
                string tableName = GetNaturalTableName(tableMatch.Groups[1].Value);
                var whereMatch = Regex.Match(sql, @"where\s+(.+?)(?:;|$)", RegexOptions.IgnoreCase);

                string intro = GetRandomTemplate("delete_intro");
                string warning = GetRandomTemplate("delete_warning");
                string condition = whereMatch.Success ? ParseWhereClause(whereMatch.Groups[1].Value) : "⚠️ SEMUA DATA";

                int format = _random.Next(3);
                switch (format)
                {
                    case 0:
                        return $"{intro} data berikut?\n\n🎯 Target: {condition}\n📦 Tabel: {tableName}\n\n⚠️ {warning}";
                    case 1:
                        return $"🗑️ {intro} {condition} dari tabel {tableName}?\n\n{warning}";
                    case 2:
                        return $"{intro}:\n• Tabel: {tableName}\n• Kriteria: {condition}\n\n⚠️ {warning}";
                    default:
                        return $"{intro} {condition} dari tabel {tableName}?\n\n{warning}";
                }
            }
            catch
            {
                return FallbackMessage(sql);
            }
        }

        private string ParseSelect(string sql)
        {
            try
            {
                var tableMatch = Regex.Match(sql, @"from\s+`?(\w+)`?", RegexOptions.IgnoreCase);
                if (!tableMatch.Success) return FallbackMessage(sql);
                
                string tableName = GetNaturalTableName(tableMatch.Groups[1].Value);
                var whereMatch = Regex.Match(sql, @"where\s+(.+?)(?:order by|limit|group by|;|$)", RegexOptions.IgnoreCase);
                var limitMatch = Regex.Match(sql, @"limit\s+(\d+)", RegexOptions.IgnoreCase);

                string intro = GetRandomTemplate("select_intro");
                string condition = whereMatch.Success ? ParseWhereClause(whereMatch.Groups[1].Value) : "semua data";
                string limit = limitMatch.Success ? $" (maksimal {limitMatch.Groups[1].Value} baris)" : "";

                // Check if it's SELECT COUNT, SELECT DISTINCT, etc.
                if (sql.ToLowerInvariant().Contains("count("))
                    return $"📊 Menghitung jumlah {condition} di tabel {tableName}?";
                
                if (sql.ToLowerInvariant().Contains("distinct"))
                    return $"🔍 Menampilkan data unik {condition} dari tabel {tableName}?";

                int format = _random.Next(3);
                switch (format)
                {
                    case 0:
                        return $"{intro} {condition} dari tabel {tableName}{limit}?";
                    case 1:
                        return $"🔍 {intro}:\n• Tabel: {tableName}\n• Filter: {condition}{limit}";
                    case 2:
                        return $"Tampilkan {condition} (tabel: {tableName}){limit}?";
                    default:
                        return $"{intro} {condition} dari {tableName}?";
                }
            }
            catch
            {
                return FallbackMessage(sql);
            }
        }

        private string[] ParseValues(string valuesString)
        {
            var values = new List<string>();
            bool inQuotes = false;
            char quoteChar = '\0';
            string current = "";

            foreach (char c in valuesString)
            {
                if ((c == '\'' || c == '"') && !inQuotes)
                {
                    inQuotes = true;
                    quoteChar = c;
                }
                else if (c == quoteChar && inQuotes)
                {
                    inQuotes = false;
                    quoteChar = '\0';
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(current.Trim().Trim('\'', '"'));
                    current = "";
                }
                else
                {
                    current += c;
                }
            }
            
            if (!string.IsNullOrEmpty(current))
                values.Add(current.Trim().Trim('\'', '"'));

            return values.ToArray();
        }

        private Dictionary<string, string> ParseSetClause(string setClause)
        {
            var updates = new Dictionary<string, string>();
            var pairs = setClause.Split(',');

            foreach (var pair in pairs)
            {
                var parts = pair.Split('=');
                if (parts.Length == 2)
                {
                    string key = parts[0].Trim().Trim('`', '"', '\'', '[', ']');
                    string value = parts[1].Trim().Trim('\'', '"');
                    updates[key] = value;
                }
            }

            return updates;
        }

        private string ParseWhereClause(string whereClause)
        {
            // Simple parsing - bisa diperluas
            whereClause = whereClause.Trim()
                .Replace("=", " sama dengan ")
                .Replace(">", " lebih dari ")
                .Replace("<", " kurang dari ")
                .Replace("!=", " tidak sama dengan ")
                .Replace("like", " mengandung ")
                .Replace("and", " dan ")
                .Replace("or", " atau ");

            // Clean up column names
            foreach (var col in _columnNames)
            {
                whereClause = Regex.Replace(whereClause, 
                    $@"\b{col.Key}\b", 
                    col.Value, 
                    RegexOptions.IgnoreCase);
            }

            return whereClause;
        }

        private string BuildDetailedDescription(string[] columns, string[] values)
        {
            var details = new List<string>();

            for (int i = 0; i < Math.Min(columns.Length, values.Length); i++)
            {
                if (string.IsNullOrWhiteSpace(values[i]) || values[i] == "NULL") 
                    continue;

                string columnName = GetNaturalColumnName(columns[i]);
                string value = values[i];

                // Format berdasarkan nama kolom
                if (columns[i].ToLower().Contains("price") || columns[i].ToLower().Contains("harga"))
                {
                    if (decimal.TryParse(value, out decimal price))
                        value = $"Rp {price:N0}";
                }
                else if (columns[i].ToLower().Contains("date") || columns[i].ToLower().Contains("tanggal"))
                {
                    if (DateTime.TryParse(value, out DateTime date))
                        value = date.ToString("dd MMM yyyy");
                }

                details.Add($"• **{columnName}**: {value}");
            }

            return string.Join("\n", details);
        }

        private string GetNaturalColumnName(string column)
        {
            string lower = column.ToLowerInvariant();
            return _columnNames.ContainsKey(lower) ? _columnNames[lower] : column;
        }

        private string GetNaturalTableName(string table)
        {
            string lower = table.ToLowerInvariant();
            return _tableNames.ContainsKey(lower) ? _tableNames[lower] : table;
        }

        private string GetRandomTemplate(string key)
        {
            if (_templates.ContainsKey(key) && _templates[key].Count > 0)
                return _templates[key][_random.Next(_templates[key].Count)];
            return "";
        }

        private string TruncateSQL(string sql, int maxLength = 100)
        {
            if (sql.Length <= maxLength) return sql;
            return sql.Substring(0, maxLength) + "...";
        }

        private string FallbackMessage(string sql)
        {
            return $"Apakah Anda yakin ingin menjalankan operasi database ini?\n\n📝 {TruncateSQL(sql)}";
        }

        public string FormattedJsonFile(string rawJson)
        {
            var formattedJson = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(rawJson), Formatting.Indented);
            return formattedJson;
        }

        public string RemoveSignScript(string currentResponse)
        {
            if (string.IsNullOrWhiteSpace(currentResponse))
                return currentResponse;

            // Hapus tanda pembuka ```sql dan penutup ```
            string result = Regex.Replace(
                currentResponse,
                @"^```json\s*|\s*```$",
                "",
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );

            result = Regex.Replace(result, @"\r\n?|\n", " ");

            return result.Trim();
        }
    }
}
