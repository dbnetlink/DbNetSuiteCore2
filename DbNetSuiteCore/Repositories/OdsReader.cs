using DbNetSuiteCore.Extensions;
using DbNetSuiteCore.Models;
using System.Data;
using System.Globalization;
using System.IO.Compression;
using System.Reflection.Metadata;
using System.Xml;

namespace DbNetSuiteCore.Repositories
{
    public class OdsReader : IDisposable
    {
        private static string[,] namespaces = new string[,]
        {
            {"table", "urn:oasis:names:tc:opendocument:xmlns:table:1.0"},
            {"office", "urn:oasis:names:tc:opendocument:xmlns:office:1.0"},
            {"style", "urn:oasis:names:tc:opendocument:xmlns:style:1.0"},
            {"text", "urn:oasis:names:tc:opendocument:xmlns:text:1.0"},
            {"draw", "urn:oasis:names:tc:opendocument:xmlns:drawing:1.0"},
            {"fo", "urn:oasis:names:tc:opendocument:xmlns:xsl-fo-compatible:1.0"},
            {"dc", "http://purl.org/dc/elements/1.1/"},
            {"meta", "urn:oasis:names:tc:opendocument:xmlns:meta:1.0"},
            {"number", "urn:oasis:names:tc:opendocument:xmlns:datastyle:1.0"},
            {"presentation", "urn:oasis:names:tc:opendocument:xmlns:presentation:1.0"},
            {"svg", "urn:oasis:names:tc:opendocument:xmlns:svg-compatible:1.0"},
            {"chart", "urn:oasis:names:tc:opendocument:xmlns:chart:1.0"},
            {"dr3d", "urn:oasis:names:tc:opendocument:xmlns:dr3d:1.0"},
            {"math", "http://www.w3.org/1998/Math/MathML"},
            {"form", "urn:oasis:names:tc:opendocument:xmlns:form:1.0"},
            {"script", "urn:oasis:names:tc:opendocument:xmlns:script:1.0"},
            {"ooo", "http://openoffice.org/2004/office"},
            {"ooow", "http://openoffice.org/2004/writer"},
            {"oooc", "http://openoffice.org/2004/calc"},
            {"dom", "http://www.w3.org/2001/xml-events"},
            {"xforms", "http://www.w3.org/2002/xforms"},
            {"xsd", "http://www.w3.org/2001/XMLSchema"},
            {"xsi", "http://www.w3.org/2001/XMLSchema-instance"},
            {"rpt", "http://openoffice.org/2005/report"},
            {"of", "urn:oasis:names:tc:opendocument:xmlns:of:1.2"},
            {"rdfa", "http://docs.oasis-open.org/opendocument/meta/rdfa#"},
            {"config", "urn:oasis:names:tc:opendocument:xmlns:config:1.0"}
        };

        public OdsReader()
        {
        }

        public void Dispose()
        {
            // Dispose of unmanaged resources if any
        }   

