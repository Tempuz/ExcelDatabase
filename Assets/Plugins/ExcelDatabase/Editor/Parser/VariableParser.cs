using System.Collections.Generic;
using System.IO;
using System.Text;
using ExcelDatabase.Editor.Library;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using UnityEditor;
using Object = UnityEngine.Object;

namespace ExcelDatabase.Editor.Parser
{
    public class VariableParser : IParser
    {
        private const int NameCol = 0;
        private const int TypeCol = 1;
        private const int ValueCol = 2;

        private const string RowTemplate = "#ROW#";
        private const string TableVariable = "$TABLE$";
        private const string TypeVariable = "$TYPE$";
        private const string NameVariable = "$NAME$";
        private const string ValueVariable = "$VALUE$";

        private static readonly string TablePath = $"{Config.TemplatePath}/Variable/Table.txt";
        private static readonly string RowPath = $"{Config.TemplatePath}/Variable/Row.txt";

        private readonly ISheet _sheet;
        private readonly string _tableName;
        private readonly string _excelPath;

        public VariableParser(Object file)
        {
            var path = AssetDatabase.GetAssetPath(file);
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read);
            _sheet = new XSSFWorkbook(stream).GetSheetAt(0);
            _tableName = ParseUtility.Format(file.name);
            _excelPath = AssetDatabase.GetAssetPath(file);
        }

        public ParseResult Parse()
        {
            var rows = ValidateRows();
            var script = BuildScript(rows);
            var distPath = ParseUtility.WriteScript(TableType.Variable, _tableName, script);
            return new ParseResult(TableType.Variable, _tableName, _excelPath, new[] { distPath });
        }

        private IEnumerable<Row> ValidateRows()
        {
            var firstRow = _sheet.GetRow(0);
            if (firstRow?.GetCellValue(NameCol) != "VariableName" ||
                firstRow.GetCellValue(TypeCol) != "DataType" ||
                firstRow.GetCellValue(ValueCol) != "Value")
            {
                throw new ParserException(_tableName, "Invalid column name");
            }

            var diffChecker = new HashSet<string>();
            for (var i = 1; i <= _sheet.LastRowNum; i++)
            {
                var poiRow = _sheet.GetRow(i);
                if (poiRow == null)
                {
                    break;
                }

                var row = new Row
                (
                    poiRow.GetCellValue(NameCol),
                    poiRow.GetCellValue(TypeCol),
                    poiRow.GetCellValue(ValueCol)
                );

                if (row.Name == string.Empty)
                {
                    break;
                }

                if (char.IsDigit(row.Name, 0))
                {
                    throw new ParserException(_tableName, $"Variable name '{row.Name}' starts with a number");
                }

                if (!diffChecker.Add(row.Name))
                {
                    throw new ParserException(_tableName, $"Duplicate variable name '{row.Name}'");
                }

                if (!ParseUtility.TypeValidators.ContainsKey(row.Type))
                {
                    throw new ParserException(_tableName, $"Invalid variable type '{row.Type}'");
                }

                if (!ParseUtility.TypeValidators[row.Type](row.Value))
                {
                    throw new ParserException(_tableName,
                        $"Variable value '{row.Value}' is not of variable type '{row.Type}'");
                }

                yield return row;
            }
        }

        private string BuildScript(IEnumerable<Row> rows)
        {
            var tableTemplate = File.ReadAllText(TablePath);
            var rowTemplate = File.ReadAllText(RowPath);
            var builder = new StringBuilder(tableTemplate).Replace(TableVariable, _tableName);

            foreach (var row in rows)
            {
                builder
                    .Replace(RowTemplate, rowTemplate + RowTemplate)
                    .Replace(TypeVariable, row.Type)
                    .Replace(NameVariable, row.Name)
                    .Replace(ValueVariable, row.Type switch
                    {
                        "float" => row.Value + 'f',
                        "bool" => row.Value.ToLower(),
                        _ => row.Value
                    });
            }

            builder.Replace(RowTemplate, string.Empty);
            return builder.ToString();
        }

        private readonly struct Row
        {
            public readonly string Name;
            public readonly string Type;
            public readonly string Value;

            public Row(string name, string type, string value)
            {
                Name = ParseUtility.Format(name);
                Type = type;
                Value = value;
            }
        }
    }
}