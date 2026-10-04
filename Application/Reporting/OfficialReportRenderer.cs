using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml;
using FTCERP.Host.Domain.Entities;

namespace FTCERP.Host.Application.Reporting;

public sealed record OfficialPerformanceReportRow(
    string Indicator,
    string TargetName,
    string Department,
    string Unit,
    string Period,
    string TargetValue,
    string ActualPerformance,
    string Variance,
    string AchievementPercent,
    string TargetAchieved,
    string Status);

public sealed record OfficialReportRenderRequest(
    string Municipality,
    string FinancialYear,
    string Period,
    string HeadingTemplate,
    string ColumnConfigurationJson,
    OfficialReportFormat Format,
    IReadOnlyList<OfficialPerformanceReportRow> Rows);

public sealed record OfficialReportRenderResult(byte[] Content, string ContentType, string Extension, string DataVersionReference, string Sha256);

public sealed record OfficialTabularReportRenderRequest(
    string Municipality,
    string FinancialYear,
    string Period,
    string HeadingTemplate,
    string ColumnConfigurationJson,
    OfficialReportFormat Format,
    OfficialReportType ReportType,
    IReadOnlyList<OfficialReportDataRow> Rows);

/// <summary>Renders every official format from one canonical report dataset.</summary>
public static class OfficialReportRenderer
{
    private sealed record Column(string Key, string Heading);

    private static readonly Column[] AllColumns =
    [
        new("indicator", "Indicator"),
        new("targetName", "Target"),
        new("department", "Department"),
        new("unit", "Unit"),
        new("period", "Period"),
        new("targetValue", "Target Value"),
        new("actualPerformance", "Actual Performance"),
        new("variance", "Variance"),
        new("achievementPercent", "Achievement Percent"),
        new("targetAchieved", "Target Achieved"),
        new("status", "Status")
    ];

    public static readonly string DefaultColumnsJson = JsonSerializer.Serialize(AllColumns.Select(column => column.Key));

    public static OfficialReportRenderResult Render(OfficialReportRenderRequest request)
    {
        var columns = ResolveColumns(request.ColumnConfigurationJson);
        var rows = request.Rows.Select(ToDataRow).ToArray();
        return RenderCore(request.Municipality, request.FinancialYear, request.Period, request.HeadingTemplate, request.Format, columns, rows);
    }

    public static OfficialReportRenderResult RenderTabular(OfficialTabularReportRenderRequest request)
    {
        var columns = OfficialReportCatalog.ResolveColumns(request.ReportType, request.ColumnConfigurationJson)
            .Select(item => new Column(item.Key, item.Heading)).ToArray();
        return RenderCore(request.Municipality, request.FinancialYear, request.Period, request.HeadingTemplate, request.Format, columns, request.Rows);
    }

