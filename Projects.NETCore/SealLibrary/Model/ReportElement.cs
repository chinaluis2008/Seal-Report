﻿//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.ComponentModel;
using Seal.Helpers;
using System.Globalization;
using System.Drawing.Design;

namespace Seal.Model
{
    /// <summary>
    /// A report element is a column to display in a report. A report element is a child of a MetaColumn.
    /// </summary>
    public class ReportElement : MetaColumn
    {
        public const string kAutomaticAscSortKeyword = "Automatic Ascendant";
        public const string kAutomaticDescSortKeyword = "Automatic Descendant";
        public const string kNoSortKeyword = "Not sorted";
        public const string kAscendantSortKeyword = "Ascendant";
        public const string kDescendantSortKeyword = "Descendant";


        /// <summary>
        /// Create a report element with a GUID
        /// </summary>
        /// <returns></returns>
        public static ReportElement Create()
        {
            return new ReportElement() { GUID = Guid.NewGuid().ToString() };                                                           
        }

        /// <summary>
        /// True if the element has an enumerated list
        /// </summary>
        public bool IsEnum
        {
            get
            {
                if (PivotPosition == PivotPosition.Data && AggregateFunction == AggregateFunction.Count) return false;
                return (EnumEL != null);
            }
        }

        /// <summary>
        /// True if the element is for numeric values
        /// </summary>
        public bool IsNumeric
        {
            get
            {
                if (PivotPosition == PivotPosition.Data && AggregateFunction == AggregateFunction.Count) return true;
                return (TypeEl == ColumnType.Numeric);
            }
        }

        /// <summary>
        /// True if the element is for text values
        /// </summary>
        public bool IsText
        {
            get
            {
                if (PivotPosition == PivotPosition.Data && AggregateFunction == AggregateFunction.Count) return false;
                return (TypeEl == ColumnType.Text || TypeEl == ColumnType.UnicodeText);
            }
        }

        /// <summary>
        /// True if the element is for Date Time values
        /// </summary>
        public bool IsDateTime
        {
            get
            {
                if (PivotPosition == PivotPosition.Data && AggregateFunction == AggregateFunction.Count) return false;
                return (TypeEl == ColumnType.DateTime);
            }
        }

        /// <summary>
        /// Depending on the element type, set defaut aggreate functions: AggregateFunction, TotalAggregateFunction
        /// </summary>
        public void SetDefaults()
        {
            //Default aggregate
            if (IsEnum)
            {
                AggregateFunction = AggregateFunction.Count;
            }
            else if (IsNumeric)
            {
                AggregateFunction = AggregateFunction.Sum;
            }
            else if (IsDateTime)
            {
                AggregateFunction = AggregateFunction.Max;
                TotalAggregateFunction = AggregateFunction.Max;
            }
            else
            {
                AggregateFunction = AggregateFunction.Count;
            }
        }

        [Browsable(false)]
        private PivotPosition _pivotPosition = PivotPosition.Row;
        /// <summary>
        /// Position of the element in the Cross Table: Row, Column, Page or Data 
        /// </summary>
        public PivotPosition PivotPosition
        {
            get { return _pivotPosition; }
            set { _pivotPosition = value; }
        }

        /// <summary>
        /// Name of the element when displayed in result tables or restrictions
        /// </summary>
        [DefaultValue(null)]
        [XmlIgnore]
        public string DisplayNameEl
        {
            get
            {
                if (!string.IsNullOrEmpty(DisplayName)) return DisplayName;
                return RawDisplayName;
            }
            set
            {
                DisplayName = value;
                if (MetaColumn != null && RawDisplayName == DisplayName) DisplayName = "";
            }
        }

        /// <summary>
        /// Name of the element translated
        /// </summary>
        [XmlIgnore]
        public string DisplayNameElTranslated
        {
            get
            {
                return Report.TranslateElement(this, DisplayNameEl);
            }
        }