        public DataTable GetDataTableFromUrl(string url, string? sheetName )
        {
            using (HttpClient client = new HttpClient())
            using (Stream stream = client.GetStreamAsync(url).Result)
            {

                using (var memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);
                    using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read, false))
                    {
                        DataSet dataSet = ReadOdsFile(archive);
                        DataTable dataTable = dataSet.GetTable(sheetName);
                        return dataTable;
                    }
                }
            }
        }

        private DataSet ReadOdsFile(ZipArchive zipFile)
        {
            XmlDocument contentXml = this.GetContentXmlFile(zipFile);

            XmlNamespaceManager nmsManager = this.InitializeXmlNamespaceManager(contentXml);

            DataSet odsFile = new DataSet();

            foreach (XmlNode tableNode in this.GetTableNodes(contentXml, nmsManager))
                odsFile.Tables.Add(this.GetSheet(tableNode, nmsManager));

            return odsFile;
        }


        private XmlDocument GetContentXmlFile(ZipArchive zipFile)
        {
            XmlDocument contentXml = new XmlDocument();

            ZipArchiveEntry? contentZipEntry = zipFile.GetEntry("content.xml");
            if (contentZipEntry != null)
            {
                using (Stream contentStream = contentZipEntry.Open())
                {
                    contentXml.Load(contentStream);
                }
            }

            return contentXml;
        }
        private XmlNamespaceManager InitializeXmlNamespaceManager(XmlDocument xmlDocument)
        {
            XmlNamespaceManager nmsManager = new XmlNamespaceManager(xmlDocument.NameTable);

            for (int i = 0; i < namespaces.GetLength(0); i++)
                nmsManager.AddNamespace(namespaces[i, 0], namespaces[i, 1]);

            return nmsManager;
        }



        // In ODF sheet is stored in table:table node
        private XmlNodeList GetTableNodes(XmlDocument contentXmlDocument, XmlNamespaceManager nmsManager)
        {
            return contentXmlDocument.SelectNodes("/office:document-content/office:body/office:spreadsheet/table:table", nmsManager)!;
        }

        private DataTable GetSheet(XmlNode tableNode, XmlNamespaceManager nmsManager)
        {
            DataTable sheet = new DataTable(tableNode.Attributes!["table:name"]!.Value);

            XmlNodeList rowNodes = tableNode.SelectNodes("table:table-row", nmsManager)!;

            int rowIndex = 0;
            foreach (XmlNode rowNode in rowNodes)
                this.GetRow(rowNode, sheet, nmsManager, ref rowIndex);

            return sheet;
        }

        private void GetRow(XmlNode rowNode, DataTable sheet, XmlNamespaceManager nmsManager, ref int rowIndex)
        {
            XmlAttribute? rowsRepeated = rowNode.Attributes!["table:number-rows-repeated"];
            if (rowsRepeated == null || Convert.ToInt32(rowsRepeated.Value, CultureInfo.InvariantCulture) == 1)
            {
                while (sheet.Rows.Count < rowIndex)
                    sheet.Rows.Add(sheet.NewRow());

                DataRow row = sheet.NewRow();

                XmlNodeList cellNodes = rowNode.SelectNodes("table:table-cell", nmsManager)!;

                int cellIndex = 0;
                foreach (XmlNode cellNode in cellNodes)
                    this.GetCell(cellNode, row, nmsManager, ref cellIndex);

                sheet.Rows.Add(row);

                rowIndex++;
            }
            else
            {
                rowIndex += Convert.ToInt32(rowsRepeated.Value, CultureInfo.InvariantCulture);
            }

            // sheet must have at least one cell
            if (sheet.Rows.Count == 0)
            {
                sheet.Rows.Add(sheet.NewRow());
                sheet.Columns.Add();
            }
        }

        private void GetCell(XmlNode cellNode, DataRow row, XmlNamespaceManager nmsManager, ref int cellIndex)
        {
            XmlAttribute cellRepeated = cellNode.Attributes!["table:number-columns-repeated"]!;
            string cellValue = this.ReadCellValue(cellNode);

            int repeats = 1;
            if (cellRepeated != null)
            {
                repeats = Convert.ToInt32(cellRepeated.Value, CultureInfo.InvariantCulture);
            }

            if (!String.IsNullOrEmpty(cellValue))
            {
                for (int i = 0; i < repeats; i++)
                {
                    DataTable sheet = row.Table;

                    while (sheet.Columns.Count <= cellIndex)
                        sheet.Columns.Add();

                    row[cellIndex] = cellValue;

                    cellIndex++;
                }
            }
            else
            {
                cellIndex += repeats;
            }
        }

        private string ReadCellValue(XmlNode cell)
        {
            XmlNode? cellVal = cell.ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name.ToLower() == "text");

            if (cellVal == null)
                return cell?.InnerText ?? string.Empty;
            else
                return cellVal?.Value ?? string.Empty;
        }
    }
}
