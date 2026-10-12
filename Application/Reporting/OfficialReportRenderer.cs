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

public sealed record TabularDocumentColumn(string Key, string Heading);

public sealed record TabularDocumentRenderRequest(
    string Municipality,
    string FinancialYear,
    string Period,
    string Heading,
    OfficialReportFormat Format,
    IReadOnlyList<TabularDocumentColumn> Columns,
    IReadOnlyList<IReadOnlyDictionary<string, string>> Rows);

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

    public static OfficialReportRenderResult RenderTable(TabularDocumentRenderRequest request)
    {
        if (request.Columns.Count == 0) throw new ArgumentException("At least one report column is required.", nameof(request));
        if (request.Columns.Any(column => string.IsNullOrWhiteSpace(column.Key) || string.IsNullOrWhiteSpace(column.Heading)))
            throw new ArgumentException("Report column keys and headings are required.", nameof(request));
        if (request.Columns.Select(column => column.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count() != request.Columns.Count)
            throw new ArgumentException("Report column keys must be unique.", nameof(request));

        var columns = request.Columns.Select(column => new Column(column.Key, column.Heading)).ToArray();
        var rows = request.Rows.Select(values => new OfficialReportDataRow(values)).ToArray();
        return RenderCore(request.Municipality, request.FinancialYear, request.Period, request.Heading, request.Format, columns, rows);
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
            AddText(archive, "[Content_Types].xml", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/></Types>");
            AddText(archive, "_rels/.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
            AddText(archive, "xl/workbook.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><bookViews><workbookView xWindow=\"0\" yWindow=\"0\" windowWidth=\"24000\" windowHeight=\"12000\"/></bookViews><sheets><sheet name=\"Performance Report\" sheetId=\"1\" r:id=\"rId1\"/></sheets><definedNames><definedName name=\"_xlnm.Print_Titles\" localSheetId=\"0\">'Performance Report'!$1:$2</definedName></definedNames></workbook>");
            AddText(archive, "xl/_rels/workbook.xml.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>");
            AddText(archive, "xl/styles.xml", XlsxStyles);
            var lastColumn = ExcelColumnName(columns.Count);
            var sheet = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetPr><pageSetUpPr fitToPage=\"1\"/></sheetPr>");
            sheet.Append("<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"2\" topLeftCell=\"A3\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>");
            sheet.Append("<sheetFormatPr defaultRowHeight=\"15\"/>");
            sheet.Append("<cols>");
            for (var index = 0; index < columns.Count; index++)
                sheet.Append("<col min=\"").Append(index + 1).Append("\" max=\"").Append(index + 1).Append("\" width=\"").Append(XlsxWidth(columns[index].Key).ToString(CultureInfo.InvariantCulture)).Append("\" customWidth=\"1\"/>");
            sheet.Append("</cols><sheetData>");
            AppendSheetRow(sheet, [heading], 1, 1);
            AppendSheetRow(sheet, columns.Select(column => column.Heading), 2, 2);
            var rowIndex = 3;
            foreach (var row in rows)
                AppendSheetRow(sheet, columns.Select(column => row.Value(column.Key)), rowIndex, rowIndex++ % 2 == 0 ? 4 : 3);
            // SpreadsheetML child order is significant. In particular, autoFilter
            // must precede mergeCells or desktop Excel rejects the package.
            sheet.Append("</sheetData><autoFilter ref=\"A2:").Append(lastColumn).Append(Math.Max(2, rowIndex - 1)).Append("\"/>")
                .Append("<mergeCells count=\"1\"><mergeCell ref=\"A1:").Append(lastColumn).Append("1\"/></mergeCells>")
                .Append("<pageMargins left=\"0.25\" right=\"0.25\" top=\"0.5\" bottom=\"0.5\" header=\"0.2\" footer=\"0.2\"/><pageSetup paperSize=\"1\" orientation=\"landscape\" fitToWidth=\"1\" fitToHeight=\"0\"/></worksheet>");
            AddText(archive, "xl/worksheets/sheet1.xml", sheet.ToString());
        }
        return result.ToArray();
    }

    private const string XlsxStyles = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><fonts count=\"3\"><font><sz val=\"10\"/><name val=\"Aptos\"/></font><font><b/><sz val=\"15\"/><color rgb=\"FF000000\"/><name val=\"Aptos Display\"/></font><font><b/><sz val=\"10\"/><color rgb=\"FFFFFFFF\"/><name val=\"Aptos\"/></font></fonts><fills count=\"4\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill><fill><patternFill patternType=\"solid\"><fgColor rgb=\"FF1F4E78\"/><bgColor indexed=\"64\"/></patternFill></fill><fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFEAF2F8\"/><bgColor indexed=\"64\"/></patternFill></fill></fills><borders count=\"2\"><border/><border><left style=\"thin\"><color rgb=\"FFD9D9D9\"/></left><right style=\"thin\"><color rgb=\"FFD9D9D9\"/></right><top style=\"thin\"><color rgb=\"FFD9D9D9\"/></top><bottom style=\"thin\"><color rgb=\"FFD9D9D9\"/></bottom><diagonal/></border></borders><cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs><cellXfs count=\"5\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyAlignment=\"1\"><alignment vertical=\"center\"/></xf><xf numFmtId=\"0\" fontId=\"2\" fillId=\"2\" borderId=\"1\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\" wrapText=\"1\"/></xf><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyBorder=\"1\" applyAlignment=\"1\"><alignment vertical=\"center\" wrapText=\"1\"/></xf><xf numFmtId=\"0\" fontId=\"0\" fillId=\"3\" borderId=\"1\" xfId=\"0\" applyFill=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment vertical=\"center\" wrapText=\"1\"/></xf></cellXfs><cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles></styleSheet>";

    private static void AppendSheetRow(StringBuilder xml, IEnumerable<string> values, int rowIndex, int styleIndex)
    {
        xml.Append("<row r=\"").Append(rowIndex).Append("\"");
        if (rowIndex == 1) xml.Append(" ht=\"24\" customHeight=\"1\"");
        else if (rowIndex == 2) xml.Append(" ht=\"30\" customHeight=\"1\"");
        xml.Append('>');
        var columnIndex = 1;
        foreach (var value in values)
            xml.Append("<c r=\"").Append(ExcelColumnName(columnIndex++)).Append(rowIndex).Append("\" s=\"").Append(styleIndex).Append("\" t=\"inlineStr\"><is><t xml:space=\"preserve\">").Append(Xml(value)).Append("</t></is></c>");
        xml.Append("</row>");
    }

    private static string ExcelColumnName(int index)
    {
        var value = new StringBuilder();
        while (index > 0) { index--; value.Insert(0, (char)('A' + index % 26)); index /= 26; }
        return value.ToString();
    }

    private static double XlsxWidth(string key) => key switch
    {
        "indicator" => 14, "targetName" => 42, "department" => 26, "unit" => 28, "period" => 16,
        "targetValue" => 16, "actualPerformance" => 18, "variance" => 20, "achievementPercent" => 18,
        "targetAchieved" => 15, "status" => 24, _ => 22
    };

    private static byte[] RenderDocx(string heading, IReadOnlyList<Column> columns, IReadOnlyList<OfficialReportDataRow> rows)
    {
        using var result = new MemoryStream();
        using (var archive = new ZipArchive(result, ZipArchiveMode.Create, true))
        {
            AddText(archive, "[Content_Types].xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/word/document.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml\"/></Types>");
            AddText(archive, "_rels/.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"word/document.xml\"/></Relationships>");
            var document = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body>");
            document.Append("<w:p><w:pPr><w:spacing w:after=\"180\"/></w:pPr><w:r><w:rPr><w:b/><w:color w:val=\"000000\"/><w:sz w:val=\"28\"/><w:szCs w:val=\"28\"/></w:rPr><w:t>").Append(Xml(heading)).Append("</w:t></w:r></w:p><w:tbl>")
                .Append("<w:tblPr><w:tblW w:w=\"0\" w:type=\"auto\"/><w:tblLayout w:type=\"fixed\"/><w:tblBorders><w:top w:val=\"single\" w:sz=\"4\" w:color=\"D9D9D9\"/><w:left w:val=\"single\" w:sz=\"4\" w:color=\"D9D9D9\"/><w:bottom w:val=\"single\" w:sz=\"4\" w:color=\"D9D9D9\"/><w:right w:val=\"single\" w:sz=\"4\" w:color=\"D9D9D9\"/><w:insideH w:val=\"single\" w:sz=\"4\" w:color=\"D9D9D9\"/><w:insideV w:val=\"single\" w:sz=\"4\" w:color=\"D9D9D9\"/></w:tblBorders><w:tblCellMar><w:top w:w=\"60\" w:type=\"dxa\"/><w:left w:w=\"60\" w:type=\"dxa\"/><w:bottom w:w=\"60\" w:type=\"dxa\"/><w:right w:w=\"60\" w:type=\"dxa\"/></w:tblCellMar></w:tblPr><w:tblGrid>");
            foreach (var column in columns) document.Append("<w:gridCol w:w=\"").Append(DocxWidth(column.Key)).Append("\"/>");
            document.Append("</w:tblGrid>");
            AppendWordRow(document, columns, columns.Select(column => column.Heading), true, false);
            var odd = false;
            foreach (var row in rows) { AppendWordRow(document, columns, columns.Select(column => row.Value(column.Key)), false, odd); odd = !odd; }
            document.Append("</w:tbl><w:sectPr><w:pgSz w:w=\"16838\" w:h=\"11906\" w:orient=\"landscape\"/><w:pgMar w:top=\"600\" w:right=\"600\" w:bottom=\"600\" w:left=\"600\" w:header=\"300\" w:footer=\"300\" w:gutter=\"0\"/></w:sectPr></w:body></w:document>");
            AddText(archive, "word/document.xml", document.ToString());
        }
        return result.ToArray();
    }

    private static void AppendWordRow(StringBuilder xml, IReadOnlyList<Column> columns, IEnumerable<string> values, bool header, bool shaded)
    {
        xml.Append("<w:tr><w:trPr>");
        if (header) xml.Append("<w:tblHeader w:val=\"true\"/>");
        xml.Append("<w:cantSplit/></w:trPr>");
        var index = 0;
        foreach (var value in values)
        {
            var width = DocxWidth(columns[index++].Key);
            xml.Append("<w:tc><w:tcPr><w:tcW w:w=\"").Append(width).Append("\" w:type=\"dxa\"/>");
            if (header) xml.Append("<w:shd w:val=\"clear\" w:color=\"auto\" w:fill=\"1F4E78\"/>");
            else if (shaded) xml.Append("<w:shd w:val=\"clear\" w:color=\"auto\" w:fill=\"EAF2F8\"/>");
            xml.Append("<w:vAlign w:val=\"center\"/></w:tcPr><w:p><w:pPr><w:spacing w:before=\"0\" w:after=\"0\" w:line=\"180\" w:lineRule=\"auto\"/>");
            if (header) xml.Append("<w:jc w:val=\"center\"/>");
            xml.Append("</w:pPr><w:r><w:rPr>");
            if (header) xml.Append("<w:b/><w:color w:val=\"FFFFFF\"/>");
            else xml.Append("<w:color w:val=\"000000\"/>");
            xml.Append("<w:sz w:val=\"14\"/><w:szCs w:val=\"14\"/></w:rPr><w:t xml:space=\"preserve\">").Append(Xml(value)).Append("</w:t></w:r></w:p></w:tc>");
        }
        xml.Append("</w:tr>");
    }

    private static int DocxWidth(string key) => key switch
    {
        "indicator" => 850, "targetName" => 1900, "department" => 1300, "unit" => 1350, "period" => 850,
        "targetValue" => 800, "actualPerformance" => 900, "variance" => 900, "achievementPercent" => 900,
        "targetAchieved" => 750, "status" => 1100, _ => 1000
    };

    private static byte[] RenderPdf(string heading, IReadOnlyList<Column> columns, IReadOnlyList<OfficialReportDataRow> rows)
    {
        const double pageWidth = 792;
        const double pageHeight = 612;
        const double left = 24;
        const double right = 24;
        const double bottom = 28;
        const double tableTop = 535;
        const double fontSize = 5.4;
        const double lineHeight = 6.7;
        const double padding = 2.5;
        var availableWidth = pageWidth - left - right;
        var weights = columns.Select(column => PdfWeight(column.Key)).ToArray();
        var weightTotal = weights.Sum();
        var widths = weights.Select(weight => availableWidth * weight / weightTotal).ToArray();
        var headerLines = columns.Select((column, index) => WrapPdf(column.Heading, widths[index] - padding * 2, fontSize, true)).ToArray();
        var headerHeight = Math.Max(18, headerLines.Max(lines => lines.Length) * lineHeight + padding * 2);
        var preparedRows = rows.Select(row =>
        {
            var lines = columns.Select((column, index) => WrapPdf(row.Value(column.Key), widths[index] - padding * 2, fontSize, false)).ToArray();
            return new PdfPreparedRow(lines, Math.Max(13, lines.Max(value => value.Length) * lineHeight + padding * 2));
        }).ToArray();
        var pages = new List<List<PdfPreparedRow>> { new() };
        var remaining = tableTop - headerHeight - bottom;
        foreach (var row in preparedRows)
        {
            if (pages[^1].Count > 0 && row.Height > remaining)
            {
                pages.Add(new());
                remaining = tableTop - headerHeight - bottom;
            }
            pages[^1].Add(row);
            remaining -= row.Height;
        }
        var regularFontObjectId = 3 + pages.Count * 2;
        var boldFontObjectId = regularFontObjectId + 1;
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            $"<< /Type /Pages /Kids [{string.Join(' ', Enumerable.Range(0, pages.Count).Select(index => $"{3 + index * 2} 0 R"))}] /Count {pages.Count} >>"
        };
        for (var pageIndex = 0; pageIndex < pages.Count; pageIndex++)
        {
            var pageObjectId = 3 + pageIndex * 2;
            var contentObjectId = pageObjectId + 1;
            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {N(pageWidth)} {N(pageHeight)}] /Resources << /Font << /F1 {regularFontObjectId} 0 R /F2 {boldFontObjectId} 0 R >> >> /Contents {contentObjectId} 0 R >>");
            var content = new StringBuilder();
            AppendPdfText(content, heading, left, pageHeight - 30, 13, true);
            AppendPdfText(content, $"Official performance report  |  Generated record set: {rows.Count} row{(rows.Count == 1 ? string.Empty : "s")}", left, pageHeight - 47, 7.5, false);
            var y = tableTop;
            AppendPdfRow(content, headerLines, widths, left, y, headerHeight, lineHeight, padding, true, false);
            y -= headerHeight;
            var shaded = false;
            foreach (var row in pages[pageIndex])
            {
                AppendPdfRow(content, row.Lines, widths, left, y, row.Height, lineHeight, padding, false, shaded);
                y -= row.Height;
                shaded = !shaded;
            }
            AppendPdfText(content, $"Page {pageIndex + 1} of {pages.Count}", pageWidth - 82, 14, 7, false);
            var stream = content.ToString();
            objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}\nendstream");
        }
        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>");
        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>");
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

    private sealed record PdfPreparedRow(string[][] Lines, double Height);

    private static double PdfWeight(string key) => key switch
    {
        "indicator" => 0.8, "targetName" => 2.25, "department" => 1.45, "unit" => 1.5, "period" => 0.9,
        "targetValue" => 0.85, "actualPerformance" => 0.95, "variance" => 1.0, "achievementPercent" => 0.95,
        "targetAchieved" => 0.8, "status" => 1.2, _ => 1.0
    };

    private static string[] WrapPdf(string? value, double width, double fontSize, bool heading)
    {
        var text = PdfText(value ?? string.Empty);
        if (text.Length == 0) return [string.Empty];
        var maximum = Math.Max(3, (int)Math.Floor(width / (fontSize * (heading ? 0.57 : 0.51))));
        var lines = new List<string>();
        var current = new StringBuilder();
        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var remainder = word;
            while (remainder.Length > maximum)
            {
                if (current.Length > 0) { lines.Add(current.ToString()); current.Clear(); }
                lines.Add(remainder[..maximum]);
                remainder = remainder[maximum..];
            }
            if (current.Length == 0) current.Append(remainder);
            else if (current.Length + 1 + remainder.Length <= maximum) current.Append(' ').Append(remainder);
            else { lines.Add(current.ToString()); current.Clear(); current.Append(remainder); }
        }
        if (current.Length > 0) lines.Add(current.ToString());
        return lines.Count == 0 ? [string.Empty] : lines.ToArray();
    }

    private static void AppendPdfRow(StringBuilder content, string[][] cells, double[] widths, double left, double top, double height, double lineHeight, double padding, bool header, bool shaded)
    {
        if (header) content.Append("0.122 0.306 0.471 rg ").Append(N(left)).Append(' ').Append(N(top - height)).Append(' ').Append(N(widths.Sum())).Append(' ').Append(N(height)).Append(" re f ");
        else if (shaded) content.Append("0.918 0.949 0.973 rg ").Append(N(left)).Append(' ').Append(N(top - height)).Append(' ').Append(N(widths.Sum())).Append(' ').Append(N(height)).Append(" re f ");
        content.Append("0.82 G 0.35 w ");
        var x = left;
        for (var index = 0; index < widths.Length; index++)
        {
            content.Append(N(x)).Append(' ').Append(N(top - height)).Append(' ').Append(N(widths[index])).Append(' ').Append(N(height)).Append(" re S ");
            var textY = top - padding - lineHeight + 1;
            foreach (var line in cells[index])
            {
                AppendPdfText(content, line, x + padding, textY, 5.4, header);
                textY -= lineHeight;
            }
            x += widths[index];
        }
    }

    private static void AppendPdfText(StringBuilder content, string value, double x, double y, double size, bool bold) =>
        content.Append("BT /").Append(bold ? "F2" : "F1").Append(' ').Append(N(size)).Append(" Tf ")
            .Append(PdfTextColor(bold, size)).Append(N(x)).Append(' ').Append(N(y)).Append(" Td (").Append(Pdf(value)).Append(") Tj ET ");

    private static string PdfTextColor(bool bold, double size) => bold && size < 10 ? "1 1 1 rg " : "0 0 0 rg ";

    private static string N(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string PdfText(string value)
    {
        var punctuation = value
            .Replace('\u2013', '-').Replace('\u2014', '-').Replace('\u2011', '-').Replace('\u2212', '-')
            .Replace('\u2018', '\'').Replace('\u2019', '\'').Replace('\u201C', '"').Replace('\u201D', '"')
            .Replace('\u2022', '-').Replace('\u00B7', '-').Replace('\u00A0', ' ');
        return string.Concat(punctuation.Normalize(NormalizationForm.FormD)
            .Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark))
            .Normalize(NormalizationForm.FormC);
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
    private static string Pdf(string value) => string.Concat(PdfText(value).Select(character => character is >= ' ' and <= '~' ? character : '?')).Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");

    private static OfficialReportDataRow ToDataRow(OfficialPerformanceReportRow row) => new(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["indicator"] = row.Indicator, ["targetName"] = row.TargetName, ["department"] = row.Department, ["unit"] = row.Unit,
        ["period"] = row.Period, ["targetValue"] = row.TargetValue, ["actualPerformance"] = row.ActualPerformance,
        ["variance"] = row.Variance, ["achievementPercent"] = row.AchievementPercent, ["targetAchieved"] = row.TargetAchieved, ["status"] = row.Status
    });
}