    private static OfficialReportRenderResult RenderCore(string municipality, string financialYear, string period, string headingTemplate, OfficialReportFormat format, IReadOnlyList<Column> columns, IReadOnlyList<OfficialReportDataRow> rows)
    {
        var heading = (headingTemplate ?? string.Empty)
            .Replace("{Municipality}", municipality, StringComparison.OrdinalIgnoreCase)
            .Replace("{FinancialYear}", financialYear, StringComparison.OrdinalIgnoreCase)
            .Replace("{Period}", period, StringComparison.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(heading)) heading = $"{financialYear} {period} PERFORMANCE REPORT";

        var canonical = JsonSerializer.SerializeToUtf8Bytes(new
        {
            Municipality = municipality,
            FinancialYear = financialYear,
            Period = period,
            Heading = heading,
            Columns = columns.Select(item => item.Key),
            Rows = rows.Select(row => columns.ToDictionary(column => column.Key, column => row.Value(column.Key), StringComparer.OrdinalIgnoreCase))
        });
        var dataVersion = Convert.ToHexString(SHA256.HashData(canonical)).ToLowerInvariant();

        var rendered = format switch
        {
            OfficialReportFormat.Csv => (RenderCsv(heading, columns, rows), "text/csv; charset=utf-8", "csv"),
            OfficialReportFormat.Xlsx => (RenderXlsx(heading, columns, rows), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx"),
            OfficialReportFormat.Docx => (RenderDocx(heading, columns, rows), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "docx"),
            OfficialReportFormat.Pdf => (RenderPdf(heading, columns, rows), "application/pdf", "pdf"),
            _ => throw new ArgumentOutOfRangeException(nameof(format), "Unsupported official report format.")
        };
        return new(rendered.Item1, rendered.Item2, rendered.Item3, dataVersion, Convert.ToHexString(SHA256.HashData(rendered.Item1)).ToLowerInvariant());
    }

    public static string[] ValidateColumns(string? json)
    {
        var columns = ResolveColumns(json);
        return columns.Select(item => item.Key).ToArray();
    }

    private static Column[] ResolveColumns(string? json)
    {
        string[] requested;
        try { requested = JsonSerializer.Deserialize<string[]>(string.IsNullOrWhiteSpace(json) ? DefaultColumnsJson : json) ?? []; }
        catch (JsonException exception) { throw new ArgumentException("Column configuration must be a JSON array of supported column names.", nameof(json), exception); }
        if (requested.Length == 0) requested = JsonSerializer.Deserialize<string[]>(DefaultColumnsJson)!;
        if (requested.Length != requested.Distinct(StringComparer.OrdinalIgnoreCase).Count()) throw new ArgumentException("Column configuration contains duplicate columns.", nameof(json));
        var lookup = AllColumns.ToDictionary(item => item.Key, StringComparer.OrdinalIgnoreCase);
        if (requested.Any(key => !lookup.ContainsKey(key))) throw new ArgumentException("Column configuration contains an unsupported column.", nameof(json));
        return requested.Select(key => lookup[key]).ToArray();
    }

    private static byte[] RenderCsv(string heading, IReadOnlyList<Column> columns, IReadOnlyList<OfficialReportDataRow> rows)
    {
        var value = new StringBuilder();
        value.AppendLine(Csv(heading));
        value.AppendLine(string.Join(',', columns.Select(column => Csv(column.Heading))));
        foreach (var row in rows) value.AppendLine(string.Join(',', columns.Select(column => Csv(row.Value(column.Key)))));
        return new UTF8Encoding(true).GetBytes(value.ToString());
    }

    private static string Csv(string? input)
    {
        var safe = input ?? string.Empty;
        if (safe.Length > 0 && "=+-@".Contains(safe[0])) safe = "'" + safe;
        return '"' + safe.Replace("\"", "\"\"") + '"';
    }

    private static byte[] RenderXlsx(string heading, IReadOnlyList<Column> columns, IReadOnlyList<OfficialReportDataRow> rows)
    {
        using var result = new MemoryStream();
        using (var archive = new ZipArchive(result, ZipArchiveMode.Create, true))
        {
            AddText(archive, "[Content_Types].xml", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/></Types>");
            AddText(archive, "_rels/.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
            AddText(archive, "xl/workbook.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Performance Report\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
            AddText(archive, "xl/_rels/workbook.xml.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/></Relationships>");
            var sheet = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
            AppendSheetRow(sheet, [heading]);
            AppendSheetRow(sheet, columns.Select(column => column.Heading));
            foreach (var row in rows) AppendSheetRow(sheet, columns.Select(column => row.Value(column.Key)));
            sheet.Append("</sheetData></worksheet>");
            AddText(archive, "xl/worksheets/sheet1.xml", sheet.ToString());
        }
        return result.ToArray();
    }

    private static void AppendSheetRow(StringBuilder xml, IEnumerable<string> values)
    {
        xml.Append("<row>");
        foreach (var value in values) xml.Append("<c t=\"inlineStr\"><is><t xml:space=\"preserve\">").Append(Xml(value)).Append("</t></is></c>");
        xml.Append("</row>");
    }

    private static byte[] RenderDocx(string heading, IReadOnlyList<Column> columns, IReadOnlyList<OfficialReportDataRow> rows)
    {
        using var result = new MemoryStream();
        using (var archive = new ZipArchive(result, ZipArchiveMode.Create, true))
        {
            AddText(archive, "[Content_Types].xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/word/document.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml\"/></Types>");
            AddText(archive, "_rels/.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"word/document.xml\"/></Relationships>");
            var document = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body>");
            document.Append("<w:p><w:r><w:rPr><w:b/></w:rPr><w:t>").Append(Xml(heading)).Append("</w:t></w:r></w:p><w:tbl>");
            AppendWordRow(document, columns.Select(column => column.Heading));
            foreach (var row in rows) AppendWordRow(document, columns.Select(column => row.Value(column.Key)));
            document.Append("</w:tbl><w:sectPr><w:pgSz w:w=\"16838\" w:h=\"11906\" w:orient=\"landscape\"/></w:sectPr></w:body></w:document>");
            AddText(archive, "word/document.xml", document.ToString());
        }
        return result.ToArray();
    }

    private static void AppendWordRow(StringBuilder xml, IEnumerable<string> values)
    {
        xml.Append("<w:tr>");
        foreach (var value in values) xml.Append("<w:tc><w:p><w:r><w:t xml:space=\"preserve\">").Append(Xml(value)).Append("</w:t></w:r></w:p></w:tc>");
        xml.Append("</w:tr>");
    }

    private static byte[] RenderPdf(string heading, IReadOnlyList<Column> columns, IReadOnlyList<OfficialReportDataRow> rows)
    {
        var dataLines = rows.Select(row => string.Join(" | ", columns.Select(column => row.Value(column.Key)))).ToArray();
        var pageData = dataLines.Chunk(55).Select(chunk => chunk.ToArray()).ToList();
        if (pageData.Count == 0) pageData.Add([]);
        var fontObjectId = 3 + pageData.Count * 2;
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            $"<< /Type /Pages /Kids [{string.Join(' ', Enumerable.Range(0, pageData.Count).Select(index => $"{3 + index * 2} 0 R"))}] /Count {pageData.Count} >>"
        };
        for (var pageIndex = 0; pageIndex < pageData.Count; pageIndex++)
        {
            var pageObjectId = 3 + pageIndex * 2;
            var contentObjectId = pageObjectId + 1;
            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 792 612] /Resources << /Font << /F1 {fontObjectId} 0 R >> >> /Contents {contentObjectId} 0 R >>");
            var content = new StringBuilder("BT /F1 7 Tf 30 570 Td 9 TL ");
            var lines = new[] { heading, $"Page {pageIndex + 1} of {pageData.Count}", string.Join(" | ", columns.Select(column => column.Heading)) }.Concat(pageData[pageIndex]);
            foreach (var line in lines) content.Append('(').Append(Pdf(Trim(line, 160))).Append(") Tj T* ");
            content.Append("ET");
            var stream = content.ToString();
            objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}\nendstream");
        }
        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
        using var output = new MemoryStream();
        using var writer = new StreamWriter(output, Encoding.ASCII, 1024, true) { NewLine = "\n" };
        writer.Write("%PDF-1.4\n"); writer.Flush();
        var offsets = new List<long> { 0 };
        for (var index = 0; index < objects.Count; index++) { offsets.Add(output.Position); writer.Write($"{index + 1} 0 obj\n{objects[index]}\nendobj\n"); writer.Flush(); }
        var xref = output.Position;
        writer.Write($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) writer.Write($"{offset:D10} 00000 n \n");
        writer.Write($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF"); writer.Flush();
        return output.ToArray();
    }

    private static void AddText(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        // ZIP entry timestamps otherwise default to the current clock and make
        // identical XLSX/DOCX renders produce different bytes and hashes.
        entry.LastWriteTime = new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private static string Xml(string? value)
    {
        var valid = string.Concat((value ?? string.Empty).Where(XmlConvert.IsXmlChar));
        return System.Security.SecurityElement.Escape(valid) ?? string.Empty;
    }
    private static string Pdf(string value) => string.Concat(value.Select(character => character is >= ' ' and <= '~' ? character : '?')).Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    private static string Trim(string value, int maximum) => value.Length <= maximum ? value : value[..(maximum - 1)] + "…";

    private static OfficialReportDataRow ToDataRow(OfficialPerformanceReportRow row) => new(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["indicator"] = row.Indicator, ["targetName"] = row.TargetName, ["department"] = row.Department, ["unit"] = row.Unit,
        ["period"] = row.Period, ["targetValue"] = row.TargetValue, ["actualPerformance"] = row.ActualPerformance,
        ["variance"] = row.Variance, ["achievementPercent"] = row.AchievementPercent, ["targetAchieved"] = row.TargetAchieved, ["status"] = row.Status
    });
}