        /// <summary>
        /// Sort order in the result tables. Page elements are sorted first, then Row, Column and Data elements.
        /// </summary>
        [DefaultValue(kAutomaticAscSortKeyword)]
        public string SortOrder { get; set; } = kAutomaticAscSortKeyword;

        /// <summary>
        /// Data type of the column
        /// </summary>
        [DefaultValue(ColumnType.Default)]
        public ColumnType TypeEd
        {
            get { return _type; }
            set
            {
                if (Type != value && _dctd != null)
                {
                    Type = value;
                    _numericStandardFormat = NumericStandardFormat.Default;
                    _datetimeStandardFormat = DateTimeStandardFormat.Default;
                    SetStandardFormat();
                }
                else Type = value;
            }
        }

        /// <summary>
        /// If true, a line showing sub-totals is added to the main data table when the value of the element changes
        /// </summary>
        [DefaultValue(false)]
        public bool ShowSubTotals { get; set; } = false;

        /// <summary>
        /// Final type of the element
        /// </summary>
        [XmlIgnore]
        public ColumnType TypeEl
        {
            get
            {
                if (_type == ColumnType.Default && !IsCommonRestrictionValue) return MetaColumn.Type;
                return _type;
            }
        }

        /// <summary>
        /// Final format of the element
        /// </summary>
        [XmlIgnore]
        public string FormatEl
        {
            get
            {
                SetStandardFormat();
                if (IsCommonRestrictionValue) return Format;

                string result = Format;
                if (string.IsNullOrEmpty(result)) result = MetaColumn.Format;
                if (string.IsNullOrEmpty(result))
                {
                    if (IsNumeric) result = Source.Repository.Configuration.NumericFormat;
                    else if (IsDateTime) result = Source.Repository.Configuration.DateTimeFormat;
                    else result = "0";
                }
                if (string.IsNullOrEmpty(result)) result = "0";
                return result;
            }
        }

        /// <summary>
        /// True is the element has a time format
        /// </summary>
        [XmlIgnore]
        public bool HasTimeEl
        {
            get
            {
                if (!IsDateTime) return false;
                return Helper.HasTimeFormat(DateTimeStandardFormat, FormatEl);
            }
        }

        /// <summary>
        /// Display value of the element
        /// </summary>
        public string ElementDisplayValue(object value)
        {
            if (value == null) return "";
            if (value is IFormattable)
            {
                string result = value.ToString();
                try
                {
                    result = ((IFormattable)value).ToString(FormatEl, Report.ExecutionView.CultureInfo);
                }
                catch { }
                return result;
            }
            return value.ToString();
        }

        /// <summary>
        /// True if the element is sorted
        /// </summary>
        [XmlIgnore]
        public bool IsSorted
        {
            get { return SortOrder != kNoSortKeyword && !string.IsNullOrEmpty(SortOrder); }
        }

        /// <summary>
        /// Get the sort value for an enumerated list
        /// </summary>
        public string GetEnumSortValue(string enumValue, bool useDisplayValue)
        {
            string result = enumValue;
            MetaEnum en = EnumEL;

            bool elementSortPosition = (IsSorted && en.UsePosition);
            MetaEV value = null;
            if (useDisplayValue) value = en.Values.FirstOrDefault(i => i.DisplayValue == enumValue);
            else value = en.Values.FirstOrDefault(i => i.Id == enumValue);

            if (value != null)
            {
                string sortPrefix = elementSortPosition ? string.Format("{0:000000}", en.Values.LastIndexOf(value)) : "";
                result = sortPrefix + Report.EnumDisplayValue(en, value.Id);
            }
            else
            {
                string sortPrefix = elementSortPosition ? "000000" : "";
                result = sortPrefix + result;
            }
            return result;
        }

        /// <summary>
        /// Final sort order of the element
        /// </summary>
        [XmlIgnore]
        public string FinalSortOrder { get; set; }

        /// <summary>
        /// Final sort as integer (without ASC or DESC)
        /// </summary>
        [XmlIgnore]
        public int FinalSort {
            get
            {
                int result = 99999;
                if (FinalSortOrder != null && FinalSortOrder.Contains(" ")) result = int.Parse(FinalSortOrder.Split(' ')[0]);
                return result;
            }
        }

        AggregateFunction _aggregateFunction = AggregateFunction.Sum;
        /// <summary>
        /// Aggregate function applied to the Data element
        /// </summary>
        [DefaultValue(AggregateFunction.Sum)]
        public AggregateFunction AggregateFunction
        {
            get { return _aggregateFunction; }
            set { _aggregateFunction = value;  }
        }

        CalculationOption _calculationOption = CalculationOption.No;
        /// <summary>
        /// For numeric Data elements, define calculation option applied on the element in the table
        /// </summary>
        [DefaultValue(CalculationOption.No)]
        public CalculationOption CalculationOption
        {
            get { return _calculationOption; }
            set
            {
                if (Source != null && _calculationOption != value)
                {
                    if (value != CalculationOption.No && (string.IsNullOrEmpty(Format) || NumericStandardFormat == NumericStandardFormat.Default)) NumericStandardFormat = NumericStandardFormat.Percentage0;
                    else if (value == CalculationOption.No && (NumericStandardFormat == NumericStandardFormat.Percentage0 || NumericStandardFormat == NumericStandardFormat.Percentage1 || NumericStandardFormat == NumericStandardFormat.Percentage2))
                    {
                        Format = "";
                        NumericStandardFormat = NumericStandardFormat.Default;
                    }
                }
                _calculationOption = value;
            }
        }

        ShowTotal _showTotal = ShowTotal.No;
        /// <summary>
        /// For Data elements, add a row or a column showing the total of the element in the table. 'Show only total' means that the columns containing the values of the element will be hidden in the table, only the column containing the total of the element is displayed.
        /// </summary>
        [DefaultValue(ShowTotal.No)]
        public ShowTotal ShowTotal
        {
            get { return _showTotal; }
            set
            {
                _showTotal = value;
                
            }
        }

        /// <summary>
        /// Aggregate function applied for the totals
        /// </summary>
        [DefaultValue(AggregateFunction.Sum)]
        public AggregateFunction TotalAggregateFunction { get; set; } = AggregateFunction.Sum;

        //Charts
        SerieDefinition _serieDefinition = SerieDefinition.None;
        /// <summary>
        /// Defines how the element is used in the chart. Row or Column elements can be either Axis or Splitter (to create a serie for each splitter value).
        /// </summary>
        [DefaultValue(SerieDefinition.None)]
        public SerieDefinition SerieDefinition
        {
            get { return _serieDefinition; }
            set
            {
                _serieDefinition = value;
                
            }
        }

        /// <summary>
        /// True is the element defines a Serie
        /// </summary>
        public bool IsSerie
        {
            get { return _nvd3Serie != NVD3SerieDefinition.None || _chartJSSerie != ChartJSSerieDefinition.None || _plotlySerie != PlotlySerieDefinition.None; }
        }

        /// <summary>
        /// For Numeric or Date Time axis, if true, the element values are used for the axis, otherwise axis values are linear. This feature does not work for all types of chart.
        /// </summary>
        [DefaultValue(true)]
        public bool AxisUseValues { get; set; } = true;


        ChartJSSerieDefinition _chartJSSerie = ChartJSSerieDefinition.None;
        /// <summary>
        /// Definition of the serie for the element in the Chart JS chart
        /// </summary>
        [DefaultValue(ChartJSSerieDefinition.None)]
        public ChartJSSerieDefinition ChartJSSerie
        {
            get { return _chartJSSerie; }
            set {
                _chartJSSerie = value;
                
            }
        }

        NVD3SerieDefinition _nvd3Serie = NVD3SerieDefinition.None;
        /// <summary>
        /// Definition of the serie for the element in the NVD3 chart
        /// </summary>
        [DefaultValue(NVD3SerieDefinition.None)]
        public NVD3SerieDefinition Nvd3Serie
        {
            get { return _nvd3Serie; }
            set {
                _nvd3Serie = value;
                
            }
        }

        PlotlySerieDefinition _plotlySerie = PlotlySerieDefinition.None;
        /// <summary>
        /// Definition of the serie for the element in the Plotly chart
        /// </summary>
        [DefaultValue(PlotlySerieDefinition.None)]
        public PlotlySerieDefinition PlotlySerie
        {
            get { return _plotlySerie; }
            set {
                _plotlySerie = value;
                
            }
        }

        private SerieSortType _serieSortType = SerieSortType.Y;
        /// <summary>
        /// Defines how the serie is sorted in the chart
        /// </summary>
        [DefaultValue(SerieSortType.Y)]
        public SerieSortType SerieSortType
        {
            get { return _serieSortType; }
            set
            {
                _serieSortType = value;
                
            }
        }

        /// <summary>
        /// Defines if the serie is sorted ascending or descending in the chart
        /// </summary>
        [DefaultValue(PointSortOrder.Ascending)]
        public PointSortOrder SerieSortOrder { get; set; } = PointSortOrder.Ascending;


        /// <summary>
        /// Not used (FUTURE). Definition of the X axis of the serie (Primary or Secondary).
        /// </summary>
        [DefaultValue(AxisType.Primary)]
        public AxisType XAxisType { get; set; } = AxisType.Primary;

        /// <summary>
        /// Definition of the Y axis of the serie (Primary or Secondary)
        /// </summary>
        [DefaultValue(AxisType.Primary)]
        public AxisType YAxisType { get; set; } = AxisType.Primary;

        /// <summary>
        /// GUID of the MetaColumn of the element
        /// </summary>
        [Browsable(false)]
        public string MetaColumnGUID { get; set; }

        /// <summary>
        /// Helper to change the column of the element
        /// </summary>
        public void ChangeColumnGUID(string guid)
        {
            _metaColumn = null;
            MetaColumnGUID = guid;
            _type = ColumnType.Default;
            _numericStandardFormat = NumericStandardFormat.Default;
            _datetimeStandardFormat = DateTimeStandardFormat.Default;
            _displayName = "";
        }

        MetaColumn _metaColumn = null;
        /// <summary>
        /// The MetaColumn of the element
        /// </summary>
        [XmlIgnore]
        public MetaColumn MetaColumn
        {
            get
            {
                if (_metaColumn == null && !string.IsNullOrEmpty(MetaColumnGUID))
                {
                    if (Model != null && Model.IsSQLModel) _metaColumn = Model.Table.Columns.FirstOrDefault(i => i.GUID == MetaColumnGUID);
                    else if (Source != null && Source.MetaData != null) _metaColumn = Source.MetaData.GetColumnFromGUID(MetaColumnGUID);
                }
                return _metaColumn;
            }
            set
            {
                _metaColumn = value;
            }
        }

        /// <summary>
        /// Change the source of the element, set MetColumn to NULL
        /// </summary>
        public void SetSourceReference(MetaSource source)
        {
            _metaColumn = null;
            _source = source;
        }

        /// <summary>
        /// Display name of the MetaColumn
        /// </summary>
        public string RawDisplayName
        {
            get
            {
                if (MetaColumn != null && PivotPosition == PivotPosition.Data && AggregateFunction != AggregateFunction.Sum && !MetaColumn.IsAggregate) return string.Format("{0} {1}", Report.Translate(Helper.GetEnumDescription(typeof(AggregateFunction), AggregateFunction) + " of"), MetaColumn.DisplayName);
                else if (MetaColumn != null) return MetaColumn.DisplayName;
                return "";
            }
        }


        /// <summary>
        /// If not empty, overwrite the default SQL used for the element in the SELECT statement
        /// </summary>
        protected string _SQL;
        /// <summary>
        /// If not empty, overwrite the default SQL used for the element in the SELECT statement
        /// </summary>
        public string SQL
        {
            get
            {
                return _SQL;
            }
            set { _SQL = value; }
        }

        /// <summary>
        /// If not empty, the script is executed to calculate custom cell value and CSS
        /// </summary>
        public string CellScript { get; set; }

        /// <summary>
        /// Optional Razor Script executed if script navigation links have been added in the CellScript
        /// </summary>
        [DefaultValue("")]
        public string NavigationScript { get; set; }

        /// <summary>
        /// If defined, the enumerated list is used for the display and for sorting
        /// </summary>
        [DefaultValue(null)]
        public string EnumGUIDEL
        {
            get { return _enumGUID; }
            set { _enumGUID = value; }
        }

        /// <summary>
        /// If Yes, it indicates that the element is an aggregate even it is set in a dimension (Page/Row/Column). By default, the metacolumn flag 'Is Aggregate' is used. This flag impacts the build of the GROUP BY Clause.
        /// </summary>
        [DefaultValue(YesNoDefault.Default)]
        public YesNoDefault ForceAggregate { get; set; } = YesNoDefault.Default;
        public bool ShouldSerializeHasAggregate() { return ForceAggregate != YesNoDefault.Default; }

        /// <summary>
        /// True if the element is an aggregate
        /// </summary>
        [XmlIgnore]
        public bool IsAggregateEl
        {
            get
            {
                if (ForceAggregate == YesNoDefault.Yes) return true;
                if (ForceAggregate == YesNoDefault.No) return false;
                if (MetaColumn != null) return MetaColumn.IsAggregate;
                return false;
            }
        }

        /// <summary>
        /// True is the element is a common restriction
        /// </summary>
        [XmlIgnore]
        public bool IsCommonRestrictionValue
        {
            get
            {
                return string.IsNullOrEmpty(MetaColumnGUID);
            }
        }

        /// <summary>
        /// True is the element is a common value
        /// </summary>
        [XmlIgnore]
        public bool IsCommonValue = false;

        /// <summary>
        /// Final enumerated list of the element
        /// </summary>
        [XmlIgnore]
        public MetaEnum EnumEL
        {
            get
            {
                if (Enum != null) return Enum;
                if (IsCommonRestrictionValue) return null;
                return MetaColumn.Enum;
            }
        }

        /// <summary>
        /// SQL of the MetaColumn
        /// </summary>
        [XmlIgnore]
        public string RawSQLColumn
        {
            get
            {
                if (IsCommonRestrictionValue) return Name;

                string result = MetaColumn.Name;
                if (PivotPosition == PivotPosition.Data && !MetaColumn.IsAggregate)
                {
                    //aggregate
                    result = string.Format("{0}({1})", AggregateFunction, result);
                }
                return result;
            }
        }

        /// <summary>
        /// Final SQL of the element
        /// </summary>
        [XmlIgnore]
        public string SQLColumn
        {
            get
            {
                return !string.IsNullOrEmpty(_SQL) ? _SQL : RawSQLColumn;
            }
        }

        private string _SQLColumnName;
        /// <summary>
        /// SQL Column name of the element
        /// </summary>
        [XmlIgnore]
        public string SQLColumnName
        {
            get { return string.IsNullOrEmpty(_SQLColumnName) && !IsCommonRestrictionValue ? MetaColumn.Name : _SQLColumnName; }
            set { _SQLColumnName = value; }
        }

        /// <summary>
        /// Current report
        /// </summary>
        protected Report _report;
        /// <summary>
        /// Current report
        /// </summary>
        [XmlIgnore]
        public Report Report
        {
            get { return _report; }
            set { _report = value; }
        }

        /// <summary>
        /// Current model
        /// </summary>
        protected ReportModel _model;
        /// <summary>
        /// Current model
        /// </summary>
        [XmlIgnore]
        public ReportModel Model
        {
            get { return _model; }
            set { _model = value; }
        }

        /// <summary>
        /// True if navigation occured
        /// </summary>
        [XmlIgnore]
        public bool IsForNavigation = false;

        /// <summary>
        /// D3 Format for a charts
        /// </summary>
        public string GetD3Format(CultureInfo culture, string NVD3ChartType)
        {
            //try to convert from .net to d3 format... from https://github.com/mbostock/d3/wiki/Formatting
            if (IsNumeric)
            {
                string format = FormatEl;
                if (format == "0" || format == "N" || format == "D" || format == "G") return ".0f";
                else if (format == "N0" || format == "D0" || format == "G0") return ",.0f";
                else if (format == "N1" || format == "D1" || format == "G1") return ",.1f";
                else if (format == "N2" || format == "D2" || format == "G2") return ",.2f";
                else if (format == "N3" || format == "D3" || format == "G3") return ",.3f";
                else if (format == "N4" || format == "D4" || format == "G4") return ",.4f";
                else if (format == "N5" || format == "D5" || format == "G5") return ",.5f";
                else if (format == "N6" || format == "D6" || format == "G6") return ",.6f";
                else if (format == "N7" || format == "D7" || format == "G7") return ",.7f";
                else if (format == "N8" || format == "D8" || format == "G8") return ",.8f";
                else if (format == "P0") return ",.0%";
                else if (format == "P1") return ",.1%";
                else if (format == "P2") return ",.2%";
                else if (format == "P3") return ",.3%";
                else if (format == "P4") return ",.4%";
                else if (format == "P5") return ",.5%";
                else if (format == "P6") return ",.6%";
                else if (format == "P7") return ",.7%";
                else if (format == "P8") return ",.8%";
                else if (format == "C0") return "$,.0f";
                else if (format == "C1") return "$,.1f";
                else if (format == "C2") return "$,.2f";
                else if (format == "C3") return "$,.3f";
                else if (format == "C4") return "$,.4f";
                else if (format == "C5") return "$,.5f";
                else if (format == "C6") return "$,.6f";
                else if (format == "C7") return "$,.7f";
                else if (format == "C8") return "$,.8f";
            }
            else if (IsDateTime)
            {
                string format = FormatEl;
                if (format == "d") format = culture.DateTimeFormat.ShortDatePattern;
                else if (format == "D") format = culture.DateTimeFormat.LongDatePattern;
                else if (format == "t") format = culture.DateTimeFormat.ShortTimePattern;
                else if (format == "T") format = culture.DateTimeFormat.LongTimePattern;
                else if (format == "g") format = culture.DateTimeFormat.ShortDatePattern + " " + culture.DateTimeFormat.ShortTimePattern;
                else if (format == "G") format = culture.DateTimeFormat.ShortDatePattern + " " + culture.DateTimeFormat.LongTimePattern;
                else if (format == "f") format = culture.DateTimeFormat.LongDatePattern + " " + culture.DateTimeFormat.ShortTimePattern;
                else if (format == "F") format = culture.DateTimeFormat.LongDatePattern + " " + culture.DateTimeFormat.LongTimePattern;

                StringBuilder result = new StringBuilder();
                for (int i = 0; i < format.Length; i++)
                {
                    if (Helper.FindReplacePattern(format, ref i, "dddd", "%A", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "ddd", "%a", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "dd", "%d", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "d", "%e", result)) continue;

                    if (Helper.FindReplacePattern(format, ref i, "MMMM", "%B", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "MMM", "%b", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "MM", "%m", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "M", "%m", result)) continue;

                    if (Helper.FindReplacePattern(format, ref i, "yyyy", "%Y", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "yyy", "%Y", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "yy", "%y", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "y", "%y", result)) continue;

                    if (Helper.FindReplacePattern(format, ref i, "HH", "%H", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "H", "%H", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "hh", "%I", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "h", "%I", result)) continue;

                    if (Helper.FindReplacePattern(format, ref i, "mm", "%M", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "m", "%M", result)) continue;

                    if (Helper.FindReplacePattern(format, ref i, "ss", "%S", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "s", "%S", result)) continue;

                    if (Helper.FindReplacePattern(format, ref i, "FFF", "%L", result)) continue;
                    result.Append(format[i]);
                }
                return result.ToString().Replace("/", culture.DateTimeFormat.DateSeparator);
            }
            return "g";
        }

        /// <summary>
        /// Moment JS Format for the chart
        /// </summary>
        public string GetMomentJSFormat(CultureInfo culture)
        {
            return Helper.ToMomentJSFormat(culture, FormatEl);
        }

        /// <summary>
        /// Excel Format of the element
        /// </summary>
        public string GetExcelFormat(CultureInfo culture)
        {
            //try to convert from .net to Excel format... 
            string format = FormatEl;
            if (IsNumeric)
            {
                if (format == "N1" || format == "D1") return "#,##0.0";
                else if (format == "N2" || format == "D2") return "#,##0.00";
                else if (format == "N3" || format == "D3") return "#,##0.000";
                else if (format == "N4" || format == "D4") return "#,##0.0000";
                else if (format == "N5" || format == "D5") return "#,##0.00000";
                else if (format == "N6" || format == "D6") return "#,##0.000000";
                else if (format == "N7" || format == "D7") return "#,##0.0000000";
                else if (format == "N8" || format == "D8") return "#,##0.00000000";
                else if (format.StartsWith("N") || format.StartsWith("D")) return "#,##0";
                else if (format == "P1") return "0.0%";
                else if (format == "P2") return "0.00%";
                else if (format == "P3") return "0.00%";
                else if (format == "P4") return "0.0000%";
                else if (format == "P5") return "0.00000%";
                else if (format == "P6") return "0.000000%";
                else if (format == "P7") return "0.0000000%";
                else if (format == "P8") return "0.00000000%";
                else if (format.StartsWith("P")) return "0%";
                else if (format == "C1") return "$ #,##0.0";
                else if (format == "C2") return "$ #,##0.00";
                else if (format == "C3") return "$ #,##0.000";
                else if (format == "C4") return "$ #,##0.0000";
                else if (format == "C5") return "$ #,##0.00000";
                else if (format == "C6") return "$ #,##0.000000";
                else if (format == "C7") return "$ #,##0.0000000";
                else if (format == "C8") return "$ #,##0.00000000";
                else if (format.StartsWith("C")) return "$ #,##0";
                else if (format.StartsWith("N") || format.StartsWith("D") || format.StartsWith("E") || format.StartsWith("F") || format.StartsWith("G") || format.StartsWith("H")) return "";
            }
            else if (IsDateTime)
            {
                if (format == "d") return culture.DateTimeFormat.ShortDatePattern;
                else if (format == "D") return culture.DateTimeFormat.LongDatePattern;
                else if (format == "t") return culture.DateTimeFormat.ShortTimePattern;
                else if (format == "T") return culture.DateTimeFormat.LongTimePattern;
                else if (format == "g") return culture.DateTimeFormat.ShortDatePattern + " " + culture.DateTimeFormat.ShortTimePattern;
                else if (format == "G") return culture.DateTimeFormat.ShortDatePattern + " " + culture.DateTimeFormat.LongTimePattern;
                else if (format == "f") return culture.DateTimeFormat.LongDatePattern + " " + culture.DateTimeFormat.ShortTimePattern;
                else if (format == "F") return culture.DateTimeFormat.LongDatePattern + " " + culture.DateTimeFormat.LongTimePattern;
                else if (format == "g") return culture.DateTimeFormat.ShortDatePattern + " " + culture.DateTimeFormat.ShortTimePattern;
            }
            return FormatEl;
        }
    }
}

